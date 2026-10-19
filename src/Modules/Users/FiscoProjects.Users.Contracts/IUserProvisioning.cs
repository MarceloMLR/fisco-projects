namespace FiscoProjects.Users.Contracts;

/// <summary>
/// Turns a person of the company directory into a user when someone includes the person in a project before the first
/// sign-in (ADR-0005). The person signs in later with the same user.
/// </summary>
public interface IUserProvisioning
{
    /// <summary>
    /// The user of the person the directory id names (as the people search returns it): the user already linked, when
    /// active, or a new one with the role User. Null when the directory does not have the person or the user is
    /// inactive.
    /// </summary>
    Task<Guid?> UserFromDirectoryAsync(string directoryId, CancellationToken cancellationToken);
}
