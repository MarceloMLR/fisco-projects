using FiscoProjects.Projects.Contracts;
using FiscoProjects.TimeTracking.Domain;

namespace FiscoProjects.TimeTracking.Application;

/// <summary>
/// The hours of the tasks of a project (ADR-0138): the planned hours in the tasks, the hours logged in them, the hours
/// logged out of the tasks of the schedule (without a task or in a task no longer in it) and each task with planned or
/// logged hours, in the order of the schedule.
/// </summary>
public sealed record ProjectTaskHours(decimal Planned, decimal Logged, decimal Outside, IReadOnlyList<TaskHours> Items);

/// <summary>The planned hours of a task, or null to take them out.</summary>
public sealed record PlannedHoursRequest(decimal? Hours);

/// <summary>Why the hours of the tasks of a project are not shown, or that they are.</summary>
public enum TaskHoursOutcome
{
    Done = 1,

    /// <summary>The project does not exist or the person does not see it, or the item is not a task of it (404).</summary>
    NotFound = 2,

    /// <summary>The person sees the project, but not its hours, as its tech lead (403).</summary>
    Forbidden = 3,
}

/// <summary>The hours of the tasks of a project, and whether the person may change the planned ones.</summary>
public sealed record TaskHoursView(ProjectTaskHours Hours, bool CanChangePlanned);

/// <summary>
/// The hours of the tasks of a project (ADR-0138, ADR-0144): each task of the schedule, also a summary task or a
/// subtask, with planned or logged hours, with its current name and its balance, and the totals of the project, for the
/// Administrator and the manager of the project, of any status.
/// </summary>
public sealed class GetTaskHours(TimeEntryProjects projects, IScheduleItems schedule, IProjectHoursStore store)
{
    public async Task<(TaskHoursView? View, TaskHoursOutcome Outcome)> ExecuteAsync(
        Guid projectId, TimeEntryViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (await TaskHoursAccess.RefusalAsync(projects, projectId, viewer, cancellationToken) is { } refusal)
        {
            return (null, refusal);
        }

        var tasks = (await schedule.ItemsAsync(projectId, cancellationToken)).Where(item => item.Type == ScheduleItemType.Task);
        var planned = await store.PlannedOfAsync(projectId, cancellationToken);
        var logged = await store.LoggedByTaskAsync(projectId, cancellationToken);
        var inTask = logged
            .Where(row => row.ScheduleItemId is not null)
            .ToDictionary(row => row.ScheduleItemId!.Value, row => row.Hours);
        var items = tasks
            .Where(task => planned.ContainsKey(task.Id) || inTask.ContainsKey(task.Id))
            .Select(task => TaskHours.Of(
                task.Id,
                task.Name,
                planned.TryGetValue(task.Id, out var hours) ? hours : null,
                inTask.GetValueOrDefault(task.Id),
                task.IsDone))
            .ToList();
        var loggedInTasks = items.Sum(item => item.Logged);
        var view = new ProjectTaskHours(
            items.Sum(item => item.Planned ?? 0m), loggedInTasks, logged.Sum(row => row.Hours) - loggedInTasks, items);
        return (new TaskHoursView(view, TimeEntryAccess.ChangesPlannedHours(viewer)), TaskHoursOutcome.Done);
    }
}

/// <summary>
/// The entries of a task of the schedule of a project (ADR-0144), also a summary task or a subtask, of every person,
/// the newest day first, with their total, for who sees the hours of the tasks; each says whether the person may change
/// it, by the rules of an entry (ADR-0128).
/// </summary>
public sealed class ListTaskTimeEntries(TimeEntryProjects projects, IScheduleItems schedule, ITimeEntryStore store, TimeEntryRows rows)
{
    public async Task<(TimeEntryList? List, TaskHoursOutcome Outcome)> ExecuteAsync(
        Guid projectId, Guid itemId, TimeEntryViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (await TaskHoursAccess.RefusalAsync(projects, projectId, viewer, cancellationToken) is { } refusal)
        {
            return (null, refusal);
        }

        if (!(await schedule.ItemsAsync(projectId, cancellationToken)).Any(item => item.Id == itemId && item.Type == ScheduleItemType.Task))
        {
            return (null, TaskHoursOutcome.NotFound);
        }

        var project = await projects.FindAsync(projectId, viewer, cancellationToken);
        var entries = await store.OfTaskAsync(projectId, itemId, cancellationToken);
        return (await rows.ListAsync(entries, new Dictionary<Guid, ProjectAccess> { [projectId] = project! }, viewer, cancellationToken),
            TaskHoursOutcome.Done);
    }
}

/// <summary>Who reaches the hours of the tasks of a project, by the rule of the <see cref="TimeEntryAccess"/>.</summary>
internal static class TaskHoursAccess
{
    /// <summary>Why the person does not reach the hours of the tasks of the project, or null when the person does.</summary>
    public static async Task<TaskHoursOutcome?> RefusalAsync(
        TimeEntryProjects projects, Guid projectId, TimeEntryViewer viewer, CancellationToken cancellationToken)
    {
        if (await projects.FindAsync(projectId, viewer, cancellationToken) is not { } project
            || !TimeEntryAccess.Sees(viewer, project))
        {
            return TaskHoursOutcome.NotFound;
        }

        return TimeEntryAccess.SeesTaskHours(viewer, project) ? null : TaskHoursOutcome.Forbidden;
    }
}

/// <summary>
/// The Administrator puts, changes or takes out the planned hours of a task of the schedule of a project of any status
/// (ADR-0138): a task, also a summary task or a subtask, never a milestone, a phase or a task of another project. The
/// schedule does not change.
/// </summary>
public sealed class ChangePlannedHours(IProjectCatalog catalog, IScheduleItems schedule, IProjectHoursStore store, TimeEntryClock clock)
{
    public async Task<ProjectHoursResult> ExecuteAsync(
        Guid projectId, Guid itemId, PlannedHoursRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await catalog.FindAsync(projectId, cancellationToken) is null
            || !(await schedule.ItemsAsync(projectId, cancellationToken))
                .Any(item => item.Id == itemId && item.Type == ScheduleItemType.Task))
        {
            return new ProjectHoursResult(ProjectHoursOutcome.NotFound);
        }

        if (request.Hours is { } hours && !PlannedHours.IsValid(hours))
        {
            return new ProjectHoursResult(ProjectHoursOutcome.Invalid, null, [AdditionalHoursField.Hours]);
        }

        var current = await store.FindPlannedAsync(projectId, itemId, cancellationToken);
        switch (request.Hours, current)
        {
            case (null, null):
                return new ProjectHoursResult(ProjectHoursOutcome.Done);
            case (null, { } existing):
                store.Remove(existing);
                break;
            case ({ } value, null):
                store.Add(new PlannedHours(projectId, itemId, value, clock.Now));
                break;
            case ({ } value, { } existing):
                if (existing.Hours == value)
                {
                    return new ProjectHoursResult(ProjectHoursOutcome.Done, existing.Id);
                }

                existing.Change(value, clock.Now);
                break;
        }

        await store.SaveChangesAsync(cancellationToken);
        return new ProjectHoursResult(ProjectHoursOutcome.Done);
    }
}
