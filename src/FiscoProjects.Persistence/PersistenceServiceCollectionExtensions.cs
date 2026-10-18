using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiscoProjects.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>Name of the connection string shared by every module (one database per installation).</summary>
    public const string ConnectionStringName = "FiscoProjects";

    /// <summary>Services shared by all modules: the migrator used by the host and the deployment.</summary>
    public static IServiceCollection AddFiscoProjectsPersistence(this IServiceCollection services)
    {
        services.AddSingleton<ModuleMigrator>();
        return services;
    }

    /// <summary>
    /// Registers a module context: its own schema and migrations history, migrations in the module assembly,
    /// and the shared connection string.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services, IConfiguration configuration)
        where TContext : ModuleDbContext<TContext>, IModuleSchema
    {
        services.AddDbContext<TContext>(options =>
            ModuleDbContextOptions.Configure<TContext>(options, configuration.GetConnectionString(ConnectionStringName)));
        services.AddSingleton(new ModuleDbContextRegistration(typeof(TContext), TContext.Schema));
        return services;
    }
}
