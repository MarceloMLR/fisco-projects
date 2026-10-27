using FiscoProjects.Files.Contracts;
using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Projects.Infrastructure;

/// <summary>
/// Renames the folder of a project to its "{code} - {name}" after the project is saved (ADR-0121). A name in use
/// beside it or a failure of the storage is logged, with the id of the project only, and the folder stays as it was,
/// with everything in it; the next change of the data of the project tries again.
/// </summary>
internal sealed partial class ProjectFolderRenaming(
    IProjectFolderStore folders, IFileStorage files, ILogger<ProjectFolderRenaming> logger) : IProjectFolderRenaming
{
    public async Task FollowAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        var name = project.FolderName;
        // A folder someone chose keeps its name (ADR-0122).
        if (project.StorageFolderId is not { } folderId || project.StorageFolderChosen || project.StorageFolderName == name)
        {
            return;
        }

        try
        {
            if (await files.RenameFolderAsync(folderId, name, cancellationToken) is null)
            {
                LogNameInUse(logger, project.Id);
                return;
            }

            await folders.RenamedAsync(project.Id, name, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogFailed(logger, project.Id, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The folder of project {ProjectId} kept its name: another item beside it has the new one.")]
    private static partial void LogNameInUse(ILogger logger, Guid projectId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The folder of project {ProjectId} kept its name: the file storage failed.")]
    private static partial void LogFailed(ILogger logger, Guid projectId, Exception exception);
}
