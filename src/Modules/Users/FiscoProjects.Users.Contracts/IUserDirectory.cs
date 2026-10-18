namespace FiscoProjects.Users.Contracts;

/// <summary>Names of the people of the installation, for modules that show who did something.</summary>
public interface IUserDirectory
{
    /// <summary>The name of each user found; unknown ids are left out.</summary>
    Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}
