namespace FiscoProjects.Administration.Contracts;

/// <summary>
/// What other modules may read of the settings of the installation: the name, for an e-mail, and the IANA time zone,
/// for deadlines and the daily summary (ADR-0013).
/// </summary>
public sealed record InstallationInfo(string Name, string TimeZone);

/// <summary>The settings of the installation, kept by the Administration module (ADR-0003).</summary>
public interface IInstallationSettings
{
    Task<InstallationInfo> GetAsync(CancellationToken cancellationToken);
}
