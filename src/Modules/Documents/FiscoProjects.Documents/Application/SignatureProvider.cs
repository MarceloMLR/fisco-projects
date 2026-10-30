using FiscoProjects.Documents.Domain;

namespace FiscoProjects.Documents.Application;

/// <summary>
/// The provider of signatures failed or refused: it did not answer, or answered with an error. The message never has
/// the signers, the term or the key (ADR-0057).
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

    /// <summary>Sends the link again to the signers who did not sign.</summary>
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

/// <summary>The provider of signatures the installation chose, if any, and whether it is the one of a request.</summary>
public sealed class SignatureProviders(IEnumerable<ISignatureProvider> providers)
{
    /// <summary>The provider chosen in the configuration; null when the installation has none (ADR-0129).</summary>
    public ISignatureProvider? Current { get; } = providers.FirstOrDefault();

    /// <summary>The provider that has the request; null for a request asked before, or at a provider no longer chosen.</summary>
    public ISignatureProvider? Of(AcceptanceTerm term)
    {
        ArgumentNullException.ThrowIfNull(term);
        return Current is { } current && term.IsSignatureRequest && string.Equals(term.SignatureProvider, current.Name, StringComparison.Ordinal)
            ? current
            : null;
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
