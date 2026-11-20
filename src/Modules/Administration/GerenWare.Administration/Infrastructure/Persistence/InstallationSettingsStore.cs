using GerenWare.Administration.Application;
using GerenWare.Administration.Contracts;
using GerenWare.Administration.Domain;
using Microsoft.EntityFrameworkCore;

namespace GerenWare.Administration.Infrastructure.Persistence;

internal sealed class InstallationSettingsStore(AdministrationDbContext db)
    : IInstallationSettingsStore, IInstallationSettings, IInstallationBrand
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
                settings.ProjectsFolder,
                settings.WorkHoursPerDay))
            .SingleAsync(cancellationToken);

    Task<InstallationBrand> IInstallationBrand.GetAsync(CancellationToken cancellationToken) =>
        db.InstallationSettings.AsNoTracking()
            .Where(settings => settings.Id == InstallationSettings.SingleId)
            .Select(settings => new InstallationBrand(
                settings.Name, settings.LogoFileId, settings.LogoContentType, settings.MainColor, settings.SecondaryColor))
            .SingleAsync(cancellationToken);
}
