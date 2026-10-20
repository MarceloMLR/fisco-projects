using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.Documents.Domain;

/// <summary>What a person may do with a term, as the drawer of the term shows it.</summary>
public sealed record AcceptanceActions(bool CanAnswer, bool CanAttach, bool CanRemind, bool CanCancel);

/// <summary>
/// The matrix of the acceptance terms (ADR-0045, ADR-0095) in one place: the Administrator and the manager see every
/// term of the project, ask for terms, attach signed terms, remind and cancel; the person of a term sees it and
/// answers it while it is pending, in any role; the others see none. A completed or cancelled project is read-only
/// (ADR-0016).
/// </summary>
public static class AcceptanceAccess
{
    /// <summary>Whether the person manages the terms of the project: the Administrator and the manager.</summary>
    public static bool Manages(DocumentViewer viewer, ProjectAccess project)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(project);
        return viewer.IsAdministrator || project.Role == ProjectMemberRole.Manager;
    }

    /// <summary>Whether the person sees the term: who manages the terms, and the person of the term.</summary>
    public static bool Sees(DocumentViewer viewer, ProjectAccess project, AcceptanceTerm term)
    {
        ArgumentNullException.ThrowIfNull(term);
        return Manages(viewer, project) || term.RecipientId == viewer.UserId;
    }

    /// <summary>What the person may do with a term the person sees, now.</summary>
    public static AcceptanceActions ActionsOf(DocumentViewer viewer, ProjectAccess project, AcceptanceTerm term)
    {
        ArgumentNullException.ThrowIfNull(term);
        var open = !project.IsClosed && term.Status == AcceptanceStatus.Pending;
        var manages = Manages(viewer, project) && open;
        return new(
            CanAnswer: open && term.RecipientId == viewer.UserId,
            CanAttach: manages && term.RecipientId != viewer.UserId,
            CanRemind: manages,
            CanCancel: manages);
    }
}
