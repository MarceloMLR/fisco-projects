using FiscoProjects.Administration.Infrastructure;
using FiscoProjects.Administration.Infrastructure.Persistence;
using FiscoProjects.Files.Contracts;
using FiscoProjects.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace FiscoProjects.Administration.Tests;

/// <summary>A migrated database with the Administration module registered as the host does, files in memory.</summary>
internal sealed class AdministrationDatabase : IAsyncDisposable
{
    private readonly TestDatabase _database;

    private AdministrationDatabase(TestDatabase database, FakeTimeProvider clock, MemoryFileStorage files)
    {
        _database = database;
        Clock = clock;
        Files = files;
    }

    public FakeTimeProvider Clock { get; }

    public MemoryFileStorage Files { get; }

    public static async Task<AdministrationDatabase> CreateAsync(SqlServerFixture sqlServer)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));
        var files = new MemoryFileStorage();
        var database = TestDatabase.Create(sqlServer.NewDatabase(), (services, configuration) =>
        {
            services.AddSingleton<TimeProvider>(clock);
            services.AddSingleton<IFileStorage>(files);
            services.AddAdministrationModule(configuration);
        });
        await database.MigrateAsync();
        return new AdministrationDatabase(database, clock, files);
    }

    /// <summary>Runs one use case in a scope of its own, as one request does.</summary>
    public async Task<TResult> RunAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> run)
        where TUseCase : notnull
    {
        await using var scope = _database.NewScope();
        return await run(scope.ServiceProvider.GetRequiredService<TUseCase>());
    }

    public async Task RunAsync<TUseCase>(Func<TUseCase, Task> run)
        where TUseCase : notnull
    {
        await using var scope = _database.NewScope();
        await run(scope.ServiceProvider.GetRequiredService<TUseCase>());
    }

    public AdministrationDbContext NewContext() => _database.NewContext<AdministrationDbContext>();

    public ValueTask DisposeAsync() => _database.DisposeAsync();
}
