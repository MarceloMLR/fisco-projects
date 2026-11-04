using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using Microsoft.AspNetCore.Builder;

namespace FiscoProjects.Api.Tests;

/// <summary>
/// The team of ERP-FIN (<see cref="ProjectTeam"/>) with the sign-in through Entra ID simulated and each person linked to
/// an object of the tenant, as after signing in through Entra; Carla opens #1 for Desenvolvimento. The page in Teams
/// reads the tickets with the token Teams gives it (ADR-0152).
/// </summary>
internal sealed class TeamsScene : IAsyncDisposable
{
    private TeamsScene(ProjectTeam team, FakeEntra entra)
    {
        Team = team;
        Entra = entra;
    }

    public static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public ProjectTeam Team { get; }

    public FakeEntra Entra { get; }

    public SessionApp App => Team.App;

    /// <summary>The object of the person in the tenant, made from the user id.</summary>
    public static string ObjectOf(Guid userId) => $"0b{userId.ToString("N")[2..8]}-1a2b-4c3d-8e4f-{userId.ToString("N")[20..]}";

    /// <param name="sqlServer">The SQL Server of the tests.</param>
    /// <param name="entraOn">Whether the installation has the sign-in through Entra ID on.</param>
    /// <param name="configure">More settings of the application.</param>
    public static async Task<TeamsScene> StartAsync(
        SqlServerFixture sqlServer, bool entraOn = true, Action<WebApplicationBuilder>? configure = null)
    {
        var entra = new FakeEntra();
        var team = await ProjectTeam.StartAsync(sqlServer, builder =>
        {
            if (entraOn)
            {
                entra.Configure(builder);
            }

            configure?.Invoke(builder);
        });
        foreach (var person in new[] { team.Paulo, team.Marina, team.Rafael, team.Carla, team.Diego, team.Lucas, team.Sofia })
        {
            await team.App.ExecuteAsync(
                "INSERT INTO [users].[identity_links] (Id, UserId, Provider, ExternalId) VALUES (@id, @user, 'entra', @object)",
                ("@id", Guid.CreateVersion7()), ("@user", person), ("@object", ObjectOf(person)));
        }

        var scene = new TeamsScene(team, entra);
        await scene.OpenTicketAsync("Rejeição 539 ao emitir nota de devolução");
        return scene;
    }

    /// <summary>Carla opens a ticket for Desenvolvimento, with the description in bold, and gives its number.</summary>
    public async Task<int> OpenTicketAsync(string title)
    {
        var carla = await Team.ClientOfAsync(Team.Carla);
        var opened = await ProjectTeam.SendAsync(carla, HttpMethod.Post, "/api/tickets", new
        {
            projectId = Team.Erp,
            team = "Development",
            priority = "High",
            title,
            description = "ignored",
            descriptionBody = JsonSerializer.Serialize(new
            {
                type = "doc",
                content = new[] { new { type = "paragraph", content = new[] { new { type = "text", text = "A SEFAZ retorna a rejeição 539." } } } },
            }),
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        return (await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
    }

    /// <summary>A reading with the token of Teams of the person, as the page in Teams does, without any cookie.</summary>
    public Task<HttpResponseMessage> ReadAsync(Guid userId, string path, TeamsTokenShape? shape = null) =>
        ReadWithTokenAsync(Entra.TeamsToken(ObjectOf(userId), shape), path);

    /// <summary>A reading with the given token, without any cookie.</summary>
    public async Task<HttpResponseMessage> ReadWithTokenAsync(string token, string path)
    {
        using var client = App.NewClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request, Cancellation);
    }

    public ValueTask DisposeAsync() => Team.DisposeAsync();
}
