using FiscoProjects.Users.Application;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>Checked when the host starts, so the sign-in through Entra ID does not start half configured (ADR-0117).</summary>
internal sealed class EntraSignInValidation : IValidateOptions<EntraSignInOptions>
{
    public ValidateOptionsResult Validate(string? name, EntraSignInOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (!Guid.TryParse(options.TenantId, out _))
        {
            failures.Add("EntraSignIn:TenantId needs the id of the tenant of the installation.");
        }

        if (!Guid.TryParse(options.ClientId, out _))
        {
            failures.Add("EntraSignIn:ClientId needs the client id of the application of the installation in Entra ID.");
        }

        if (!Uri.TryCreate(options.Instance, UriKind.Absolute, out var instance) || instance.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add("EntraSignIn:Instance needs the https address of the Microsoft identity platform.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
