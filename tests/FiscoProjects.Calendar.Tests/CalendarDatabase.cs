using FiscoProjects.Calendar.Infrastructure;
using FiscoProjects.Calendar.Infrastructure.Persistence;
using FiscoProjects.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FiscoProjects.Calendar.Tests;

/// <summary>A migrated database with the Calendar module registered as the host does.</summary>
internal sealed class CalendarDatabase : IAsyncDisposable
{
    private readonly TestDatabase _database;

    private CalendarDatabase(TestDatabase database) => _database = database;

    public static async Task<CalendarDatabase> CreateAsync(SqlServerFixture sqlServer)
    {
        var database = TestDatabase.Create(sqlServer.NewDatabase(), (services, configuration) =>
            services.AddCalendarModule(configuration));
        await database.MigrateAsync();
        return new CalendarDatabase(database);
    }

    /// <summary>Runs one use case in a scope of its own, as one request does.</summary>
    public async Task<TResult> RunAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> run)
        where TUseCase : notnull
    {
        await using var scope = _database.NewScope();
        return await run(scope.ServiceProvider.GetRequiredService<TUseCase>());
    }

    public CalendarDbContext NewContext() => _database.NewContext<CalendarDbContext>();

    public Task<IReadOnlyList<(string Schema, string Table)>> TablesAsync() => _database.TablesAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();
}
