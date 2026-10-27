using FiscoProjects.Administration.Contracts;
using FiscoProjects.Files.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Application;

/// <summary>Why a folder may not be the folder of a project (ADR-0122).</summary>
public enum ProjectFolderRefusal
{
    /// <summary>Another project uses it.</summary>
    InUse = 1,

    /// <summary>It is inside the folder of a project.</summary>
    InsideProject = 2,

    /// <summary>It is the folder of the projects of the installation (ADR-0121).</summary>
    ProjectsFolder = 3,

    /// <summary>It holds the folder of the projects or the folder of a project.</summary>
    AboveProjects = 4,

    /// <summary>It is the folder of the files of the system, as the logos, or inside it.</summary>
    SystemFolder = 5,

    /// <summary>It does not exist, is a file or is outside the root of the storage.</summary>
    NotFound = 6,
}

/// <summary>A folder of the storage and, when it may not be chosen, why and the code of the project in the way.</summary>
public sealed record ProjectFolderChoice(string Id, string Name, ProjectFolderRefusal? Refusal, string? ProjectCode);

/// <summary>The folders inside a folder of the storage (the root when none), and the same about the folder itself.</summary>
public sealed record ProjectFolderList(ProjectFolderChoice? Current, IReadOnlyList<ProjectFolderChoice> Folders);

/// <summary>
/// The rules of the choice of the folder of a project (ADR-0122), in one place for the list, the creation and the
/// change: any folder inside the root of the storage, but the root, the folder of the projects and the folders above
/// it, the folder of the system, the folder of another project and the folders inside it or above it. The folders of
/// the projects come from the projects; the ones above, from going up from each of them, once for each request.
/// </summary>
public sealed class ProjectFolderChoices(IFileStorage files, IInstallationSettings settings, IProjectFolderStore folders)
{
    /// <summary>The folders inside a folder, in alphabetical order, for the project being edited (none for a new one).</summary>
    /// <returns>Null when the folder does not exist.</returns>
    public async Task<ProjectFolderList?> ListAsync(string? parentId, Guid? projectId, CancellationToken cancellationToken)
    {
        IReadOnlyList<StoredFolder> inside;
        try
        {
            inside = await files.ListFoldersAsync(string.IsNullOrEmpty(parentId) ? null : parentId, cancellationToken);
        }
        catch (Exception exception) when (exception is StorageFolderNotFoundException or ArgumentException)
        {
            return null;
        }

        var rules = await RulesAsync(projectId, cancellationToken);
        if (string.IsNullOrEmpty(parentId))
        {
            return new ProjectFolderList(null, [.. inside.Select(folder => rules.ChoiceOf(folder, []))]);
        }

        var above = await AncestorsAsync(parentId, cancellationToken);
        var current = await NamedAsync(parentId, above, cancellationToken);
        if (current is null)
        {
            return null;
        }

        IReadOnlyList<string> chain = [parentId, .. above];
        return new ProjectFolderList(
            rules.ChoiceOf(current, above), [.. inside.Select(folder => rules.ChoiceOf(folder, chain))]);
    }

    /// <summary>The folder chosen with its name, or why it may not be chosen.</summary>
    public async Task<ProjectFolderChoice> CheckAsync(string folderId, Guid? projectId, CancellationToken cancellationToken)
    {
        try
        {
            await files.ListFoldersAsync(folderId, cancellationToken);
        }
        catch (Exception exception) when (exception is StorageFolderNotFoundException or ArgumentException)
        {
            return new ProjectFolderChoice(folderId, string.Empty, ProjectFolderRefusal.NotFound, null);
        }

        var above = await AncestorsAsync(folderId, cancellationToken);
        var folder = await NamedAsync(folderId, above, cancellationToken);
        return folder is null
            ? new ProjectFolderChoice(folderId, string.Empty, ProjectFolderRefusal.NotFound, null)
            : (await RulesAsync(projectId, cancellationToken)).ChoiceOf(folder, above);
    }

