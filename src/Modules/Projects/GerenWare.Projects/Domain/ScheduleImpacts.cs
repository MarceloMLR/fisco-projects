namespace GerenWare.Projects.Domain;

/// <summary>
/// What the items linked to a ticket reach in the schedule (ADR-0164): the linked tasks and milestones that still exist,
/// in the order of the schedule; the milestones not reached that come after them through the dependencies, by their
/// day and, on the same day, the nearest first; and whether one of them is on the critical path, when a delay in it moves the end of the project.
/// </summary>
public sealed record ScheduleReach(IReadOnlyList<ScheduleItem> Items, IReadOnlyList<ScheduleItem> Milestones, bool OnCriticalPath)
{
    public static ScheduleReach None { get; } = new([], [], false);
}

/// <summary>
/// The rule of what depends on the items linked to a ticket (ADR-0164), in one place, on the dates the schedule has now
/// (ADR-0051): a task with subtasks counts by its subtasks, the only items with dependencies (ADR-0088); from them, the
/// dependencies of any kind (ADR-0137) lead to the items that come after, and the milestones among them not reached yet
/// are the ones threatened. Nothing here changes a date.
/// </summary>
public static class ScheduleImpacts
{
    /// <summary>What the linked items reach; the ids that are not a task or milestone of the schedule are left out.</summary>
    /// <param name="schedule">The schedule of the project of the ticket.</param>
    /// <param name="itemIds">The tasks and milestones linked to the ticket.</param>
    /// <param name="critical">The items on the critical path (<see cref="ScheduleCriticalPath"/>).</param>
    public static ScheduleReach Of(Schedule schedule, IEnumerable<Guid> itemIds, IReadOnlySet<Guid> critical)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(itemIds);
        ArgumentNullException.ThrowIfNull(critical);
        var wanted = itemIds.ToHashSet();
        var items = InOrder(schedule, schedule.Items.Where(item => wanted.Contains(item.Id)));
        if (items.Count == 0)
        {
            return ScheduleReach.None;
        }

        var starts = items.SelectMany(item => OwnDated(schedule, item)).ToHashSet();
        var after = After(schedule, starts);
        var milestones = schedule.Items
            .Where(item => item.Kind == ScheduleItemKind.Milestone && item.ReachedOn is null
                && after.ContainsKey(item.Id) && !wanted.Contains(item.Id))
            .OrderBy(item => item.Start)
            .ThenBy(item => after[item.Id])
            .ThenBy(item => item.Name, StringComparer.CurrentCulture)
            .ToList();
        return new ScheduleReach(items, milestones, starts.Any(critical.Contains));
    }

    /// <summary>The milestones not reached that come after one item through the dependencies, for the threats of each milestone.</summary>
    public static IReadOnlySet<Guid> MilestonesAfter(Schedule schedule, Guid itemId)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        if (schedule.ItemOf(itemId) is not { } item)
        {
            return new HashSet<Guid>();
        }

        var after = After(schedule, [.. OwnDated(schedule, item)]);
        return schedule.Items
            .Where(other => other.Kind == ScheduleItemKind.Milestone && other.ReachedOn is null
                && other.Id != itemId && after.ContainsKey(other.Id))
            .Select(other => other.Id)
            .ToHashSet();
    }

    /// <summary>The item itself, or the subtasks of a task with subtasks, which hold its dates and dependencies.</summary>
    private static IEnumerable<Guid> OwnDated(Schedule schedule, ScheduleItem item)
    {
        var subtasks = schedule.SubtasksOf(item.Id);
        return subtasks.Count == 0 ? [item.Id] : subtasks.Select(subtask => subtask.Id);
    }

    /// <summary>
    /// Every item that comes after the ones given through the dependencies, without them, with the fewest dependencies
    /// that lead to it.
    /// </summary>
    private static Dictionary<Guid, int> After(Schedule schedule, IReadOnlyCollection<Guid> starts)
    {
        var successors = schedule.Dependencies.ToLookup(dependency => dependency.PredecessorId, dependency => dependency.SuccessorId);
        var reached = starts.ToDictionary(id => id, _ => 0);
        var next = new Queue<Guid>(starts);
        while (next.TryDequeue(out var id))
        {
            foreach (var successor in successors[id])
            {
                if (reached.TryAdd(successor, reached[id] + 1))
                {
                    next.Enqueue(successor);
                }
            }
        }

        foreach (var start in starts)
        {
            reached.Remove(start);
        }

        return reached;
    }

    /// <summary>The items in the order of the schedule: by the phase, the start and the name.</summary>
    private static List<ScheduleItem> InOrder(Schedule schedule, IEnumerable<ScheduleItem> items)
    {
        var positions = schedule.Phases.ToDictionary(phase => phase.Id, phase => phase.Position);
        return
        [
            .. items
                .OrderBy(item => positions.GetValueOrDefault(item.PhaseId))
                .ThenBy(item => item.Start)
                .ThenBy(item => item.Name, StringComparer.CurrentCulture),
        ];
    }
}
