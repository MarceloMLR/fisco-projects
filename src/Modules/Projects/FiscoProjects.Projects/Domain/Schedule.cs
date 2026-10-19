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
/// owner (none for a milestone), the progress (zero for a milestone; none keeps it as it is, zero in a new item) and the
/// items it depends on.
/// </summary>
public sealed record ScheduleItemPlan(
    Guid PhaseId,
    string Name,
    DateOnly Start,
    int Duration,
    Guid? OwnerId,
    int? Progress,
    IReadOnlyCollection<Guid> PredecessorIds);

/// <summary>
/// The schedule of a project (ADR-0019): phases and, in them, tasks and milestones, in business days of the calendar of
/// the installation (ADR-0018), with dependencies of the kind "finishes, then starts" that push the next items later
/// and never pull them earlier, and the progress of the tasks (ADR-0020). Its id is the id of the project.
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
            RemoveItem(item.Id);
        }

        _phases.Remove(phase);
        return true;
    }

    /// <summary>
    /// A new task or milestone. The start goes to the first business day it may have: on a business day, and after the
    /// items it depends on. A task that reaches 100% keeps today as the day it was completed.
    /// </summary>
    /// <exception cref="ArgumentException">The plan is not valid for the kind or points outside the schedule.</exception>
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
        Apply(item, plan, today);
        _items.Add(item);
        foreach (var predecessor in predecessors)
        {
            _dependencies.Add(new ScheduleDependency(Id, predecessor, item.Id));
        }

        item.Place(EarliestStart(item, plan.Start, days), days);
        return item;
    }

    /// <summary>
    /// Changes a task or milestone (the kind stays) and pushes, in chain, the items that come after it, only as much as
    /// each needs to start after the ones before it. An earlier start pulls nobody.
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
        var after = ItemsAfter(itemId);
        if (predecessors.Any(after.Contains))
        {
            throw new ScheduleCycleException();
        }

        Apply(item, plan, today);
        _dependencies.RemoveAll(dependency => dependency.SuccessorId == itemId && !predecessors.Contains(dependency.PredecessorId));
        foreach (var predecessor in predecessors.Where(id => !_dependencies.Any(d => d.SuccessorId == itemId && d.PredecessorId == id)))
        {
            _dependencies.Add(new ScheduleDependency(Id, predecessor, itemId));
        }

        item.Place(EarliestStart(item, plan.Start, days), days);
        return Push(item, after, days);
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
            [.. PredecessorsOf(itemId).Select(predecessor => predecessor.Id)]);
        return ChangeItem(itemId, plan, days, today);
    }

    /// <summary>
    /// Updates the progress of a task (ADR-0020): at 100% it keeps today as the day it was completed, below it the day
    /// goes away. False when the item is not in the schedule.
    /// </summary>
    /// <exception cref="ArgumentException">The item is a milestone, or the progress is not from 0 to 100.</exception>
    public bool UpdateProgress(Guid itemId, int progress, DateOnly today)
    {
        if (ItemOf(itemId) is not { } item)
        {
            return false;
        }

        if (item.Kind != ScheduleItemKind.Task || !IsValidProgress(item.Kind, progress))
        {
            throw new ArgumentException("Only a task has a progress, from 0 to 100.", nameof(progress));
        }

        item.SetProgress(progress, today);
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
        return true;
    }

    /// <summary>Removes a task or milestone and its dependencies; nothing else moves. False when it is not in the schedule.</summary>
    public bool RemoveItem(Guid itemId)
    {
        if (ItemOf(itemId) is not { } item)
        {
            return false;
        }

        _dependencies.RemoveAll(dependency => dependency.PredecessorId == itemId || dependency.SuccessorId == itemId);
        _items.Remove(item);
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

    /// <summary>The plan checked against the kind and the phases, and applied to the item but the dates.</summary>
    private void Apply(ScheduleItem item, ScheduleItemPlan plan, DateOnly today)
    {
        if (PhaseOf(plan.PhaseId) is null)
        {
            throw new ArgumentException("The phase is not in the schedule.", nameof(plan));
        }

        if (!IsValidDay(plan.Start) || !IsValidDuration(item.Kind, plan.Duration)
            || (plan.Progress is { } progress && !IsValidProgress(item.Kind, progress))
            || (item.Kind == ScheduleItemKind.Milestone && plan.OwnerId is not null))
        {
            throw new ArgumentException("The plan is not valid for the kind of item.", nameof(plan));
        }

        item.Set(plan.PhaseId, NameOrThrow(ValidItemName(plan.Name), plan.Name), plan.Duration, plan.OwnerId);
        if (plan.Progress is { } asked)
        {
            item.SetProgress(asked, today);
        }
    }

    /// <summary>The items the plan depends on, once each, all in the schedule and none the item itself.</summary>
    private HashSet<Guid> CheckedPredecessors(Guid itemId, ScheduleItemPlan plan)
    {
        var predecessors = plan.PredecessorIds.ToHashSet();
        if (predecessors.Contains(itemId) || predecessors.Any(id => ItemOf(id) is null))
        {
            throw new ArgumentException("The item depends on items of the schedule other than itself.", nameof(plan));
        }

        return predecessors;
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
    private HashSet<Guid> ItemsAfter(Guid itemId)
    {
        var found = new HashSet<Guid>();
        var pending = new Queue<Guid>([itemId]);
        while (pending.TryDequeue(out var current))
        {
            foreach (var successor in SuccessorIdsOf(current))
            {
                if (found.Add(successor))
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
/// it was reached (ADR-0020, ADR-0086).
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

    internal void Set(Guid phaseId, string name, int duration, Guid? ownerId)
    {
        PhaseId = phaseId;
        Name = name;
        Duration = duration;
        OwnerId = ownerId;
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
