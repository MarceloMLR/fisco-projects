using FiscoProjects.Audit.Contracts;
using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Domain;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Application;

/// <summary>Who the identity provider says the person is.</summary>
public sealed record ProviderIdentity(string Provider, string ExternalId, string Name, string Email);

public abstract record SignInResult
{
    private SignInResult() { }

    public sealed record SignedIn(Guid UserId) : SignInResult;

    /// <summary>The user exists but may no longer use the system.</summary>
    public sealed record Inactive : SignInResult;
}

/// <summary>
/// Signs in an identity: creates the user and the provider link on the first sign-in, with the least privileged
/// role (ADR-0005, ADR-0006), and recognizes the same user afterwards, refreshing name and e-mail. The configured
/// first administrator becomes Administrator when created, or later while the installation has no active
/// administrator; a role chosen on the Users screen is never undone by the configuration. The sign-in and the
/// refusal of a deactivated user are security events of the audit (ADR-0039).
/// </summary>
public sealed class SignInWithIdentity(
    IUserStore store, TimeProvider time, IOptions<FirstAdministratorOptions> firstAdministrator, ISecurityEvents securityEvents)
{
    public async Task<SignInResult> ExecuteAsync(ProviderIdentity identity, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var user = await store.FindByIdentityAsync(identity.Provider, identity.ExternalId, cancellationToken);
        var created = user is null;

        if (user is null)
        {
            user = User.Create(identity.Name, identity.Email, now);
            store.Add(user, IdentityLink.For(user, identity.Provider, identity.ExternalId));
        }
        else if (!user.IsActive)
        {
            await securityEvents.SignInRefusedAsync(identity.Email, SignInRefusal.Inactive, cancellationToken);
            return new SignInResult.Inactive();
        }
        else
        {
            user.RecordSignIn(identity.Name, identity.Email, now);
        }

        if (firstAdministrator.Value.Matches(identity)
            && user.Role != SystemRole.Administrator
            && (created || !await store.AnyActiveAdministratorAsync(cancellationToken)))
        {
            user.ChangeRole(SystemRole.Administrator);
        }

        await store.SaveChangesAsync(cancellationToken);
        await securityEvents.SignedInAsync(user.Id, identity.Provider, cancellationToken);
        return new SignInResult.SignedIn(user.Id);
    }
}
