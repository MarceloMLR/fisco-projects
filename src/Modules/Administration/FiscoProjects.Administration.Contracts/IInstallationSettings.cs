namespace FiscoProjects.Administration.Contracts;

/// <summary>
/// What other modules may read of the settings of the installation: the name, for an e-mail, the IANA time zone, for
/// deadlines and the daily summary (ADR-0013), the time of the daily summary (ADR-0037), how many days the
/// notifications of the bell are kept (ADR-0038), how many years the audit is kept (ADR-0110) and the folder of the
/// projects, where the folder of each project is created in the file storage ("" for its root, ADR-0121).
/// </summary>
public sealed record InstallationInfo(
    string Name,
    string TimeZone,
    TimeOnly DailySummaryAt,
    int NotificationRetentionDays,
    int AuditRetentionYears = 5,
    string ProjectsFolder = "")
{
    /// <summary>The time zone of the installation; UTC when the system does not know it.</summary>
    public TimeZoneInfo Zone => TimeZoneInfo.TryFindSystemTimeZoneById(TimeZone, out var zone) ? zone : TimeZoneInfo.Utc;

    /// <summary>The day of a moment in the time zone of the installation (ADR-0013), such as today.</summary>
    public DateOnly DayOf(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone));

    /// <summary>
    /// The moment, in UTC, a day starts in the time zone of the installation (ADR-0013), such as the start of a period
    /// chosen in days; when the clock jumps over midnight, the first moment of the day that exists.
    /// </summary>
    public DateTime StartOf(DateOnly day)
    {
        var zone = Zone;
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        while (zone.IsInvalidTime(local))
        {
            local = local.AddMinutes(15);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }
}

/// <summary>The settings of the installation, kept by the Administration module (ADR-0003).</summary>
public interface IInstallationSettings
{
    Task<InstallationInfo> GetAsync(CancellationToken cancellationToken);
}
