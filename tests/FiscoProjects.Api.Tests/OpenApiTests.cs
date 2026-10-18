using System.Net;
using System.Text.Json;

namespace FiscoProjects.Api.Tests;

// Spec: plataforma-api, requirement "Contrato OpenAPI no desenvolvimento".
public sealed class OpenApiTests
{
    private const string DocumentPath = "/openapi/v1.json";

    [Fact(DisplayName = "Contrato no ambiente de desenvolvimento")]
    public async Task Development_PublishesOpenApi31Document()
    {
        await using var factory = TestApp.RealHost(TestApp.Development);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(DocumentPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        document.RootElement.GetProperty("openapi").GetString().Should().StartWith("3.1");
    }

    [Fact(DisplayName = "Contrato fora do desenvolvimento")]
    public async Task Production_DoesNotPublishOpenApiDocument()
    {
        await using var factory = TestApp.RealHost(TestApp.Production);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(DocumentPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.ShouldHaveMediaType(ResponseAssertions.ProblemJson);
    }
}
