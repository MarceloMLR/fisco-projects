using FiscoProjects.Administration.Application;
using FiscoProjects.Administration.Contracts;
using FiscoProjects.Administration.Infrastructure.Persistence;
using FiscoProjects.Persistence;
using FiscoProjects.SharedKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FiscoProjects.Administration.Infrastructure;

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
        services.AddScoped<ChangeInstallationColors>();
        services.AddScoped<ReplaceInstallationLogo>();
        services.AddScoped<RemoveInstallationLogo>();
        // The main reads, once after the API starts (ADR-0106).
        services.AddScoped<IWarmUp, AdministrationWarmUp>();
        return services;
    }
}
