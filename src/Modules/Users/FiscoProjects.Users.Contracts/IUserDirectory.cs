namespace FiscoProjects.Users.Contracts;

/// <summary>
/// A person of the installation as other modules show it, such as in the people of a project. The last sign-in is null
/// for a person included from the directory who never signed in; a person who can be a manager is active and has a
/// system role that may manage a project (<see cref="SystemRoles.ManagesProjects"/>).
/// </summary>
public sealed record PersonInfo(Guid Id, string Name, string Email, DateTime? LastSignInAt, bool IsActive, bool CanBeManager)
{
    /// <summary>Whether the person receives the daily summary of e-mails (ADR-0037); on until the person turns it off.</summary>
    public bool ReceivesDailySummary { get; init; } = true;
}

/// <summary>Names and data of the people of the installation, for modules that show who did something.</summary>
public interface IUserDirectory
{
    /// <summary>The name of each user found; unknown ids are left out.</summary>
    Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);

    /// <summary>The data of each user found; unknown ids are left out.</summary>
    Task<IReadOnlyList<PersonInfo>> PeopleAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}
