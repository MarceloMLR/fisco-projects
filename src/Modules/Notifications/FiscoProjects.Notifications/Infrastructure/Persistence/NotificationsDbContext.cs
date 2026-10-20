using FiscoProjects.Notifications.Domain;
using FiscoProjects.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Notifications.Infrastructure.Persistence;

internal sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : ModuleDbContext<NotificationsDbContext>(options), IModuleSchema
{
    public static string Schema => "notifications";

    public DbSet<OutboxEmail> OutboxEmails => Set<OutboxEmail>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
}

// For dotnet ef to create the context without starting the API.
internal sealed class NotificationsDbContextFactory : ModuleDesignTimeDbContextFactory<NotificationsDbContext>
{
    protected override NotificationsDbContext CreateContext(DbContextOptions<NotificationsDbContext> options) => new(options);
}
