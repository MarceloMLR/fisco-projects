using System.Net;

namespace FiscoProjects.Api.Tests;

// Spec: plataforma-api, requirement "Saúde da aplicação".
public sealed class HealthTests
{
    [Fact(DisplayName = "Aplicação no ar")]
    public async Task Health_WhenRunning_ReturnsHealthy()
    {
        await using var factory = TestApp.RealHost(TestApp.Production);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ApiSetup.HealthPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Be("Healthy");
    }
}
