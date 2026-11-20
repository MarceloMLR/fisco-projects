using GerenWare.Calendar.Contracts;

namespace GerenWare.Projects.Domain;

/// <summary>A milestone not reached that the projection of a late task moves: its day now, the projected one and the business days it moves.</summary>
public sealed record ProjectedMilestone(Guid Id, string Name, DateOnly Day, DateOnly ProjectedDay, int Days);

/// <summary>The end of the project now, the projected one and the business days it moves.</summary>
public sealed record ProjectEndProjection(DateOnly End, DateOnly ProjectedEnd, int Days);

/// <summary>
/// What a late task would do to the schedule if what is left of it took the planned pace from today (ADR-0164): its
/// projected start and end, the business days its end goes past the planned one, the milestones not reached that would
/// move, by their day, and the end of the project, when it moves. Nothing is saved.
/// </summary>
public sealed record ScheduleProjection(
    Guid ItemId,
    DateOnly Start,
    DateOnly End,
    int Days,
    IReadOnlyList<ProjectedMilestone> Milestones,
    ProjectEndProjection? ProjectEnd);

/// <summary>
/// The rule of the projection of the delay (ADR-0164), in one place, on the dates the schedule has now (ADR-0051): a
/// late task, open and with dates of its own, that should have ended (ADR-0020) or should have started and is at 0%,
/// takes what is left of it (the duration times the part left by the progress, in business days, rounded up, at least
/// 1) from today, or from the next business day when today is not one; at 0%, it starts that day. The items after it
/// move by the rules of the dependencies (ADR-0137) only when they no longer fit, as a change moves them, and a done
/// item stays. Each projection is made on copies of the items, from the dates of now, and changes nothing.
/// </summary>
public static class ScheduleProjections
{
    /// <summary>Whether a task is late for the projection: open, with dates of its own, past its end, or past its start at 0%.</summary>
    public static bool IsBehind(Schedule schedule, ScheduleItem item, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(item);
        return IsBehind(item, schedule.IsSummary(item.Id), today);
    }

    /// <summary>The projection of a task, or null when it is not late for the projection (<see cref="IsBehind(Schedule, ScheduleItem, DateOnly)"/>).</summary>
    public static ScheduleProjection? Of(Schedule schedule, Guid itemId, DateOnly today, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(days);
        var graph = new Graph(schedule);
        return graph.Items.TryGetValue(itemId, out var item) && IsBehind(item, graph.Summaries.Contains(item.Id), today)
            ? graph.Project(item, today, days)
            : null;
    }

    /// <summary>The projection of each task late for the projection, by its id; empty when none is.</summary>
    public static IReadOnlyDictionary<Guid, ScheduleProjection> AllOf(Schedule schedule, DateOnly today, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(days);
        var graph = new Graph(schedule);
        return graph.Items.Values
            .Where(item => IsBehind(item, graph.Summaries.Contains(item.Id), today))
            .ToDictionary(item => item.Id, item => graph.Project(item, today, days));
    }

    private static bool IsBehind(ScheduleItem item, bool isSummary, DateOnly today) =>
        item.Kind == ScheduleItemKind.Task && !isSummary && !item.IsDone
        && (item.End < today || (item.Start < today && item.Progress == 0));

    private static DateOnly OnABusinessDay(DateOnly day, IBusinessDays days) =>
        days.IsBusinessDay(day) ? day : days.AddBusinessDays(day, 1);

    /// <summary>The items and dependencies of a schedule, read once for every projection of it.</summary>
    private sealed class Graph
    {
        private readonly ILookup<Guid, ScheduleDependency> _into;
        private readonly ILookup<Guid, Guid> _successors;
        private readonly DateOnly _end;

        public Graph(Schedule schedule)
        {
            Items = schedule.Items.ToDictionary(item => item.Id);
            Summaries = schedule.Items.Where(item => item.ParentId is not null).Select(item => item.ParentId!.Value).ToHashSet();
            _into = schedule.Dependencies.ToLookup(dependency => dependency.SuccessorId);
            _successors = schedule.Dependencies.ToLookup(dependency => dependency.PredecessorId, dependency => dependency.SuccessorId);
            _end = Items.Count == 0 ? DateOnly.MinValue : Items.Values.Max(item => item.End);
        }

