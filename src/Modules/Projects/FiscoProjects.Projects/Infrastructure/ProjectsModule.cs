using FiscoProjects.Persistence;
using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FiscoProjects.Projects.Infrastructure;

/// <summary>Services of the Projects module: for now, the clients (ADR-0015).</summary>
public static class ProjectsModule
{
    public static IServiceCollection AddProjectsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddModuleDbContext<ProjectsDbContext>(configuration);
        services.AddScoped<IClientStore, ClientStore>();
        services.AddScoped<ListClients>();
        services.AddScoped<AddClient>();
        services.AddScoped<ChangeClient>();
        services.AddScoped<DeactivateClient>();
        services.AddScoped<ReactivateClient>();
        services.AddScoped<RemoveClient>();
        services.AddScoped<GetClientLogo>();
        services.AddScoped<ReplaceClientLogo>();
        services.AddScoped<RemoveClientLogo>();
        return services;
    }
}
