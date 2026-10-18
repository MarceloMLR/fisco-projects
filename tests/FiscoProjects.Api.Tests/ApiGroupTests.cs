using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace FiscoProjects.Api.Tests;

// Spec: plataforma-api, requirement "Rotas dos módulos sob /api".
public sealed class ApiGroupTests
{
    [Fact(DisplayName = "Rota de um módulo")]
    public async Task ModuleRoute_IsServedUnderApiPrefix()
    {
        await using var app = await TestApp.ComposedAsync(TestApp.Production, testApp =>
            testApp.MapApiGroup().MapGet("/example", () => Results.Text("ok")).AllowAnonymous());
        using var client = app.GetTestClient();

        var underApi = await client.GetAsync("/api/example", TestContext.Current.CancellationToken);
        var withoutPrefix = await client.GetAsync("/example", TestContext.Current.CancellationToken);

        underApi.StatusCode.Should().Be(HttpStatusCode.OK);
        (await underApi.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("ok");
        withoutPrefix.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
