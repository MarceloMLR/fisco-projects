using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace FiscoProjects.Api.Tests;

/// <summary>
/// Ways to run the API in tests: the real host (Program) or the same composition plus test-only routes.
/// </summary>
internal static class TestApp
{
    public const string Development = "Development";
    public const string Production = "Production";

    public static WebApplicationFactory<Program> RealHost(string environment) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(host => host.UseEnvironment(environment));

    public static async Task<WebApplication> ComposedAsync(string environment, Action<WebApplication> mapTestRoutes)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.AddFiscoProjectsApi();

        var app = builder.Build();
        app.UseFiscoProjectsApi();
        mapTestRoutes(app);

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }
}
