using FiscoProjects.Audit.Contracts;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Application;

/// <summary>
/// Local sign-in (ADR-0078): outside production, a person of the demo directory signs in with the e-mail alone,
/// while there is no Microsoft tenant. Off by default; the host refuses to start in production with it on.
/// </summary>
public sealed class LocalSignInOptions
{
    public const string Section = "LocalSignIn";

    public bool Enabled { get; set; }

    public List<LocalPerson> People { get; set; } = [];
}

/// <summary>A fictitious person of the demo directory.</summary>
public sealed class LocalPerson
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}

public abstract record LocalSignInResult
{
    private LocalSignInResult() { }

    public sealed record SignedIn(Guid UserId) : LocalSignInResult;

    public sealed record NotInDirectory : LocalSignInResult;

    /// <summary>The person is in the directory, but the user may no longer use the system.</summary>
    public sealed record Inactive : LocalSignInResult;

    public sealed record Disabled : LocalSignInResult;
}

/// <remarks>
/// With the sign-in through Entra ID on too, as in the development environment in Azure (ADR-0117), an e-mail outside the
/// demo directory is not refused: the sign-in page takes it to Microsoft, so no refusal is recorded.
/// </remarks>
public sealed class SignInLocally(
    IOptions<LocalSignInOptions> options, IOptions<EntraSignInOptions> entra, SignInWithIdentity signIn, ISecurityEvents securityEvents)
{
    public const string Provider = "local";

    public bool IsEnabled => options.Value.Enabled;

    public async Task<LocalSignInResult> ExecuteAsync(string email, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            return new LocalSignInResult.Disabled();
        }

        var externalId = Normalize(email);
        var person = options.Value.People.FirstOrDefault(person => Normalize(person.Email) == externalId);
        if (person is null)
        {
            if (!entra.Value.Enabled)
            {
                await securityEvents.SignInRefusedAsync(email ?? string.Empty, SignInRefusal.NotInDirectory, cancellationToken);
            }

            return new LocalSignInResult.NotInDirectory();
        }

        var result = await signIn.ExecuteAsync(
            new ProviderIdentity(Provider, externalId, person.Name, person.Email), cancellationToken);
        return result switch
        {
            SignInResult.SignedIn signedIn => new LocalSignInResult.SignedIn(signedIn.UserId),
            _ => new LocalSignInResult.Inactive(),
        };
    }

    /// <summary>The identifier of a local identity: the e-mail without spaces, in lowercase.</summary>
    public static string Normalize(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}
