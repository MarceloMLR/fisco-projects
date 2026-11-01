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
/// items it depends on, the task it is in, none when it is right in the phase (ADR-0088), and whether the client of the
/// project is in charge of the task instead of a person (ADR-0102). For a task with subtasks, only the phase, the name
/// and the owner count: its dates and progress come from them.
/// </summary>
public sealed record ScheduleItemPlan(
    Guid PhaseId,
    string Name,
    DateOnly Start,
    int Duration,
    Guid? OwnerId,
    int? Progress,
    IReadOnlyCollection<Guid> PredecessorIds,
    Guid? ParentId = null,
    bool OwnedByClient = false,
    DateOnly? Deadline = null,
    IReadOnlyList<ScheduleLink>? Links = null);

/// <summary>
/// The schedule of a project (ADR-0019): phases and, in them, tasks and milestones, in business days of the calendar of
/// the installation (ADR-0018), with dependencies of a kind and a lag (ADR-0137) that push the next items later and
/// never pull them earlier, and the progress of the tasks (ADR-0020). A task right in a phase may hold subtasks,
/// tasks and milestones one level down; it is then a summary, with its dates and progress from them, and only the items
/// with dates of their own have dependencies (ADR-0088). Its id is the id of the project.
/// </summary>
public sealed class Schedule : Entity
{
    public const int PhaseNameMaxLength = 150;
    public const int ItemNameMaxLength = 200;
    public const int MaxDuration = 999;

    /// <summary>The most business days an item of an outline may start after the first start: about a hundred years.</summary>
    public const int MaxStartOffset = 30_000;

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

    /// <summary>The baseline of the project, once the manager saves one (ADR-0021).</summary>
    public ScheduleBaseline? Baseline { get; private set; }

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
            _dependencies.Add(NewDependency(predecessor, item.Id, plan));
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
        // A plan with the kinds and lags sets them on the dependencies it keeps; without them, each keeps its own.
        foreach (var kept in _dependencies.Where(d => d.SuccessorId == itemId && plan.Links is not null))
        {
            var link = LinkOf(kept.PredecessorId, plan);
            kept.Set(link.Type, link.Lag);
        }

        foreach (var predecessor in predecessors.Where(id => !_dependencies.Any(d => d.SuccessorId == itemId && d.PredecessorId == id)))
        {
            _dependencies.Add(NewDependency(predecessor, itemId, plan));
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
            item.ParentId,
            item.OwnedByClient,
            item.Deadline);
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

    /// <summary>
    /// Saves the baseline of the project on the day, or updates it: the dates of every task and milestone, the tasks
    /// with subtasks included, and of the project (ADR-0021). False without tasks nor milestones.
    /// </summary>
    public bool SaveBaseline(DateOnly today)
    {
        if (_items.Count == 0)
        {
            return false;
        }

        Baseline ??= new ScheduleBaseline(Id);
        Baseline.Save(_items, today);
        return true;
    }

    /// <summary>
    /// Makes the schedule of a new project from a model (ADR-0017): its phases in order, and its items in the order of the
    /// model, each one starting as many business days after the start, on a business day, as it did in the schedule it
    /// came from, in its task and after the items it depends on, as any inclusion places it (ADR-0018, ADR-0019).
    /// </summary>
    /// <exception cref="InvalidOperationException">The schedule already has phases.</exception>
    public void ApplyTemplate(ProjectTemplate template, DateOnly start, IBusinessDays days, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(template);
        ApplyTemplate(template.ToOutline(), start, days, today);
    }

