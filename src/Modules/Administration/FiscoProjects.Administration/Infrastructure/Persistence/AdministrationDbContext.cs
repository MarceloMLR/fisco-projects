using FiscoProjects.Administration.Domain;
using FiscoProjects.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Administration.Infrastructure.Persistence;

internal sealed class AdministrationDbContext(DbContextOptions<AdministrationDbContext> options)
    : ModuleDbContext<AdministrationDbContext>(options), IModuleSchema
{
    public static string Schema => "administration";

    public DbSet<InstallationSettings> InstallationSettings => Set<InstallationSettings>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdministrationDbContext).Assembly);
}

// For dotnet ef to create the context without starting the API.
internal sealed class AdministrationDbContextFactory : ModuleDesignTimeDbContextFactory<AdministrationDbContext>
{
    protected override AdministrationDbContext CreateContext(DbContextOptions<AdministrationDbContext> options) => new(options);
}
