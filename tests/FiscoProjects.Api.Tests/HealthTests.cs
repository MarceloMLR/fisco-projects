using System.Net;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: plataforma-api, requirement "Saúde da aplicação".
public sealed class HealthTests(SqlServerFixture sqlServer)
{
    [Fact(DisplayName = "Aplicação no ar")]
    public async Task Health_WithDatabaseReachable_ReturnsHealthy()
    {
        var connectionString = await sqlServer.CreateDatabaseAsync(TestContext.Current.CancellationToken);

        var (status, body) = await GetHealthAsync(connectionString);

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Be("Healthy");
    }

    [Fact(DisplayName = "Banco fora do ar")]
    public async Task Health_WithDatabaseDown_ReturnsUnhealthyWithoutDetails()
    {
        const string unreachable = "Server=127.0.0.1,1;Database=FiscoProjects;User Id=sa;Password=unused;TrustServerCertificate=True";

        var (status, body) = await GetHealthAsync(unreachable);

        status.Should().Be(HttpStatusCode.ServiceUnavailable);
        body.Should().Be("Unhealthy");
    }

    [Fact(DisplayName = "Banco não configurado")]
    public async Task Health_WithoutConnectionString_ReturnsUnhealthy()
    {
        var (status, body) = await GetHealthAsync(connectionString: null);

        status.Should().Be(HttpStatusCode.ServiceUnavailable);
        body.Should().Be("Unhealthy");
    }

    private static async Task<(HttpStatusCode Status, string Body)> GetHealthAsync(string? connectionString)
    {
        await using var factory = TestApp.RealHost(TestApp.Production, connectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiSetup.HealthPath, TestContext.Current.CancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
