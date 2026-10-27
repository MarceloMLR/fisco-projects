namespace FiscoProjects.Projects.Contracts;

/// <summary>
/// What a module keeps in the folder of a project (ADR-0122): the folder of a project changes only while no module has
/// stored a file of it, and, after a change, each module takes the empty folders it made to the new one. Implemented
/// by the modules that store files of a project, as the documents and the tickets.
/// </summary>
public interface IProjectFolderUse
{
    /// <summary>Whether the module stored any file of the project.</summary>
    /// <param name="projectId">The project.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    Task<bool> HasFilesAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>
    /// The folder of the project changed while it had no files, already saved: the module forgets the folders it made,
    /// removes the empty ones from the previous folder, up to it, and may make them in the new one.
    /// </summary>
    /// <param name="projectId">The project.</param>
    /// <param name="previousFolderId">The folder the project had; null when it had none.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    Task FolderChangedAsync(Guid projectId, string? previousFolderId, CancellationToken cancellationToken);
}
