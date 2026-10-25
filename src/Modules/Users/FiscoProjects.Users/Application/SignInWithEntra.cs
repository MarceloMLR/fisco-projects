using System.Security.Claims;
using FiscoProjects.Audit.Contracts;

namespace FiscoProjects.Users.Application;

/// <summary>
/// The sign-in through Entra ID (ADR-0005, ADR-0117), from the "EntraSignIn" section. Off by default: it needs the
/// application of the installation in Entra ID and the managed identity of the App Service, which proves to Entra
/// that the request comes from the application.
/// </summary>
public sealed class EntraSignInOptions
{
    public const string Section = "EntraSignIn";

    /// <summary>The authentication scheme of the sign-in, which the route of the start challenges.</summary>
    public const string Scheme = "entra";

    /// <summary>The item of the sign-in that carries the e-mail typed, only until it becomes the hint to Microsoft.</summary>
    public const string LoginHintItem = "fiscoprojects.loginHint";

    /// <summary>The item of the sign-in with the user of the expired session, if there was one.</summary>
    public const string PreviousUserItem = "fiscoprojects.previousUser";

    public bool Enabled { get; set; }

    /// <summary>The address of the Microsoft identity platform, ending with a slash.</summary>
    public string Instance { get; set; } = "https://login.microsoftonline.com/";

    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    /// <summary>The issuer of the tenant of the installation, which the tokens and the managed identity share.</summary>
    public string Authority => $"{Instance.TrimEnd('/')}/{TenantId}/v2.0";
}

/// <summary>What the ID token of Entra says about the person (ADR-0117).</summary>
public sealed record EntraIdentity(
    string? ObjectId, string? Name, string? Email, string? PreferredUsername, string? AccountStatus, string? IdentityProvider)
{
    /// <summary>The claims of a validated ID token, by their names in the token.</summary>
    public static EntraIdentity From(ClaimsPrincipal principal) => new(
        ValueOf(principal, "oid"),
        ValueOf(principal, "name"),
        ValueOf(principal, "email"),
        ValueOf(principal, "preferred_username"),
        ValueOf(principal, "acct"),
        ValueOf(principal, "idp"));

    /// <summary>
    /// A guest of the tenant (ADR-0007): the optional claim "acct" is 1 for guests, and "idp" names the home tenant or
    /// the Microsoft account of an identity that comes from somewhere else.
    /// </summary>
    public bool IsGuest => AccountStatus == "1" || !string.IsNullOrWhiteSpace(IdentityProvider);

    /// <summary>The e-mail of the token, or the sign-in name when the person has no mailbox.</summary>
    public string EmailOrUsername => (string.IsNullOrWhiteSpace(Email) ? PreferredUsername : Email)?.Trim() ?? string.Empty;

    private static string? ValueOf(ClaimsPrincipal principal, string type) => principal.FindFirst(type)?.Value;
}

public abstract record EntraSignInResult
{
    private EntraSignInResult() { }

    public sealed record SignedIn(Guid UserId) : EntraSignInResult;

    /// <summary>A guest account of the tenant: refused, and no user is created (ADR-0007).</summary>
    public sealed record Guest : EntraSignInResult;

    /// <summary>The user exists but may no longer use the system.</summary>
    public sealed record Inactive : EntraSignInResult;

    /// <summary>The token has no object id, so the person cannot be recognized.</summary>
    public sealed record Unrecognized : EntraSignInResult;
}

/// <summary>
/// Signs in the person of a validated Entra ID token (ADR-0117): refuses guests before anything is created, and
/// otherwise follows the common path of every provider, with the link "entra" by the immutable object id.
/// </summary>
public sealed class SignInWithEntra(SignInWithIdentity signIn, ISecurityEvents securityEvents)
{
    public const string Provider = "entra";

    public async Task<EntraSignInResult> ExecuteAsync(EntraIdentity identity, CancellationToken cancellationToken)
    {
        var email = identity.EmailOrUsername;
        if (identity.IsGuest)
        {
            await securityEvents.SignInRefusedAsync(email, SignInRefusal.Guest, cancellationToken);
            return new EntraSignInResult.Guest();
        }

        if (string.IsNullOrWhiteSpace(identity.ObjectId))
        {
            return new EntraSignInResult.Unrecognized();
        }

        var name = string.IsNullOrWhiteSpace(identity.Name) ? email : identity.Name.Trim();
        var result = await signIn.ExecuteAsync(
            new ProviderIdentity(Provider, identity.ObjectId.Trim().ToLowerInvariant(), name, email), cancellationToken);
        return result switch
        {
            SignInResult.SignedIn signedIn => new EntraSignInResult.SignedIn(signedIn.UserId),
            _ => new EntraSignInResult.Inactive(),
        };
    }
}
