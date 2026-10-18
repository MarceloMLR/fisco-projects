using FiscoProjects.Audit.Contracts;
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

    private UsersDatabase(TestDatabase database, FakeTimeProvider clock, RecordedSecurityEvents securityEvents)
    {
        _database = database;
        Clock = clock;
        SecurityEvents = securityEvents;
    }

    public FakeTimeProvider Clock { get; }

    /// <summary>The security events the module reported to the audit.</summary>
    public RecordedSecurityEvents SecurityEvents { get; }

    public static Task<UsersDatabase> CreateAsync(SqlServerFixture sqlServer, ProviderIdentity? firstAdministrator = null) =>
        OpenAsync(sqlServer.NewDatabase(), firstAdministrator);

    /// <summary>The same database, as the host would see it with another first administrator configured.</summary>
    public Task<UsersDatabase> WithFirstAdministratorAsync(ProviderIdentity firstAdministrator) =>
        OpenAsync(_database.ConnectionString, firstAdministrator);

    private static async Task<UsersDatabase> OpenAsync(string connectionString, ProviderIdentity? firstAdministrator)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));
        var securityEvents = new RecordedSecurityEvents();
        var database = TestDatabase.Create(connectionString, (services, configuration) =>
        {
            services.AddSingleton<TimeProvider>(clock);
            services.AddSingleton<ISecurityEvents>(securityEvents);
            services.AddUsersModule(configuration);
            services.Configure<FirstAdministratorOptions>(options =>
            {
                options.Provider = firstAdministrator?.Provider;
                options.ExternalId = firstAdministrator?.ExternalId;
            });
        });
        await database.MigrateAsync();
        return new UsersDatabase(database, clock, securityEvents);
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

/// <summary>Stands in for the Audit module: keeps the security events in the order they came.</summary>
internal sealed class RecordedSecurityEvents : ISecurityEvents
{
    public List<string> Events { get; } = [];

    public Task SignedInAsync(Guid userId, string provider, CancellationToken cancellationToken)
    {
        Events.Add($"SignedIn {userId} {provider}");
        return Task.CompletedTask;
    }

    public Task SignInRefusedAsync(string identity, SignInRefusal reason, CancellationToken cancellationToken)
    {
        Events.Add($"SignInRefused {identity} {reason}");
        return Task.CompletedTask;
    }
}
