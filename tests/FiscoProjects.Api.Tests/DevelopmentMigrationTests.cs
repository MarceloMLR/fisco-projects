using FiscoProjects.Persistence;
using FiscoProjects.Testing;
using FiscoProjects.Testing.Samples;
using Microsoft.AspNetCore.Builder;

namespace FiscoProjects.Api.Tests;

// Spec: persistencia, requirement "Migração automática só no desenvolvimento".
public sealed class DevelopmentMigrationTests(SqlServerFixture sqlServer)
{
    [Fact(DisplayName = "API sobe no desenvolvimento")]
    public async Task Development_AppliesPendingMigrationsOnStartup()
    {
        var connectionString = sqlServer.NewDatabase();

        await using var app = await StartWithAlphaModuleAsync(TestApp.Development, connectionString);

        var tables = await SqlServerQueries.TablesAsync(connectionString, TestContext.Current.CancellationToken);
        tables.Should().Contain(("alpha", "records"));
    }

    [Fact(DisplayName = "API sobe em produção")]
    public async Task Production_DoesNotTouchTheDatabaseOnStartup()
    {
        var connectionString = sqlServer.NewDatabase();

        await using var app = await StartWithAlphaModuleAsync(TestApp.Production, connectionString);

        (await SqlServerQueries.DatabaseExistsAsync(connectionString, TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    private static Task<WebApplication> StartWithAlphaModuleAsync(string environment, string connectionString) =>
        TestApp.ComposedAsync(environment, _ => { }, builder =>
        {
            builder.Configuration[TestApp.ConnectionStringKey] = connectionString;
            builder.Services.AddModuleDbContext<AlphaDbContext>(builder.Configuration);
        });
}
