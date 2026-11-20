namespace GerenWare.Projects.Contracts;

/// <summary>The state of the balance of the hours of a project (ADR-0128), as the status report shows it.</summary>
public enum ReportHoursState
{
    Over = 1,
    NearLimit = 2,
    Within = 3,
    NoContract = 4,
}

/// <summary>A task of the schedule with planned or logged hours (ADR-0138): its planned hours, if any, and the logged ones.</summary>
public sealed record ReportTaskHours(Guid ItemId, string Name, decimal? Planned, decimal Logged);

/// <summary>
/// The hours of a project for the status report (ADR-0166), only the totals: the contracted hours (null without them),
/// the additional and the logged ones, the balance (null without contracted hours) and its state; each task with planned
/// or logged hours, in the order of the schedule, and the hours logged out of the tasks. Never a person nor an entry.
/// </summary>
public sealed record ReportHours(
    decimal? Contracted,
    decimal Additional,
    decimal Logged,
    decimal? Balance,
    ReportHoursState State,
    IReadOnlyList<ReportTaskHours> Tasks,
    decimal Outside);

/// <summary>The hours of a project for the status report (ADR-0166); the TimeTracking module implements it.</summary>
public interface IProjectReportHours
{
    /// <summary>The totals of the project; null when the person may not show them, by the rule of the hours module.</summary>
    Task<ReportHours?> ForReportAsync(Guid projectId, Guid userId, bool isAdministrator, CancellationToken cancellationToken);
}
