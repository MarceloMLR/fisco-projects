using GerenWare.Persistence;
using GerenWare.TimeTracking.Domain;
using Microsoft.EntityFrameworkCore;

namespace GerenWare.TimeTracking.Infrastructure.Persistence;

internal sealed class TimeTrackingDbContext(DbContextOptions<TimeTrackingDbContext> options)
    : ModuleDbContext<TimeTrackingDbContext>(options), IModuleSchema
{
    public static string Schema => "timetracking";

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    public DbSet<ContractedHours> ContractedHours => Set<ContractedHours>();

    public DbSet<AdditionalHours> AdditionalHours => Set<AdditionalHours>();

    public DbSet<PlannedHours> PlannedHours => Set<PlannedHours>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TimeTrackingDbContext).Assembly);
}

// For dotnet ef to create the context without starting the API.
internal sealed class TimeTrackingDbContextFactory : ModuleDesignTimeDbContextFactory<TimeTrackingDbContext>
{
    protected override TimeTrackingDbContext CreateContext(DbContextOptions<TimeTrackingDbContext> options) => new(options);
}
