using FiscoProjects.Calendar.Contracts;
using FiscoProjects.MyWork.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Application;

/// <summary>A task of a person, with the code of its project and the name of its phase, for Meu trabalho.</summary>
public sealed record OwnedTask(Guid ProjectId, string ProjectCode, string Phase, ScheduleItem Item);

/// <summary>A milestone not reached of a project, with the project, as the store reads it.</summary>
public sealed record UpcomingMilestone(Guid ProjectId, string ProjectCode, string ProjectName, Guid Id, string Name, DateOnly Day);

/// <summary>
/// The tasks of the person in Meu trabalho (ADR-0070, ADR-0096): with dates of their own (a task with subtasks counts
/// through them, ADR-0088), below 100%, late or ending in the window, with the business days of delay of the calendar of
/// the installation (ADR-0018).
/// </summary>
public sealed class ScheduleWorkSource(IScheduleStore schedules, IWorkCalendar calendar) : IMyWorkSource
{
    public async Task<IReadOnlyList<MyWorkItem>> ItemsAsync(MyWorkQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var tasks = await schedules.OwnedTasksAsync(query.UserId, query.IsAdministrator, query.Until, cancellationToken);
        if (tasks.Count == 0)
        {
            return [];
        }

        var days = await calendar.GetAsync(cancellationToken);
        return
        [
            .. tasks.Select(task => new MyWorkItem(
                MyWorkKind.Task, task.Item.Id, task.ProjectId, task.ProjectCode, task.Item.Name, MyWorkViews.ForMe)
            {
                DueOn = task.Item.End,
                DaysLate = task.Item.IsLate(query.Today) ? task.Item.DaysLate(query.Today, days) : null,
                Task = new MyWorkTask(task.Phase, task.Item.Progress),
            }),
        ];
    }
}

/// <summary>A milestone ahead in Meu trabalho: the business days left until it and whether its acceptance was asked for.</summary>
public sealed record MilestoneAhead(UpcomingMilestone Milestone, int BusinessDaysLeft, bool AcceptanceRequested);

/// <summary>The milestones ahead and today in the time zone of the installation.</summary>
public sealed record MilestonesAhead(IReadOnlyList<MilestoneAhead> Items, DateOnly Today);

/// <summary>
/// The next milestones of the projects of the person (ADR-0070, ADR-0096): the first 10 not reached from today on, in
/// the open projects the person is in, with the business days left (none on the day itself) and whether a request of
/// acceptance waits for the answer (ADR-0035).
/// </summary>
public sealed class ListUpcomingMilestones(
    IScheduleStore schedules, IWorkCalendar calendar, InstallationClock clock, IEnumerable<IProjectAcceptances> acceptances)
{
    public const int Count = 10;

    public async Task<MilestonesAhead> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var today = await clock.TodayAsync(cancellationToken);
        var milestones = await schedules.UpcomingMilestonesAsync(userId, today, Count, cancellationToken);
        if (milestones.Count == 0)
        {
            return new MilestonesAhead([], today);
        }

        var days = await calendar.GetAsync(cancellationToken);
        var projectIds = milestones.Select(milestone => milestone.ProjectId).Distinct().ToList();
        var requested = new HashSet<Guid>();
        foreach (var module in acceptances)
        {
            requested.UnionWith(await module.RequestedMilestonesAsync(projectIds, cancellationToken));
        }

        return new MilestonesAhead(
            [.. milestones.Select(milestone => new MilestoneAhead(
                milestone, days.CountBusinessDays(today.AddDays(1), milestone.Day), requested.Contains(milestone.Id)))],
            today);
    }
}
