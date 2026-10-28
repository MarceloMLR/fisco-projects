using FiscoProjects.Persistence;

namespace FiscoProjects.Api;

/// <summary>
/// <c>migrate</c>, the command of the production deployment (ADR-0125): with the services of the API built and the server
/// not started, it applies the pending migrations of every module with the configured connection and, with the managed
/// identity of the application in <c>Database:AppIdentity</c> (<c>Name</c> and <c>ObjectId</c>), gives it the reading
/// and the writing of the data. The background processes and the checks of the startup do not run.
/// </summary>
public static partial class MigrateCommand
{
    public const string Name = "migrate";

    public static bool IsRequested(IReadOnlyList<string> args) => args is [Name, ..];

    /// <summary>Runs the command and gives the exit code of the process: 0 when everything was applied.</summary>
    public static async Task<int> RunAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(MigrateCommand).FullName!);
        var connectionString = configuration.GetConnectionString(PersistenceServiceCollectionExtensions.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            LogNoConnection(logger);
            return 1;
        }

        var identity = configuration.GetSection("Database:AppIdentity");
        var name = identity["Name"];
        var objectId = Guid.Empty;
        if (!string.IsNullOrWhiteSpace(name) && !Guid.TryParse(identity["ObjectId"], out objectId))
        {
            LogNoObjectId(logger);
            return 1;
        }

        await services.GetRequiredService<ModuleMigrator>().MigrateAllAsync(cancellationToken);
        LogMigrated(logger);

        if (!string.IsNullOrWhiteSpace(name))
        {
            await DatabaseAccess.EnsureExternalUserAsync(connectionString, name, objectId, cancellationToken);
            await DatabaseAccess.GrantDataAsync(connectionString, name, cancellationToken);
            LogGranted(logger, name);
        }

        return 0;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "No database connection string configured: nothing was migrated.")]
    private static partial void LogNoConnection(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Database:AppIdentity:ObjectId is not the object id of the identity of the application.")]
    private static partial void LogNoObjectId(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "The migrations of every module are applied.")]
    private static partial void LogMigrated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "The identity {Name} reads and writes the data of the database.")]
    private static partial void LogGranted(ILogger logger, string name);
}
