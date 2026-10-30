using FiscoProjects.Documents.Domain;

namespace FiscoProjects.Documents.Application;

/// <summary>The answer of the local page: done, a link no longer valid, or a reason that is not valid.</summary>
public enum LocalSigningOutcome
{
    Done = 1,
    NotFound = 2,
    Invalid = 3,
}

/// <summary>
/// A signer of the local provider signs or refuses on the page of the link (ADR-0129); the request takes the change at
/// once, by the same use case as the check in the background.
/// </summary>
public sealed class AnswerLocalSigning(ILocalSigningPage page, SignatureProviders providers, ApplySignatureStatus apply)
{
    public async Task<LocalSigningOutcome> SignAsync(string token, CancellationToken cancellationToken)
    {
        if (await page.SignAsync(token, cancellationToken) is not { } documentId)
        {
            return LocalSigningOutcome.NotFound;
        }

        await ApplyAsync(documentId, cancellationToken);
        return LocalSigningOutcome.Done;
    }

    public async Task<LocalSigningOutcome> RefuseAsync(string token, string? reason, CancellationToken cancellationToken)
    {
        if (await page.FindAsync(token, cancellationToken) is null)
        {
            return LocalSigningOutcome.NotFound;
        }

        if (AcceptanceTerm.ReasonOf(reason) is not { } valid)
        {
            return LocalSigningOutcome.Invalid;
        }

        if (await page.RefuseAsync(token, valid, cancellationToken) is not { } documentId)
        {
            return LocalSigningOutcome.NotFound;
        }

        await ApplyAsync(documentId, cancellationToken);
        return LocalSigningOutcome.Done;
    }

    private async Task ApplyAsync(string documentId, CancellationToken cancellationToken)
    {
        // The local provider of the installation, or the one of the signers of the demonstration (ADR-0130).
        foreach (var provider in providers.All.Where(provider => provider is ILocalSigningPage))
        {
            await apply.ForDocumentAsync(provider.Name, documentId, cancellationToken);
        }
    }
}
