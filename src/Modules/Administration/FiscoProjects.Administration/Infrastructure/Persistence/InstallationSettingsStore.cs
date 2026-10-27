using FiscoProjects.Administration.Application;
using FiscoProjects.Administration.Contracts;
using FiscoProjects.Administration.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Administration.Infrastructure.Persistence;

internal sealed class InstallationSettingsStore(AdministrationDbContext db) : IInstallationSettingsStore, IInstallationSettings
{
    public Task<InstallationSettings> GetAsync(CancellationToken cancellationToken) =>
        db.InstallationSettings.SingleAsync(settings => settings.Id == InstallationSettings.SingleId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    Task<InstallationInfo> IInstallationSettings.GetAsync(CancellationToken cancellationToken) =>
        db.InstallationSettings.AsNoTracking()
            .Where(settings => settings.Id == InstallationSettings.SingleId)
            .Select(settings => new InstallationInfo(
                settings.Name,
                settings.TimeZone,
                settings.DailySummaryAt,
                settings.NotificationRetentionDays,
                settings.AuditRetentionYears,
                settings.ProjectsFolder))
            .SingleAsync(cancellationToken);
}
