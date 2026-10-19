using FiscoProjects.Files.Contracts;
using FiscoProjects.Projects.Infrastructure;
using FiscoProjects.Projects.Infrastructure.Persistence;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace FiscoProjects.Projects.Tests;

/// <summary>
/// A migrated database with the Projects module registered as the host does, files in memory and the people of the
/// installation in a list the test fills.
/// </summary>
internal sealed class ProjectsDatabase : IAsyncDisposable
{
    private readonly TestDatabase _database;

    private ProjectsDatabase(TestDatabase database, FakeTimeProvider clock, MemoryFileStorage files, PeopleOfTheTest people)
    {
        _database = database;
        Clock = clock;
        Files = files;
        People = people;
    }

    public FakeTimeProvider Clock { get; }

    public MemoryFileStorage Files { get; }

    public PeopleOfTheTest People { get; }

    public static async Task<ProjectsDatabase> CreateAsync(SqlServerFixture sqlServer)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));
        var files = new MemoryFileStorage();
        var people = new PeopleOfTheTest();
        var database = TestDatabase.Create(sqlServer.NewDatabase(), (services, configuration) =>
        {
            services.AddSingleton<TimeProvider>(clock);
            services.AddSingleton<IFileStorage>(files);
            services.AddSingleton<IUserAccess>(people);
            services.AddSingleton<IUserDirectory>(people);
            services.AddSingleton<IUserProvisioning>(people);
            services.AddProjectsModule(configuration);
        });
        await database.MigrateAsync();
        return new ProjectsDatabase(database, clock, files, people);
    }

    /// <summary>Runs one use case in a scope of its own, as one request does.</summary>
    public async Task<TResult> RunAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> run)
        where TUseCase : notnull
    {
        await using var scope = _database.NewScope();
        return await run(scope.ServiceProvider.GetRequiredService<TUseCase>());
    }

    public ProjectsDbContext NewContext() => _database.NewContext<ProjectsDbContext>();

    public ValueTask DisposeAsync() => _database.DisposeAsync();
}
