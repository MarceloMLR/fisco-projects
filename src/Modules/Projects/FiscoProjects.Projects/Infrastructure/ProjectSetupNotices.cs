using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Contracts;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Projects.Infrastructure;

/// <summary>
/// Tells the other modules that a project was created (ADR-0092), after it is saved. A failure of a module is logged
/// and does not undo the project.
/// </summary>
internal sealed partial class ProjectSetupNotices(
    IEnumerable<IProjectSetup> listeners, ILogger<ProjectSetupNotices> logger) : IProjectSetupNotices
{
    public async Task ProjectCreatedAsync(Guid projectId, CancellationToken cancellationToken)
    {
        foreach (var listener in listeners)
        {
            try
            {
                await listener.ProjectCreatedAsync(projectId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogFailed(logger, listener.GetType().Name, projectId, exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "{Listener} failed to set up the new project {ProjectId}.")]
    private static partial void LogFailed(ILogger logger, string listener, Guid projectId, Exception exception);
}
