using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace FiscoProjects.Testing;

/// <summary>A signature of a simulated document: the signer, the action (null for the account) and what the signer did.</summary>
public sealed class FakeAutentiqueSignature
{
    public required string PublicId { get; init; }

    public string? Name { get; init; }

    public required string Email { get; init; }

    /// <summary>"SIGN" for a signer; null for the account that created the document.</summary>
    public string? Action { get; init; }

    public DateTime? SignedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public string? Reason { get; set; }
}

/// <summary>A document of the simulated Autentique, as the application created it.</summary>
public sealed class FakeAutentiqueDocument
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? Message { get; init; }

    public bool Refusable { get; init; }

    public bool Sandbox { get; init; }

    public required string FileName { get; init; }

    public required byte[] File { get; init; }

    public DateTime? DeadlineAt { get; set; }

    public List<FakeAutentiqueSignature> Signatures { get; } = [];

    /// <summary>The signatures of the signers, without the one of the account.</summary>
    public IEnumerable<FakeAutentiqueSignature> Signers => Signatures.Where(signature => signature.Action is not null);

    /// <summary>The ids sent again by resendSignatures, in order.</summary>
    public List<string> Resent { get; } = [];
}

/// <summary>A request the simulation got: the operation, or "download", and whether it had the key.</summary>
public sealed record FakeAutentiqueRequest(string Operation, Uri Address, bool HadKey);

/// <summary>
/// The GraphQL API of the Autentique simulated inside the process (ADR-0056, ADR-0130): the creation of documents with
/// the file, as the GraphQL multipart request sends it, the reading of a document, the sending of the links again, the
/// deadline that blocks the document, the deletion and the download of the signed PDF. Like the real one, a wrong key
/// answers 401, the limit of calls 429, and the other failures 200 with the codes in <c>errors</c>; the account that
/// creates the document is one of its signatures, without an action. The signers answer by
/// <see cref="Sign(string, int, DateTime)"/> and <see cref="Refuse"/>, as on the page of the Autentique. Nothing reaches
/// the network.
/// </summary>
public sealed partial class FakeAutentique : HttpMessageHandler
{
    /// <summary>The key of the API the simulation accepts.</summary>
    public const string Key = "key-of-the-autentique-of-the-tests";

    /// <summary>The e-mail of the account that creates the documents, one of the signatures of each.</summary>
    public const string AccountEmail = "conta.autentique@empresa.example";

    private readonly ConcurrentDictionary<string, FakeAutentiqueDocument> _documents = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<FakeAutentiqueRequest> _requests = new();

    /// <summary>When on, every request answers 503, as the Autentique out of service.</summary>
    public bool Down { get; set; }

    /// <summary>When on, every request answers 429, as the limit of calls of the plan reached.</summary>
    public bool Limited { get; set; }

    /// <summary>When on, sending the links again is refused, as the Autentique a short while after it sent them.</summary>
    public bool ResendBlocked { get; set; }

    /// <summary>
    /// Where the signed PDFs are, as the Autentique gives their addresses: its API by default, or another host, as a
    /// storage of files.
    /// </summary>
    public string SignedFilesAt { get; set; } = "https://api.autentique.com.br/documentos/";

    /// <summary>The documents created, in no particular order.</summary>
    public IReadOnlyCollection<FakeAutentiqueDocument> Documents => [.. _documents.Values];

    /// <summary>The requests got, in order.</summary>
    public IReadOnlyList<FakeAutentiqueRequest> Requests => [.. _requests];

    /// <summary>The address of the signed PDF of a document, as the Autentique gives it.</summary>
    public Uri SignedAddressOf(string documentId) => new($"{SignedFilesAt}{documentId}/assinado.pdf");

