namespace FiscoProjects.Projects.Domain;

/// <summary>Who asks: the person of the session and whether the person is the Administrator of the system.</summary>
public sealed record ProjectViewer(Guid UserId, bool IsAdministrator);

/// <summary>
/// What a person may do in a project, from the system role and the role in the project (ADR-0043). This is the only
/// place with the rule; the API returns it with the project, so the screens show only what the person may do.
/// </summary>
/// <param name="CanView">Sees the project, its people and everything in it.</param>
/// <param name="CanEditData">Changes the code, the name, the description and the client.</param>
/// <param name="CanChangeStatus">Pauses, completes, cancels and reopens the project.</param>
/// <param name="CanManagePeople">Includes people, changes their roles and removes them (ADR-0042).</param>
/// <param name="CanManageManagers">Does the same with the managers; the tech lead manages the people, but not them.</param>
public sealed record ProjectPermissions(
    bool CanView, bool CanEditData, bool CanChangeStatus, bool CanManagePeople, bool CanManageManagers)
{
    private static readonly ProjectPermissions All = new(true, true, true, true, true);
    private static readonly ProjectPermissions None = new(false, false, false, false, false);

    public static ProjectPermissions Of(ProjectViewer viewer, ProjectRole? role)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (viewer.IsAdministrator)
        {
            return All;
        }

        return role switch
        {
            ProjectRole.Manager => All,
            ProjectRole.TechLead => new(
                CanView: true, CanEditData: true, CanChangeStatus: false, CanManagePeople: true, CanManageManagers: false),
            ProjectRole.Contributor or ProjectRole.Developer or ProjectRole.Viewer => None with { CanView = true },
            _ => None,
        };
    }

    /// <summary>
    /// Whether the person may include (no current role), change or remove (no new role) a person of the project: the
    /// tech lead does it with everyone but the managers, and never makes someone a manager (ADR-0042).
    /// </summary>
    public bool MayManage(ProjectRole? current, ProjectRole? next) =>
        CanManagePeople && (CanManageManagers || (current != ProjectRole.Manager && next != ProjectRole.Manager));
}
