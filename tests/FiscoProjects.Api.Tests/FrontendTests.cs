using System.Net;

namespace FiscoProjects.Api.Tests;

// Spec: aplicacao-web, requirement "Frontend servido pela aplicação".
public sealed class FrontendTests : IDisposable
{
    private readonly FakeFrontendBuild build = new();

    [Fact(DisplayName = "Página inicial")]
    public async Task Root_ReturnsTheMainPage_WithoutCache()
    {
        var response = await GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ShouldHaveMediaType("text/html");
        response.Headers.CacheControl!.NoCache.Should().BeTrue();
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain(FakeFrontendBuild.IndexMarker);
    }

    [Fact(DisplayName = "Rota do frontend")]
    public async Task ScreenPath_ReturnsTheMainPage()
    {
        var response = await GetAsync("/projects/123");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.ShouldHaveMediaType("text/html");
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain(FakeFrontendBuild.IndexMarker);
    }

    [Fact(DisplayName = "Arquivo do build")]
    public async Task BuildFile_IsCachedForAYear_WithoutRevalidation()
    {
        var response = await GetAsync(FakeFrontendBuild.AssetPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.Public.Should().BeTrue();
        response.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromDays(365));
        response.Headers.CacheControl.Extensions.Select(e => e.Name).Should().Contain("immutable");
    }

    public void Dispose() => build.Dispose();

    private async Task<HttpResponseMessage> GetAsync(string path)
    {
        await using var factory = TestApp.RealHost(TestApp.Production, webRoot: build.Folder);
        using var client = factory.CreateClient();
        return await client.GetAsync(path, TestContext.Current.CancellationToken);
    }
}
