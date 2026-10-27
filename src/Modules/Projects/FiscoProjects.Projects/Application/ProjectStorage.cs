using FiscoProjects.Administration.Contracts;
using FiscoProjects.Files.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Application;

/// <summary>What the folder of a project needs from the project: the code, the name and the folder kept.</summary>
public sealed record ProjectFolderInfo(string Code, string Name, string? FolderId, string? FolderName);

/// <summary>A project that has its folder in the storage, by the code, for the rules of the choice of a folder (ADR-0122).</summary>
public sealed record ProjectFolderOwner(Guid ProjectId, string Code, string FolderId);

/// <summary>
/// Where the folder of each project is kept, outside the audit and without the tracking of the project, so a file of
/// another module never saves the project with it (ADR-0121).
/// </summary>
public interface IProjectFolderStore
{
    Task<ProjectFolderInfo?> FindAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>Keeps the folder only when the project has none, and gives back the one the project has after that.</summary>
    Task<string?> KeepAsync(Guid projectId, string folderId, string folderName, CancellationToken cancellationToken);

    /// <summary>Forgets the folder, when it is still the one given, so the next file creates it again.</summary>
    Task ForgetAsync(Guid projectId, string folderId, CancellationToken cancellationToken);

    /// <summary>The name the folder of the project got.</summary>
    Task RenamedAsync(Guid projectId, string folderName, CancellationToken cancellationToken);

    /// <summary>The projects that have a folder, with it.</summary>
    Task<IReadOnlyList<ProjectFolderOwner>> OwnersAsync(CancellationToken cancellationToken);
}

/// <summary>
/// The folder of each project in the file storage (ADR-0030, ADR-0121): created with the first file, as
/// "{code} - {name}" inside the folder of the projects of the installation, or the folder of that name already at the
/// root, from before the project kept its folder. A folder removed outside the system is created again.
/// </summary>
public sealed class ProjectStorage(IProjectFolderStore folders, IFileStorage files, IInstallationSettings settings) : IProjectStorage
{
    public async Task<string> EnsureFolderAsync(Guid projectId, string path, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            var folder = await ProjectFolderAsync(projectId, cancellationToken);
            try
            {
                return (await files.EnsureFolderAsync(folder, path, cancellationToken)).Id;
            }
            catch (StorageFolderNotFoundException) when (attempt == 0)
            {
                // Removed straight in the storage: the project gets a new folder, once.
                await folders.ForgetAsync(projectId, folder, cancellationToken);
            }
        }
    }

    private async Task<string> ProjectFolderAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await folders.FindAsync(projectId, cancellationToken)
            ?? throw new KeyNotFoundException($"The project {projectId} does not exist.");
        if (project.FolderId is { } known)
        {
            return known;
        }

        var name = Project.FolderNameOf(project.Code, project.Name);
        var area = (await settings.GetAsync(cancellationToken)).ProjectsFolder;
        var folder = await FolderFromBeforeAsync(name, area, cancellationToken)
            ?? (await files.EnsureFolderAsync(null, area.Length == 0 ? name : $"{area}/{name}", cancellationToken)).Id;

        // Two first files at the same time find the same folder, and the first one kept stays.
        return await folders.KeepAsync(projectId, folder, name, cancellationToken) ?? folder;
    }

    /// <summary>
    /// The folder "{code} - {name}" at the root, where the modules built the path before the project kept its folder;
    /// with the folder of the projects at the root, it is the same path, found by the storage itself.
    /// </summary>
    private async Task<string?> FolderFromBeforeAsync(string name, string area, CancellationToken cancellationToken)
    {
        if (area.Length == 0)
        {
            return null;
        }

        var stored = FileNames.Normalize(name);
        return (await files.ListFoldersAsync(null, cancellationToken))
            .FirstOrDefault(folder => string.Equals(folder.Name, stored, StringComparison.OrdinalIgnoreCase))?.Id;
    }
}

/// <summary>
/// Renames the folder of a project to its "{code} - {name}" after the project is saved (ADR-0121). A name in use
/// beside it or a failure of the storage is logged and leaves the folder as it was; the next change tries again.
/// </summary>
public interface IProjectFolderRenaming
{
    Task FollowAsync(Project project, CancellationToken cancellationToken);
}
