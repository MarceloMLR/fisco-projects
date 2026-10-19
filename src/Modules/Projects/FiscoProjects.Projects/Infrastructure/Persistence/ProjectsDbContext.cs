using FiscoProjects.Persistence;
using FiscoProjects.Projects.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

internal sealed class ProjectsDbContext(DbContextOptions<ProjectsDbContext> options)
    : ModuleDbContext<ProjectsDbContext>(options), IModuleSchema
{
    public static string Schema => "projects";

    public DbSet<Client> Clients => Set<Client>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProjectsDbContext).Assembly);
}

// For dotnet ef to create the context without starting the API.
internal sealed class ProjectsDbContextFactory : ModuleDesignTimeDbContextFactory<ProjectsDbContext>
{
    protected override ProjectsDbContext CreateContext(DbContextOptions<ProjectsDbContext> options) => new(options);
}
