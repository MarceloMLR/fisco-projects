using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Domain;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Projects.Infrastructure;

/// <summary>
/// Tells the other modules that a person left a project or got another role (ADR-0042), after the change is saved.
/// A failure of a module is logged and does not undo the change of the people.
/// </summary>
internal sealed partial class MemberChangeNotices(
    IEnumerable<IProjectMemberChanges> listeners, ILogger<MemberChangeNotices> logger) : IMemberChangeNotices
{
    public async Task NotifyAsync(
        Guid projectId, Guid userId, ProjectRole? role, Guid actorId, CancellationToken cancellationToken)
    {
        foreach (var listener in listeners)
        {
            try
            {
                await listener.MemberChangedAsync(
                    projectId, userId, role is { } value ? (ProjectMemberRole)value : null, actorId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogFailed(logger, listener.GetType().Name, projectId, exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "{Listener} failed to settle a change of the people of project {ProjectId}.")]
    private static partial void LogFailed(ILogger logger, string listener, Guid projectId, Exception exception);
}
