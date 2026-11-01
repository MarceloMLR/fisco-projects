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

/// <summary>
/// The hours of the tasks of a project for the Administrator (ADR-0138): each task of the schedule, also a summary task
/// or a subtask, with planned or logged hours, with its current name and its balance, and the totals of the project.
/// Only the Administrator reaches it, by the policy of the routes. Null when the project does not exist.
/// </summary>
public sealed class GetTaskHours(IProjectCatalog catalog, IScheduleItems schedule, IProjectHoursStore store)
{
    public async Task<ProjectTaskHours?> ExecuteAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (await catalog.FindAsync(projectId, cancellationToken) is null)
        {
            return null;
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
        return new ProjectTaskHours(
            items.Sum(item => item.Planned ?? 0m), loggedInTasks, logged.Sum(row => row.Hours) - loggedInTasks, items);
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
