namespace FiscoProjects.Users.Application;

/// <summary>Why the token of Teams did not recognize a person (ADR-0152).</summary>
public enum TeamsRefusal
{
    /// <summary>A guest account of the tenant (ADR-0007).</summary>
    Guest,

    /// <summary>The token has no object id.</summary>
    Unrecognized,

    /// <summary>No user has the link "entra" of the object id: the person never signed in nor was included.</summary>
    Unknown,

    /// <summary>The user may no longer use the system (ADR-0008).</summary>
    Inactive,
}

public abstract record TeamsSignInResult
{
    private TeamsSignInResult() { }

    public sealed record Recognized(Guid UserId) : TeamsSignInResult;

    public sealed record Refused(TeamsRefusal Reason) : TeamsSignInResult;
}

/// <summary>
/// The person of a validated token of Teams (ADR-0152): the user of the link "entra" by the object id, as in the sign-in
/// through Entra ID (ADR-0117), with the same rule for guests. Nothing is created nor written: the token only reads, so
/// a refusal is not a sign-in refused, and a person who never signed in nor was included stays unknown.
/// </summary>
public sealed class SignInFromTeams(IUserStore store)
{
    public async Task<TeamsSignInResult> ExecuteAsync(EntraIdentity identity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (identity.IsGuest)
        {
            return new TeamsSignInResult.Refused(TeamsRefusal.Guest);
        }

        if (identity.ExternalId is not { } externalId)
        {
            return new TeamsSignInResult.Refused(TeamsRefusal.Unrecognized);
        }

        var user = await store.FindByIdentityAsync(SignInWithEntra.Provider, externalId, cancellationToken);
        return user switch
        {
            null => new TeamsSignInResult.Refused(TeamsRefusal.Unknown),
            { IsActive: false } => new TeamsSignInResult.Refused(TeamsRefusal.Inactive),
            _ => new TeamsSignInResult.Recognized(user.Id),
        };
    }
}
