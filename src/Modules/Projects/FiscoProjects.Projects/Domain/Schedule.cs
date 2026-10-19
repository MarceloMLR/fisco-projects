using FiscoProjects.Calendar.Contracts;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Projects.Domain;

/// <summary>A task has a start and a duration in business days; a milestone is one day, without duration (ADR-0018).</summary>
public enum ScheduleItemKind
{
    Task = 1,
    Milestone = 2,
}

/// <summary>The dependency would make an item come, through the chain, before itself (ADR-0019).</summary>
public sealed class ScheduleCycleException() : InvalidOperationException("The dependency would make a cycle.");

/// <summary>
/// What a task or a milestone is asked to be: the phase, the name, the start, the duration (zero for a milestone), the
/// owner (none for a milestone), the progress (zero for a milestone; none keeps it as it is, zero in a new item), the
/// items it depends on and the task it is in, none when it is right in the phase (ADR-0088). For a task with subtasks,
/// only the phase, the name and the owner count: its dates and progress come from them.
/// </summary>
public sealed record ScheduleItemPlan(
    Guid PhaseId,
    string Name,
    DateOnly Start,
    int Duration,
    Guid? OwnerId,
    int? Progress,
    IReadOnlyCollection<Guid> PredecessorIds,
    Guid? ParentId = null);

/// <summary>
/// The schedule of a project (ADR-0019): phases and, in them, tasks and milestones, in business days of the calendar of
/// the installation (ADR-0018), with dependencies of the kind "finishes, then starts" that push the next items later
/// and never pull them earlier, and the progress of the tasks (ADR-0020). A task right in a phase may hold subtasks,
/// tasks and milestones one level down; it is then a summary, with its dates and progress from them, and only the items
/// with dates of their own have dependencies (ADR-0088). Its id is the id of the project.
/// </summary>
public sealed class Schedule : Entity
{
    public const int PhaseNameMaxLength = 150;
    public const int ItemNameMaxLength = 200;
    public const int MaxDuration = 999;

    /// <summary>The first and the last day a schedule may use, so a date computation never walks without end.</summary>
    public static readonly DateOnly FirstDay = new(2000, 1, 1);

    public static readonly DateOnly LastDay = new(2099, 12, 31);

    private readonly List<SchedulePhase> _phases = [];
    private readonly List<ScheduleItem> _items = [];
    private readonly List<ScheduleDependency> _dependencies = [];

    private Schedule()
    {
    }

    public Schedule(Guid projectId)
    {
        Id = projectId;
    }

    public Guid ProjectId => Id;

    public IReadOnlyList<SchedulePhase> Phases => _phases;

    public IReadOnlyList<ScheduleItem> Items => _items;

    public IReadOnlyList<ScheduleDependency> Dependencies => _dependencies;

    /// <summary>The name of a phase without the spaces at the ends, when it is not empty and has up to 150 characters.</summary>
    public static string? ValidPhaseName(string? name) => Trimmed(name, PhaseNameMaxLength);

    /// <summary>The name of a task or milestone without the spaces at the ends, when it is not empty and has up to 200 characters.</summary>
    public static string? ValidItemName(string? name) => Trimmed(name, ItemNameMaxLength);

    public static bool IsValidDay(DateOnly day) => day >= FirstDay && day <= LastDay;

    /// <summary>A task lasts from 1 to 999 business days; a milestone has no duration.</summary>
    public static bool IsValidDuration(ScheduleItemKind kind, int duration) =>
        kind == ScheduleItemKind.Milestone ? duration == 0 : duration is >= 1 and <= MaxDuration;

    /// <summary>A task goes from 0 to 100%; a milestone has no progress (it is reached or not, ADR-0020).</summary>
    public static bool IsValidProgress(ScheduleItemKind kind, int progress) =>
        kind == ScheduleItemKind.Milestone ? progress == 0 : progress is >= 0 and <= 100;

    public SchedulePhase? PhaseOf(Guid phaseId) => _phases.FirstOrDefault(phase => phase.Id == phaseId);

    public ScheduleItem? ItemOf(Guid itemId) => _items.FirstOrDefault(item => item.Id == itemId);

    /// <summary>The items the item depends on.</summary>
    public IReadOnlyList<ScheduleItem> PredecessorsOf(Guid itemId) =>
        [.. _dependencies.Where(dependency => dependency.SuccessorId == itemId).Select(dependency => Item(dependency.PredecessorId))];

