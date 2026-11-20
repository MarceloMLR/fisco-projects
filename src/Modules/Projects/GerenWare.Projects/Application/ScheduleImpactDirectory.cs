using GerenWare.Calendar.Contracts;
using GerenWare.Projects.Contracts;
using GerenWare.Projects.Domain;

namespace GerenWare.Projects.Application;

/// <summary>
/// What the items linked to a ticket reach in the schedule of its project (ADR-0164), by the rule of
/// <see cref="ScheduleImpacts"/>, on the current dates and the calendar of the installation (ADR-0051).
/// </summary>
public sealed class ScheduleImpactDirectory(IScheduleStore schedules, IWorkCalendar calendar, InstallationClock clock) : IScheduleImpacts
{
    public async Task<ScheduleImpact> ImpactOfAsync(
        Guid projectId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(itemIds);
        if (itemIds.Count == 0 || await schedules.FindAsync(projectId, cancellationToken) is not { } schedule)
        {
            return ScheduleImpact.None;
        }

        var days = await calendar.GetAsync(cancellationToken);
        var today = await clock.TodayAsync(cancellationToken);
        var reach = ScheduleImpacts.Of(schedule, itemIds, ScheduleCriticalPath.Of(schedule.Items, schedule.Dependencies, days));
        if (reach.Items.Count == 0)
        {
            return ScheduleImpact.None;
        }

        var lateDays = ScheduleFigures.LateDaysOf(schedule.Items, today, days);
        int LateOf(ScheduleItem item) => lateDays.TryGetValue(item.Id, out var late) ? late : 0;
        return new ScheduleImpact(
            [.. reach.Items.Select(item => new ImpactedScheduleItem(
                item.Id,
                item.Kind == ScheduleItemKind.Milestone ? ScheduleItemType.Milestone : ScheduleItemType.Task,
                item.Name,
                item.Start,
                item.End,
                item.Progress,
                item.IsDone,
                LateOf(item)))],
            [.. reach.Milestones.Select(milestone => new ThreatenedMilestone(milestone.Id, milestone.Name, milestone.Start, LateOf(milestone)))],
            reach.OnCriticalPath);
    }
}
