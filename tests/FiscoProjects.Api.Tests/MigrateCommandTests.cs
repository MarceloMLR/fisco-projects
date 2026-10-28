using FiscoProjects.Persistence;
using FiscoProjects.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;

namespace FiscoProjects.Api.Tests;

// Spec: persistencia, requirement "Migrações pela publicação" (ADR-0125): the command of the production deployment, run
// with the real composition of the API against the SQL Server of the tests.
public sealed class MigrateCommandTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Migrações pelo comando da publicação")]
    public async Task Migrate_AppliesEveryModule_AndAgainChangesNothing()
    {
        // In production, the database comes from the infrastructure, empty.
        var connectionString = await sqlServer.CreateDatabaseAsync(Cancellation);

        var first = await MigrateAsync(connectionString);
        var tables = await SqlServerQueries.TablesAsync(connectionString, Cancellation);
        var again = await MigrateAsync(connectionString);

        first.Should().Be(0);
        again.Should().Be(0);
        tables.Should().Contain([("users", "users"), ("projects", "projects"), ("tickets", "tickets"), ("audit", "entries")]);
        (await SqlServerQueries.TablesAsync(connectionString, Cancellation)).Should().Equal(tables);
    }

    [Fact(DisplayName = "Comando de migração sem conexão")]
    public async Task Migrate_WithoutAConnection_Fails()
    {
        (await MigrateAsync(string.Empty)).Should().Be(1);
    }

    [Fact(DisplayName = "Acesso da aplicação ao banco")]
    public async Task AppAccess_ReadsWritesAndNumbersTickets_WithoutChangingTheDatabase()
    {
        var connectionString = await sqlServer.CreateDatabaseAsync(Cancellation);
        (await MigrateAsync(connectionString)).Should().Be(0);
        // Only Azure SQL has users of Entra ID: a user of SQL Server takes the place of the managed identity.
        var app = await UserAsync(connectionString);

        await DatabaseAccess.GrantDataAsync(connectionString, app.Name, Cancellation);
        await DatabaseAccess.GrantDataAsync(connectionString, app.Name, Cancellation);

        var permissions = await ScalarsAsync(app.ConnectionString,
            "SELECT HAS_PERMS_BY_NAME('users.users', 'OBJECT', 'SELECT')",
            "SELECT HAS_PERMS_BY_NAME('users.users', 'OBJECT', 'INSERT')",
            "SELECT HAS_PERMS_BY_NAME('users.users', 'OBJECT', 'UPDATE')",
            "SELECT HAS_PERMS_BY_NAME('users.users', 'OBJECT', 'DELETE')",
            "SELECT HAS_PERMS_BY_NAME(NULL, 'DATABASE', 'CREATE TABLE')",
            "SELECT HAS_PERMS_BY_NAME(NULL, 'DATABASE', 'ALTER')",
            "SELECT HAS_PERMS_BY_NAME('users', 'SCHEMA', 'ALTER')");
        permissions.Should().Equal(1, 1, 1, 1, 0, 0, 0);
        // The number of a new ticket comes from the sequence.
        (await ScalarsAsync(app.ConnectionString, "SELECT CAST(NEXT VALUE FOR [tickets].[ticket_numbers] AS int)"))
            .Should().Equal(1);
        var create = async () => await ScalarsAsync(app.ConnectionString, "CREATE TABLE [dbo].[forbidden] ([Id] int); SELECT 0");
        await create.Should().ThrowAsync<SqlException>();
    }

    /// <summary>Runs the command as the deployment does, with the composition of the API in production.</summary>
    private static async Task<int> MigrateAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = TestApp.Production });
        builder.Configuration[TestApp.ConnectionStringKey] = connectionString;
        builder.AddFiscoProjectsApi();
        await using var app = builder.Build();
        return await MigrateCommand.RunAsync(app.Services, Cancellation);
    }

    /// <summary>A user of the database without any access, with its own login.</summary>
    private static async Task<(string Name, string ConnectionString)> UserAsync(string connectionString)
    {
        var name = $"app_{Guid.NewGuid():N}";
        const string password = "Application_Login_1";
        await ScalarsAsync(new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" }.ConnectionString,
            $"CREATE LOGIN [{name}] WITH PASSWORD = '{password}'; SELECT 0");
        await ScalarsAsync(connectionString, $"CREATE USER [{name}] FOR LOGIN [{name}]; SELECT 0");
        var builder = new SqlConnectionStringBuilder(connectionString) { UserID = name, Password = password };
        return (name, builder.ConnectionString);
    }

    /// <summary>The integer each query gives.</summary>
    private static async Task<List<int>> ScalarsAsync(string connectionString, params string[] queries)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(Cancellation);
        var values = new List<int>();
        foreach (var query in queries)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = query;
            values.Add(Convert.ToInt32(await command.ExecuteScalarAsync(Cancellation), System.Globalization.CultureInfo.InvariantCulture));
        }

        return values;
    }
}
