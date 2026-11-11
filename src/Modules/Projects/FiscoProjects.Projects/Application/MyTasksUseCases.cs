using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Application;

/// <summary>A project with tasks of the person: the id, the code and the name.</summary>
public sealed record TaskProject(Guid Id, string Code, string Name);

/// <summary>A task of the person in Minhas tarefas (ADR-0158): the item, its phase and, for a subtask, the task it is in.</summary>
public sealed record MyTask(ScheduleItem Item, string Phase, string? Parent);

/// <summary>A project with the tasks of the person, and the view of its schedule the tasks are read in.</summary>
public sealed record MyTasksProject(TaskProject Project, ScheduleView View, IReadOnlyList<MyTask> Tasks);

/// <summary>The tasks of the person by project, by the code, and today in the time zone of the installation.</summary>
public sealed record MyTasks(DateOnly Today, IReadOnlyList<MyTasksProject> Projects);

/// <summary>
/// Minhas tarefas (ADR-0158): the tasks with dates of their own the person is in charge of, in the projects not completed
/// nor cancelled the person is in, each read in the schedule of its project as the Schedule tab shows it, so the delay,
/// the deadline, the float and the comments are the same. The ones below 100% by the end, from the earliest, and, when
/// asked, the ones completed in the last 30 days, from the latest. Nothing changes in who sees what (ADR-0043).
/// </summary>
public sealed class ListMyTasks(IScheduleStore schedules, GetSchedule getSchedule, InstallationClock clock)
{
    /// <summary>The calendar days back the completed tasks are shown.</summary>
    public const int CompletedDays = 30;

    public async Task<MyTasks> ExecuteAsync(ProjectViewer viewer, bool withDone, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var today = await clock.TodayAsync(cancellationToken);
        var completedFrom = today.AddDays(-CompletedDays);
        var projects = await schedules.ProjectsWithTasksOfAsync(
            viewer.UserId, viewer.IsAdministrator, withDone ? completedFrom : null, cancellationToken);
        var result = new List<MyTasksProject>();
        foreach (var project in projects)
        {
            if (await getSchedule.ExecuteAsync(project.Id, viewer, cancellationToken) is not { } view)
            {
                continue;
            }

            var items = view.Figures.Phases.SelectMany(phase => phase.Items).ToDictionary(item => item.Id);
            var mine = view.Figures.Phases
                .SelectMany(phase => phase.Items.Select(item => (Phase: phase.Phase.Name, Item: item)))
                .Where(entry => entry.Item.Kind == ScheduleItemKind.Task
                    && entry.Item.OwnerId == viewer.UserId
                    && !view.Figures.Summaries.Contains(entry.Item.Id))
                .ToList();
            var open = mine.Where(entry => entry.Item.Progress < 100).OrderBy(entry => entry.Item.End);
            var done = withDone
                ? mine.Where(entry => entry.Item.Progress >= 100 && entry.Item.CompletedOn >= completedFrom)
                    .OrderByDescending(entry => entry.Item.CompletedOn)
                : Enumerable.Empty<(string Phase, ScheduleItem Item)>();
            var tasks = open.Concat(done)
                .Select(entry => new MyTask(
                    entry.Item,
                    entry.Phase,
                    entry.Item.ParentId is { } parent && items.TryGetValue(parent, out var task) ? task.Name : null))
                .ToList();
            if (tasks.Count > 0)
            {
                result.Add(new MyTasksProject(project, view, tasks));
            }
        }

        return new MyTasks(today, result);
    }
}
