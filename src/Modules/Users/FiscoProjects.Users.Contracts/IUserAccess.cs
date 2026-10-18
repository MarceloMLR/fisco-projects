namespace FiscoProjects.Users.Contracts;

/// <summary>What the system lets a person do right now: read on every request, so changes apply at once.</summary>
public sealed record UserAccess(Guid UserId, bool IsActive, SystemRole Role);

/// <summary>Access of the person of a session (ADR-0006, ADR-0054).</summary>
public interface IUserAccess
{
    /// <summary>The user's access, or null when the user does not exist.</summary>
    Task<UserAccess?> FindAsync(Guid userId, CancellationToken cancellationToken);
}
