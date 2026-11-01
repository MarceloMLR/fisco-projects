using FiscoProjects.SharedKernel;

namespace FiscoProjects.Projects.Domain;

/// <summary>A phase as it was at a moment, with every field of it.</summary>
public sealed record PhaseState(Guid Id, string Name, int Position);

/// <summary>A task or milestone as it was at a moment, with every field of it.</summary>
public sealed record ItemState(
    Guid Id,
    Guid PhaseId,
    Guid? ParentId,
    ScheduleItemKind Kind,
    string Name,
    DateOnly Start,
    int Duration,
    DateOnly End,
    int Progress,
    Guid? OwnerId,
    DateOnly? CompletedOn,
    DateOnly? ReachedOn,
    bool OwnedByClient = false,
    DateOnly? Deadline = null,
    string? Note = null);

/// <summary>A dependency as it was at a moment.</summary>
public sealed record DependencyState(
    Guid Id,
    Guid PredecessorId,
    Guid SuccessorId,
    ScheduleLinkType Type = ScheduleLinkType.FinishToStart,
    int Lag = 0);

/// <summary>The phases, items and dependencies of a schedule at a moment (ADR-0091).</summary>
public sealed record ScheduleSnapshot(
    IReadOnlyList<PhaseState> Phases,
    IReadOnlyList<ItemState> Items,
    IReadOnlyList<DependencyState> Dependencies);

/// <summary>A record before and after a change: null before when it was created, null after when it was removed.</summary>
public sealed record RecordChange<T>(T? Before, T? After)
    where T : class
{
    public RecordChange<T> Inverse() => new(After, Before);
}

/// <summary>
/// What a change of a schedule changed (ADR-0091): only the phases, items and dependencies that changed, each before and
/// after it. Applying it checks that every record is as it was before, and leaves it as it was after.
/// </summary>
public sealed record ScheduleDiff(
    IReadOnlyList<RecordChange<PhaseState>> Phases,
    IReadOnlyList<RecordChange<ItemState>> Items,
    IReadOnlyList<RecordChange<DependencyState>> Dependencies)
{
    public bool IsEmpty => Phases.Count == 0 && Items.Count == 0 && Dependencies.Count == 0;

    /// <summary>The records that differ between two moments of a schedule.</summary>
    public static ScheduleDiff Between(ScheduleSnapshot before, ScheduleSnapshot after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        return new ScheduleDiff(
            Changed(before.Phases, after.Phases, phase => phase.Id),
            Changed(before.Items, after.Items, item => item.Id),
            Changed(before.Dependencies, after.Dependencies, dependency => dependency.Id));
    }

    /// <summary>The same change the other way: what undoing it does.</summary>
    public ScheduleDiff Inverse() =>
        new([.. Phases.Select(change => change.Inverse())],
            [.. Items.Select(change => change.Inverse())],
            [.. Dependencies.Select(change => change.Inverse())]);

    private static List<RecordChange<T>> Changed<T>(IReadOnlyList<T> before, IReadOnlyList<T> after, Func<T, Guid> idOf)
        where T : class
    {
        var old = before.ToDictionary(idOf);
        var current = after.ToDictionary(idOf);
        var changes = new List<RecordChange<T>>();
        foreach (var (id, was) in old)
        {
            var now = current.GetValueOrDefault(id);
            if (!Equals(was, now))
            {
                changes.Add(new RecordChange<T>(was, now));
            }
        }

        changes.AddRange(current.Where(pair => !old.ContainsKey(pair.Key)).Select(pair => new RecordChange<T>(null, pair.Value)));
        return changes;
    }
}

/// <summary>The kind of a change of the schedule, for the screen to say what undoing or redoing it does (ADR-0091).</summary>
public enum ScheduleAction
{
    PhaseAdded = 1,
    PhaseRenamed = 2,
    PhaseRemoved = 3,
    TaskAdded = 4,
    MilestoneAdded = 5,
    TaskChanged = 6,
    MilestoneChanged = 7,
    TaskRemoved = 8,
    MilestoneRemoved = 9,
    DatesChanged = 10,
    ProgressUpdated = 11,
    MilestoneReached = 12,
    MilestoneUnmarked = 13,
}

/// <summary>What a change of the schedule was, by its kind and the name of the phase or item.</summary>
public sealed record ScheduleChangeLabel(ScheduleAction Action, string Name);

/// <summary>
/// A change of the schedule a person made, kept for the person to undo and redo it (ADR-0021, ADR-0091): its kind, the
/// name of the phase or item and what changed. The changes of a person in a project come in the order of their numbers.
/// </summary>
public sealed class ScheduleChange : Entity
{
    /// <summary>How many changes each person keeps in each project.</summary>
    public const int Kept = 20;

    private ScheduleChange()
    {
        Name = string.Empty;
        Diff = new ScheduleDiff([], [], []);
    }

    public ScheduleChange(Guid projectId, Guid actorId, int number, ScheduleChangeLabel label, ScheduleDiff diff)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(diff);
        ProjectId = projectId;
        ActorId = actorId;
        Number = number;
        Action = label.Action;
        Name = label.Name;
        Diff = diff;
    }

    public Guid ProjectId { get; private init; }

    /// <summary>The person who made it, the only one who undoes it.</summary>
    public Guid ActorId { get; private init; }

    /// <summary>The order of the changes of the person in the project: a later change has a greater number.</summary>
    public int Number { get; private init; }

    public ScheduleAction Action { get; private init; }

    public string Name { get; private init; }

    public ScheduleDiff Diff { get; private init; }

    /// <summary>Undone, waiting to be redone.</summary>
    public bool Undone { get; private set; }

    public ScheduleChangeLabel Label => new(Action, Name);

    /// <summary>Undoes it in the schedule; false, changing nothing, when someone changed its records after it.</summary>
    public bool UndoIn(Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        if (!schedule.ApplyChange(Diff.Inverse()))
        {
            return false;
        }

        Undone = true;
        return true;
    }

    /// <summary>Does it again in the schedule; false, changing nothing, when someone changed its records after the undo.</summary>
    public bool RedoIn(Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        if (!schedule.ApplyChange(Diff))
        {
            return false;
        }

        Undone = false;
        return true;
    }
}
