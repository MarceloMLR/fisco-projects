using FiscoProjects.Audit.Infrastructure;
using FiscoProjects.Audit.Infrastructure.Persistence;
using FiscoProjects.Persistence;
using FiscoProjects.SharedKernel;
using FiscoProjects.Testing;
using FiscoProjects.Testing.Samples;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace FiscoProjects.Audit.Tests;

/// <summary>
/// A migrated database with the Audit module registered as the host does, a sample module (gamma) whose saves
/// are audited, a directory of names, which also names the people the gamma links point to, and a clock the test
/// controls. The gamma records have no source of names.
/// </summary>
internal sealed class AuditDatabase : IAsyncDisposable
{
    private readonly TestDatabase _database;

    private AuditDatabase(TestDatabase database, FakeTimeProvider clock, Dictionary<Guid, string> names)
    {
        _database = database;
        Clock = clock;
        Names = names;
    }

    public FakeTimeProvider Clock { get; }

    /// <summary>The people the directory knows, by id.</summary>
    public Dictionary<Guid, string> Names { get; }

    public static async Task<AuditDatabase> CreateAsync(SqlServerFixture sqlServer)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));
        var names = new Dictionary<Guid, string>();
        var database = TestDatabase.Create(sqlServer.NewDatabase(), (services, configuration) =>
        {
            services.AddSingleton<TimeProvider>(clock);
            services.AddSingleton<IUserDirectory>(new Directory(names));
            services.AddSingleton<IAuditReferenceNames>(new PersonNames(new Directory(names)));
            services.AddAuditModule(configuration);
            services.AddModuleDbContext<GammaDbContext>(configuration);
        });
        await database.MigrateAsync();
        return new AuditDatabase(database, clock, names);
    }

    /// <summary>
    /// Runs as one request does, in a scope of its own; with a user, as that person's session.
    /// </summary>
    public async Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> run, Guid? sessionUser = null)
        where TService : notnull
    {
        await using var scope = _database.NewScope();
        var http = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        http.HttpContext = sessionUser is null ? null : new DefaultHttpContext { User = UserSession.For(sessionUser.Value) };
        try
        {
            return await run(scope.ServiceProvider.GetRequiredService<TService>());
        }
        finally
        {
            http.HttpContext = null;
        }
    }

    public Task RunAsync<TService>(Func<TService, Task> run, Guid? sessionUser = null)
        where TService : notnull =>
        RunAsync<TService, bool>(async service =>
        {
            await run(service);
            return true;
        }, sessionUser);

    public AuditDbContext NewAuditContext() => _database.NewContext<AuditDbContext>();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    private sealed class PersonNames(IUserDirectory directory) : IAuditReferenceNames
    {
        public string Kind => GammaDbContext.PersonReference;

        public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            directory.NamesAsync(ids, cancellationToken);
    }

    private sealed class Directory(Dictionary<Guid, string> names) : IUserDirectory
    {
        public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                names.Where(name => userIds.Contains(name.Key)).ToDictionary(name => name.Key, name => name.Value));
    }
}
