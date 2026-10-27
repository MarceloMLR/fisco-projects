using FiscoProjects.Files.Contracts;
using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Contracts;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Projects.Infrastructure;

/// <summary>
/// After a change of the folder of a project, already saved (ADR-0122): each module takes its empty folders to the new
/// folder, then the previous folder, when the system created it and it was left empty, goes. A failure is logged with
/// the id of the project only; what stays behind is empty folders, and the modules make their folders again on use.
/// </summary>
internal sealed partial class ProjectFolderCleanup(
    IEnumerable<IProjectFolderUse> modules, IFileStorage files, ILogger<ProjectFolderCleanup> logger) : IProjectFolderCleanup
{
    public async Task FolderChangedAsync(
        Guid projectId, string? previousSystemFolderId, string? previousFolderId, CancellationToken cancellationToken)
    {
        foreach (var module in modules)
        {
            try
            {
                await module.FolderChangedAsync(projectId, previousFolderId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogModuleFailed(logger, module.GetType().Name, projectId, exception);
            }
        }

        if (previousSystemFolderId is null)
        {
            return;
        }

        try
        {
            await files.DeleteFolderIfEmptyAsync(previousSystemFolderId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogFolderFailed(logger, projectId, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Module} failed to take its folders to the new folder of project {ProjectId}.")]
    private static partial void LogModuleFailed(ILogger logger, string module, Guid projectId, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The previous folder of project {ProjectId} was not removed.")]
    private static partial void LogFolderFailed(ILogger logger, Guid projectId, Exception exception);
}
