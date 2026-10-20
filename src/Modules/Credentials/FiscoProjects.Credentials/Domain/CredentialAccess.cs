using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.Credentials.Domain;

/// <summary>Who asks: the person of the session and whether the person is the Administrator of the system.</summary>
public sealed record CredentialViewer(Guid UserId, bool IsAdministrator);

/// <summary>
/// What a person may do with the credentials of a project besides seeing them and their passwords, which everyone in
/// it does (ADR-0045): include and change, and remove and import. A completed or cancelled project is read-only for
/// everyone (ADR-0016), which the actions tell apart from these.
/// </summary>
public sealed record CredentialPermissions(bool CanEdit, bool CanDelete);

/// <summary>The matrix of the credentials (ADR-0045) in one place.</summary>
public static class CredentialAccess
{
    /// <summary>
    /// The permissions of a person in a project the person sees: the Administrator, the manager and the tech lead do
    /// everything; the contributor and the developer include and change; the viewer only sees.
    /// </summary>
    public static CredentialPermissions Of(CredentialViewer viewer, ProjectAccess project)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(project);
        if (viewer.IsAdministrator)
        {
            return new(CanEdit: true, CanDelete: true);
        }

        return project.Role switch
        {
            ProjectMemberRole.Manager or ProjectMemberRole.TechLead => new(CanEdit: true, CanDelete: true),
            ProjectMemberRole.Contributor or ProjectMemberRole.Developer => new(CanEdit: true, CanDelete: false),
            _ => new(CanEdit: false, CanDelete: false),
        };
    }
}