    private async Task<FolderRules> RulesAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        var owners = (await folders.OwnersAsync(cancellationToken))
            .Where(owner => owner.ProjectId != projectId)
            .GroupBy(owner => owner.FolderId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Code, StringComparer.Ordinal);
        var root = await files.ListFoldersAsync(null, cancellationToken);
        var system = root.FirstOrDefault(folder => string.Equals(folder.Name, ReplaceClientLogo.Folder, StringComparison.OrdinalIgnoreCase))?.Id;
        var projectsFolder = await FolderAtAsync((await settings.GetAsync(cancellationToken)).ProjectsFolder, root, cancellationToken);

        var aboveProjects = new HashSet<string>(StringComparer.Ordinal);
        foreach (var start in owners.Keys.Append(projectsFolder).OfType<string>())
        {
            aboveProjects.UnionWith(await AncestorsAsync(start, cancellationToken));
        }

        return new FolderRules(owners, projectsFolder, aboveProjects, system);
    }

    /// <summary>The folder at a path, found without creating anything; null when it does not exist yet.</summary>
    private async Task<string?> FolderAtAsync(string path, IReadOnlyList<StoredFolder> root, CancellationToken cancellationToken)
    {
        if (path.Length == 0)
        {
            return null;
        }

        IReadOnlyList<StoredFolder> level = root;
        string? found = null;
        foreach (var segment in FileNames.Segments(path))
        {
            found = level.FirstOrDefault(folder => string.Equals(folder.Name, segment, StringComparison.OrdinalIgnoreCase))?.Id;
            if (found is null)
            {
                return null;
            }

            level = await files.ListFoldersAsync(found, cancellationToken);
        }

        return found;
    }

    /// <summary>The folders above a folder, from the nearest one up, without the root.</summary>
    private async Task<IReadOnlyList<string>> AncestorsAsync(string folderId, CancellationToken cancellationToken)
    {
        var above = new List<string>();
        for (var current = await files.FolderOfAsync(folderId, cancellationToken);
            current is not null && !above.Contains(current);
            current = await files.FolderOfAsync(current, cancellationToken))
        {
            above.Add(current);
        }

        return above;
    }

    /// <summary>The folder with its name, read in the folder above it.</summary>
    private async Task<StoredFolder?> NamedAsync(string folderId, IReadOnlyList<string> above, CancellationToken cancellationToken) =>
        (await files.ListFoldersAsync(above.Count == 0 ? null : above[0], cancellationToken))
            .FirstOrDefault(folder => folder.Id == folderId);

    private sealed record FolderRules(
        IReadOnlyDictionary<string, string> Owners, string? ProjectsFolder, IReadOnlySet<string> AboveProjects, string? System)
    {
        /// <summary>The choice of a folder whose folders above, from the nearest one, are given.</summary>
        public ProjectFolderChoice ChoiceOf(StoredFolder folder, IReadOnlyList<string> above)
        {
            if (Owners.TryGetValue(folder.Id, out var owner))
            {
                return new ProjectFolderChoice(folder.Id, folder.Name, ProjectFolderRefusal.InUse, owner);
            }

            if (above.FirstOrDefault(Owners.ContainsKey) is { } holder)
            {
                return new ProjectFolderChoice(folder.Id, folder.Name, ProjectFolderRefusal.InsideProject, Owners[holder]);
            }

            ProjectFolderRefusal? refusal =
                folder.Id == ProjectsFolder ? ProjectFolderRefusal.ProjectsFolder
                : AboveProjects.Contains(folder.Id) ? ProjectFolderRefusal.AboveProjects
                : folder.Id == System || (System is not null && above.Contains(System)) ? ProjectFolderRefusal.SystemFolder
                : null;
            return new ProjectFolderChoice(folder.Id, folder.Name, refusal, null);
        }
    }
}

/// <summary>The folder of a project as the screens show it, whether it was chosen, has files and may change (ADR-0122).</summary>
public sealed record ProjectFolderView(string Name, bool Chosen, bool HasFiles, bool CanChange);

