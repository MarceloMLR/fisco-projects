using FiscoProjects.Testing;
using FiscoProjects.Users.Infrastructure;
using FiscoProjects.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace FiscoProjects.Users.Tests;

/// <summary>A migrated database with the Users module registered as the host does, and a clock the test controls.</summary>
internal sealed class UsersDatabase : IAsyncDisposable
{
    private readonly TestDatabase _database;

    private UsersDatabase(TestDatabase database, FakeTimeProvider clock)
    {
        _database = database;
        Clock = clock;
    }

    public FakeTimeProvider Clock { get; }

    public static async Task<UsersDatabase> CreateAsync(SqlServerFixture sqlServer)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));
        var database = TestDatabase.Create(sqlServer.NewDatabase(), (services, configuration) =>
        {
            services.AddSingleton<TimeProvider>(clock);
            services.AddUsersModule(configuration);
        });
        await database.MigrateAsync();
        return new UsersDatabase(database, clock);
    }

    /// <summary>Runs one use case in a scope of its own, as one request does.</summary>
    public async Task<TResult> RunAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> run)
        where TUseCase : notnull
    {
        await using var scope = _database.NewScope();
        return await run(scope.ServiceProvider.GetRequiredService<TUseCase>());
    }

    public UsersDbContext NewContext() => _database.NewContext<UsersDbContext>();

    public async Task DeactivateAsync(Guid userId)
    {
        await using var db = NewContext();
        await db.Users.Where(user => user.Id == userId)
            .ExecuteUpdateAsync(user => user.SetProperty(u => u.IsActive, false), TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();
}
