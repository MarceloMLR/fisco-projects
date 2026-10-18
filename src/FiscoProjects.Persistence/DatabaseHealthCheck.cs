using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FiscoProjects.Persistence;

/// <summary>Checks that the installation database answers a simple query (ADR-0057).</summary>
public sealed class DatabaseHealthCheck(IConfiguration configuration) : IHealthCheck
{
    private const int ConnectTimeoutSeconds = 5;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString(PersistenceServiceCollectionExtensions.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("The database connection string is not configured.");
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = ConnectTimeoutSeconds };
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("The database is not reachable.", exception);
        }
    }
}
