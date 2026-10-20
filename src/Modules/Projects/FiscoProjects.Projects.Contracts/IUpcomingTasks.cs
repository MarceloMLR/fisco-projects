namespace FiscoProjects.Projects.Contracts;

/// <summary>A task with an owner that ends soon, for the daily summary of the owner (ADR-0037).</summary>
public sealed record UpcomingTask(Guid OwnerId, Guid ProjectId, string ProjectCode, string ProjectName, Guid ItemId, string Name, DateOnly End);

/// <summary>The tasks that end in a period, for the daily summary (ADR-0037).</summary>
public interface IUpcomingTasks
{
    /// <summary>
    /// The tasks not completed, with an owner, that end between the days (both included), in projects not completed nor
    /// cancelled; a task with subtasks is left out, as its subtasks are there.
    /// </summary>
    Task<IReadOnlyList<UpcomingTask>> EndingAsync(DateOnly first, DateOnly last, CancellationToken cancellationToken);
}
