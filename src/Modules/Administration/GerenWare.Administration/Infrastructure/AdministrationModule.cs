using GerenWare.Administration.Application;
using GerenWare.Administration.Contracts;
using GerenWare.Administration.Infrastructure.Persistence;
using GerenWare.Persistence;
using GerenWare.SharedKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GerenWare.Administration.Infrastructure;

/// <summary>Services of the Administration module: the settings of the installation (ADR-0003, ADR-0046).</summary>
public static class AdministrationModule
{
    public static IServiceCollection AddAdministrationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddModuleDbContext<AdministrationDbContext>(configuration);
        services.AddScoped<InstallationSettingsStore>();
        services.AddScoped<IInstallationSettingsStore>(provider => provider.GetRequiredService<InstallationSettingsStore>());
        services.AddScoped<IInstallationSettings>(provider => provider.GetRequiredService<InstallationSettingsStore>());
        services.AddScoped<GetInstallation>();
        services.AddScoped<GetInstallationLogo>();
        services.AddScoped<GetGeneralRules>();
        services.AddScoped<UpdateInstallation>();
        services.AddScoped<RenameInstallation>();
        services.AddScoped<ChangeProjectsFolder>();
        services.AddScoped<ChangeInstallationColors>();
        services.AddScoped<ReplaceInstallationLogo>();
        services.AddScoped<RemoveInstallationLogo>();
        // The main reads, once after the API starts (ADR-0106).
        services.AddScoped<IWarmUp, AdministrationWarmUp>();
        return services;
    }
}
