using FiscoProjects.Users.Domain;

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
/// Signs in an identity: creates the user and the provider link on the first sign-in (ADR-0005), and recognizes
/// the same user afterwards, refreshing name and e-mail.
/// </summary>
public sealed class SignInWithIdentity(IUserStore store, TimeProvider time)
{
    public async Task<SignInResult> ExecuteAsync(ProviderIdentity identity, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var user = await store.FindByIdentityAsync(identity.Provider, identity.ExternalId, cancellationToken);

        if (user is null)
        {
            user = User.Create(identity.Name, identity.Email, now);
            store.Add(user, IdentityLink.For(user, identity.Provider, identity.ExternalId));
        }
        else if (!user.IsActive)
        {
            return new SignInResult.Inactive();
        }
        else
        {
            user.RecordSignIn(identity.Name, identity.Email, now);
        }

        await store.SaveChangesAsync(cancellationToken);
        return new SignInResult.SignedIn(user.Id);
    }
}
