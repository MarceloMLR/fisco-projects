using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiscoProjects.Persistence.Tests;

/// <summary>A database of its own for one test, with the module contexts registered as the host does.</summary>
internal sealed class TestDatabase : IAsyncDisposable
{
    private readonly ServiceProvider _services;

    private TestDatabase(string connectionString, ServiceProvider services)
    {
        ConnectionString = connectionString;
        _services = services;
    }

    public string ConnectionString { get; }

    public static TestDatabase Create(string connectionString, Action<IServiceCollection, IConfiguration> registerModules)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{PersistenceServiceCollectionExtensions.ConnectionStringName}"] = connectionString,
            })
            .Build();

        var services = new ServiceCollection().AddFiscoProjectsPersistence();
        registerModules(services, configuration);
        return new TestDatabase(connectionString, services.BuildServiceProvider());
    }

    public Task MigrateAsync() =>
        _services.GetRequiredService<ModuleMigrator>().MigrateAllAsync(TestContext.Current.CancellationToken);

    /// <summary>A fresh context, so reads come from the database and not from the change tracker.</summary>
    public TContext NewContext<TContext>()
        where TContext : notnull => _services.CreateScope().ServiceProvider.GetRequiredService<TContext>();

    public async Task<IReadOnlyList<(string Schema, string Table)>> TablesAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES ORDER BY 1, 2";
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        var tables = new List<(string, string)>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            tables.Add((reader.GetString(0), reader.GetString(1)));
        }

        return tables;
    }

    public async Task<IReadOnlyList<string>> AppliedMigrationsAsync(string schema)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT MigrationId FROM [{schema}].[__EFMigrationsHistory] ORDER BY MigrationId";
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        var migrations = new List<string>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            migrations.Add(reader.GetString(0));
        }

        return migrations;
    }

    public ValueTask DisposeAsync() => _services.DisposeAsync();
}
