using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.SharedKernel;
using FiscoProjects.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FiscoProjects.Api.Tests;

// Spec: plataforma-api, requirement "Aquecimento depois de subir" (ADR-0106).
public sealed class WarmUpTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    [Fact(DisplayName = "Leituras principais aquecidas")]
    public async Task EveryModule_WarmsUpItsReads_WithoutChangingAnything()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync("Manager");
        var carla = await team.ClientOfAsync("Contributor");
        var phase = await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"/api/projects/{team.Erp}/schedule/phases", new { name = "Configuração" }));
        (await SendAsync(marina, HttpMethod.Post, $"/api/projects/{team.Erp}/schedule/items", new
        {
            kind = "Task",
            phaseId = phase,
            name = "Cadastro de clientes",
            start = "2026-10-19",
            duration = 5,
            ownerId = team.Diego,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
        })).StatusCode.Should().Be(HttpStatusCode.Created);
        (await SendAsync(carla, HttpMethod.Post, "/api/tickets", new
        {
            projectId = team.Erp,
            team = "Development",
            priority = "High",
            title = "Rejeição 539 ao emitir nota de devolução",
            description = "A SEFAZ retorna a rejeição 539.",
        })).StatusCode.Should().Be(HttpStatusCode.Created);
        (await SendAsync(marina, HttpMethod.Post, $"/api/projects/{team.Erp}/credentials", new
        {
            portal = "Portal da SEFAZ",
            environment = "Homologação",
            url = "https://sefaz.example",
            username = "erp.fin",
            password = "Fict1cia!",
        })).StatusCode.Should().Be(HttpStatusCode.Created);
        var before = await CountsAsync(team.App);

        var modules = new List<string>();
        await using (var scope = team.App.Services.CreateAsyncScope())
        {
            foreach (var warmUp in scope.ServiceProvider.GetServices<IWarmUp>())
            {
                // Each warm-up runs to the end: a failure here would only be logged in the API.
                await warmUp.RunAsync(Cancellation);
                modules.Add(warmUp.Module);
            }
        }

        modules.Should().BeEquivalentTo("Users", "Administration", "Projects", "Tickets", "Documents", "Credentials", "Notifications");
        // Only reads: no audit, no folder made, nothing marked as read.
        (await CountsAsync(team.App)).Should().Equal(before);
    }

    [Fact(DisplayName = "Aquecimento depois de subir")]
    public async Task WarmUp_RunsAfterTheStart_AndGoesOnAfterAFailure()
    {
        var reached = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var app = await SessionApp.StartAsync(sqlServer, builder =>
        {
            builder.Configuration[TestApp.WarmUpKey] = "true";
            builder.Services.AddScoped<IWarmUp>(_ => new FailingWarmUp());
            builder.Services.AddScoped<IWarmUp>(_ => new ReachedWarmUp(reached));
        });

        // The API answers while it warms up, and a module that fails does not stop the next ones.
        using var anonymous = app.NewClient();
        (await anonymous.GetAsync("/api/installation", Cancellation)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await reached.Task.WaitAsync(TimeSpan.FromSeconds(60), Cancellation)).Should().Be("Reached");
    }

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    /// <summary>The rows a read must never change.</summary>
    private static async Task<int[]> CountsAsync(SessionApp app) =>
    [
        await app.CountAsync("SELECT COUNT(*) FROM [audit].[entries]"),
        await app.CountAsync("SELECT COUNT(*) FROM [documents].[folders]"),
        await app.CountAsync("SELECT COUNT(*) FROM [notifications].[notifications] WHERE ReadAt IS NULL"),
    ];

    private sealed class FailingWarmUp : IWarmUp
    {
        public string Module => "Failing";

        public Task RunAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("The database is away.");
    }

    private sealed class ReachedWarmUp(TaskCompletionSource<string> reached) : IWarmUp
    {
        public string Module => "Reached";

        public Task RunAsync(CancellationToken cancellationToken)
        {
            reached.TrySetResult(Module);
            return Task.CompletedTask;
        }
    }
}
