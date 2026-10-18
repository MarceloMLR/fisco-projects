using System.Text.Json.Nodes;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: aplicacao-web, requirement "Tipos da API gerados do contrato".
public sealed class ApiContractTests
{
    [Fact(DisplayName = "Contrato versionado desatualizado")]
    public async Task VersionedContract_MatchesThePublishedOne()
    {
        await using var factory = TestApp.RealHost(TestApp.Development);
        using var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var published = JsonNode.Parse(await client.GetStringAsync("/openapi/v1.json", cancellationToken))!.AsObject();
        published.Remove("servers"); // depends on the address of the request, not on the API
        var versioned = JsonNode.Parse(await File.ReadAllTextAsync(
            RepositoryFiles.PathOf("web", "src", "api", "openapi.json"), cancellationToken));

        JsonNode.DeepEquals(published, versioned).Should().BeTrue(
            "the API changed: run `npm run api:update` in web/ and commit web/src/api");
    }
}
