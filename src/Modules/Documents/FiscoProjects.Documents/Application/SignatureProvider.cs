using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using FiscoProjects.Documents.Domain;

namespace FiscoProjects.Documents.Application;

/// <summary>Why the provider of signatures refused a call.</summary>
public enum SignatureFailure
{
    /// <summary>It did not answer, or answered with an error.</summary>
    Failed = 1,

    /// <summary>It sent the link to the signers a short while ago and does not send it again yet (ADR-0130).</summary>
    RemindedRecently = 2,
}

/// <summary>
/// The provider of signatures failed or refused: it did not answer, or answered with an error, as the
/// <see cref="Failure"/> says. The message never has the signers, the term or the key (ADR-0057).
/// </summary>
public sealed class SignatureProviderException : Exception
{
    public SignatureProviderException()
        : base("The provider of signatures failed.")
    {
    }

    public SignatureProviderException(string message)
        : base(message)
    {
    }

    public SignatureProviderException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public SignatureProviderException(string message, SignatureFailure failure)
        : base(message)
    {
        Failure = failure;
    }

    public SignatureFailure Failure { get; } = SignatureFailure.Failed;
}

/// <summary>A signer as the provider receives it: the name and the e-mail.</summary>
public sealed record SignatureSigner(string Name, string Email);

/// <summary>A term to sign: the title, the message, the name and the content of the PDF, and the signers in order.</summary>
public sealed record SignatureRequest(string Title, string? Message, string FileName, Stream Content, IReadOnlyList<SignatureSigner> Signers);

/// <summary>What the provider gave to a term sent: the id of its document and the id of each signer, in the same order.</summary>
public sealed record SentSignature(string DocumentId, IReadOnlyList<string> SignerIds);

/// <summary>Where a signer stands at the provider: pending, signed at a moment or refused with a reason.</summary>
public sealed record SignerState(string SignerId, AcceptanceSignerStatus Status, DateTime? At = null, string? Reason = null);

/// <summary>The signed PDF of a document, with its name.</summary>
public sealed record SignedFile(string Name, Stream Content);

/// <summary>
/// A provider of electronic signatures (ADR-0129), the only place each provider is known: the Autentique, and the local
/// one for development and tests. Changing the provider is writing another implementation and choosing it in the
/// configuration (<c>Signature:Provider</c>), as the files (ADR-0004). Every failure is a
/// <see cref="SignatureProviderException"/>. The tests of the contract are the same for each implementation.
/// </summary>
public interface ISignatureProvider
{
    /// <summary>The name of the provider, kept with each request, as the configuration names it.</summary>
    string Name { get; }

    /// <summary>Sends the term to the signers, who sign at the same time, without an order; the provider tells them.</summary>
    Task<SentSignature> SendAsync(SignatureRequest request, CancellationToken cancellationToken);

    /// <summary>Where each signer of the document stands.</summary>
    Task<IReadOnlyList<SignerState>> StatusAsync(string documentId, CancellationToken cancellationToken);

    /// <summary>
    /// Sends the link again to the signers who did not sign; a provider that sent it a short while ago refuses with
    /// <see cref="SignatureFailure.RemindedRecently"/>.
    /// </summary>
    Task RemindAsync(string documentId, CancellationToken cancellationToken);

    /// <summary>Cancels the document, so nobody else signs it; a document already closed stays as it is.</summary>
    Task CancelAsync(string documentId, CancellationToken cancellationToken);

    /// <summary>The PDF signed by every signer.</summary>
    Task<SignedFile> SignedFileAsync(string documentId, CancellationToken cancellationToken);

    /// <summary>Deletes the document at the provider, when the provider lets; a document it no longer has is no error.</summary>
    Task DeleteAsync(string documentId, CancellationToken cancellationToken);
}

/// <summary>
/// A provider that sends no e-mail and gives the link of each signer, which the drawer of the term shows (the local
/// provider, outside production; ADR-0129).
/// </summary>
public interface ISigningLinks
{
    /// <summary>The address, in the screens, where the signer of the document signs; null when the signer answered.</summary>
    Task<string?> LinkOfAsync(string documentId, string signerId, CancellationToken cancellationToken);
}

/// <summary>
/// Outside production, the local provider that keeps the requests whose signers are all of the domains of the
/// demonstration, beside the provider of the installation, as the e-mails of the demonstration stay in the local folder
/// (ADR-0123, ADR-0130).
/// </summary>
public sealed class SignatureDemonstration(ISignatureProvider provider, IEnumerable<string> domains)
{
    private readonly HashSet<string> _domains = new(domains, StringComparer.OrdinalIgnoreCase);

    public ISignatureProvider Provider { get; } = provider;

