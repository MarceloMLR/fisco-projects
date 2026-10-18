namespace FiscoProjects.Audit.Contracts;

/// <summary>Why a sign-in was refused.</summary>
public enum SignInRefusal
{
    /// <summary>No person of the directory has this identity.</summary>
    NotInDirectory = 1,

    /// <summary>The user exists but was deactivated.</summary>
    Inactive = 2,
}

/// <summary>
/// Security events that are not a change to an item, recorded in the audit (ADR-0039). Role changes,
/// deactivations and reactivations are changes to the user and are recorded when the Users module saves them.
/// </summary>
public interface ISecurityEvents
{
    /// <summary>A person signed in, with the identity provider used (<c>local</c>, <c>entra</c>).</summary>
    Task SignedInAsync(Guid userId, string provider, CancellationToken cancellationToken);

    /// <summary>A sign-in was refused: the identity used (as typed or sent by the provider) and why.</summary>
    Task SignInRefusedAsync(string identity, SignInRefusal reason, CancellationToken cancellationToken);
}
