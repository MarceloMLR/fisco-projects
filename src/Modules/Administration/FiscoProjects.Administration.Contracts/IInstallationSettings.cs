namespace FiscoProjects.Administration.Contracts;

/// <summary>What other modules may read of the settings of the installation, such as the name for an e-mail.</summary>
public sealed record InstallationInfo(string Name);

/// <summary>The settings of the installation, kept by the Administration module (ADR-0003).</summary>
public interface IInstallationSettings
{
    Task<InstallationInfo> GetAsync(CancellationToken cancellationToken);
}