    /// <summary>Whether every signer is of a domain of the demonstration.</summary>
    public bool Takes(IEnumerable<string> emails)
    {
        ArgumentNullException.ThrowIfNull(emails);
        var all = emails.ToList();
        return all.Count > 0 && all.TrueForAll(email => email.LastIndexOf('@') is var at and >= 0 && _domains.Contains(email[(at + 1)..].Trim()));
    }
}

/// <summary>
/// The provider of signatures the installation chose, if any, the one of each new request and the one of a request
/// (ADR-0129, ADR-0130): outside production, the requests of the signers of the demonstration go to the local provider.
/// </summary>
public sealed class SignatureProviders(IEnumerable<ISignatureProvider> providers, SignatureDemonstration? demonstration = null)
{
    /// <summary>The provider chosen in the configuration; null when the installation has none (ADR-0129).</summary>
    public ISignatureProvider? Current { get; } = providers.FirstOrDefault();

    /// <summary>Every provider that may have a request: the one of the installation and the one of the demonstration.</summary>
    public IReadOnlyList<ISignatureProvider> All =>
        Current is not { } current ? [] : demonstration is null || demonstration.Provider == current ? [current] : [current, demonstration.Provider];

    /// <summary>The provider of a new request to these signers; null when the installation has none.</summary>
    public ISignatureProvider? For(IEnumerable<string> emails) =>
        Current is null ? null : demonstration is not null && demonstration.Takes(emails) ? demonstration.Provider : Current;

    /// <summary>The provider that has the request; null for a request asked before, or at a provider no longer chosen.</summary>
    public ISignatureProvider? Of(AcceptanceTerm term)
    {
        ArgumentNullException.ThrowIfNull(term);
        return term.IsSignatureRequest
            ? All.FirstOrDefault(provider => string.Equals(term.SignatureProvider, provider.Name, StringComparison.Ordinal))
            : null;
    }
}

/// <summary>A notice of the provider, read: whether its signature is right, and the document it is about, if any.</summary>
public sealed record SignatureNotice(bool IsValid, string? DocumentId)
{
    /// <summary>A notice without the signature of the secret of the installation.</summary>
    public static SignatureNotice Forged { get; } = new(false, null);
}

/// <summary>
/// A provider that tells the changes of its documents by notices to the installation (webhooks), checked by the
/// signature of the notice with the secret of the installation (ADR-0130). The notice only names the document: what
/// changed always comes from the provider.
/// </summary>
public interface ISignatureNotices
{
    /// <summary>The name of the provider of the notices.</summary>
    string Provider { get; }

    /// <summary>Reads a notice by its raw body and its headers; never throws for a malformed one.</summary>
    SignatureNotice Read(byte[] body, Func<string, string?> header);
}

/// <summary>
/// The documents whose notice arrived, waiting for the check in the background (ADR-0130): in memory, up to
/// <see cref="Capacity"/>, each once while it waits. A notice lost, when the queue is full or the application restarts,
/// is left to the periodic check.
/// </summary>
public sealed class SignatureNoticeQueue
{
    public const int Capacity = 500;

    private readonly Channel<(string Provider, string DocumentId)> _channel =
        Channel.CreateBounded<(string Provider, string DocumentId)>(new BoundedChannelOptions(Capacity) { SingleReader = true });

    private readonly ConcurrentDictionary<(string Provider, string DocumentId), byte> _waiting = new();

    /// <summary>Puts the check of the document in the queue; false when it already waits there, or the queue is full.</summary>
    public bool Add(string provider, string documentId)
    {
        var item = (provider, documentId);
        if (!_waiting.TryAdd(item, 0))
        {
            return false;
        }

        if (_channel.Writer.TryWrite(item))
        {
            return true;
        }

        _waiting.TryRemove(item, out _);
        return false;
    }

    /// <summary>The documents in the order they arrived, as long as the application runs.</summary>
    public async IAsyncEnumerable<(string Provider, string DocumentId)> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            _waiting.TryRemove(item, out _);
            yield return item;
        }
    }
}

/// <summary>A request as the local page of a signer shows it: the title, the message, the name of the signer and the PDF.</summary>
public sealed record LocalSigning(string DocumentId, string SignerName, string Title, string? Message, string FileName);

/// <summary>
/// The page where a signer of the local provider signs or refuses, without a session, by the link of the signer
/// (ADR-0129); only outside production. Every answer is null when the link is not of a signer still pending in an open
/// request.
/// </summary>
public interface ILocalSigningPage
{
    Task<LocalSigning?> FindAsync(string token, CancellationToken cancellationToken);

    Task<Stream?> OpenAsync(string token, CancellationToken cancellationToken);

    /// <summary>The signer signs; the document, or null when the link is no longer valid.</summary>
    Task<string?> SignAsync(string token, CancellationToken cancellationToken);

    /// <summary>The signer refuses with the reason; the document, or null when the link is no longer valid.</summary>
    Task<string?> RefuseAsync(string token, string reason, CancellationToken cancellationToken);
}
