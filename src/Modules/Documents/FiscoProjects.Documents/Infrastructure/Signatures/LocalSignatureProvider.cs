using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Domain;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Documents.Infrastructure.Signatures;

/// <summary>
/// The local provider of signatures (ADR-0129), for development, the demonstration and the tests, never in production:
/// each request is a folder with the PDF and a JSON with the signers, each with a code of its own for the link of the
/// local page, where the signer signs or refuses. It sends no e-mail: the drawer of the term shows the links. The
/// "signed" PDF is the term itself.
/// </summary>
internal sealed partial class LocalSignatureProvider(string folder, TimeProvider clock, ILogger<LocalSignatureProvider> logger)
    : ISignatureProvider, ISigningLinks, ILocalSigningPage, IDisposable
{
    private const string StateFile = "request.json";
    private const string DocumentFile = "term.pdf";

    // A code is 32 random bytes in base64url, without padding.
    private const int TokenLength = 43;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    // One change at a time, in this process: the requests are files.
    private readonly SemaphoreSlim _lock = new(1, 1);

    public string Name => SignatureOptions.LocalProvider;

    public void Dispose() => _lock.Dispose();

    public async Task<SentSignature> SendAsync(SignatureRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Signers.Count == 0)
        {
            throw Failed("send");
        }

        var documentId = Guid.CreateVersion7().ToString("N");
        var state = new LocalRequest
        {
            Title = request.Title,
            Message = request.Message,
            FileName = request.FileName,
            Status = LocalRequestStatus.Open,
            Signers =
            [
                .. request.Signers.Select(signer => new LocalSigner
                {
                    Id = Guid.CreateVersion7().ToString("N"),
                    Name = signer.Name,
                    Email = signer.Email,
                    Token = NewToken(),
                    Status = AcceptanceSignerStatus.Pending,
                }),
            ],
        };
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var path = Path.Combine(folder, documentId);
            Directory.CreateDirectory(path);
            await using (var file = File.Create(Path.Combine(path, DocumentFile)))
            {
                await request.Content.CopyToAsync(file, cancellationToken);
            }

            await WriteAsync(documentId, state, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }

        return new SentSignature(documentId, [.. state.Signers.Select(signer => signer.Id)]);
    }

    public async Task<IReadOnlyList<SignerState>> StatusAsync(string documentId, CancellationToken cancellationToken)
    {
        var state = await ReadAsync(documentId, cancellationToken) ?? throw Failed("status");
        return [.. state.Signers.Select(signer => new SignerState(signer.Id, signer.Status, signer.At, signer.Reason))];
    }

    public Task RemindAsync(string documentId, CancellationToken cancellationToken) =>
        ChangeAsync(documentId, "remind", state =>
        {
            if (state.Status != LocalRequestStatus.Open)
            {
                return false;
            }

            state.Reminders++;
            return true;
        }, cancellationToken);

    public Task CancelAsync(string documentId, CancellationToken cancellationToken) =>
        ChangeAsync(documentId, "cancel", state =>
        {
            if (state.Status == LocalRequestStatus.Open)
            {
                state.Status = LocalRequestStatus.Cancelled;
            }

            return true;
        }, cancellationToken);

    public async Task<SignedFile> SignedFileAsync(string documentId, CancellationToken cancellationToken)
    {
        var state = await ReadAsync(documentId, cancellationToken);
        if (state is null || state.Signers.Exists(signer => signer.Status != AcceptanceSignerStatus.Signed))
        {
            throw Failed("signed file");
        }

        return new SignedFile(state.FileName, File.OpenRead(Path.Combine(folder, documentId, DocumentFile)));
    }

    public async Task DeleteAsync(string documentId, CancellationToken cancellationToken)
    {
        if (!IsDocumentId(documentId))
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var path = Path.Combine(folder, documentId);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string?> LinkOfAsync(string documentId, string signerId, CancellationToken cancellationToken) =>
        await ReadAsync(documentId, cancellationToken) is { Status: LocalRequestStatus.Open } state
        && state.Signers.Find(signer => signer.Id == signerId) is { Status: AcceptanceSignerStatus.Pending } signer
            ? $"/sign/{signer.Token}"
            : null;

    public async Task<LocalSigning?> FindAsync(string token, CancellationToken cancellationToken) =>
        await PendingAsync(token, cancellationToken) is { } found
            ? new LocalSigning(found.DocumentId, found.Signer.Name, found.State.Title, found.State.Message, found.State.FileName)
            : null;

    public async Task<Stream?> OpenAsync(string token, CancellationToken cancellationToken) =>
        await PendingAsync(token, cancellationToken) is { } found ? File.OpenRead(Path.Combine(folder, found.DocumentId, DocumentFile)) : null;

    public Task<string?> SignAsync(string token, CancellationToken cancellationToken) =>
        AnswerAsync(token, AcceptanceSignerStatus.Signed, null, cancellationToken);

    public Task<string?> RefuseAsync(string token, string reason, CancellationToken cancellationToken) =>
        AnswerAsync(token, AcceptanceSignerStatus.Refused, reason, cancellationToken);

    private async Task<string?> AnswerAsync(string token, AcceptanceSignerStatus status, string? reason, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (await FindPendingAsync(token, cancellationToken) is not { } found)
            {
                return null;
            }

            found.Signer.Status = status;
            found.Signer.At = clock.GetUtcNow().UtcDateTime;
            found.Signer.Reason = reason;
            if (status == AcceptanceSignerStatus.Refused || found.State.Signers.TrueForAll(signer => signer.Status == AcceptanceSignerStatus.Signed))
            {
                found.State.Status = LocalRequestStatus.Closed;
            }

            await WriteAsync(found.DocumentId, found.State, cancellationToken);
            return found.DocumentId;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<(string DocumentId, LocalRequest State, LocalSigner Signer)?> PendingAsync(string token, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await FindPendingAsync(token, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    // The signer of the code, still pending in an open request; the code is checked before any file is read.
    private async Task<(string DocumentId, LocalRequest State, LocalSigner Signer)?> FindPendingAsync(string token, CancellationToken cancellationToken)
    {
        if (token.Length != TokenLength || !token.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            || !Directory.Exists(folder))
        {
            return null;
        }

        foreach (var path in Directory.EnumerateDirectories(folder))
        {
            var documentId = Path.GetFileName(path);
            if (await ReadAsync(documentId, cancellationToken) is not { Status: LocalRequestStatus.Open } state)
            {
                continue;
            }

            if (state.Signers.Find(signer => string.Equals(signer.Token, token, StringComparison.Ordinal)) is { } signer)
            {
                return signer.Status == AcceptanceSignerStatus.Pending ? (documentId, state, signer) : null;
            }
        }

        return null;
    }

    private async Task ChangeAsync(string documentId, string operation, Func<LocalRequest, bool> change, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (await ReadAsync(documentId, cancellationToken) is not { } state || !change(state))
            {
                throw Failed(operation);
            }

            await WriteAsync(documentId, state, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<LocalRequest?> ReadAsync(string documentId, CancellationToken cancellationToken)
    {
        var path = IsDocumentId(documentId) ? Path.Combine(folder, documentId, StateFile) : null;
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        await using var file = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<LocalRequest>(file, Json, cancellationToken);
    }

    private async Task WriteAsync(string documentId, LocalRequest state, CancellationToken cancellationToken)
    {
        var path = Path.Combine(folder, documentId, StateFile);
        var temporary = path + ".tmp";
        await using (var file = File.Create(temporary))
        {
            await JsonSerializer.SerializeAsync(file, state, Json, cancellationToken);
        }

        File.Move(temporary, path, overwrite: true);
    }

    private SignatureProviderException Failed(string operation)
    {
        LogFailed(logger, operation);
        return new SignatureProviderException($"The local provider of signatures refused the {operation}.");
    }

    private static bool IsDocumentId(string documentId) => Guid.TryParseExact(documentId, "N", out _);

    private static string NewToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [LoggerMessage(Level = LogLevel.Warning, Message = "The local provider of signatures refused the {Operation}.")]
    private static partial void LogFailed(ILogger logger, string operation);

    private enum LocalRequestStatus
    {
        Open = 1,
        Closed = 2,
        Cancelled = 3,
    }

    private sealed class LocalRequest
    {
        public string Title { get; set; } = string.Empty;

        public string? Message { get; set; }

        public string FileName { get; set; } = string.Empty;

        public LocalRequestStatus Status { get; set; }

        public int Reminders { get; set; }

        public List<LocalSigner> Signers { get; set; } = [];
    }

    private sealed class LocalSigner
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public AcceptanceSignerStatus Status { get; set; }

        public DateTime? At { get; set; }

        public string? Reason { get; set; }
    }
}
