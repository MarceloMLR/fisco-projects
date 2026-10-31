using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Documents.Infrastructure.Signatures;

/// <summary>
/// The Autentique as the provider of signatures (ADR-0129, ADR-0130), through its GraphQL API with the key of the
/// installation: the term goes with each signer by e-mail, who gets the link from the Autentique; the cancellation puts
/// the deadline of the document at now, which blocks it; and the notices of the Autentique (webhooks) are checked by the
/// HMAC SHA-256 of the raw body with the secret of the installation. Every failure is a
/// <see cref="SignatureProviderException"/> with the operation, the HTTP code and the code of the Autentique, never the
/// key, the e-mails or the term (ADR-0057), and goes to the log with the same text.
/// </summary>
internal sealed partial class AutentiqueSignatureProvider(
    IHttpClientFactory clients, IOptions<SignatureOptions> options, TimeProvider clock, ILogger<AutentiqueSignatureProvider> logger)
    : ISignatureProvider, ISignatureNotices
{
    /// <summary>The name of the <see cref="HttpClient"/> of the Autentique.</summary>
    public const string ClientName = "autentique";

    /// <summary>The header of the signature of each notice.</summary>
    public const string NoticeHeader = "X-Autentique-Signature";

    /// <summary>The API of the Autentique, only GraphQL.</summary>
    public static readonly Uri Endpoint = new("https://api.autentique.com.br/v2/graphql");

    // The code of the Autentique for links sent again too soon after they were sent.
    private const string ResentRecently = "too_many_resent_emails";

    // The largest signed PDF taken from the Autentique, above the 20 MB of its professional plan.
    private const long MaxSignedBytes = 64L * 1024 * 1024;

    private const string CreateMutation = """
        mutation CreateDocument($document: DocumentInput!, $signers: [SignerInput!]!, $file: Upload!, $sandbox: Boolean) {
          createDocument(document: $document, signers: $signers, file: $file, sandbox: $sandbox) {
            id
            signatures { public_id email action { name } user { email } }
          }
        }
        """;

    private const string DocumentQuery = """
        query Document($id: UUID!) {
          document(id: $id) {
            id
            name
            deadline_at
            files { signed }
            signatures {
              public_id
              email
              action { name }
              user { email }
              signed { created_at }
              rejected { reason created_at }
            }
          }
        }
        """;

    private const string ResendMutation = """
        mutation ResendSignatures($public_ids: [UUID]!) {
          resendSignatures(public_ids: $public_ids)
        }
        """;

    private const string BlockMutation = """
        mutation BlockDocument($id: UUID!, $document: UpdateDocumentInput!) {
          updateDocument(id: $id, document: $document) { id }
        }
        """;

    private const string DeleteMutation = """
        mutation DeleteDocument($id: UUID!) {
          deleteDocument(id: $id)
        }
        """;

    public string Name => SignatureOptions.AutentiqueProvider;

    string ISignatureNotices.Provider => Name;

    private AutentiqueSignatureOptions Account => options.Value.Autentique;

    public async Task<SentSignature> SendAsync(SignatureRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Signers.Count == 0)
        {
            throw Failed("send", "no signer");
        }

        var document = new JsonObject { ["name"] = request.Title, ["refusable"] = true };
        if (!string.IsNullOrWhiteSpace(request.Message))
        {
            document["message"] = request.Message;
        }

        var signers = new JsonArray([.. request.Signers.Select(signer => (JsonNode?)new JsonObject
        {
            ["email"] = signer.Email,
            ["name"] = signer.Name,
            ["action"] = "SIGN",
        })]);
        var operations = new JsonObject
        {
            ["query"] = CreateMutation,
            ["variables"] = new JsonObject
            {
                ["document"] = document,
                ["signers"] = signers,
                ["file"] = null,
                ["sandbox"] = Account.Sandbox,
            },
        };

        // The content of the caller stays open: the Autentique gets a copy.
        await using var copy = new MemoryStream();
        await request.Content.CopyToAsync(copy, cancellationToken);
        var file = new ByteArrayContent(copy.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        using var content = new MultipartFormDataContent
        {
            { new StringContent(operations.ToJsonString(), Encoding.UTF8, "application/json"), "operations" },
            { new StringContent("""{"file":["variables.file"]}""", Encoding.UTF8, "application/json"), "map" },
            { file, "file", AsciiName(request.FileName) },
        };
        var created = (await CallAsync("send", content, cancellationToken))["createDocument"] as JsonObject
            ?? throw Failed("send", "no document");
        var documentId = Id(created["id"]) ?? throw Failed("send", "no document");

        // Each signer by the e-mail, unique in the request; the other signatures, as of the account, are not ours.
        var signatures = SignaturesOf(created);
        var ids = new List<string>();
        foreach (var signer in request.Signers)
        {
            if (signatures.FirstOrDefault(signature => signature.Signs && !ids.Contains(signature.Id)
                    && string.Equals(signature.Email, signer.Email, StringComparison.OrdinalIgnoreCase)) is not { } signature)
            {
                await ForgetAsync(documentId);
                throw Failed("send", "a signer is missing in the answer");
            }

            ids.Add(signature.Id);
        }

        return new SentSignature(documentId, ids);
    }

    public async Task<IReadOnlyList<SignerState>> StatusAsync(string documentId, CancellationToken cancellationToken)
    {
        var document = await DocumentAsync(documentId, "status", cancellationToken);
        return [.. document.Signers.Select(signature => signature switch
        {
            { Rejected: true } => new SignerState(signature.Id, AcceptanceSignerStatus.Refused, signature.RejectedAt, signature.Reason),
            { Signed: true } => new SignerState(signature.Id, AcceptanceSignerStatus.Signed, signature.SignedAt),
            _ => new SignerState(signature.Id, AcceptanceSignerStatus.Pending),
        })];
    }

    public async Task RemindAsync(string documentId, CancellationToken cancellationToken)
    {
        var document = await DocumentAsync(documentId, "remind", cancellationToken);
        if (document.IsClosed(clock.GetUtcNow().UtcDateTime))
        {
            throw Failed("remind", "the document is closed");
        }

        var pending = document.Signers.Where(signature => signature is { Rejected: false, Signed: false }).Select(signature => signature.Id).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        using var content = Json(ResendMutation, new JsonObject { ["public_ids"] = new JsonArray([.. pending.Select(id => (JsonNode?)id)]) });
        await CallAsync("remind", content, cancellationToken);
    }

    public async Task CancelAsync(string documentId, CancellationToken cancellationToken)
    {
        // The deadline at now blocks the document, even with a signature done; one signed by everybody stays as it is.
        var now = clock.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        using var content = Json(BlockMutation, new JsonObject
        {
            ["id"] = documentId,
            ["document"] = new JsonObject { ["deadline_at"] = now },
        });
        await CallAsync("cancel", content, cancellationToken, accepted: "document_signed");
    }

    public async Task<SignedFile> SignedFileAsync(string documentId, CancellationToken cancellationToken)
    {
        var document = await DocumentAsync(documentId, "signed file", cancellationToken);
        if (document.Signers.Count == 0 || document.Signers.Exists(signature => !signature.Signed || signature.Rejected))
        {
            throw Failed("signed file", "not signed by every signer");
        }

        if (!Uri.TryCreate(document.SignedFile, UriKind.Absolute, out var address) || address.Scheme != Uri.UriSchemeHttps)
        {
            throw Failed("signed file", "no address");
        }

        using var message = new HttpRequestMessage(HttpMethod.Get, address);
        // The key only goes to the Autentique itself.
        if (IsAutentique(address))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Account.ApiKey);
        }

        var signed = new MemoryStream();
        try
        {
            using var response = await TransmitAsync(message, "signed file", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw Failed("signed file", $"HTTP {(int)response.StatusCode}");
            }

            if (response.Content.Headers.ContentLength > MaxSignedBytes)
            {
                throw Failed("signed file", "too large");
            }

            await using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                await CopyLimitedAsync(stream, signed, cancellationToken);
            }

            if (!IsPdf(signed))
            {
                throw Failed("signed file", "not a PDF");
            }
        }
        catch
        {
            await signed.DisposeAsync();
            throw;
        }

        signed.Position = 0;
        return new SignedFile($"{document.Name}.pdf", signed);
    }

    public async Task DeleteAsync(string documentId, CancellationToken cancellationToken)
    {
        using var content = Json(DeleteMutation, new JsonObject { ["id"] = documentId });
        await CallAsync("delete", content, cancellationToken, accepted: "document_not_found");
    }

    public SignatureNotice Read(byte[] body, Func<string, string?> header)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(header);
        var secret = Account.WebhookSecret;
        var given = header(NoticeHeader)?.Trim();
        if (string.IsNullOrEmpty(secret) || given is not { Length: SHA256.HashSizeInBytes * 2 })
        {
            return SignatureNotice.Forged;
        }

        byte[] provided;
        try
        {
            provided = Convert.FromHexString(given);
        }
        catch (FormatException)
        {
            return SignatureNotice.Forged;
        }

        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);
        return CryptographicOperations.FixedTimeEquals(expected, provided)
            ? new SignatureNotice(true, NoticeDocumentOf(body))
            : SignatureNotice.Forged;
    }

    /// <summary>
    /// The document of a notice: its own id in a <c>document.*</c> event, the document of the signature in a
    /// <c>signature.*</c> one; the resource is at <c>event.data</c> or, in some examples of the Autentique, at
    /// <c>event.data.object</c>.
    /// </summary>
    internal static string? NoticeDocumentOf(byte[] body)
    {
        try
        {
            var root = JsonNode.Parse(body);
            var notice = root?["event"] as JsonObject ?? root as JsonObject;
            var data = notice?["data"] as JsonObject;
            var resource = data?["object"] as JsonObject ?? data;
            return Text(notice?["type"])?.Split('.')[0] switch
            {
                "document" => Id(resource?["id"]),
                "signature" => Id(resource?["document"]) ?? Id((resource?["document"] as JsonObject)?["id"]),
                _ => null,
            };
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }

    private async Task<AutentiqueDocument> DocumentAsync(string documentId, string operation, CancellationToken cancellationToken)
    {
        if (Id(documentId) is null)
        {
            throw Failed(operation, "document_not_found");
        }

        using var content = Json(DocumentQuery, new JsonObject { ["id"] = documentId });
        var document = (await CallAsync(operation, content, cancellationToken))["document"] as JsonObject
            ?? throw Failed(operation, "document_not_found");
        return new AutentiqueDocument(
            Text(document["name"]) ?? "Termo assinado",
            DateOf(document["deadline_at"]),
            Text((document["files"] as JsonObject)?["signed"]),
            [.. SignaturesOf(document).Where(signature => signature.Signs)]);
    }

    private static List<AutentiqueSignature> SignaturesOf(JsonObject document) =>
        [.. (document["signatures"] as JsonArray ?? []).OfType<JsonObject>().Select(signature =>
        {
            var rejected = signature["rejected"] as JsonObject;
            return new AutentiqueSignature(
                Id(signature["public_id"]) ?? string.Empty,
                Text(signature["email"]) ?? Text((signature["user"] as JsonObject)?["email"]),
                Signs: signature["action"] is JsonObject { } action && Text(action["name"]) is not null,
                Signed: signature["signed"] is JsonObject,
                SignedAt: DateOf((signature["signed"] as JsonObject)?["created_at"]),
                Rejected: rejected is not null,
                RejectedAt: DateOf(rejected?["created_at"]),
                Reason: Text(rejected?["reason"]));
        }).Where(signature => signature.Id.Length > 0)];

    private async Task<JsonObject> CallAsync(string operation, HttpContent content, CancellationToken cancellationToken, string? accepted = null)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Account.ApiKey);
        using var response = await TransmitAsync(message, operation, HttpCompletionOption.ResponseContentRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // 401: the key; 429: the limit of calls of the plan.
            throw Failed(operation, $"HTTP {(int)response.StatusCode}");
        }

        JsonNode? answer;
        try
        {
            answer = JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (JsonException)
        {
            throw Failed(operation, "unreadable answer");
        }

        if (answer?["errors"] is JsonArray { Count: > 0 } errors)
        {
            var code = CodeOf(errors);
            if (code == ResentRecently)
            {
                // The Autentique keeps the links from being sent again a short while after they were sent.
                LogResentRecently(logger, operation);
                throw new SignatureProviderException($"The Autentique failed to {operation}: {code}.", SignatureFailure.RemindedRecently);
            }

            return accepted is not null && code == accepted ? new JsonObject() : throw Failed(operation, code);
        }

        return answer?["data"] as JsonObject ?? throw Failed(operation, "no data");
    }

    private async Task<HttpResponseMessage> TransmitAsync(
        HttpRequestMessage message, string operation, HttpCompletionOption completion, CancellationToken cancellationToken)
    {
        try
        {
            return await clients.CreateClient(ClientName).SendAsync(message, completion, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw Failed(operation, "unreachable", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw Failed(operation, "timeout", exception);
        }
    }

    // A document the Autentique took but whose answer is not understood does not stay to be signed.
    private async Task ForgetAsync(string documentId)
    {
        try
        {
            await CancelAsync(documentId, CancellationToken.None);
        }
        catch (SignatureProviderException)
        {
            // Already in the log.
        }
    }

    /// <summary>
    /// The code of the first error: a code of the Autentique (<c>document_not_found</c>), the first code of a
    /// validation, or the category; never the text of a message, which may repeat the values sent.
    /// </summary>
    private static string CodeOf(JsonArray errors)
    {
        var error = errors.OfType<JsonObject>().FirstOrDefault();
        var message = Text(error?["message"]);
        var extensions = error?["extensions"] as JsonObject;
        if (message == "validation" && extensions?["validation"] is JsonObject validation
            && validation.Select(field => (field.Value as JsonArray)?.FirstOrDefault()).Select(Text).FirstOrDefault(code => code is not null) is { } first
            && IsCode(first))
        {
            return $"validation {first}";
        }

        if (message is not null && IsCode(message))
        {
            return message;
        }

        return Text(extensions?["category"]) is { } category && IsCode(category) ? $"error of the category {category}" : "error";
    }

    private static bool IsCode(string text) => CodePattern().IsMatch(text);

    private static bool IsAutentique(Uri address) =>
        address.Scheme == Uri.UriSchemeHttps
        && (address.Host.Equals("autentique.com.br", StringComparison.OrdinalIgnoreCase)
            || address.Host.EndsWith(".autentique.com.br", StringComparison.OrdinalIgnoreCase));

    private static bool IsPdf(MemoryStream content) =>
        content.Length >= 5 && content.GetBuffer().AsSpan(0, 5).SequenceEqual("%PDF-"u8);

    private async Task CopyLimitedAsync(Stream source, MemoryStream target, CancellationToken cancellationToken)
    {
        var buffer = new byte[81_920];
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (target.Length + read > MaxSignedBytes)
            {
                throw Failed("signed file", "too large");
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    private static StringContent Json(string query, JsonObject variables) =>
        new(new JsonObject { ["query"] = query, ["variables"] = variables }.ToJsonString(), Encoding.UTF8, "application/json");

    // The Autentique takes the name of the document from the title; the name of the file only needs to be a PDF.
    private static string AsciiName(string name)
    {
        var ascii = new string([.. name.Normalize(NormalizationForm.FormD).Where(letter => letter is >= ' ' and <= '~' and not '"' and not '\\')]);
        return ascii.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? ascii : "termo.pdf";
    }

    private static string? Text(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text) ? text : null;

    // The ids of the Autentique are hexadecimal, with or without dashes.
    private static string? Id(JsonNode? node) => Id(Text(node));

    private static string? Id(string? text) => text is { Length: > 0 and <= AcceptanceSigner.ProviderIdMaxLength } && IdPattern().IsMatch(text) ? text : null;

    // The dates of the Autentique are ISO 8601, in UTC; one without the zone is taken as UTC.
    private static DateTime? DateOf(JsonNode? node) =>
        Text(node) is { } text && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var at)
            ? at
            : null;

    private SignatureProviderException Failed(string operation, string detail, Exception? inner = null)
    {
        var message = $"The Autentique failed to {operation}: {detail}.";
        LogFailed(logger, operation, detail);
        return inner is null ? new SignatureProviderException(message) : new SignatureProviderException(message, inner);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "The Autentique sent the links a short while ago and did not {Operation} yet.")]
    private static partial void LogResentRecently(ILogger logger, string operation);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The Autentique failed to {Operation}: {Detail}.")]
    private static partial void LogFailed(ILogger logger, string operation, string detail);

    [GeneratedRegex("^[a-z][a-z0-9_]*(:[a-z0-9_]+)?$")]
    private static partial Regex CodePattern();

    [GeneratedRegex("^[A-Za-z0-9-]+$")]
    private static partial Regex IdPattern();

    /// <summary>A signature of a document: the id, the e-mail, whether it signs and what the signer did.</summary>
    private sealed record AutentiqueSignature(
        string Id, string? Email, bool Signs, bool Signed, DateTime? SignedAt, bool Rejected, DateTime? RejectedAt, string? Reason);

    /// <summary>A document as the provider reads it: the name, the deadline, the signed file and the signers.</summary>
    private sealed record AutentiqueDocument(string Name, DateTime? DeadlineAt, string? SignedFile, List<AutentiqueSignature> Signers)
    {
        /// <summary>Blocked by the deadline, or refused by a signer.</summary>
        public bool IsClosed(DateTime now) => DeadlineAt <= now || Signers.Exists(signature => signature.Rejected);
    }
}
