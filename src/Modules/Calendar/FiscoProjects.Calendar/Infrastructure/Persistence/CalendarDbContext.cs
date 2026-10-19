using FiscoProjects.Calendar.Domain;
using FiscoProjects.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Calendar.Infrastructure.Persistence;

internal sealed class CalendarDbContext(DbContextOptions<CalendarDbContext> options)
    : ModuleDbContext<CalendarDbContext>(options), IModuleSchema
{
    public static string Schema => "calendar";

    public DbSet<WorkCalendar> WorkCalendars => Set<WorkCalendar>();

    public DbSet<CalculatedHoliday> CalculatedHolidays => Set<CalculatedHoliday>();

    public DbSet<LocalHoliday> LocalHolidays => Set<LocalHoliday>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CalendarDbContext).Assembly);
}

// For dotnet ef to create the context without starting the API.
internal sealed class CalendarDbContextFactory : ModuleDesignTimeDbContextFactory<CalendarDbContext>
{
    protected override CalendarDbContext CreateContext(DbContextOptions<CalendarDbContext> options) => new(options);
}
