namespace FiscoProjects.Users.Contracts;

/// <summary>Whether a person may still use the system: the session checks it on every request (ADR-0054).</summary>
public interface IUserAccess
{
    Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken);
}
