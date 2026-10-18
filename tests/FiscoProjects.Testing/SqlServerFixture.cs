using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace FiscoProjects.Testing;

/// <summary>
/// One SQL Server container per test project (use it as an xUnit assembly fixture). Each test gets its own
/// database name, so tests never see each other's data.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    /// <summary>Same image as compose.yaml, so tests and local development run the same SQL Server.</summary>
    public const string Image = "mcr.microsoft.com/mssql/server:2022-CU27-GDR1-ubuntu-22.04";

    private readonly MsSqlContainer _container = new MsSqlBuilder(Image).Build();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    /// <summary>Connection string to an empty database of its own, already created.</summary>
    public async Task<string> CreateDatabaseAsync(CancellationToken cancellationToken)
    {
        var connectionString = NewDatabase();
        var builder = new SqlConnectionStringBuilder(connectionString);
        var database = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{database}]";
        await command.ExecuteNonQueryAsync(cancellationToken);
        return connectionString;
    }

    /// <summary>Connection string to a database of its own. The database is created by the first migration.</summary>
    public string NewDatabase()
    {
        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = $"test_{Guid.NewGuid():N}",
        };
        return builder.ConnectionString;
    }
}
