using Microsoft.Data.SqlClient;

namespace FiscoProjects.Persistence;

/// <summary>
/// Creates the installation database when it does not exist yet. Used only in development: in Azure the
/// database is created by the infrastructure (ADR-0053).
/// </summary>
public static class DatabaseCreator
{
    /// <summary>SQL Server error "Cannot open database requested by the login".</summary>
    private const int CannotOpenDatabase = 4060;

    public static async Task EnsureExistsAsync(string connectionString, CancellationToken cancellationToken)
    {
        // A database that already exists opens directly, also for an identity that cannot see the other databases
        // from master, as the managed identity on Azure SQL (ADR-0114).
        if (await OpensAsync(connectionString, cancellationToken))
        {
            return;
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        var database = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            IF DB_ID(@name) IS NULL
            BEGIN
                DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@name);
                EXEC (@sql);
            END
            """;
        command.Parameters.AddWithValue("@name", database);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> OpensAsync(string connectionString, CancellationToken cancellationToken)
    {
        // Outside the pool, so that the failed attempt leaves nothing behind for the connections of the modules.
        var builder = new SqlConnectionStringBuilder(connectionString) { Pooling = false };
        try
        {
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch (SqlException exception) when (exception.Number == CannotOpenDatabase)
        {
            return false;
        }
    }
}
