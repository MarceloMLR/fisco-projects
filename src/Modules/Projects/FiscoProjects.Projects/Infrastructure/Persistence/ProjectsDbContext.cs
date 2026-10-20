using FiscoProjects.Persistence;
using FiscoProjects.Projects.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

internal sealed class ProjectsDbContext(DbContextOptions<ProjectsDbContext> options)
    : ModuleDbContext<ProjectsDbContext>(options), IModuleSchema
{
    public static string Schema => "projects";

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<Schedule> Schedules => Set<Schedule>();

    public DbSet<SchedulePhase> SchedulePhases => Set<SchedulePhase>();

    public DbSet<ScheduleItem> ScheduleItems => Set<ScheduleItem>();

    public DbSet<ScheduleDependency> ScheduleDependencies => Set<ScheduleDependency>();

    public DbSet<ScheduleBaseline> ScheduleBaselines => Set<ScheduleBaseline>();

    public DbSet<ScheduleChange> ScheduleChanges => Set<ScheduleChange>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProjectsDbContext).Assembly);
}

// For dotnet ef to create the context without starting the API.
internal sealed class ProjectsDbContextFactory : ModuleDesignTimeDbContextFactory<ProjectsDbContext>
{
    protected override ProjectsDbContext CreateContext(DbContextOptions<ProjectsDbContext> options) => new(options);
}
