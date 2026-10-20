namespace FiscoProjects.Projects.Contracts;

/// <summary>What an item of a schedule is: a phase, a task (also a subtask) or a milestone.</summary>
public enum ScheduleItemType
{
    Phase = 1,
    Task = 2,
    Milestone = 3,
}

/// <summary>
/// A phase, task or milestone of a schedule, with its current name, as the other modules see it; a milestone with its
/// day and the day it was reached, if it was (ADR-0086).
/// </summary>
public sealed record ScheduleItemInfo(Guid Id, ScheduleItemType Type, string Name, DateOnly? Day = null, DateOnly? ReachedOn = null);

/// <summary>
/// The phases, tasks and milestones of the schedule of a project, for the modules that point to them, as the link of a
/// document (ADR-0031, ADR-0092).
/// </summary>
public interface IScheduleItems
{
    /// <summary>The phases, then the tasks and milestones, in the order of the schedule; none without a schedule.</summary>
    Task<IReadOnlyList<ScheduleItemInfo>> ItemsAsync(Guid projectId, CancellationToken cancellationToken);
}

/// <summary>The milestones of the schedules, for the modules whose items reach them, as the signed acceptance terms (ADR-0035).</summary>
public interface IScheduleMilestones
{
    /// <summary>
    /// Marks the milestone reached on the day of the installation, if it was not yet, in the transaction of the
    /// schedule and out of the undo of anyone (ADR-0091).
    /// </summary>
    /// <param name="projectId">The project.</param>
    /// <param name="milestoneId">The milestone.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    /// <returns>False when it is not a milestone of the schedule of the project (anymore).</returns>
    Task<bool> ReachAsync(Guid projectId, Guid milestoneId, CancellationToken cancellationToken);
}