    /// <summary>The tasks and milestones inside a task (ADR-0088).</summary>
    public IReadOnlyList<ScheduleItem> SubtasksOf(Guid itemId) => [.. _items.Where(item => item.ParentId == itemId)];

    /// <summary>A task with subtasks: its dates and progress come from them, and it has no dependencies (ADR-0088).</summary>
    public bool IsSummary(Guid itemId) => _items.Any(item => item.ParentId == itemId);

    /// <summary>
    /// Whether a task may hold the item (none for a new one) in the phase: a task right in that phase, other than the
    /// item, which holds no subtasks itself, so there is one level only (ADR-0088).
    /// </summary>
    public bool MayHold(Guid parentId, Guid? itemId, Guid phaseId) =>
        ItemOf(parentId) is { Kind: ScheduleItemKind.Task, ParentId: null } parent
        && parent.Id != itemId
        && parent.PhaseId == phaseId
        && (itemId is not { } id || !IsSummary(id));

    /// <summary>A new phase, after the others.</summary>
    public SchedulePhase AddPhase(string name)
    {
        var position = _phases.Count == 0 ? 1 : _phases.Max(phase => phase.Position) + 1;
        var phase = new SchedulePhase(Id, NameOrThrow(ValidPhaseName(name), name), position);
        _phases.Add(phase);
        return phase;
    }

    /// <summary>Renames a phase; false when it is not in the schedule.</summary>
    public bool RenamePhase(Guid phaseId, string name)
    {
        if (PhaseOf(phaseId) is not { } phase)
        {
            return false;
        }

        phase.Rename(NameOrThrow(ValidPhaseName(name), name));
        return true;
    }

    /// <summary>
    /// Removes a phase with its tasks and milestones and their dependencies; the items of other phases keep their dates.
    /// False when the phase is not in the schedule.
    /// </summary>
    public bool RemovePhase(Guid phaseId)
    {
        if (PhaseOf(phaseId) is not { } phase)
        {
            return false;
        }

        foreach (var item in _items.Where(item => item.PhaseId == phaseId).ToList())
        {
            if (_items.Contains(item))
            {
                Remove(item);
            }
        }

        _phases.Remove(phase);
        return true;
    }

    /// <summary>
    /// A new task or milestone. The start goes to the first business day it may have: on a business day, and after the
    /// items it depends on. A task that reaches 100% keeps today as the day it was completed. The first subtask of a
    /// task takes the dependencies of the task, and pushes the items after it as a change would (ADR-0088).
    /// </summary>
    /// <exception cref="ArgumentException">The plan is not valid for the kind or points outside the schedule.</exception>
    /// <exception cref="ScheduleCycleException">An item it would depend on comes after the task it goes into.</exception>
    public ScheduleItem AddItem(ScheduleItemKind kind, ScheduleItemPlan plan, IBusinessDays days, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(days);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown kind of schedule item.");
        }

        var item = new ScheduleItem(Id, kind);
        var predecessors = CheckedPredecessors(item.Id, plan);
        var heir = NewSummary(item.Id, plan);
        if (heir is not null && predecessors.Overlaps(ItemsAfter(heir.Value)))
        {
            throw new ScheduleCycleException();
        }

        Apply(item, plan, today);
        _items.Add(item);
        foreach (var predecessor in predecessors)
        {
            _dependencies.Add(new ScheduleDependency(Id, predecessor, item.Id));
        }

        if (heir is { } summary)
        {
            PassDependencies(summary, item.Id);
        }

