using FiscoProjects.Calendar.Contracts;
using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Application;

/// <summary>The schedule of a project not completed nor cancelled, with the code and the name of the project.</summary>
public sealed record OpenSchedule(Guid ProjectId, string Code, string Name, Schedule Schedule);

/// <summary>A project whose tasks and milestones change with the calendar as it is now, and how many of them.</summary>
public sealed record RecalculatedSchedule(Guid ProjectId, string Code, string Name, int Items);

/// <summary>
/// What the calendar as it is now changes in the schedules of the open projects, without changing them (ADR-0126): the
/// recalculation run on copies of the schedules, so a schedule in the calendar as it is now does not show.
/// </summary>
public sealed class PreviewScheduleRecalculation(IScheduleStore schedules, IWorkCalendar calendar)
{
    public async Task<IReadOnlyList<RecalculatedSchedule>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var days = await calendar.GetAsync(cancellationToken);
        return [.. (await schedules.OpenSchedulesAsync(cancellationToken))
            .Select(open => new RecalculatedSchedule(open.ProjectId, open.Code, open.Name, open.Schedule.Recalculate(days)))
            .Where(changed => changed.Items > 0)];
    }
}

/// <summary>
/// Recalculates the dates of the schedules of the open projects in the calendar as it is now, asked for by the
/// Administrator (ADR-0126). Each project goes in a serializable transaction of its own, checked again inside it, so a
/// failure in the middle keeps the projects before it, and asking again completes the others. The owners of the tasks
/// whose dates changed hear about it (ADR-0037), and the recalculation stays out of the undo (ADR-0091).
/// </summary>
public sealed class RecalculateSchedules(
    IProjectStore projects, IScheduleStore schedules, IWorkCalendar calendar, ProjectNotices notices)
{
    /// <returns>The projects recalculated, by the code, with how many tasks and milestones changed in each.</returns>
    public async Task<IReadOnlyList<RecalculatedSchedule>> ExecuteAsync(Guid actorId, CancellationToken cancellationToken)
    {
        var days = await calendar.GetAsync(cancellationToken);
        var recalculated = new List<RecalculatedSchedule>();
        foreach (var open in await schedules.OpenSchedulesAsync(cancellationToken))
        {
            if (open.Schedule.Recalculate(days) == 0)
            {
                continue;
            }

            var saved = await projects.InSerializableTransactionAsync(async token =>
            {
                var project = await projects.FindAsync(open.ProjectId, token);
                var schedule = project is null || project.IsClosed ? null : await schedules.FindAsync(open.ProjectId, token);
                if (schedule is null)
                {
                    return null;
                }

                var before = schedule.Snapshot();
                var items = schedule.Recalculate(days);
                if (items == 0)
                {
                    return null;
                }

                await projects.SaveChangesAsync(token);
                return new Saved(project!, before, schedule.Snapshot(), items);
            }, cancellationToken);
            if (saved is null)
            {
                continue;
            }

            await notices.DatesChangedAsync(saved.Project, saved.Before, saved.After, actorId, cancellationToken);
            recalculated.Add(new RecalculatedSchedule(saved.Project.Id, saved.Project.Code, saved.Project.Name, saved.Items));
        }

        return recalculated;
    }

    /// <summary>The schedule of a project before and after the recalculation saved, and how many items changed.</summary>
    private sealed record Saved(Project Project, ScheduleSnapshot Before, ScheduleSnapshot After, int Items);
}
