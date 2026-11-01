using FiscoProjects.Calendar.Contracts;

namespace FiscoProjects.Projects.Domain;

/// <summary>
/// A phase as the screens show it: the dates from the first start to the last end of its items (none without items),
/// the progress weighted by the duration of its tasks, whether a task or a milestone of it is late, and its items in
/// order, each task followed by its subtasks (ADR-0088).
/// </summary>
public sealed record PhaseFigures(
    SchedulePhase Phase, DateOnly? Start, DateOnly? End, int Progress, bool IsLate, IReadOnlyList<ScheduleItem> Items);

/// <summary>
/// The numbers of a schedule on a day, the same for the Schedule tab, the list of projects and the header (ADR-0016,
/// ADR-0019, ADR-0020, ADR-0071).
/// </summary>
/// <param name="Today">The day, in the time zone of the installation.</param>
/// <param name="Start">The first start of the schedule; null without items.</param>
/// <param name="End">The last end of the schedule; null without items.</param>
/// <param name="Progress">The progress of the project, weighted by the duration of the tasks; milestones do not count.</param>
/// <param name="LateTasks">How many tasks are late.</param>
/// <param name="LateMilestones">How many milestones are late (ADR-0086).</param>
/// <param name="CurrentPhase">The first late phase or, without delays, the last phase that has started; null before the start.</param>
/// <param name="CompletedOn">With every task at 100%, the day the last one was completed.</param>
/// <param name="Phases">The phases in order: by start, then the ones without items in the order they were created.</param>
/// <param name="Milestones">The days of the milestones.</param>
/// <param name="Summaries">The tasks with subtasks, whose dates and progress come from them (ADR-0088).</param>
/// <param name="LateItems">The late tasks and milestones, and the tasks with a late subtask.</param>
public sealed record ScheduleFigures(
    DateOnly Today,
    DateOnly? Start,
    DateOnly? End,
    int Progress,
    int LateTasks,
    int LateMilestones,
    SchedulePhase? CurrentPhase,
    DateOnly? CompletedOn,
    IReadOnlyList<PhaseFigures> Phases,
    IReadOnlyList<DateOnly> Milestones,
    IReadOnlySet<Guid> Summaries,
    IReadOnlySet<Guid> LateItems)
{
    /// <summary>The numbers of a schedule from its phases and items, on a day.</summary>
    public static ScheduleFigures Of(IEnumerable<SchedulePhase> phases, IEnumerable<ScheduleItem> items, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(phases);
        ArgumentNullException.ThrowIfNull(items);
        var all = items.ToList();
        // A task with subtasks counts through them, never twice (ADR-0088).
        var summaries = all.Where(item => item.ParentId is not null).Select(item => item.ParentId!.Value).ToHashSet();
        var own = all.Where(item => !summaries.Contains(item.Id)).ToList();
        var late = own.Where(item => item.IsLate(today)).Select(item => item.Id).ToHashSet();
        late.UnionWith(own.Where(item => late.Contains(item.Id) && item.ParentId is not null).Select(item => item.ParentId!.Value));
        var byPhase = all.ToLookup(item => item.PhaseId);
        var phaseFigures = phases
            .Select(phase => PhaseOf(phase, byPhase[phase.Id], summaries, late))
            .OrderBy(figures => figures.Start is null)
            .ThenBy(figures => figures.Start)
            .ThenBy(figures => figures.Phase.Position)
            .ToList();

        var tasks = own.Where(item => item.Kind == ScheduleItemKind.Task).ToList();
        var dated = phaseFigures.Where(figures => figures.Start is not null).ToList();
        var current = dated.FirstOrDefault(figures => figures.IsLate)
            ?? dated.LastOrDefault(figures => figures.Start <= today);
        var completed = tasks.Count > 0 && tasks.All(task => task.Progress == 100)
            ? tasks.Max(task => task.CompletedOn)
            : null;

        return new ScheduleFigures(
            today,
            all.Count == 0 ? null : all.Min(item => item.Start),
            all.Count == 0 ? null : all.Max(item => item.End),
            WeightedProgress(tasks),
            tasks.Count(task => task.IsLate(today)),
            all.Count(item => item.Kind == ScheduleItemKind.Milestone && item.IsLate(today)),
            current?.Phase,
            completed,
            phaseFigures,
            [.. all.Where(item => item.Kind == ScheduleItemKind.Milestone).Select(item => item.Start).Order()],
            summaries,
            late);
    }

    /// <summary>
    /// The business days of delay of each late task and milestone on a day (<see cref="ScheduleItem.DaysLate"/>), and of
    /// each task with a late subtask, which takes the most of its subtasks (ADR-0088); the others are not in it.
    /// </summary>
    public static IReadOnlyDictionary<Guid, int> LateDaysOf(IEnumerable<ScheduleItem> items, DateOnly today, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(days);
        var all = items.ToList();
        var summaries = all.Where(item => item.ParentId is not null).Select(item => item.ParentId!.Value).ToHashSet();
        var late = all
            .Where(item => !summaries.Contains(item.Id) && item.IsLate(today))
            .ToDictionary(item => item.Id, item => item.DaysLate(today, days));
        foreach (var group in all.Where(item => item.ParentId is not null && late.ContainsKey(item.Id)).GroupBy(item => item.ParentId!.Value))
        {
            late[group.Key] = group.Max(item => late[item.Id]);
        }

        return late;
    }

    /// <summary>
    /// How far each task and milestone with a deadline went past it (positive) or is before it (negative), in business
    /// days on a day (<see cref="ScheduleItem.DeadlineDeviation"/>); the items without a deadline are not in it (GanttA-2).
    /// </summary>
    public static IReadOnlyDictionary<Guid, int> DeadlineDeviationsOf(IEnumerable<ScheduleItem> items, DateOnly today, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(days);
        return items
            .Where(item => item.Deadline is not null)
            .ToDictionary(item => item.Id, item => item.DeadlineDeviation(today, days)!.Value);
    }

    /// <summary>How many open tasks and milestones missed their deadline on a day (<see cref="ScheduleItem.MissesDeadline"/>).</summary>
    public static int MissedDeadlinesOf(IEnumerable<ScheduleItem> items, DateOnly today, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(days);
        return items.Count(item => item.MissesDeadline(today, days));
    }

    /// <summary>The items of a phase by start, end, tasks before milestones of the same day, and name.</summary>
    public static IReadOnlyList<ScheduleItem> InOrder(IEnumerable<ScheduleItem> items) =>
        [.. items
            .OrderBy(item => item.Start)
            .ThenBy(item => item.End)
            .ThenBy(item => item.Kind)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.Id)];

    /// <summary>The progress weighted by the duration of the tasks, rounded to the nearest whole number; zero without tasks.</summary>
    public static int WeightedProgress(IEnumerable<ScheduleItem> items)
    {
        var tasks = items.Where(item => item.Kind == ScheduleItemKind.Task).ToList();
        var days = tasks.Sum(task => (long)task.Duration);
        if (days == 0)
        {
            return 0;
        }

        var done = tasks.Sum(task => (long)task.Duration * task.Progress);
        // Rounded half up, in whole numbers: (2 × done + days) / (2 × days).
        return (int)(((2 * done) + days) / (2 * days));
    }

    /// <summary>The items of a phase in order (<see cref="InOrder"/>), each task followed by its subtasks, also in order.</summary>
    public static IReadOnlyList<ScheduleItem> InTreeOrder(IEnumerable<ScheduleItem> items)
    {
        var all = items.ToList();
        var ids = all.Select(item => item.Id).ToHashSet();
        var subtasks = all.Where(item => item.ParentId is { } parentId && ids.Contains(parentId)).ToLookup(item => item.ParentId!.Value);
        return [.. InOrder(all.Where(item => item.ParentId is not { } parentId || !ids.Contains(parentId)))
            .SelectMany(item => (IEnumerable<ScheduleItem>)[item, .. InOrder(subtasks[item.Id])])];
    }

    private static PhaseFigures PhaseOf(
        SchedulePhase phase, IEnumerable<ScheduleItem> items, IReadOnlySet<Guid> summaries, IReadOnlySet<Guid> late)
    {
        var ordered = InTreeOrder(items);
        return new PhaseFigures(
            phase,
            ordered.Count == 0 ? null : ordered.Min(item => item.Start),
            ordered.Count == 0 ? null : ordered.Max(item => item.End),
            WeightedProgress(ordered.Where(item => !summaries.Contains(item.Id))),
            ordered.Any(item => late.Contains(item.Id)),
            ordered);
    }
}