        item.Place(EarliestStart(item, plan.Start, days), days);
        Push(item, ItemsAfter(item.Id), days);
        Summarize(days);
        return item;
    }

    /// <summary>
    /// Changes a task or milestone (the kind stays) and pushes, in chain, the items that come after it, only as much as
    /// each needs to start after the ones before it. An earlier start pulls nobody. An item that goes into a task
    /// without subtasks takes the dependencies of the task; a task with subtasks changes only its name, phase (with the
    /// subtasks) and owner (ADR-0088).
    /// </summary>
    /// <returns>How many other items were pushed, or null when the item is not in the schedule.</returns>
    /// <exception cref="ArgumentException">The plan is not valid for the kind or points outside the schedule.</exception>
    /// <exception cref="ScheduleCycleException">An item it would depend on already depends on it.</exception>
    public int? ChangeItem(Guid itemId, ScheduleItemPlan plan, IBusinessDays days, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(days);
        if (ItemOf(itemId) is not { } item)
        {
            return null;
        }

        var predecessors = CheckedPredecessors(itemId, plan);
        if (IsSummary(itemId))
        {
            Apply(item, plan, today);
            Summarize(days);
            return 0;
        }

        var heir = item.ParentId == plan.ParentId ? null : NewSummary(itemId, plan);
        // The task it goes into passes it its dependencies: check the cycle with them, before changing anything.
        List<Guid> inherited = heir is { } task
            ? [.. PredecessorsOf(task).Select(predecessor => predecessor.Id).Where(id => id != itemId)]
            : [];
        var successors = SuccessorIdsOf(itemId)
            .Concat(heir is { } next ? SuccessorIdsOf(next).Where(id => id != itemId) : Enumerable.Empty<Guid>())
            .ToHashSet();
        var after = ItemsAfter(successors, itemId);
        if (predecessors.Concat(inherited).Any(after.Contains))
        {
            throw new ScheduleCycleException();
        }

        Apply(item, plan, today);
        _dependencies.RemoveAll(dependency => dependency.SuccessorId == itemId && !predecessors.Contains(dependency.PredecessorId));
        foreach (var predecessor in predecessors.Where(id => !_dependencies.Any(d => d.SuccessorId == itemId && d.PredecessorId == id)))
        {
            _dependencies.Add(new ScheduleDependency(Id, predecessor, itemId));
        }

        if (heir is { } summary)
        {
            PassDependencies(summary, itemId);
        }

        item.Place(EarliestStart(item, plan.Start, days), days);
        var pushed = Push(item, ItemsAfter(itemId), days);
        Summarize(days);
        return pushed;
    }

    /// <summary>
    /// The duration a task gets from a start, moved to a business day, to an end, in business days of the calendar;
    /// null when the end comes before that start or the duration would pass 999 days (ADR-0018).
    /// </summary>
    public static int? DurationBetween(DateOnly start, DateOnly end, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(days);
        if (!IsValidDay(start) || !IsValidDay(end))
        {
            return null;
        }

        var duration = days.CountBusinessDays(OnABusinessDay(start, days), end);
        return duration is >= 1 and <= MaxDuration ? duration : null;
    }

    /// <summary>
    /// The non-business days from the first day of the month before <paramref name="start"/> to the last day of the
    /// month after <paramref name="end"/>, for the timeline to shade them without the rule of the calendar (ADR-0051).
    /// </summary>
    public static IReadOnlyList<DateOnly> NonWorkingDaysAround(DateOnly start, DateOnly end, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(days);
        var first = new DateOnly(start.Year, start.Month, 1).AddMonths(-1);
        var last = new DateOnly(end.Year, end.Month, 1).AddMonths(2).AddDays(-1);
        var found = new List<DateOnly>();
        for (var day = first; day <= last; day = day.AddDays(1))
        {
            if (!days.IsBusinessDay(day))
            {
                found.Add(day);
            }
        }

        return found;
    }

    /// <summary>
    /// Moves a task or milestone in the timeline (ADR-0019, ADR-0051): a new start keeps the duration in business days;
    /// with an end, a task lasts the business days from the start to it. The rest of the item stays, with its progress,
    /// and the items after it are pushed as in <see cref="ChangeItem"/>.
    /// </summary>
    /// <returns>How many other items were pushed, or null when the item is not in the schedule.</returns>
    /// <exception cref="ArgumentException">An end for a milestone, or an end that gives no valid duration.</exception>
    public int? Reschedule(Guid itemId, DateOnly start, DateOnly? end, IBusinessDays days, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(days);
        if (ItemOf(itemId) is not { } item)
        {
            return null;
        }

        if (IsSummary(itemId))
        {
            throw new ArgumentException("A task with subtasks takes its dates from them.", nameof(start));
        }

        var duration = item.Duration;
        if (end is { } last)
        {
            duration = item.Kind == ScheduleItemKind.Task && DurationBetween(start, last, days) is { } between
                ? between
                : throw new ArgumentException("The end gives no valid duration for the item.", nameof(end));
        }

        var plan = new ScheduleItemPlan(
            item.PhaseId,
            item.Name,
            start,
            duration,
            item.OwnerId,
            Progress: null,
            [.. PredecessorsOf(itemId).Select(predecessor => predecessor.Id)],
            item.ParentId);
        return ChangeItem(itemId, plan, days, today);
    }

    /// <summary>
    /// Updates the progress of a task (ADR-0020): at 100% it keeps today as the day it was completed, below it the day
    /// goes away. False when the item is not in the schedule.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The item is a milestone or a task with subtasks (ADR-0088), or the progress is not from 0 to 100.
    /// </exception>
    public bool UpdateProgress(Guid itemId, int progress, DateOnly today)
    {
        if (ItemOf(itemId) is not { } item)
        {
            return false;
        }

        if (item.Kind != ScheduleItemKind.Task || IsSummary(itemId) || !IsValidProgress(item.Kind, progress))
        {
            throw new ArgumentException("Only a task without subtasks has a progress of its own, from 0 to 100.", nameof(progress));
        }

        item.SetProgress(progress, today);
        SummarizeProgress();
        return true;
    }

    /// <summary>
    /// Marks a milestone as reached, keeping the day it was first reached, or as not reached, without the day (ADR-0020,
    /// ADR-0086). False when the item is not in the schedule.
    /// </summary>
    /// <exception cref="ArgumentException">The item is a task.</exception>
    public bool SetReached(Guid itemId, bool reached, DateOnly today)
    {
        if (ItemOf(itemId) is not { } item)
        {
            return false;
        }

        if (item.Kind != ScheduleItemKind.Milestone)
        {
            throw new ArgumentException("Only a milestone is reached.", nameof(itemId));
        }

        item.SetReached(reached, today);
        SummarizeProgress();
        return true;
    }

    /// <summary>
    /// Removes a task or milestone and its dependencies; nothing else moves. A task with subtasks goes with them, and
    /// the last subtask of a task leaves its dependencies to the task (ADR-0088). False when it is not in the schedule.
    /// </summary>
    public bool RemoveItem(Guid itemId, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(days);
        if (ItemOf(itemId) is not { } item)
        {
            return false;
        }

        Remove(item);
        Summarize(days);
        return true;
    }

    /// <summary>
    /// The tasks of a person who left the project or became a viewer lose the owner, with the same dates and progress
    /// (ADR-0042, ADR-0043).
    /// </summary>
    /// <returns>How many tasks lost the owner.</returns>
    public int Unassign(Guid userId)
    {
        var owned = _items.Where(item => item.OwnerId == userId).ToList();
        foreach (var item in owned)
        {
            item.Unassign();
        }

        return owned.Count;
    }

    /// <summary>The dates, the progress and the delays of the schedule on a day (<see cref="ScheduleFigures"/>).</summary>
    public ScheduleFigures Figures(DateOnly today) => ScheduleFigures.Of(_phases, _items, today);

    private static string? Trimmed(string? text, int maxLength)
    {
        var trimmed = text?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > maxLength ? null : trimmed;
    }

    private static string NameOrThrow(string? valid, string name) =>
        valid ?? throw new ArgumentException($"'{name}' is no valid name.", nameof(name));

    /// <summary>The first day an item may start after another: the next business day for a task, the same day for a milestone.</summary>
    private static DateOnly EarliestAfter(ScheduleItem predecessor, ScheduleItemKind kind, IBusinessDays days) =>
        kind == ScheduleItemKind.Milestone ? predecessor.End : days.AddBusinessDays(predecessor.End, 1);

    private static DateOnly OnABusinessDay(DateOnly day, IBusinessDays days) =>
        days.IsBusinessDay(day) ? day : days.AddBusinessDays(day, 1);

    /// <summary>
    /// The plan checked against the kind, the phases and the task it goes into, and applied to the item but the dates. A
    /// task with subtasks takes only the phase, which its subtasks follow, the name and the owner (ADR-0088).
    /// </summary>
    private void Apply(ScheduleItem item, ScheduleItemPlan plan, DateOnly today)
    {
        if (PhaseOf(plan.PhaseId) is null)
        {
            throw new ArgumentException("The phase is not in the schedule.", nameof(plan));
        }

        var summary = IsSummary(item.Id);
        if ((!summary && (!IsValidDay(plan.Start) || !IsValidDuration(item.Kind, plan.Duration)))
            || (plan.Progress is { } progress && (summary || !IsValidProgress(item.Kind, progress)))
            || (item.Kind == ScheduleItemKind.Milestone && plan.OwnerId is not null)
            || (plan.ParentId is { } parentId && !MayHold(parentId, item.Id, plan.PhaseId)))
        {
            throw new ArgumentException("The plan is not valid for the kind of item.", nameof(plan));
        }

        var name = NameOrThrow(ValidItemName(plan.Name), plan.Name);
        item.Set(plan.PhaseId, name, summary ? item.Duration : plan.Duration, plan.OwnerId, plan.ParentId);
        if (summary)
        {
            foreach (var subtask in SubtasksOf(item.Id))
            {
                subtask.MoveTo(plan.PhaseId);
            }
        }

        if (plan.Progress is { } asked)
        {
            item.SetProgress(asked, today);
        }
    }

    /// <summary>
    /// The items the plan depends on, once each, all in the schedule and none the item itself. Only items with dates of
    /// their own have dependencies: none a task with subtasks, nor the task the item goes into (ADR-0088).
    /// </summary>
    private HashSet<Guid> CheckedPredecessors(Guid itemId, ScheduleItemPlan plan)
    {
        var predecessors = plan.PredecessorIds.ToHashSet();
        if (predecessors.Contains(itemId) || predecessors.Any(id => ItemOf(id) is null)
            || predecessors.Any(IsSummary)
            || (plan.ParentId is { } parentId && predecessors.Contains(parentId))
            || (predecessors.Count > 0 && IsSummary(itemId)))
        {
            throw new ArgumentException("The item depends on items of the schedule with dates of their own, other than itself.", nameof(plan));
        }

        return predecessors;
    }

    /// <summary>The task that becomes a summary when the item goes into it: one without subtasks yet.</summary>
    private Guid? NewSummary(Guid itemId, ScheduleItemPlan plan) =>
        plan.ParentId is { } parentId && MayHold(parentId, itemId, plan.PhaseId) && !IsSummary(parentId) ? parentId : null;

    /// <summary>
    /// Moves the dependencies of an item, before and after it, to another: a task to its first subtask, or the last
    /// subtask back to its task (ADR-0088). A dependency between the two goes away, and one the other already has stays once.
    /// </summary>
    private void PassDependencies(Guid fromId, Guid toId)
    {
        var moving = _dependencies.Where(d => d.PredecessorId == fromId || d.SuccessorId == fromId).ToList();
        _dependencies.RemoveAll(moving.Contains);
        foreach (var dependency in moving)
        {
            var predecessor = dependency.PredecessorId == fromId ? toId : dependency.PredecessorId;
            var successor = dependency.SuccessorId == fromId ? toId : dependency.SuccessorId;
            if (predecessor != successor && !_dependencies.Any(d => d.PredecessorId == predecessor && d.SuccessorId == successor))
            {
                _dependencies.Add(new ScheduleDependency(Id, predecessor, successor));
            }
        }
    }

    /// <summary>Removes an item with its subtasks and dependencies; the last subtask leaves its dependencies to the task.</summary>
    private void Remove(ScheduleItem item)
    {
        foreach (var subtask in SubtasksOf(item.Id))
        {
            _dependencies.RemoveAll(d => d.PredecessorId == subtask.Id || d.SuccessorId == subtask.Id);
            _items.Remove(subtask);
        }

        if (item.ParentId is { } parentId && SubtasksOf(parentId).Count == 1)
        {
            PassDependencies(item.Id, parentId);
        }

        _dependencies.RemoveAll(d => d.PredecessorId == item.Id || d.SuccessorId == item.Id);
        _items.Remove(item);
    }

    /// <summary>
    /// The dates, the duration and the progress of each task with subtasks, from them (ADR-0088): from the first start to
    /// the last end, the business days between them, and the progress weighted by the duration of its tasks.
    /// </summary>
    private void Summarize(IBusinessDays days)
    {
        foreach (var summary in _items.Where(item => IsSummary(item.Id)).ToList())
        {
            var subtasks = SubtasksOf(summary.Id);
            var start = subtasks.Min(subtask => subtask.Start);
            var end = subtasks.Max(subtask => subtask.End);
            summary.Summarize(start, end, days.CountBusinessDays(start, end));
        }

        SummarizeProgress();
    }

    /// <summary>
    /// The progress of each task with subtasks, weighted by the duration of its tasks, with the day the last reached 100%.
    /// With milestones only, 100% once all are reached, on the day the last was (ADR-0088).
    /// </summary>
    private void SummarizeProgress()
    {
        foreach (var summary in _items.Where(item => IsSummary(item.Id)).ToList())
        {
            var subtasks = SubtasksOf(summary.Id);
            var tasks = subtasks.Where(subtask => subtask.Kind == ScheduleItemKind.Task).ToList();
            if (tasks.Count > 0)
            {
                summary.SetSummaryProgress(
                    ScheduleFigures.WeightedProgress(tasks),
                    tasks.All(task => task.Progress == 100) ? tasks.Max(task => task.CompletedOn) : null);
            }
            else
            {
                var reached = subtasks.All(subtask => subtask.ReachedOn is not null);
                summary.SetSummaryProgress(reached ? 100 : 0, reached ? subtasks.Max(subtask => subtask.ReachedOn) : null);
            }
        }
    }

    /// <summary>The asked start on a business day, or later when the items before it end later.</summary>
    private DateOnly EarliestStart(ScheduleItem item, DateOnly asked, IBusinessDays days)
    {
        var start = OnABusinessDay(asked, days);
        foreach (var predecessor in PredecessorsOf(item.Id))
        {
            var earliest = EarliestAfter(predecessor, item.Kind, days);
            if (earliest > start)
            {
                start = earliest;
            }
        }

        return OnABusinessDay(start, days);
    }

    /// <summary>Every item that depends, directly or through others, on the item.</summary>
    private HashSet<Guid> ItemsAfter(Guid itemId) => ItemsAfter(SuccessorIdsOf(itemId), itemId);

    /// <summary>The items and every item that depends on them, directly or through others, never passing through the item.</summary>
    private HashSet<Guid> ItemsAfter(IEnumerable<Guid> successors, Guid itemId)
    {
        var found = successors.Where(id => id != itemId).ToHashSet();
        var pending = new Queue<Guid>(found);
        while (pending.TryDequeue(out var current))
        {
            foreach (var successor in SuccessorIdsOf(current))
            {
                if (successor != itemId && found.Add(successor))
                {
                    pending.Enqueue(successor);
                }
            }
        }

        return found;
    }

    /// <summary>
    /// Moves later each item after the changed one that starts before the items it depends on allow, in topological
    /// order, so each item is looked at once, after all the items before it.
    /// </summary>
    private int Push(ScheduleItem changed, HashSet<Guid> after, IBusinessDays days)
    {
        var waiting = after.ToDictionary(
            id => id,
            id => _dependencies.Count(dependency => dependency.SuccessorId == id && after.Contains(dependency.PredecessorId)));
        var ready = new Queue<ScheduleItem>(SuccessorIdsOf(changed.Id).Where(id => waiting[id] == 0).Select(Item).OrderBy(item => item.Start));
        var pushed = 0;
        while (ready.TryDequeue(out var item))
        {
            var earliest = PredecessorsOf(item.Id).Max(predecessor => EarliestAfter(predecessor, item.Kind, days));
            if (earliest > item.Start)
            {
                item.Place(OnABusinessDay(earliest, days), days);
                pushed++;
            }

            foreach (var successor in SuccessorIdsOf(item.Id))
            {
                if (--waiting[successor] == 0)
                {
                    ready.Enqueue(Item(successor));
                }
            }
        }

        return pushed;
    }

    private IEnumerable<Guid> SuccessorIdsOf(Guid itemId) =>
        _dependencies.Where(dependency => dependency.PredecessorId == itemId).Select(dependency => dependency.SuccessorId);

    private ScheduleItem Item(Guid itemId) =>
        ItemOf(itemId) ?? throw new InvalidOperationException("A dependency points outside the schedule.");
}

