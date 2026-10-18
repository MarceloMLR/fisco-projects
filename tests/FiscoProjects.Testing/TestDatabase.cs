using FiscoProjects.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FiscoProjects.Testing;

/// <summary>A database of its own for one test, with the module contexts registered as the host does.</summary>
public sealed class TestDatabase : IAsyncDisposable
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

    /// <summary>A scope of its own, as one request of the host has.</summary>
    public AsyncServiceScope NewScope() => _services.CreateAsyncScope();

    public Task<IReadOnlyList<(string Schema, string Table)>> TablesAsync() =>
        SqlServerQueries.TablesAsync(ConnectionString, TestContext.Current.CancellationToken);

    public Task<IReadOnlyList<string>> AppliedMigrationsAsync(string schema) =>
        SqlServerQueries.AppliedMigrationsAsync(ConnectionString, schema, TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => _services.DisposeAsync();
}
