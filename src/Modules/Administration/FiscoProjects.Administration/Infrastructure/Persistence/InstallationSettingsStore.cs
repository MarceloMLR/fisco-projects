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

    async Task<InstallationInfo> IInstallationSettings.GetAsync(CancellationToken cancellationToken)
    {
        var name = await db.InstallationSettings.AsNoTracking()
            .Where(settings => settings.Id == InstallationSettings.SingleId)
            .Select(settings => settings.Name)
            .SingleAsync(cancellationToken);
        return new InstallationInfo(name);
    }
}