/// <summary>A phase of a schedule: a name and, in it, tasks and milestones (ADR-0019).</summary>
public sealed class SchedulePhase : Entity
{
    private SchedulePhase()
    {
        Name = string.Empty;
    }

    internal SchedulePhase(Guid projectId, string name, int position)
    {
        ProjectId = projectId;
        Name = name;
        Position = position;
    }

    public Guid ProjectId { get; private init; }

    public string Name { get; private set; }

    /// <summary>The order the phases were created in, for the phases still without items.</summary>
    public int Position { get; private init; }

    internal void Rename(string name) => Name = name;
}

/// <summary>
/// A task or a milestone of a schedule. The end is computed from the start and the duration in business days; a
/// milestone ends on its start. A task completed (100%) keeps the day it was completed, and a milestone reached, the day
/// it was reached (ADR-0020, ADR-0086). A subtask is inside a task of the same phase; the task then keeps the dates,
/// the duration and the progress the schedule computes from its subtasks (ADR-0088).
/// </summary>
public sealed class ScheduleItem : Entity
{
    private ScheduleItem()
    {
        Name = string.Empty;
    }

    internal ScheduleItem(Guid projectId, ScheduleItemKind kind)
    {
        ProjectId = projectId;
        Kind = kind;
        Name = string.Empty;
    }

