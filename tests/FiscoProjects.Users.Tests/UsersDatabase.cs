using FiscoProjects.Testing;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
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

    public static Task<UsersDatabase> CreateAsync(SqlServerFixture sqlServer, ProviderIdentity? firstAdministrator = null) =>
        OpenAsync(sqlServer.NewDatabase(), firstAdministrator);

    /// <summary>The same database, as the host would see it with another first administrator configured.</summary>
    public Task<UsersDatabase> WithFirstAdministratorAsync(ProviderIdentity firstAdministrator) =>
        OpenAsync(_database.ConnectionString, firstAdministrator);

    private static async Task<UsersDatabase> OpenAsync(string connectionString, ProviderIdentity? firstAdministrator)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));
        var database = TestDatabase.Create(connectionString, (services, configuration) =>
        {
            services.AddSingleton<TimeProvider>(clock);
            services.AddUsersModule(configuration);
            services.Configure<FirstAdministratorOptions>(options =>
            {
                options.Provider = firstAdministrator?.Provider;
                options.ExternalId = firstAdministrator?.ExternalId;
            });
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

    public async Task SetRoleAsync(Guid userId, SystemRole role)
    {
        await using var db = NewContext();
        await db.Users.Where(user => user.Id == userId)
            .ExecuteUpdateAsync(user => user.SetProperty(u => u.Role, role), TestContext.Current.CancellationToken);
    }

    public async Task<SystemRole> RoleOfAsync(Guid userId)
    {
        await using var db = NewContext();
        return await db.Users.Where(user => user.Id == userId).Select(user => user.Role)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();
}
