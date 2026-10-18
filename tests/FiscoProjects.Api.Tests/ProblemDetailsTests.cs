using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace FiscoProjects.Api.Tests;

// Spec: plataforma-api, requirements "Erros no formato Problem Details" and "Erro inesperado sem detalhes internos".
public sealed class ProblemDetailsTests
{
    [Theory(DisplayName = "Rota inexistente")]
    [InlineData(TestApp.Development)]
    [InlineData(TestApp.Production)]
    public async Task UnknownRoute_ReturnsNotFoundProblemDetails(string environment)
    {
        await using var factory = TestApp.RealHost(environment);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/does-not-exist", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.ShouldHaveMediaType(ResponseAssertions.ProblemJson);
        using var problem = await ReadJsonAsync(response);
        problem.RootElement.GetProperty("status").GetInt32().Should().Be(404);
        problem.RootElement.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Theory(DisplayName = "Erro inesperado numa rota")]
    [InlineData(TestApp.Development)]
    [InlineData(TestApp.Production)]
    public async Task UnhandledException_ReturnsProblemDetailsWithoutInternals(string environment)
    {
        const string secretMessage = "internal secret detail";
        await using var app = await TestApp.ComposedAsync(environment, testApp =>
            testApp.MapGet("/test/failure", IResult () => throw new InvalidOperationException(secretMessage)));
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/test/failure", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.ShouldHaveMediaType(ResponseAssertions.ProblemJson);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().NotContain(secretMessage)
            .And.NotContain(nameof(InvalidOperationException))
            .And.NotContain("   at ");
        using var problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("status").GetInt32().Should().Be(500);
        problem.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
}
