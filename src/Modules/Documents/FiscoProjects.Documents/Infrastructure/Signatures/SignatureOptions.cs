using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Documents.Infrastructure.Signatures;

/// <summary>The provider of electronic signatures of the installation (ADR-0129).</summary>
public sealed class SignatureOptions
{
    public const string Section = "Signature";

    /// <summary>A page of the system itself, for development and tests: nothing leaves (ADR-0004).</summary>
    public const string LocalProvider = "Local";

    /// <summary>Which provider signs the terms; empty while none is configured, and the terms are only attached.</summary>
    public string? Provider { get; set; }

    /// <summary>How often the requests at the provider are checked in the background, in minutes.</summary>
    public int CheckMinutes { get; set; } = 15;

    public LocalSignatureOptions Local { get; set; } = new();

    public bool IsLocal => string.Equals(Provider, LocalProvider, StringComparison.OrdinalIgnoreCase);
}

public sealed class LocalSignatureOptions
{
    /// <summary>The folder of the requests of the local provider, relative to the application, out of Git.</summary>
    public string Folder { get; set; } = "App_Data/signatures";
}

/// <summary>
/// The application does not start with a provider it does not know, nor with the local one in production: nobody would
/// get the link to sign (ADR-0129), as the local sign-in (ADR-0078) and the local e-mails (ADR-0036).
/// </summary>
internal sealed class SignatureOptionsValidation(IHostEnvironment environment) : IValidateOptions<SignatureOptions>
{
    public ValidateOptionsResult Validate(string? name, SignatureOptions options)
    {
        if (options.CheckMinutes < 1)
        {
            return ValidateOptionsResult.Fail("The requests at the provider of signatures are checked every 1 minute or more (Signature:CheckMinutes).");
        }

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            return ValidateOptionsResult.Success;
        }

        if (!options.IsLocal)
        {
            return ValidateOptionsResult.Fail(
                $"The provider of signatures '{options.Provider}' (Signature:Provider) is unknown. Use {SignatureOptions.LocalProvider} outside production, or leave it empty.");
        }

        if (environment.IsProduction())
        {
            return ValidateOptionsResult.Fail(
                "The local provider of signatures (Signature:Provider = Local) is only for outside production (ADR-0129): nobody would get the link to sign. Configure a provider of signatures, or leave it empty, to start the application.");
        }

        return string.IsNullOrWhiteSpace(options.Local.Folder)
            ? ValidateOptionsResult.Fail("The local provider of signatures needs a folder (Signature:Local:Folder).")
            : ValidateOptionsResult.Success;
    }
}
