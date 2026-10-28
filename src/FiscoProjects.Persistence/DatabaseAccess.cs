using Microsoft.Data.SqlClient;

namespace FiscoProjects.Persistence;

/// <summary>
/// The access of the application to a database it does not administer, as in production (ADR-0114, ADR-0125): its
/// managed identity becomes a user of the database that reads and writes the data of every module, the tables and the
/// sequences of the migrations to come included, and changes nothing else in the database.
/// </summary>
public static class DatabaseAccess
{
    /// <summary>
    /// Creates, if missing, the user of an identity of Entra ID by its object id, so that the server needs no reading
    /// of the directory. Only Azure SQL has users of Entra ID.
    /// </summary>
    public static Task EnsureExternalUserAsync(
        string connectionString, string name, Guid objectId, CancellationToken cancellationToken) =>
        RunAsync(connectionString, """
            IF DATABASE_PRINCIPAL_ID(@name) IS NULL
            BEGIN
                DECLARE @sql nvarchar(max) = N'CREATE USER ' + QUOTENAME(@name)
                    + N' FROM EXTERNAL PROVIDER WITH OBJECT_ID = ' + QUOTENAME(@objectId, N'''');
                EXEC (@sql);
            END
            """, [("@name", name), ("@objectId", objectId.ToString("D"))], cancellationToken);

    /// <summary>Gives a user of the database the reading and the writing of the data, and nothing else; again, nothing changes.</summary>
    public static Task GrantDataAsync(string connectionString, string name, CancellationToken cancellationToken) =>
        RunAsync(connectionString, """
            DECLARE @sql nvarchar(max) =
                N'ALTER ROLE db_datareader ADD MEMBER ' + QUOTENAME(@name) + N'; '
                + N'ALTER ROLE db_datawriter ADD MEMBER ' + QUOTENAME(@name) + N';';
            EXEC (@sql);
            """, [("@name", name)], cancellationToken);

    private static async Task RunAsync(
        string connectionString, string sql, (string Name, string Value)[] parameters, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
