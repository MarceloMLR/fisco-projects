using FiscoProjects.Files.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Application;

/// <summary>What deleting a project would remove (ADR-0041): its code, its name and how many items of each kind it has.</summary>
public sealed record ProjectDeletionSummary(Guid Id, string Code, string Name, IReadOnlyDictionary<ProjectItemKind, int> Counts)
{
    public int CountOf(ProjectItemKind kind) => Counts.TryGetValue(kind, out var count) ? count : 0;
}

public enum ProjectDeletionOutcome
{
    Deleted,
    NotFound,
    Forbidden,

    /// <summary>The code typed is not the code of the project: nothing was removed.</summary>
    CodeMismatch,
}

/// <summary>
/// Removes, in one serializable transaction, the rows of the project in the other modules and in this one, and records
/// the deletion in the audit, once, with the project as it was and how many items of each kind were removed (ADR-0111).
/// </summary>
public interface IProjectDeletionStore
{
    Task DeleteAsync(Project project, CancellationToken cancellationToken);
}

/// <summary>
/// The summary of the deletion of a project, for the Administrator (ADR-0041): what each module has of it, the tasks and
/// milestones and the people. Null for whom does not see the project, and forbidden for the others.
/// </summary>
public sealed class GetProjectDeletion(IProjectStore store, IScheduleStore schedules, IEnumerable<IProjectDeletion> modules)
{
    public async Task<(ProjectDeletionOutcome Outcome, ProjectDeletionSummary? Summary)> ExecuteAsync(
        Guid id, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var project = await store.FindAsync(id, cancellationToken);
        var permissions = project is null ? null : ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        if (project is null || permissions is not { CanView: true })
        {
            return (ProjectDeletionOutcome.NotFound, null);
        }

        if (!permissions.CanDelete)
        {
            return (ProjectDeletionOutcome.Forbidden, null);
        }

        var counts = new Dictionary<ProjectItemKind, int>
        {
            [ProjectItemKind.ScheduleItems] = (await schedules.FindAsync(id, cancellationToken))?.Items.Count ?? 0,
            [ProjectItemKind.Members] = project.Members.Count,
        };
        foreach (var module in modules)
        {
            foreach (var count in await module.CountAsync(id, cancellationToken))
            {
                counts[count.Kind] = counts.GetValueOrDefault(count.Kind) + count.Count;
            }
        }

        return (ProjectDeletionOutcome.Deleted, new ProjectDeletionSummary(project.Id, project.Code, project.Name, counts));
    }
}

/// <summary>
/// Deletes a project for good, with everything in it, by the Administrator, who types its code to confirm (ADR-0041,
/// ADR-0111): first each module removes the files it stored, up to the folder of the project, which goes last when it
/// was left empty, never the folder of the projects (ADR-0121); then the rows of every module go in one transaction,
/// with one audit record. A failure leaves the project, and deleting it again finishes the work.
/// </summary>
public sealed class DeleteProject(
    IProjectStore store, IProjectDeletionStore deletion, IEnumerable<IProjectDeletion> modules, IFileStorage files)
{
    public async Task<ProjectDeletionOutcome> ExecuteAsync(
        Guid id, string? code, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var project = await store.FindAsync(id, cancellationToken);
        var permissions = project is null ? null : ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        if (project is null || permissions is not { CanView: true })
        {
            return ProjectDeletionOutcome.NotFound;
        }

        if (!permissions.CanDelete)
        {
            return ProjectDeletionOutcome.Forbidden;
        }

        if (!string.Equals(code?.Trim(), project.Code, StringComparison.OrdinalIgnoreCase))
        {
            return ProjectDeletionOutcome.CodeMismatch;
        }

        foreach (var module in modules)
        {
            await module.DeleteFilesAsync(id, project.StorageFolderId, cancellationToken);
        }

        if (project.StorageFolderId is { } folder)
        {
            await files.DeleteFolderIfEmptyAsync(folder, cancellationToken);
        }

        await deletion.DeleteAsync(project, cancellationToken);
        return ProjectDeletionOutcome.Deleted;
    }
}
