namespace FiscoProjects.Projects.Domain;

/// <summary>Who asks: the person of the session and whether the person is the Administrator of the system.</summary>
public sealed record ProjectViewer(Guid UserId, bool IsAdministrator);

/// <summary>
/// What a person may do in a project, from the system role and the role in the project (ADR-0043). This is the only
/// place with the rule; the API returns it with the project, so the screens show only what the person may do.
/// </summary>
public sealed record ProjectPermissions(bool CanView, bool CanEditData, bool CanChangeStatus)
{
    public static ProjectPermissions Of(ProjectViewer viewer, ProjectRole? role)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (viewer.IsAdministrator)
        {
            return new ProjectPermissions(CanView: true, CanEditData: true, CanChangeStatus: true);
        }

        return role switch
        {
            ProjectRole.Manager => new ProjectPermissions(CanView: true, CanEditData: true, CanChangeStatus: true),
            ProjectRole.TechLead => new ProjectPermissions(CanView: true, CanEditData: true, CanChangeStatus: false),
            ProjectRole.Contributor or ProjectRole.Developer or ProjectRole.Viewer =>
                new ProjectPermissions(CanView: true, CanEditData: false, CanChangeStatus: false),
            _ => new ProjectPermissions(CanView: false, CanEditData: false, CanChangeStatus: false),
        };
    }
}
