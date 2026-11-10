using System.Globalization;
using FiscoProjects.Notifications.Contracts;
using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Application;

/// <summary>
/// What the projects tell the notices (ADR-0037, ADR-0038), after the change is saved: the person included in a
/// project or with another role, the people of a project whose status changed, the owners of the tasks whose dates
/// changed, one item per task, and the new owner of a task (ADR-0156).
/// </summary>
public sealed class ProjectNotices(IEnumerable<INoticeBoard> boards)
{
    public Task IncludedAsync(
        Project project, IReadOnlyCollection<(Guid UserId, ProjectRole Role)> people, Guid actorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(people);
        return boards.PostAsync(
            [.. people.Select(person => new Notice(
                NoticeKind.ProjectMemberAdded, [person.UserId], actorId, project.Id, PathOf(project), RoleValues(project, person.Role)))],
            cancellationToken);
    }

    public Task RoleChangedAsync(Project project, Guid userId, ProjectRole role, Guid actorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        return boards.PostAsync(
            [new Notice(NoticeKind.ProjectRoleChanged, [userId], actorId, project.Id, PathOf(project), RoleValues(project, role))],
            cancellationToken);
    }

    public Task StatusChangedAsync(Project project, Guid actorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        return boards.PostAsync(
            [new Notice(
                NoticeKind.ProjectStatusChanged,
                [.. project.Members.Select(member => member.UserId)],
                actorId,
                project.Id,
                PathOf(project),
                new Dictionary<string, string>
                {
                    [NoticeValues.Project] = project.Code,
                    [NoticeValues.Status] = project.Status.ToString(),
                })],
            cancellationToken);
    }

    /// <summary>The owners of the tasks whose start or end changed, also pushed in chain (ADR-0019).</summary>
    public Task DatesChangedAsync(
        Project project, ScheduleSnapshot before, ScheduleSnapshot after, Guid actorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        var old = before.Items.ToDictionary(item => item.Id);
        var notices = after.Items
            .Where(item => item.OwnerId is not null
                && old.TryGetValue(item.Id, out var was)
                && (was.Start != item.Start || was.End != item.End))
            .Select(item => new Notice(
                NoticeKind.ScheduleDatesChanged,
                [item.OwnerId!.Value],
                actorId,
                project.Id,
                $"/projects/{project.Id}/schedule",
                new Dictionary<string, string>
                {
                    [NoticeValues.Project] = project.Code,
                    [NoticeValues.Item] = item.Name,
                    [NoticeValues.Start] = item.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    [NoticeValues.End] = item.End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                },
                $"schedule-item:{item.Id}"))
            .ToList();
        return boards.PostAsync(notices, cancellationToken);
    }

    /// <summary>
    /// The people who became the owners of tasks (ADR-0156): of a task included with an owner, or whose owner changed to
    /// them, also from the client or from nobody. Only the new owner hears about it, and the link opens the task.
    /// </summary>
    public Task AssignedAsync(
        Project project, ScheduleSnapshot before, ScheduleSnapshot after, Guid actorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        var old = before.Items.ToDictionary(item => item.Id);
        var notices = after.Items
            .Where(item => item.Kind == ScheduleItemKind.Task
                && item.OwnerId is not null
                && (!old.TryGetValue(item.Id, out var was) || was.OwnerId != item.OwnerId))
            .Select(item => new Notice(
                NoticeKind.TaskAssigned,
                [item.OwnerId!.Value],
                actorId,
                project.Id,
                $"/projects/{project.Id}/schedule?item={item.Id}",
                new Dictionary<string, string>
                {
                    [NoticeValues.Project] = project.Code,
                    [NoticeValues.Item] = item.Name,
                    [NoticeValues.Title] = item.Name,
                    [NoticeValues.End] = item.End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                }))
            .ToList();
        return boards.PostAsync(notices, cancellationToken);
    }

    private static string PathOf(Project project) => $"/projects/{project.Id}";

    private static Dictionary<string, string> RoleValues(Project project, ProjectRole role) => new()
    {
        [NoticeValues.Project] = project.Code,
        [NoticeValues.Role] = role.ToString(),
    };
}
