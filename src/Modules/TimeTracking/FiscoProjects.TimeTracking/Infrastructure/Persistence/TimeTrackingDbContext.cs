using FiscoProjects.Persistence;
using FiscoProjects.TimeTracking.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.TimeTracking.Infrastructure.Persistence;

internal sealed class TimeTrackingDbContext(DbContextOptions<TimeTrackingDbContext> options)
    : ModuleDbContext<TimeTrackingDbContext>(options), IModuleSchema
{
    public static string Schema => "timetracking";

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TimeTrackingDbContext).Assembly);
}

// For dotnet ef to create the context without starting the API.
internal sealed class TimeTrackingDbContextFactory : ModuleDesignTimeDbContextFactory<TimeTrackingDbContext>
{
    protected override TimeTrackingDbContext CreateContext(DbContextOptions<TimeTrackingDbContext> options) => new(options);
}
