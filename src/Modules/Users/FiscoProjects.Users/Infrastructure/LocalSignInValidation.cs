using FiscoProjects.Users.Application;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>Checked when the host starts, so a wrong setup stops the application instead of running.</summary>
internal sealed class LocalSignInValidation(IHostEnvironment environment) : IValidateOptions<LocalSignInOptions>
{
    public ValidateOptionsResult Validate(string? name, LocalSignInOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (environment.IsProduction())
        {
            return ValidateOptionsResult.Fail(
                "Local sign-in (LocalSignIn:Enabled) is not allowed in production (ADR-0078). Turn it off to start the application.");
        }

        var failures = new List<string>();
        if (options.People.Any(person => string.IsNullOrWhiteSpace(person.Name) || string.IsNullOrWhiteSpace(person.Email)))
        {
            failures.Add("Every person of LocalSignIn:People needs a name and an e-mail.");
        }

        var repeated = options.People
            .GroupBy(person => SignInLocally.Normalize(person.Email))
            .Where(group => group.Key.Length > 0 && group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (repeated.Count > 0)
        {
            failures.Add($"LocalSignIn:People repeats the e-mail {string.Join(", ", repeated)}.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