    /// <summary>The signature of a notice with the secret: the hex HMAC SHA-256 of the raw body.</summary>
    public static string SignatureOf(string body, string secret) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body)));

    /// <summary>
    /// A notice of the Autentique about a document, as the live API sends it: the resource at <c>event.data</c>, its own
    /// id in a <c>document.*</c> event and the document of the signature in a <c>signature.*</c> one.
    /// </summary>
    public static string NoticeOf(string type, string documentId)
    {
        var data = type.StartsWith("signature.", StringComparison.Ordinal)
            ? new JsonObject { ["public_id"] = Guid.NewGuid().ToString(), ["object"] = "signature", ["document"] = documentId, ["action"] = "Sign" }
            : new JsonObject { ["id"] = documentId, ["object"] = "document", ["name"] = "Termo" };
        return new JsonObject
        {
            ["id"] = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            ["object"] = "webhook",
            ["name"] = "FiscoProjects",
            ["format"] = "json",
            ["event"] = new JsonObject
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["object"] = "event",
                ["organization"] = 1,
                ["type"] = type,
                ["data"] = data,
                ["created_at"] = "2026-10-30T12:00:00.000000Z",
            },
        }.ToJsonString();
    }

    /// <summary>The document of the id; throws when there is none.</summary>
    public FakeAutentiqueDocument DocumentOf(string documentId) => _documents[documentId];

    /// <summary>The signer at <paramref name="index"/> of the document signs on the page of the Autentique.</summary>
    public void Sign(string documentId, int index, DateTime at)
    {
        var signer = OpenSignerOf(documentId, index);
        signer.SignedAt = at;
    }

    /// <summary>The signer at <paramref name="index"/> of the document refuses with the reason.</summary>
    public void Refuse(string documentId, int index, string reason, DateTime at)
    {
        var signer = OpenSignerOf(documentId, index);
        signer.RejectedAt = at;
        signer.Reason = reason;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await Task.Yield();
        var address = request.RequestUri!;
        var hadKey = request.Headers.Authorization is { Scheme: "Bearer", Parameter: Key };
        if (request.Method == HttpMethod.Get)
        {
            _requests.Enqueue(new("download", address, request.Headers.Authorization is not null));
            return Down ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : Download(address);
        }

        var (query, variables, file) = await ReadAsync(request, cancellationToken);
        var operation = OperationOf(query);
        _requests.Enqueue(new(operation, address, request.Headers.Authorization is not null));
        if (Down)
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }

        if (Limited)
        {
            return Answer(HttpStatusCode.TooManyRequests, new JsonObject { ["message"] = "Too Many Attempts." });
        }

        if (!hadKey || address != new Uri("https://api.autentique.com.br/v2/graphql"))
        {
            return Answer(HttpStatusCode.Unauthorized, new JsonObject { ["message"] = "unauthorized" });
        }

        return operation switch
        {
            "createDocument" => Create(variables, file),
            "document" => Read(variables),
            "resendSignatures" => Resend(variables),
            "updateDocument" => Block(variables),
            "deleteDocument" => Delete(variables),
            _ => Error("graphql", "Cannot query field."),
        };
    }

    private HttpResponseMessage Create(JsonObject variables, (string Name, byte[] Content)? file)
    {
        if (file is null || variables["document"] is not JsonObject document || variables["signers"] is not JsonArray signers || signers.Count == 0)
        {
            return Error("validation", "validation");
        }

        var created = new FakeAutentiqueDocument
        {
            Id = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(25)),
            Name = (string)document["name"]!,
            Message = (string?)document["message"],
            Refusable = (bool?)document["refusable"] ?? false,
            Sandbox = (bool?)variables["sandbox"] ?? false,
            FileName = file.Value.Name,
            File = file.Value.Content,
        };
        created.Signatures.Add(new FakeAutentiqueSignature { PublicId = Guid.NewGuid().ToString(), Email = AccountEmail, Name = "Conta" });
        foreach (var signer in signers.OfType<JsonObject>())
        {
            created.Signatures.Add(new FakeAutentiqueSignature
            {
                PublicId = Guid.NewGuid().ToString(),
                Email = (string)signer["email"]!,
                Name = (string?)signer["name"],
                Action = (string?)signer["action"] ?? "SIGN",
            });
        }

        _documents[created.Id] = created;
        return Data("createDocument", DocumentJson(created));
    }

    private HttpResponseMessage Read(JsonObject variables) =>
        _documents.TryGetValue((string?)variables["id"] ?? string.Empty, out var document)
            ? Data("document", DocumentJson(document))
            : Error("document_not_found", "document");

    private HttpResponseMessage Resend(JsonObject variables)
    {
        var ids = (variables["public_ids"] as JsonArray ?? new JsonArray()).Select(id => (string)id!).ToList();
        var document = _documents.Values.FirstOrDefault(item => item.Signatures.Exists(signature => ids.Contains(signature.PublicId)));
        if (document is null)
        {
            return Error("signature_not_found", "resendSignatures");
        }

        if (ResendBlocked)
        {
            return Error("too_many_resent_emails", "resendSignatures");
        }

        document.Resent.AddRange(ids);
        return Data("resendSignatures", true);
    }

    private HttpResponseMessage Block(JsonObject variables)
    {
        if (!_documents.TryGetValue((string?)variables["id"] ?? string.Empty, out var document))
        {
            return Error("document_not_found", "updateDocument");
        }

        if (document.Signers.All(signature => signature.SignedAt is not null))
        {
            return Error("document_signed", "updateDocument");
        }

        var deadline = (string?)(variables["document"] as JsonObject)?["deadline_at"];
        document.DeadlineAt = DateTime.Parse(deadline!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        return Data("updateDocument", new JsonObject { ["id"] = document.Id });
    }

    private HttpResponseMessage Delete(JsonObject variables) =>
        _documents.TryRemove((string?)variables["id"] ?? string.Empty, out _)
            ? Data("deleteDocument", true)
            : Error("document_not_found", "deleteDocument");

    private HttpResponseMessage Download(Uri address)
    {
        var match = SignedFile().Match(address.AbsolutePath);
        if (!match.Success || !_documents.TryGetValue(match.Groups[1].Value, out var document) || !document.Signatures.Exists(item => item.SignedAt is not null))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        // The PDF with the signatures: the original with the mark of the Autentique.
        var signed = document.File.Concat(Encoding.ASCII.GetBytes("\n% assinado pela Autentique\n")).ToArray();
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(signed) };
        response.Content.Headers.ContentType = new("application/pdf");
        return response;
    }

    private FakeAutentiqueSignature OpenSignerOf(string documentId, int index)
    {
        var document = _documents[documentId];
        if (document.DeadlineAt is not null || document.Signers.Any(signature => signature.RejectedAt is not null))
        {
            throw new InvalidOperationException("The document is blocked at the Autentique.");
        }

        return document.Signers.ElementAt(index);
    }

    private JsonObject DocumentJson(FakeAutentiqueDocument document) => new()
    {
        ["id"] = document.Id,
        ["name"] = document.Name,
        ["sandbox"] = document.Sandbox,
        ["deadline_at"] = DateText(document.DeadlineAt),
        ["files"] = new JsonObject
        {
            ["signed"] = document.Signatures.Exists(signature => signature.SignedAt is not null) ? SignedAddressOf(document.Id).AbsoluteUri : null,
        },
        ["signatures"] = new JsonArray([.. document.Signatures.Select(signature => (JsonNode?)new JsonObject
        {
            ["public_id"] = signature.PublicId,
            ["name"] = signature.Name,
            ["email"] = signature.Email,
            ["action"] = signature.Action is null ? null : new JsonObject { ["name"] = signature.Action },
            ["user"] = new JsonObject { ["email"] = signature.Email },
            ["signed"] = signature.SignedAt is { } signed ? new JsonObject { ["created_at"] = DateText(signed) } : null,
            ["rejected"] = signature.RejectedAt is { } rejected
                ? new JsonObject { ["created_at"] = DateText(rejected), ["reason"] = signature.Reason }
                : null,
        })]),
    };

    private static string? DateText(DateTime? at) =>
        at?.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture);

    // The operation, by the first field of the query: the document reading last, its name being part of the others.
    private static string OperationOf(string query)
    {
        var mutation = Mutation().Match(query);
        return mutation.Success ? mutation.Groups[1].Value : DocumentField().IsMatch(query) ? "document" : "unknown";
    }

    // A JSON request, or the GraphQL multipart request: "operations", "map" and the file it maps to variables.file.
    private static async Task<(string Query, JsonObject Variables, (string Name, byte[] Content)? File)> ReadAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is MultipartFormDataContent multipart)
        {
            JsonObject? operations = null;
            string? map = null;
            (string Name, byte[] Content)? file = null;
            foreach (var part in multipart)
            {
                var disposition = part.Headers.ContentDisposition!;
                var name = disposition.Name?.Trim('"');
                if (name == "operations")
                {
                    operations = JsonNode.Parse(await part.ReadAsStringAsync(cancellationToken)) as JsonObject;
                }
                else if (name == "map")
                {
                    map = await part.ReadAsStringAsync(cancellationToken);
                }
                else if (name == "file")
                {
                    file = ((disposition.FileName ?? string.Empty).Trim('"'), await part.ReadAsByteArrayAsync(cancellationToken));
                }
            }

            var variables = operations?["variables"] as JsonObject ?? new JsonObject();
            var mapped = map is not null && JsonNode.DeepEquals(JsonNode.Parse(map), JsonNode.Parse("""{"file":["variables.file"]}"""))
                && variables.ContainsKey("file") && variables["file"] is null;
            return ((string?)operations?["query"] ?? string.Empty, variables, mapped ? file : null);
        }

        var body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken)) as JsonObject;
        return ((string?)body?["query"] ?? string.Empty, body?["variables"] as JsonObject ?? new JsonObject(), null);
    }

    private static HttpResponseMessage Data(string field, JsonNode? value) =>
        Answer(HttpStatusCode.OK, new JsonObject { ["data"] = new JsonObject { [field] = value } });

    private static HttpResponseMessage Error(string message, string field) =>
        Answer(HttpStatusCode.OK, new JsonObject
        {
            ["errors"] = new JsonArray(new JsonObject { ["message"] = message, ["path"] = new JsonArray(field) }),
            ["data"] = new JsonObject { [field] = null },
        });

    private static HttpResponseMessage Answer(HttpStatusCode status, JsonObject body) =>
        new(status) { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };

    [GeneratedRegex(@"/documentos/([0-9a-f]+)/assinado\.pdf$")]
    private static partial Regex SignedFile();

    [GeneratedRegex(@"\b(createDocument|resendSignatures|updateDocument|deleteDocument)\s*\(")]
    private static partial Regex Mutation();

    [GeneratedRegex(@"(?<![A-Za-z_])document\s*\(")]
    private static partial Regex DocumentField();
}
