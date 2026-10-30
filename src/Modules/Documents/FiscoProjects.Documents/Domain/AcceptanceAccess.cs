using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.Documents.Domain;

/// <summary>What the person may do with a term, as the drawer of the term shows it.</summary>
public sealed record AcceptanceActions(bool CanAttach, bool CanRemind, bool CanCancel);

/// <summary>
/// The matrix of the acceptance terms (ADR-0129) in one place: only the Administrator and the manager see the terms,
/// ask for signatures, attach signed terms, remind and cancel; the others of the project see none. A completed or
/// cancelled project is read-only (ADR-0016).
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

    /// <summary>
    /// What who manages the terms may do with a term now: a pending request takes the signed term and the cancellation;
    /// the reminder only at the provider that has it (<paramref name="providerReached"/>), never in a request asked of a
    /// person before (ADR-0129).
    /// </summary>
    public static AcceptanceActions ActionsOf(DocumentViewer viewer, ProjectAccess project, AcceptanceTerm term, bool providerReached)
    {
        ArgumentNullException.ThrowIfNull(term);
        var open = Manages(viewer, project) && !project.IsClosed && term.Status == AcceptanceStatus.Pending;
        return new(
            CanAttach: open,
            CanRemind: open && term.IsSignatureRequest && providerReached,
            CanCancel: open);
    }
}
