using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: usuarios-e-papeis, requirement "Diretório da Microsoft" (ADR-0119). Entra ID and Microsoft Graph are simulated
// inside the process (FakeEntra, FakeGraph).
public sealed class MicrosoftDirectoryTests(SqlServerFixture sqlServer)
{
    private const string HelenaId = "6f1e2d3c-4b5a-4978-8695-a4b3c2d1e0f9";
    private const string HelenaEmail = "helena.duarte@empresa.example";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Pessoas do diretório da Microsoft")]
    public async Task Search_BringsTheActiveMembersOfTheTenant()
    {
        var entra = Tenant();
        await using var app = await StartAsync(entra);
        var (client, _) = await app.SignedInClientAsync();

        var found = await client.GetFromJsonAsync<JsonElement>("/api/people?search=he", Cancellation);

        var helena = found.EnumerateArray().Single(person => person.GetProperty("name").GetString() == "Helena Duarte");
        helena.GetProperty("id").ValueKind.Should().Be(JsonValueKind.Null);
        helena.GetProperty("directoryId").GetString().Should().Be($"entra:{HelenaId}");
        helena.GetProperty("email").GetString().Should().Be(HelenaEmail);
        Names(found).Should().NotContain(["Helga Moura", "Heitor Lins"]);
    }

    [Fact(DisplayName = "Pessoa do diretório da Microsoft incluída antes de entrar")]
    public async Task PersonIncludedBeforeTheFirstSignIn_SignsInAsTheSameUser()
    {
        var entra = Tenant();
        await using var app = await StartAsync(entra);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        var created = await SendAsync(admin, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-FIN",
            name = "Implantação do ERP financeiro",
            description = (string?)null,
            clientId = (Guid?)null,
            managerId = marina,
            techLeadId = (Guid?)null,
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var projectId = (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        using var manager = await app.SignInAsync(marina);
        var found = await manager.GetFromJsonAsync<JsonElement>("/api/people?search=helena", Cancellation);
        var directoryId = found.EnumerateArray().Single().GetProperty("directoryId").GetString();

        var included = await SendAsync(manager, HttpMethod.Post, $"/api/projects/{projectId}/members",
            new { userId = (Guid?)null, directoryId, role = "Developer" });
        included.StatusCode.Should().Be(HttpStatusCode.Created);
        var helenaId = (await included.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        using var browser = app.NewClient();
        var (_, back) = await entra.SignInAsync(browser, FakeEntra.Member(HelenaId, "Helena Duarte", HelenaEmail));

        back.StatusCode.Should().Be(HttpStatusCode.Found);
        var session = await browser.GetFromJsonAsync<JsonElement>("/api/session", Cancellation);
        session.GetProperty("id").GetGuid().Should().Be(helenaId);
        (await browser.GetAsync($"/api/projects/{projectId}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await app.CountAsync("SELECT COUNT(*) FROM [users].[users]")).Should().Be(3, "no second user for Helena");
    }

    [Fact(DisplayName = "Diretório da Microsoft fora do ar")]
    public async Task GraphDown_TheSearchStillBringsTheUsers()
    {
        var entra = Tenant();
        entra.Graph.Down = true;
        await using var app = await StartAsync(entra);
        var (client, _) = await app.SignedInClientAsync();

        var response = await client.GetAsync("/api/people?search=marina", Cancellation);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Names(await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).Should().Contain("Marina Costa");
        entra.Graph.Requests.Should().NotBeEmpty();
    }

    /// <summary>The fictitious tenant: Helena, an active member; Helga, disabled; Heitor, a guest.</summary>
    private static FakeEntra Tenant()
    {
        var entra = new FakeEntra();
        entra.Graph.Users.Add(new FakeGraphUser(HelenaId, "Helena Duarte", HelenaEmail, HelenaEmail));
        entra.Graph.Users.Add(new FakeGraphUser(
            "1c2d3e4f-5a6b-4c7d-8e9f-0a1b2c3d4e5f", "Helga Moura", "helga.moura@empresa.example", "helga.moura@empresa.example", Enabled: false));
        entra.Graph.Users.Add(new FakeGraphUser(
            "2d3e4f5a-6b7c-4d8e-9f0a-1b2c3d4e5f6a", "Heitor Lins", "heitor@fornecedor.example", "heitor_fornecedor.example#EXT#@empresa.example", Guest: true));
        return entra;
    }

    private async Task<SessionApp> StartAsync(FakeEntra entra)
    {
        var app = await SessionApp.StartAsync(sqlServer, entra.Configure);
        app.Clock.SetUtcNow(DateTimeOffset.UtcNow);
        return app;
    }

    private static List<string?> Names(JsonElement people) =>
        [.. people.EnumerateArray().Select(person => person.GetProperty("name").GetString())];

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object body)
    {
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF", "1");
        return await client.SendAsync(request, Cancellation);
    }
}
