namespace FiscoProjects.Projects.Contracts;

/// <summary>The role of a person in a project (ADR-0040), as the other modules see it.</summary>
public enum ProjectMemberRole
{
    Manager = 1,
    TechLead = 2,
    Contributor = 3,
    Developer = 4,
    Viewer = 5,
}

/// <summary>
/// A project as a person sees it: its code and name, whether it is completed or cancelled (read-only, ADR-0016) and the
/// role of the person in it; the role is null for the Administrator, who sees every project without being in it.
/// </summary>
public sealed record ProjectAccess(Guid Id, string Code, string Name, bool IsClosed, ProjectMemberRole? Role);

/// <summary>A person of a project and the role in it.</summary>
public sealed record ProjectMemberInfo(Guid ProjectId, Guid UserId, ProjectMemberRole Role);

/// <summary>
/// Who is in each project, for the modules whose items belong to a project, such as the tickets (ADR-0022, ADR-0044):
/// everyone in a project sees it, and the Administrator sees every project.
/// </summary>
public interface IProjectAccess
{
    /// <summary>The projects the person sees, by code: every project for the Administrator.</summary>
    Task<IReadOnlyList<ProjectAccess>> VisibleAsync(Guid userId, bool isAdministrator, CancellationToken cancellationToken);

    /// <summary>The project as the person sees it; null when it does not exist or the person does not see it.</summary>
    Task<ProjectAccess?> FindAsync(Guid projectId, Guid userId, bool isAdministrator, CancellationToken cancellationToken);

    /// <summary>The people of the projects with their roles; none for a project that does not exist.</summary>
    Task<IReadOnlyList<ProjectMemberInfo>> MembersAsync(IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken);
}
