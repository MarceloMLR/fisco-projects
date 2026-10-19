using FiscoProjects.Administration.Contracts;
using FiscoProjects.Calendar.Contracts;

namespace FiscoProjects.Projects.Tests;

/// <summary>
/// The installation of the tests, as the Calendar and Administration modules would tell it: the business days of
/// <see cref="TestBusinessDays.Of2026"/> and the time zone of São Paulo.
/// </summary>
internal sealed class TestInstallation : IWorkCalendar, IInstallationSettings
{
    public Task<IBusinessDays> GetAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IBusinessDays>(TestBusinessDays.Of2026);

    Task<InstallationInfo> IInstallationSettings.GetAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new InstallationInfo("Empresa de demonstração", "America/Sao_Paulo"));
}
