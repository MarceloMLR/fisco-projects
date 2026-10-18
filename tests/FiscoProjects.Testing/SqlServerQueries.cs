using Microsoft.Data.SqlClient;

namespace FiscoProjects.Testing;

/// <summary>Small queries the tests use to look at the database from the outside.</summary>
public static class SqlServerQueries
{
    public static async Task<IReadOnlyList<(string Schema, string Table)>> TablesAsync(
        string connectionString, CancellationToken cancellationToken)
    {
        var rows = await QueryAsync(connectionString,
            "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES ORDER BY 1, 2",
            reader => (reader.GetString(0), reader.GetString(1)), cancellationToken);
        return rows;
    }

    public static Task<IReadOnlyList<string>> AppliedMigrationsAsync(
        string connectionString, string schema, CancellationToken cancellationToken) =>
        QueryAsync(connectionString,
            $"SELECT MigrationId FROM [{schema}].[__EFMigrationsHistory] ORDER BY MigrationId",
            reader => reader.GetString(0), cancellationToken);

    public static async Task<bool> DatabaseExistsAsync(string connectionString, CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var database = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = @name";
        command.Parameters.AddWithValue("@name", database);
        return (int)(await command.ExecuteScalarAsync(cancellationToken))! > 0;
    }

    private static async Task<IReadOnlyList<T>> QueryAsync<T>(
        string connectionString, string sql, Func<SqlDataReader, T> read, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var rows = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(read(reader));
        }

        return rows;
    }
}