    /// <summary>
    /// Makes the schedule of a new project from an outline, of a model or of a file of the MS Project (ADR-0017,
    /// ADR-0135): its phases in order, and its items in order, each one starting as many business days after the start,
    /// on a business day, as the outline says, in its task and after the items it depends on, as any inclusion places it
    /// (ADR-0018, ADR-0019), with its owner or the client. Once every item has its dates, a task at 100% is completed on
    /// its end and a milestone at 100% reached on its day, never after today (ADR-0020, ADR-0086).
    /// </summary>
    /// <exception cref="InvalidOperationException">The schedule already has phases.</exception>
    /// <exception cref="ArgumentException">
    /// The outline points outside itself, has a dependency on or of a task with subtasks, or from an item that does not
    /// come before, or an item is not valid for its kind (as in <see cref="AddItem"/>).
    /// </exception>
    public void ApplyTemplate(SchedulePlanOutline outline, DateOnly start, IBusinessDays days, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(outline);
        ArgumentNullException.ThrowIfNull(days);
        if (_phases.Count > 0)
        {
            throw new InvalidOperationException("A model goes only into an empty schedule.");
        }

        CheckOutline(outline);
        var first = OnABusinessDay(start, days);
        var phases = outline.Phases.Select(phase => AddPhase(phase.Name).Id).ToList();
        var predecessors = outline.Dependencies.ToLookup(dependency => dependency.Successor, dependency => dependency.Predecessor);
        var links = outline.Dependencies.ToLookup(dependency => dependency.Successor);
        var items = new List<ScheduleItem>();
        foreach (var (item, index) in outline.Items.Select((item, index) => (item, index)))
        {
            if (!Enum.IsDefined(item.Kind))
            {
                throw new ArgumentException("Unknown kind of schedule item.", nameof(outline));
            }

            var plan = new ScheduleItemPlan(
                phases[item.Phase],
                item.Name,
                days.AddBusinessDays(first, item.StartOffset),
                item.Kind == ScheduleItemKind.Milestone ? 0 : item.Duration,
                item.OwnerId,
                Progress: 0,
                [.. predecessors[index].Distinct().Select(position => items[position].Id)],
                item.Parent is { } parent ? items[parent].Id : null,
                item.OwnedByClient,
                item.DeadlineOffset is { } deadline ? days.AddBusinessDays(first, deadline) : null,
                [.. links[index].Select(link => new ScheduleLink(items[link.Predecessor].Id, link.Type, link.Lag))]);
            // As an inclusion places it (AddItem), but once: the outline has every item after its task and the items
            // it depends on, and no dependency of a task with subtasks, so nothing is pushed nor passed on, and the
            // tasks with subtasks take their dates from them at the end. A file of 2,000 tasks stays quick.
            var made = new ScheduleItem(Id, item.Kind);
            var before = CheckedPredecessors(made.Id, plan);
            Apply(made, plan, today);
            _items.Add(made);
            foreach (var predecessor in before)
            {
                _dependencies.Add(NewDependency(predecessor, made.Id, plan));
            }

            made.Place(EarliestStart(made, plan.Start, days), days);
            items.Add(made);
        }

        Summarize(days);

        foreach (var (item, made) in outline.Items.Zip(items).Where(pair => pair.First.Progress > 0 && !IsSummary(pair.Second.Id)))
        {
            if (made.Kind == ScheduleItemKind.Task)
            {
                made.SetProgress(item.Progress, Earlier(made.End, today));
            }
            else
            {
                made.SetReached(true, Earlier(made.Start, today));
            }
        }

        SummarizeProgress();
    }

    private static DateOnly Earlier(DateOnly day, DateOnly other) => day < other ? day : other;

    /// <summary>
    /// The outline holds together: each item in a phase, each subtask in an item before it, each dependency from an item
    /// before the other, between items without subtasks, and each progress valid for the kind (100 for a milestone reached).
    /// </summary>
    private static void CheckOutline(SchedulePlanOutline outline)
    {
        var parents = outline.Items.Where(item => item.Parent is not null).Select(item => item.Parent!.Value).ToHashSet();
        var valid = outline.Items.Select((item, index) =>
                item.Phase >= 0 && item.Phase < outline.Phases.Count
                && (item.Parent is not { } parent || (parent >= 0 && parent < index))
                && item.StartOffset is >= 0 and <= MaxStartOffset
                && item.DeadlineOffset is null or (>= -MaxStartOffset and <= MaxStartOffset)
                && (item.Kind == ScheduleItemKind.Milestone ? item.Progress is 0 or 100 : IsValidProgress(item.Kind, item.Progress)))
            .All(ok => ok)
            && outline.Dependencies.All(dependency =>
                dependency.Predecessor >= 0 && dependency.Predecessor < dependency.Successor
                && dependency.Successor < outline.Items.Count
                && ScheduleLinks.IsValid(dependency.Type, dependency.Lag)
                && !parents.Contains(dependency.Predecessor) && !parents.Contains(dependency.Successor));
        if (!valid)
        {
            throw new ArgumentException("The outline does not hold together.", nameof(outline));
        }
    }

