using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>Checked when the host starts, so a wrong setup stops the application instead of running.</summary>
internal sealed class EmailOptionsValidation(IHostEnvironment environment) : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        if (options.KeepDays < 1)
        {
            return ValidateOptionsResult.Fail("The queue keeps the e-mails for 1 day or more (Email:KeepDays).");
        }

        if (options.SiteUrl is { Length: > 0 } site
            && !(Uri.TryCreate(site, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)))
        {
            return ValidateOptionsResult.Fail("The address of the screens is an address http or https (Email:SiteUrl).");
        }

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            return ValidateOptionsResult.Success;
        }

        if (!options.IsLocal)
        {
            return ValidateOptionsResult.Fail(
                $"The e-mail sending '{options.Provider}' (Email:Provider) is unknown. Use {EmailOptions.LocalProvider}.");
        }

        if (environment.IsProduction())
        {
            return ValidateOptionsResult.Fail(
                "The local e-mail sending (Email:Provider = Local) is not allowed in production (ADR-0036): nothing would leave. Configure the sending by the Microsoft Graph to start the application.");
        }

        return string.IsNullOrWhiteSpace(options.Local.Folder)
            ? ValidateOptionsResult.Fail("The local e-mail sending needs a folder (Email:Local:Folder).")
            : ValidateOptionsResult.Success;
    }
}