/// <summary>The folder of a project, for whoever sees it.</summary>
public sealed class GetProjectFolder(IProjectStore store, IEnumerable<IProjectFolderUse> modules)
{
    public async Task<ProjectFolderView?> ExecuteAsync(Guid projectId, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var project = await store.FindAsync(projectId, cancellationToken);
        var permissions = project is null ? null : ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        if (project is null || permissions is not { CanView: true })
        {
            return null;
        }

        var hasFiles = await ProjectFiles.AnyAsync(modules, projectId, cancellationToken);
        return new ProjectFolderView(
            project.StorageFolderName ?? project.FolderName,
            project.StorageFolderChosen,
            hasFiles,
            permissions.CanChooseFolder && !project.IsClosed && !hasFiles);
    }
}

public enum ProjectFolderOutcome
{
    Saved = 1,
    NotFound = 2,
    Forbidden = 3,
    Closed = 4,

    /// <summary>A module stored a file of the project, so the folder stays (ADR-0122).</summary>
    HasFiles = 5,

    /// <summary>The folder may not be chosen; the refusal says why.</summary>
    Refused = 6,
}

public sealed record ProjectFolderResult(ProjectFolderOutcome Outcome, ProjectFolderRefusal? Refusal = null);

/// <summary>
/// Changes the folder of a project to one that already exists, or back to the one the system creates (null), by the
/// Administrator and the manager of an open project without files (ADR-0122). The project is saved first; then each
/// module takes its empty folders to the new one, and the previous folder, when the system created it and it was left
/// empty, goes.
/// </summary>
public sealed class ChangeProjectFolder(
    IProjectStore store,
    ProjectFolderChoices choices,
    IEnumerable<IProjectFolderUse> modules,
    IProjectFolderCleanup cleanup)
{
    public async Task<ProjectFolderResult> ExecuteAsync(
        Guid projectId, string? folderId, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var project = await store.FindAsync(projectId, cancellationToken);
        var permissions = project is null ? null : ProjectPermissions.Of(viewer, project.RoleOf(viewer.UserId));
        if (project is null || permissions is not { CanView: true })
        {
            return new(ProjectFolderOutcome.NotFound);
        }

        if (!permissions.CanChooseFolder)
        {
            return new(ProjectFolderOutcome.Forbidden);
        }

        if (project.IsClosed)
        {
            return new(ProjectFolderOutcome.Closed);
        }

        var folder = string.IsNullOrWhiteSpace(folderId) ? null : folderId;
        if (folder is null ? !project.StorageFolderChosen : folder == project.StorageFolderId)
        {
            return new(ProjectFolderOutcome.Saved);
        }

        if (await ProjectFiles.AnyAsync(modules, projectId, cancellationToken))
        {
            return new(ProjectFolderOutcome.HasFiles);
        }

        string? name = null;
        if (folder is not null)
        {
            var choice = await choices.CheckAsync(folder, projectId, cancellationToken);
            if (choice.Refusal is { } refusal)
            {
                return new(ProjectFolderOutcome.Refused, refusal);
            }

            name = choice.Name;
        }

        var (previous, previousChosen) = (project.StorageFolderId, project.StorageFolderChosen);
        if (folder is null)
        {
            project.UseSystemFolder();
        }
        else
        {
            project.ChooseFolder(folder, name!);
        }

        await store.SaveChangesAsync(cancellationToken);
        await cleanup.FolderChangedAsync(projectId, previousChosen ? null : previous, previous, cancellationToken);
        return new(ProjectFolderOutcome.Saved);
    }
}

/// <summary>
/// After a change of the folder of a project, already saved (ADR-0122): tells each module, then removes the previous
/// folder when the system created it and it was left empty. A failure is logged; what is left is only empty folders.
/// </summary>
public interface IProjectFolderCleanup
{
    /// <param name="projectId">The project.</param>
    /// <param name="previousSystemFolderId">The previous folder, when the system created it.</param>
    /// <param name="previousFolderId">The previous folder, whoever made it.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    Task FolderChangedAsync(Guid projectId, string? previousSystemFolderId, string? previousFolderId, CancellationToken cancellationToken);
}

/// <summary>Whether any module stored a file of the project (ADR-0122).</summary>
internal static class ProjectFiles
{
    public static async Task<bool> AnyAsync(IEnumerable<IProjectFolderUse> modules, Guid projectId, CancellationToken cancellationToken)
    {
        foreach (var module in modules)
        {
            if (await module.HasFilesAsync(projectId, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }
}
