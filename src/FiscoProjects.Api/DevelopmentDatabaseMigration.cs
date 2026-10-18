using FiscoProjects.Persistence;

namespace FiscoProjects.Api;

/// <summary>
/// Applies the pending migrations of every module when the API starts in Development. Other environments
/// get their migrations from the deployment (ADR-0055), never from the running API.
/// </summary>
internal sealed class DevelopmentDatabaseMigration(ModuleMigrator migrator) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => migrator.MigrateAllAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
