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

    public const string ConnectionStringKey = "ConnectionStrings:FiscoProjects";

    public static WebApplicationFactory<Program> RealHost(
        string environment, string? connectionString = null, string? webRoot = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseEnvironment(environment);
            if (webRoot is not null)
            {
                host.UseWebRoot(webRoot);
            }

            // Always set, so a developer's user secrets never point a test at their local database.
            host.UseSetting(ConnectionStringKey, connectionString ?? string.Empty);
        });

    public static async Task<WebApplication> ComposedAsync(
        string environment, Action<WebApplication> mapTestRoutes, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        configure?.Invoke(builder);
        builder.AddFiscoProjectsApi();

        var app = builder.Build();
        app.UseFiscoProjectsApi();
        mapTestRoutes(app);

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }
}