    public Guid ProjectId { get; private init; }

    public Guid PhaseId { get; private set; }

    /// <summary>The task the item is inside, in the same phase; none when it is right in the phase (ADR-0088).</summary>
    public Guid? ParentId { get; private set; }

    public ScheduleItemKind Kind { get; private init; }

    public string Name { get; private set; }

    public DateOnly Start { get; private set; }

    /// <summary>Business days, counting the start; zero for a milestone.</summary>
    public int Duration { get; private set; }

    public DateOnly End { get; private set; }

    /// <summary>From 0 to 100%; always zero for a milestone.</summary>
    public int Progress { get; private set; }

    /// <summary>The person of the project in charge of the task; never for a milestone.</summary>
    public Guid? OwnerId { get; private set; }

    /// <summary>The day the task reached 100%, in the time zone of the installation; null below 100%.</summary>
    public DateOnly? CompletedOn { get; private set; }

    /// <summary>The day the milestone was reached, in the time zone of the installation; null while it is not, and for a task.</summary>
    public DateOnly? ReachedOn { get; private set; }

    /// <summary>
    /// A task is late when its end passed and it has not reached 100% (ADR-0020); a milestone, when its day passed and it
    /// was not reached (ADR-0086).
    /// </summary>
    public bool IsLate(DateOnly today) =>
        End < today && (Kind == ScheduleItemKind.Task ? Progress < 100 : ReachedOn is null);

