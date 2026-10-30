using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Documents.Infrastructure.Signatures;

/// <summary>The provider of electronic signatures of the installation (ADR-0129, ADR-0130).</summary>
public sealed class SignatureOptions
{
    public const string Section = "Signature";

    /// <summary>A page of the system itself, for development and tests: nothing leaves (ADR-0004).</summary>
    public const string LocalProvider = "Local";

    /// <summary>The Autentique, through its API (ADR-0130).</summary>
    public const string AutentiqueProvider = "Autentique";

    /// <summary>Which provider signs the terms; empty while none is configured, and the terms are only attached.</summary>
    public string? Provider { get; set; }

    /// <summary>How often the requests at the provider are checked in the background, in minutes.</summary>
    public int CheckMinutes { get; set; } = 15;

    public LocalSignatureOptions Local { get; set; } = new();

    public AutentiqueSignatureOptions Autentique { get; set; } = new();

    /// <summary>
    /// The domains of the signers of the demonstration, only outside production: a request whose signers are all of
    /// them goes to the local provider instead of the provider of the installation, as the e-mails of the demonstration
    /// stay in the local folder (ADR-0123, ADR-0130). Without effect with the local provider or without a provider.
    /// </summary>
    public List<string> LocalDomains { get; set; } = [];

    public bool IsLocal => string.Equals(Provider, LocalProvider, StringComparison.OrdinalIgnoreCase);

    public bool IsAutentique => string.Equals(Provider, AutentiqueProvider, StringComparison.OrdinalIgnoreCase);

    /// <summary>The domains of the demonstration that count: only with a provider other than the local one.</summary>
    public IReadOnlyCollection<string> DemonstrationDomains =>
        IsAutentique ? [.. LocalDomains.Where(domain => !string.IsNullOrWhiteSpace(domain)).Select(domain => domain.Trim().TrimStart('@'))] : [];
}

public sealed class LocalSignatureOptions
{
    /// <summary>The folder of the requests of the local provider, relative to the application, out of Git.</summary>
    public string Folder { get; set; } = "App_Data/signatures";
}

/// <summary>The account of the installation at the Autentique (ADR-0130); the key and the secret come from the Key Vault.</summary>
public sealed class AutentiqueSignatureOptions
{
    /// <summary>The key of the API, from the panel of the Autentique (API keys).</summary>
    public string? ApiKey { get; set; }

    /// <summary>The secret of the notice (webhook) registered in the panel of the Autentique; without it, no notice.</summary>
    public string? WebhookSecret { get; set; }

    /// <summary>Test documents, without cost and without validity, only outside production.</summary>
    public bool Sandbox { get; set; }
}

/// <summary>
/// The application does not start with a provider it does not know, nor with the local one, the sandbox of the
/// Autentique or the domains of the demonstration in production: nobody would get the link to sign, or the terms would
/// have no validity (ADR-0129, ADR-0130), as the local sign-in (ADR-0078) and the local e-mails (ADR-0036).
/// </summary>
internal sealed class SignatureOptionsValidation(IHostEnvironment environment) : IValidateOptions<SignatureOptions>
{
    public ValidateOptionsResult Validate(string? name, SignatureOptions options)
    {
        if (options.CheckMinutes < 1)
        {
            return ValidateOptionsResult.Fail("The requests at the provider of signatures are checked every 1 minute or more (Signature:CheckMinutes).");
        }

        if (environment.IsProduction() && options.LocalDomains.Exists(domain => !string.IsNullOrWhiteSpace(domain)))
        {
            return ValidateOptionsResult.Fail(
                "The domains of the demonstration for the signatures (Signature:LocalDomains) are only for outside production (ADR-0130): the terms of those signers would stay in the local provider.");
        }

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            return ValidateOptionsResult.Success;
        }

        if (options.IsAutentique)
        {
            return ValidateAutentique(options);
        }

        if (!options.IsLocal)
        {
            return ValidateOptionsResult.Fail(
                $"The provider of signatures '{options.Provider}' (Signature:Provider) is unknown. Use {SignatureOptions.AutentiqueProvider}, {SignatureOptions.LocalProvider} outside production, or leave it empty.");
        }

        if (environment.IsProduction())
        {
            return ValidateOptionsResult.Fail(
                "The local provider of signatures (Signature:Provider = Local) is only for outside production (ADR-0129): nobody would get the link to sign. Configure a provider of signatures, or leave it empty, to start the application.");
        }

        return LocalFolder(options);
    }

    private ValidateOptionsResult ValidateAutentique(SignatureOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Autentique.ApiKey))
        {
            return ValidateOptionsResult.Fail(
                "The Autentique needs the key of its API (Signature:Autentique:ApiKey), from the Key Vault (docs/instalacao.md).");
        }

        if (options.Autentique.Sandbox && environment.IsProduction())
        {
            return ValidateOptionsResult.Fail(
                "The sandbox of the Autentique (Signature:Autentique:Sandbox) is only for outside production (ADR-0130): its documents have no validity and are deleted in a few days.");
        }

        return options.DemonstrationDomains.Count > 0 ? LocalFolder(options) : ValidateOptionsResult.Success;
    }

    private static ValidateOptionsResult LocalFolder(SignatureOptions options) =>
        string.IsNullOrWhiteSpace(options.Local.Folder)
            ? ValidateOptionsResult.Fail("The local provider of signatures needs a folder (Signature:Local:Folder).")
            : ValidateOptionsResult.Success;
}
