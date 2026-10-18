using FiscoProjects.Persistence;

namespace FiscoProjects.Api;

/// <summary>
/// In Development, creates the database if needed and applies the pending migrations of every module when the
/// API starts. Other environments get their migrations from the deployment (ADR-0055), never from the running API.
/// </summary>
internal sealed partial class DevelopmentDatabaseMigration(
    ModuleMigrator migrator, IConfiguration configuration, ILogger<DevelopmentDatabaseMigration> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString(PersistenceServiceCollectionExtensions.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            LogMissingConnectionString(logger);
            return;
        }

        await DatabaseCreator.EnsureExistsAsync(connectionString, cancellationToken);
        await migrator.MigrateAllAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "No database connection string configured; skipping the development migrations. See CLAUDE.md, section 6.")]
    private static partial void LogMissingConnectionString(ILogger logger);
}