        public Dictionary<Guid, ScheduleItem> Items { get; }

        public HashSet<Guid> Summaries { get; }

        public ScheduleProjection Project(ScheduleItem task, DateOnly today, IBusinessDays days)
        {
            var from = OnABusinessDay(today, days);
            var left = Math.Max(1, ((task.Duration * (100 - task.Progress)) + 99) / 100);
            var start = task.Progress == 0 ? from : task.Start;
            var end = days.AddBusinessDays(from, left - 1);
            var moved = new Dictionary<Guid, ScheduleItem>
            {
                [task.Id] = new(task.ProjectId, task.State() with
                {
                    Start = start,
                    End = end,
                    Duration = Math.Max(1, days.CountBusinessDays(start, end)),
                }),
            };

            foreach (var item in After(task.Id))
            {
                if (item.IsDone)
                {
                    continue;
                }

                DateOnly? earliest = null;
                foreach (var dependency in _into[item.Id])
                {
                    var day = dependency.EarliestStart(Current(dependency.PredecessorId, moved), item, days);
                    if (earliest is null || day > earliest)
                    {
                        earliest = day;
                    }
                }

                if (earliest is { } first && first > item.Start)
                {
                    var copy = new ScheduleItem(item.ProjectId, item.State());
                    copy.Place(OnABusinessDay(first, days), days);
                    moved[item.Id] = copy;
                }
            }

            var milestones = moved.Values
                .Where(copy => copy.Kind == ScheduleItemKind.Milestone)
                .Select(copy => (Before: Items[copy.Id], After: copy))
                .Select(pair => new ProjectedMilestone(
                    pair.Before.Id,
                    pair.Before.Name,
                    pair.Before.Start,
                    pair.After.Start,
                    days.CountBusinessDays(pair.Before.Start.AddDays(1), pair.After.Start)))
                .OrderBy(milestone => milestone.Day)
                .ThenBy(milestone => milestone.Name, StringComparer.CurrentCulture)
                .ToList();
            var projectedEnd = Items.Keys.Max(id => Current(id, moved).End);
            return new ScheduleProjection(
                task.Id,
                start,
                end,
                days.CountBusinessDays(task.End.AddDays(1), end),
                milestones,
                projectedEnd > _end ? new ProjectEndProjection(_end, projectedEnd, days.CountBusinessDays(_end.AddDays(1), projectedEnd)) : null);
        }

        private ScheduleItem Current(Guid id, Dictionary<Guid, ScheduleItem> moved) =>
            moved.TryGetValue(id, out var copy) ? copy : Items[id];

        /// <summary>
        /// Every item that depends, directly or through others, on the task, each one after all the items before it among
        /// them, so each is looked at once (as the push of a change).
        /// </summary>
        private List<ScheduleItem> After(Guid taskId)
        {
            var after = new HashSet<Guid>();
            var pending = new Queue<Guid>([taskId]);
            while (pending.TryDequeue(out var current))
            {
                foreach (var successor in _successors[current])
                {
                    if (successor != taskId && after.Add(successor))
                    {
                        pending.Enqueue(successor);
                    }
                }
            }

            var waiting = after.ToDictionary(
                id => id,
                id => _into[id].Count(dependency => after.Contains(dependency.PredecessorId)));
            var ready = new Queue<Guid>(after.Where(id => waiting[id] == 0).OrderBy(id => Items[id].Start));
            var ordered = new List<ScheduleItem>();
            while (ready.TryDequeue(out var id))
            {
                ordered.Add(Items[id]);
                foreach (var successor in _successors[id].Where(after.Contains))
                {
                    if (--waiting[successor] == 0)
                    {
                        ready.Enqueue(successor);
                    }
                }
            }

            return ordered;
        }
    }
}
