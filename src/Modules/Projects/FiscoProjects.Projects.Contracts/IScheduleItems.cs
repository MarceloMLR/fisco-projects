namespace FiscoProjects.Projects.Contracts;

/// <summary>What an item of a schedule is: a phase, a task (also a subtask) or a milestone.</summary>
public enum ScheduleItemType
{
    Phase = 1,
    Task = 2,
    Milestone = 3,
}

/// <summary>A phase, task or milestone of a schedule, with its current name, as the other modules see it.</summary>
public sealed record ScheduleItemInfo(Guid Id, ScheduleItemType Type, string Name);

/// <summary>
/// The phases, tasks and milestones of the schedule of a project, for the modules that point to them, as the link of a
/// document (ADR-0031, ADR-0092).
/// </summary>
public interface IScheduleItems
{
    /// <summary>The phases, then the tasks and milestones, in the order of the schedule; none without a schedule.</summary>
    Task<IReadOnlyList<ScheduleItemInfo>> ItemsAsync(Guid projectId, CancellationToken cancellationToken);
}