    /// <summary>
    /// Places again, in the calendar as it is now, each task and milestone with dates of its own that is not done
    /// (ADR-0126): in the order of the dependencies, it keeps its start, moved to the next business day when it is no
    /// longer one, and its duration in business days, and starts after the items it depends on, as any change places it.
    /// An item that now ends earlier pulls nobody (ADR-0019). A task at 100% and a milestone reached keep their dates,
    /// and the tasks with subtasks take theirs from them again (ADR-0088). A schedule made in the calendar as it is now
    /// does not change.
    /// </summary>
    /// <returns>How many tasks and milestones changed the start, the end or the duration.</returns>
    public int Recalculate(IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(days);
        var before = _items.ToDictionary(item => item.Id, item => (item.Start, item.End, item.Duration));
        foreach (var item in InDependencyOrder().Where(item => !item.IsDone))
        {
            item.Place(EarliestStart(item, item.Start, days), days);
        }

        Summarize(days);
        return _items.Count(item => before[item.Id] != (item.Start, item.End, item.Duration));
    }

    /// <summary>The dates, the progress and the delays of the schedule on a day (<see cref="ScheduleFigures"/>).</summary>
    public ScheduleFigures Figures(DateOnly today) => ScheduleFigures.Of(_phases, _items, today);

    /// <summary>The phases, items and dependencies as they are now, to compare with after a change (ADR-0091).</summary>
    public ScheduleSnapshot Snapshot() =>
        new([.. _phases.Select(phase => phase.State())],
            [.. _items.Select(item => item.State())],
            [.. _dependencies.Select(dependency => dependency.State())]);

    /// <summary>
    /// Applies a change of the schedule, to undo or redo it (ADR-0091): every record it touches must be as the change
    /// found it, and the schedule it leaves must hold together (each item in a phase, each subtask in a task, each
    /// dependency between items, without cycles). Then each record becomes as the change left it, with the same ids.
    /// False, changing nothing, otherwise: someone changed those records after it.
    /// </summary>
    public bool ApplyChange(ScheduleDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);
        var phases = _phases.ToDictionary(phase => phase.Id, phase => phase.State());
        var items = _items.ToDictionary(item => item.Id, item => item.State());
        var dependencies = _dependencies.ToDictionary(dependency => dependency.Id, dependency => dependency.State());
        if (!Found(diff.Phases, phases, phase => phase.Id)
            || !Found(diff.Items, items, item => item.Id)
            || !Found(diff.Dependencies, dependencies, dependency => dependency.Id))
        {
            return false;
        }

        Leave(diff.Phases, phases, phase => phase.Id);
        Leave(diff.Items, items, item => item.Id);
        Leave(diff.Dependencies, dependencies, dependency => dependency.Id);
        if (!HoldsTogether(phases, items, dependencies))
        {
            return false;
        }

        foreach (var change in diff.Dependencies)
        {
            var id = (change.Before ?? change.After)!.Id;
            _dependencies.RemoveAll(dependency => dependency.Id == id);
            if (change.After is { } state)
            {
                _dependencies.Add(new ScheduleDependency(Id, state));
            }
        }

        foreach (var change in diff.Items)
        {
            var id = (change.Before ?? change.After)!.Id;
            if (change.After is not { } state)
            {
                _items.RemoveAll(item => item.Id == id);
            }
            else if (ItemOf(id) is { } item)
            {
                item.Restore(state);
            }
            else
            {
                _items.Add(new ScheduleItem(Id, state));
            }
        }

        foreach (var change in diff.Phases)
        {
            var id = (change.Before ?? change.After)!.Id;
            if (change.After is not { } state)
            {
                _phases.RemoveAll(phase => phase.Id == id);
            }
            else if (PhaseOf(id) is { } phase)
            {
                phase.Rename(state.Name);
            }
            else
            {
                _phases.Add(new SchedulePhase(Id, state));
            }
        }

