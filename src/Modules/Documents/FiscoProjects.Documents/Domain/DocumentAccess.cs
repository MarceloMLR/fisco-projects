using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.Documents.Domain;

/// <summary>Who asks: the person of the session and whether the person is the Administrator of the system.</summary>
public sealed record DocumentViewer(Guid UserId, bool IsAdministrator);

/// <summary>
/// What a person may do with the documents of a project, by the role in it (ADR-0045): send (also new versions), link
/// and set the common statuses; approve; and delete. Which folders the person sees is <see cref="DocumentAccess.SeesFolder"/>
/// (ADR-0148). A completed or cancelled project is read-only for everyone (ADR-0016), which the actions tell apart from these.
/// </summary>
public sealed record DocumentPermissions(bool CanUpload, bool CanApprove, bool CanDelete)
{
    /// <summary>Whether the person may set the status: approve, or the other statuses for who sends documents.</summary>
    public bool MaySet(DocumentStatus status) => status == DocumentStatus.Approved ? CanApprove : CanUpload;
}

/// <summary>The matrix of the documents (ADR-0045, ADR-0093) in one place.</summary>
public static class DocumentAccess
{
    /// <summary>
    /// The permissions of a person in a project the person sees: the Administrator, the manager and the tech lead do
    /// everything; the contributor and the developer send, link and set the common statuses; the viewer only sees.
    /// </summary>
    public static DocumentPermissions Of(DocumentViewer viewer, ProjectAccess project)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(project);
        if (viewer.IsAdministrator || project.Role == ProjectMemberRole.Manager)
        {
            return new(CanUpload: true, CanApprove: true, CanDelete: true);
        }

        return project.Role switch
        {
            ProjectMemberRole.TechLead => new(CanUpload: true, CanApprove: true, CanDelete: true),
            ProjectMemberRole.Contributor or ProjectMemberRole.Developer =>
                new(CanUpload: true, CanApprove: false, CanDelete: false),
            _ => new(CanUpload: false, CanApprove: false, CanDelete: false),
        };
    }

    /// <summary>
    /// Whether the person sees a folder of a project the person sees, and its documents (ADR-0148): the Administrator and
    /// the manager see every folder; the folder of the terms is only theirs (ADR-0129); the others see the folders whose
    /// roles include theirs.
    /// </summary>
    public static bool SeesFolder(DocumentViewer viewer, ProjectAccess project, DocumentFolder folder)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(folder);
        if (viewer.IsAdministrator || project.Role == ProjectMemberRole.Manager)
        {
            return true;
        }

        return !folder.IsAcceptance && FolderVisibility.Sees(folder.VisibleTo, project.Role);
    }

    /// <summary>Whether the person changes who sees the folders of a project (ADR-0148): only the Administrator.</summary>
    public static bool MayChangeFolderVisibility(DocumentViewer viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        return viewer.IsAdministrator;
    }

    /// <summary>
    /// Whether the person includes, changes and removes the links of a project the person sees (ADR-0045): everyone but
    /// the viewer, who only sees them.
    /// </summary>
    public static bool MayEditLinks(DocumentViewer viewer, ProjectAccess project)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(project);
        return viewer.IsAdministrator || project.Role is not (null or ProjectMemberRole.Viewer);
    }
}
