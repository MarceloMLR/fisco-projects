namespace GerenWare.Projects.Contracts;

/// <summary>
/// A task (also a summary or a subtask) or a milestone linked to a ticket, as the schedule has it now (ADR-0164): its
/// dates (a milestone starts and ends on its day), the progress (zero for a milestone), whether it is done and the
/// business days it is late, zero when it is not.
/// </summary>
public sealed record ImpactedScheduleItem(
    Guid Id, ScheduleItemType Type, string Name, DateOnly Start, DateOnly End, int Progress, bool IsDone, int LateDays);

/// <summary>A milestone not reached that comes after the linked items through the dependencies, with its day and delay.</summary>
public sealed record ThreatenedMilestone(Guid Id, string Name, DateOnly Day, int LateDays);

/// <summary>
/// What the items linked to a ticket reach in the schedule (ADR-0164): the ones that still exist, in the order of the
/// schedule, the milestones not reached after them, by the day, and whether one of them is on the critical path.
/// </summary>
public sealed record ScheduleImpact(
    IReadOnlyList<ImpactedScheduleItem> Items, IReadOnlyList<ThreatenedMilestone> Milestones, bool OnCriticalPath)
{
    public static ScheduleImpact None { get; } = new([], [], false);
}

/// <summary>
/// What the items linked to a ticket reach in the schedule of its project (ADR-0164), computed by the schedule on its
/// current dates (ADR-0051), for the Tickets module.
/// </summary>
public interface IScheduleImpacts
{
    /// <summary>What the items reach; the ids that are not a task or milestone of the project are left out.</summary>
    Task<ScheduleImpact> ImpactOfAsync(Guid projectId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken);
}