        return true;
    }

    /// <summary>Whether each record a change touches is now as the change found it.</summary>
    private static bool Found<T>(IEnumerable<RecordChange<T>> changes, Dictionary<Guid, T> now, Func<T, Guid> idOf)
        where T : class =>
        changes.All(change => Equals(now.GetValueOrDefault(idOf((change.Before ?? change.After)!)), change.Before));

    private static void Leave<T>(IEnumerable<RecordChange<T>> changes, Dictionary<Guid, T> now, Func<T, Guid> idOf)
        where T : class
    {
        foreach (var change in changes)
        {
            var id = idOf((change.Before ?? change.After)!);
            if (change.After is { } state)
            {
                now[id] = state;
            }
            else
            {
                now.Remove(id);
            }
        }
    }

    private static bool HoldsTogether(
        Dictionary<Guid, PhaseState> phases, Dictionary<Guid, ItemState> items, Dictionary<Guid, DependencyState> dependencies)
    {
        if (items.Values.Any(item => !phases.ContainsKey(item.PhaseId) || (item.ParentId is { } parent && !items.ContainsKey(parent)))
            || dependencies.Values.Any(dependency => !items.ContainsKey(dependency.PredecessorId) || !items.ContainsKey(dependency.SuccessorId)))
        {
            return false;
        }

        // No cycles: every item can be put after the ones before it.
        var waiting = items.Keys.ToDictionary(id => id, id => dependencies.Values.Count(dependency => dependency.SuccessorId == id));
        var successors = dependencies.Values.ToLookup(dependency => dependency.PredecessorId, dependency => dependency.SuccessorId);
        var ready = new Queue<Guid>(waiting.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        var placed = 0;
        while (ready.TryDequeue(out var id))
        {
            placed++;
            foreach (var successor in successors[id])
            {
                if (--waiting[successor] == 0)
                {
                    ready.Enqueue(successor);
                }
            }
        }

        return placed == items.Count;
    }

    private static string? Trimmed(string? text, int maxLength)
    {
        var trimmed = text?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > maxLength ? null : trimmed;
    }

    private static string NameOrThrow(string? valid, string name) =>
        valid ?? throw new ArgumentException($"'{name}' is no valid name.", nameof(name));

    /// <summary>The first day an item may start after another: the next business day for a task, the same day for a milestone.</summary>
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
            || (item.Kind == ScheduleItemKind.Milestone && (plan.OwnerId is not null || plan.OwnedByClient))
            || (plan.OwnerId is not null && plan.OwnedByClient)
            || (plan.Deadline is { } deadline && !IsValidDay(deadline))
            || (plan.ParentId is { } parentId && !MayHold(parentId, item.Id, plan.PhaseId)))
        {
            throw new ArgumentException("The plan is not valid for the kind of item.", nameof(plan));
        }

        var name = NameOrThrow(ValidItemName(plan.Name), plan.Name);
        item.Set(plan.PhaseId, name, summary ? item.Duration : plan.Duration, plan.OwnerId, plan.ParentId, plan.OwnedByClient);
        item.SetDeadline(plan.Deadline);
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

        if (plan.Links is { } links
            && (links.Any(link => !ScheduleLinks.IsValid(link.Type, link.Lag) || !predecessors.Contains(link.PredecessorId))
                || links.Select(link => link.PredecessorId).Distinct().Count() < links.Count))
        {
            throw new ArgumentException("Each link is of a known kind, with a lag from -99 to 999, on one item it depends on.", nameof(plan));
        }

        return predecessors;
    }

    /// <summary>The kind and the lag the plan asks for an item it depends on: "finishes, then starts" without lag by default.</summary>
    private static ScheduleLink LinkOf(Guid predecessorId, ScheduleItemPlan plan) =>
        plan.Links?.FirstOrDefault(link => link.PredecessorId == predecessorId) ?? new ScheduleLink(predecessorId);

    private ScheduleDependency NewDependency(Guid predecessorId, Guid successorId, ScheduleItemPlan plan)
    {
        var link = LinkOf(predecessorId, plan);
        return new ScheduleDependency(Id, predecessorId, successorId, link.Type, link.Lag);
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
                _dependencies.Add(new ScheduleDependency(Id, predecessor, successor, dependency.Type, dependency.Lag));
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

    /// <summary>The asked start on a business day, or later when the items before it allow only later (ADR-0137).</summary>
    private DateOnly EarliestStart(ScheduleItem item, DateOnly asked, IBusinessDays days)
    {
        var start = OnABusinessDay(asked, days);
        var earliest = EarliestByDependencies(item, days);
        return earliest is { } day && day > start ? OnABusinessDay(day, days) : start;
    }

    /// <summary>The latest of the earliest starts the dependencies of an item allow; none without dependencies.</summary>
    private DateOnly? EarliestByDependencies(ScheduleItem item, IBusinessDays days)
    {
        DateOnly? earliest = null;
        foreach (var dependency in _dependencies.Where(dependency => dependency.SuccessorId == item.Id))
        {
            var day = dependency.EarliestStart(Item(dependency.PredecessorId), item, days);
            if (earliest is null || day > earliest)
            {
                earliest = day;
            }
        }

        return earliest;
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
            if (EarliestByDependencies(item, days) is { } earliest && earliest > item.Start)
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

    /// <summary>The tasks and milestones with dates of their own, each one after every item it depends on.</summary>
    private List<ScheduleItem> InDependencyOrder()
    {
        var waiting = _items.Where(item => !IsSummary(item.Id)).ToDictionary(
            item => item.Id,
            item => _dependencies.Count(dependency => dependency.SuccessorId == item.Id));
        var ready = new Queue<ScheduleItem>(_items.Where(item => waiting.TryGetValue(item.Id, out var count) && count == 0).OrderBy(item => item.Start));
        var ordered = new List<ScheduleItem>();
        while (ready.TryDequeue(out var item))
        {
            ordered.Add(item);
            foreach (var successor in SuccessorIdsOf(item.Id).Where(waiting.ContainsKey))
            {
                waiting[successor]--;
                if (waiting[successor] == 0)
                {
                    ready.Enqueue(Item(successor));
                }
            }
        }

        return ordered;
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

    /// <summary>A phase back as it was, with its id (ADR-0091).</summary>
    internal SchedulePhase(Guid projectId, PhaseState state)
    {
        Id = state.Id;
        ProjectId = projectId;
        Name = state.Name;
        Position = state.Position;
    }

    public Guid ProjectId { get; private init; }

    public string Name { get; private set; }

    /// <summary>The order the phases were created in, for the phases still without items.</summary>
    public int Position { get; private init; }

    internal void Rename(string name) => Name = name;

    internal PhaseState State() => new(Id, Name, Position);
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

    /// <summary>A task or milestone back as it was, with its id (ADR-0091).</summary>
    internal ScheduleItem(Guid projectId, ItemState state)
    {
        Id = state.Id;
        ProjectId = projectId;
        Kind = state.Kind;
        Name = string.Empty;
        Restore(state);
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

    /// <summary>
    /// The client of the project is in charge of the task, as the tests or the approvals on its side, instead of a person
    /// of the project (ADR-0102); never for a milestone, nor with an owner.
    /// </summary>
    public bool OwnedByClient { get; private set; }

    /// <summary>The day the task reached 100%, in the time zone of the installation; null below 100%.</summary>
    public DateOnly? CompletedOn { get; private set; }

    /// <summary>The day the milestone was reached, in the time zone of the installation; null while it is not, and for a task.</summary>
    public DateOnly? ReachedOn { get; private set; }

    /// <summary>
    /// The day the task or milestone has to be done by, as a legal deadline: it never moves the dates, only says how far
    /// the item went past it (GanttA-2); none without one.
    /// </summary>
    public DateOnly? Deadline { get; private set; }

    /// <summary>
    /// A task is late when its end passed and it has not reached 100% (ADR-0020); a milestone, when its day passed and it
    /// was not reached (ADR-0086).
    /// </summary>
    public bool IsLate(DateOnly today) => End < today && !IsDone;

    /// <summary>A task at 100%, or a milestone reached.</summary>
    public bool IsDone => Kind == ScheduleItemKind.Task ? Progress == 100 : ReachedOn is not null;

    /// <summary>
    /// The day compared with the deadline: the day a task was completed or a milestone reached; while it is open, its
    /// end (the day of a milestone), or today when that passed, as a late item ends no sooner than today (ADR-0020).
    /// </summary>
    public DateOnly DoneOrDueOn(DateOnly today)
    {
        if (IsDone)
        {
            return Kind == ScheduleItemKind.Task ? CompletedOn ?? End : ReachedOn!.Value;
        }

        return End < today ? today : End;
    }

    /// <summary>
    /// The business days the item went past its deadline (positive) or is before it (negative), zero on it, in the
    /// calendar of the installation (ADR-0018); null without a deadline.
    /// </summary>
    public int? DeadlineDeviation(DateOnly today, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(days);
        if (Deadline is not { } deadline)
        {
            return null;
        }

        var day = DoneOrDueOn(today);
        return day > deadline
            ? days.CountBusinessDays(deadline.AddDays(1), day)
            : -days.CountBusinessDays(day.AddDays(1), deadline);
    }

    /// <summary>An open item past its deadline; a done one keeps its deviation, but is no longer missing it.</summary>
    public bool MissesDeadline(DateOnly today, IBusinessDays days) => !IsDone && DeadlineDeviation(today, days) > 0;

    /// <summary>
    /// The business days of delay on a day: from the day after the end up to the day, both included, in the calendar of
    /// the installation (ADR-0018); zero when it is not late, and while the delay began on non-business days only.
    /// </summary>
    public int DaysLate(DateOnly today, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(days);
        return IsLate(today) ? days.CountBusinessDays(End.AddDays(1), today) : 0;
    }

    internal void Set(Guid phaseId, string name, int duration, Guid? ownerId, Guid? parentId, bool ownedByClient = false)
    {
        PhaseId = phaseId;
        Name = name;
        Duration = duration;
        OwnerId = ownerId;
        ParentId = parentId;
        OwnedByClient = ownerId is null && ownedByClient;
    }

    internal void SetDeadline(DateOnly? deadline) => Deadline = deadline;

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

    internal ItemState State() =>
        new(Id, PhaseId, ParentId, Kind, Name, Start, Duration, End, Progress, OwnerId, CompletedOn, ReachedOn, OwnedByClient, Deadline);

    /// <summary>Every field as it was; the kind never changes.</summary>
    internal void Restore(ItemState state)
    {
        PhaseId = state.PhaseId;
        ParentId = state.ParentId;
        Name = state.Name;
        Start = state.Start;
        Duration = state.Duration;
        End = state.End;
        Progress = state.Progress;
        OwnerId = state.OwnerId;
        OwnedByClient = state.OwnedByClient;
        CompletedOn = state.CompletedOn;
        ReachedOn = state.ReachedOn;
        Deadline = state.Deadline;
    }
}

/// <summary>An item that starts only after another ends (ADR-0019).</summary>
public sealed class ScheduleDependency : Entity
{
    private ScheduleDependency()
    {
    }

    internal ScheduleDependency(
        Guid projectId,
        Guid predecessorId,
        Guid successorId,
        ScheduleLinkType type = ScheduleLinkType.FinishToStart,
        int lag = 0)
    {
        ProjectId = projectId;
        PredecessorId = predecessorId;
        SuccessorId = successorId;
        Type = type;
        Lag = lag;
    }

    /// <summary>A dependency back as it was, with its id (ADR-0091).</summary>
    internal ScheduleDependency(Guid projectId, DependencyState state)
        : this(projectId, state.PredecessorId, state.SuccessorId, state.Type, state.Lag)
    {
        Id = state.Id;
    }

    internal DependencyState State() => new(Id, PredecessorId, SuccessorId, Type, Lag);

    public Guid ProjectId { get; private init; }

    /// <summary>The item that ends first.</summary>
    public Guid PredecessorId { get; private init; }

    /// <summary>The item that starts after it.</summary>
    public Guid SuccessorId { get; private init; }

    /// <summary>How the item after it follows it (ADR-0137).</summary>
    public ScheduleLinkType Type { get; private set; } = ScheduleLinkType.FinishToStart;

    /// <summary>The business days of wait (positive) or of lead (negative), from -99 to 999 (ADR-0137).</summary>
    public int Lag { get; private set; }

    /// <summary>The earliest start of the item after it, by the kind and the lag.</summary>
    public DateOnly EarliestStart(ScheduleItem before, ScheduleItem next, IBusinessDays days) =>
        ScheduleLinks.EarliestStart(Type, Lag, before, next, days);

    internal void Set(ScheduleLinkType type, int lag)
    {
        Type = type;
        Lag = lag;
    }
}