    /// <summary>
    /// The business days of delay on a day: from the day after the end up to the day, both included, in the calendar of
    /// the installation (ADR-0018); zero when it is not late, and while the delay began on non-business days only.
    /// </summary>
    public int DaysLate(DateOnly today, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(days);
        return IsLate(today) ? days.CountBusinessDays(End.AddDays(1), today) : 0;
    }

    internal void Set(Guid phaseId, string name, int duration, Guid? ownerId, Guid? parentId)
    {
        PhaseId = phaseId;
        Name = name;
        Duration = duration;
        OwnerId = ownerId;
        ParentId = parentId;
    }

    /// <summary>A subtask goes with its task to another phase.</summary>
    internal void MoveTo(Guid phaseId) => PhaseId = phaseId;

    /// <summary>The dates and the duration of a task with subtasks, from them.</summary>
    internal void Summarize(DateOnly start, DateOnly end, int duration)
    {
        Start = start;
        End = end;
        Duration = duration;
    }

    /// <summary>The progress of a task with subtasks, from them, with the day they were all completed.</summary>
    internal void SetSummaryProgress(int progress, DateOnly? completedOn)
    {
        Progress = progress;
        CompletedOn = completedOn;
    }

    internal void SetProgress(int progress, DateOnly today)
    {
        if (progress == 100 && (Progress < 100 || CompletedOn is null))
        {
            CompletedOn = today;
        }
        else if (progress < 100)
        {
            CompletedOn = null;
        }

        Progress = progress;
    }

    /// <summary>Starts on a business day, ending as many business days later as the duration says.</summary>
    internal void Place(DateOnly start, IBusinessDays days)
    {
        Start = start;
        End = Kind == ScheduleItemKind.Milestone ? start : days.AddBusinessDays(start, Duration - 1);
    }

    internal void Unassign() => OwnerId = null;

    internal void SetReached(bool reached, DateOnly today) => ReachedOn = reached ? ReachedOn ?? today : null;
}

/// <summary>An item that starts only after another ends (ADR-0019).</summary>
public sealed class ScheduleDependency : Entity
{
    private ScheduleDependency()
    {
    }

    internal ScheduleDependency(Guid projectId, Guid predecessorId, Guid successorId)
    {
        ProjectId = projectId;
        PredecessorId = predecessorId;
        SuccessorId = successorId;
    }

    public Guid ProjectId { get; private init; }

    /// <summary>The item that ends first.</summary>
    public Guid PredecessorId { get; private init; }

    /// <summary>The item that starts after it.</summary>
    public Guid SuccessorId { get; private init; }
}
