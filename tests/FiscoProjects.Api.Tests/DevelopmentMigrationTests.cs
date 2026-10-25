using FiscoProjects.Persistence;
using FiscoProjects.Testing;
using FiscoProjects.Testing.Samples;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;

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

    [Fact(DisplayName = "Banco ainda não criado no desenvolvimento")]
    public async Task Development_CreatesTheDatabaseEvenWithoutModules()
    {
        var connectionString = sqlServer.NewDatabase();

        await using var app = await TestApp.ComposedAsync(TestApp.Development, _ => { },
            builder => builder.Configuration[TestApp.ConnectionStringKey] = connectionString);

        (await SqlServerQueries.DatabaseExistsAsync(connectionString, TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact(DisplayName = "Banco criado pela infraestrutura")]
    public async Task Development_UsesTheDatabaseOfTheInfrastructure_WithoutSeeingTheOthers()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var connectionString = await sqlServer.CreateDatabaseAsync(cancellation);
        var limited = await LimitedLoginAsync(connectionString, cancellation);

        await using var app = await StartWithAlphaModuleAsync(TestApp.Development, limited);

        var tables = await SqlServerQueries.TablesAsync(connectionString, cancellation);
        tables.Should().Contain(("alpha", "records"));
    }

    [Fact(DisplayName = "API sobe em produção")]
    public async Task Production_DoesNotTouchTheDatabaseOnStartup()
    {
        var connectionString = sqlServer.NewDatabase();

        await using var app = await StartWithAlphaModuleAsync(TestApp.Production, connectionString);

        (await SqlServerQueries.DatabaseExistsAsync(connectionString, TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    /// <summary>
    /// A login that owns only this database and cannot see the others from master, as the managed identity of the
    /// application on Azure SQL (ADR-0114).
    /// </summary>
    private static async Task<string> LimitedLoginAsync(string connectionString, CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var login = $"app_{Guid.NewGuid():N}";
        const string password = "Limited_Login_1";
        await RunAsync(new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" }.ConnectionString,
            $"CREATE LOGIN [{login}] WITH PASSWORD = '{password}'; DENY VIEW ANY DATABASE TO [{login}];", cancellationToken);
        await RunAsync(connectionString, $"CREATE USER [{login}] FOR LOGIN [{login}]; ALTER ROLE db_owner ADD MEMBER [{login}];", cancellationToken);
        builder.UserID = login;
        builder.Password = password;
        return builder.ConnectionString;
    }

    private static async Task RunAsync(string connectionString, string sql, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Task<WebApplication> StartWithAlphaModuleAsync(string environment, string connectionString) =>
        TestApp.ComposedAsync(environment, _ => { }, builder =>
        {
            builder.Configuration[TestApp.ConnectionStringKey] = connectionString;
            builder.Services.AddModuleDbContext<AlphaDbContext>(builder.Configuration);
        });
}
