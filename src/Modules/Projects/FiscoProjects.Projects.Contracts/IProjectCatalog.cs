namespace FiscoProjects.Projects.Contracts;

/// <summary>The status of a project (ADR-0016), as the other modules see it, with the names of the module's own.</summary>
public enum ProjectCardStatus
{
    Planning = 1,
    InProgress = 2,
    Paused = 3,
    Completed = 4,
    Cancelled = 5,
}

/// <summary>A project as a list of the Administration shows it: the code, the name, the status and the client, if any.</summary>
public sealed record ProjectCard(Guid Id, string Code, string Name, ProjectCardStatus Status, string? ClientName)
{
    /// <summary>Completed or cancelled: the project takes no more work (ADR-0016).</summary>
    public bool IsClosed => Status is ProjectCardStatus.Completed or ProjectCardStatus.Cancelled;
}

/// <summary>
/// Every project of the installation with its status and client, for the modules whose screens of the Administration
/// list the projects, such as the hours of the projects (ADR-0128). It says nothing about who is in each project: that
/// is the <see cref="IProjectAccess"/>.
/// </summary>
public interface IProjectCatalog
{
    /// <summary>Every project, by code.</summary>
    Task<IReadOnlyList<ProjectCard>> AllAsync(CancellationToken cancellationToken);

    /// <summary>The project; null when it does not exist.</summary>
    Task<ProjectCard?> FindAsync(Guid projectId, CancellationToken cancellationToken);
}
