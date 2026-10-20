namespace FiscoProjects.Administration.Contracts;

/// <summary>
/// What other modules may read of the settings of the installation: the name, for an e-mail, the IANA time zone, for
/// deadlines and the daily summary (ADR-0013), and the time of the daily summary (ADR-0037).
/// </summary>
public sealed record InstallationInfo(string Name, string TimeZone, TimeOnly DailySummaryAt);

/// <summary>The settings of the installation, kept by the Administration module (ADR-0003).</summary>
public interface IInstallationSettings
{
    Task<InstallationInfo> GetAsync(CancellationToken cancellationToken);
}
