using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Api.Tests;

// Spec: avisos, requirements "Aviso na aba Atividade", "Canal dos avisos na hora", "Fila dos avisos do Teams" and
// "Configuração do Teams conferida", with the sending by Microsoft Graph simulated (ADR-0056, ADR-0149, ADR-0151).
public sealed class GraphTeamsTests(SqlServerFixture sqlServer)
{
    private const string DiegoObject = "0f3c8a12-5b7e-4d21-9c44-7a1e2b3c4d5e";
    private const string CatalogId = "a7b1c2d3-e4f5-4a6b-8c9d-0e1f2a3b4c5d";
    private const string DiegoEmail = "diego.torres@empresa.example";
    private const string Assigned = "Chamado #1 atribuído a você: Rejeição 539 ao emitir nota de devolução";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private sealed record Scene(SessionApp App, FakeEntra Entra, Guid Marina, Guid Diego) : IAsyncDisposable
    {
        public FakeGraph Graph => Entra.Graph;

        public async Task AssignToDiegoAsync()
        {
            using var marina = await App.SignInAsync(Marina);
            (await SendAsync(marina, HttpMethod.Put, "/api/tickets/assignee", new { numbers = new[] { 1 }, assigneeId = Diego }))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        public async Task ChooseAsync(bool email, bool teams)
        {
            using var diego = await App.SignInAsync(Diego);
            (await SendAsync(diego, HttpMethod.Put, "/api/profile/immediate-notices", new { email, teams }))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        public ValueTask DisposeAsync() => App.DisposeAsync();
    }

    /// <summary>
    /// ERP-FIN, managed by Marina, with Diego as developer, who signs in by Entra unless told otherwise; Carla opens #1 for
    /// Desenvolvimento; the notices of Teams on, sent by the simulation of Graph, and Diego with the e-mail and Teams on.
    /// </summary>
    private async Task<Scene> StartAsync(bool diegoByEntra = true)
    {
        var entra = new FakeEntra();
        var app = await SessionApp.StartAsync(sqlServer, builder =>
        {
            entra.Configure(builder);
            builder.Configuration["Teams:Provider"] = "Graph";
            builder.Services.AddHttpClient("graph-teams").ConfigurePrimaryHttpMessageHandler(() => entra.Graph);
        }, recordTeams: false);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        var rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.TechLead);
        var carla = await app.CreateUserAsync("Carla Mendes", SystemRole.User);
        var diego = await app.CreateUserAsync("Diego Torres", SystemRole.User);
        if (diegoByEntra)
        {
            await app.ExecuteAsync(
                "INSERT INTO [users].[identity_links] (Id, UserId, Provider, ExternalId) VALUES (@id, @user, 'entra', @object)",
                ("@id", Guid.CreateVersion7()), ("@user", diego), ("@object", DiegoObject));
        }

        var created = await SendAsync(admin, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-FIN",
            name = "Implantação do ERP financeiro",
            managerId = marina,
            techLeadId = rafael,
        });
        var erp = (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        await app.AddProjectMemberAsync(erp, carla, "Contributor");
        await app.AddProjectMemberAsync(erp, diego, "Developer");
        using (var asCarla = await app.SignInAsync(carla))
        {
            (await SendAsync(asCarla, HttpMethod.Post, "/api/tickets", new
            {
                projectId = erp,
                team = "Development",
                priority = "High",
                title = "Rejeição 539 ao emitir nota de devolução",
                description = "A SEFAZ retorna a rejeição 539.",
            })).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        await app.EmailsAsync();
        await app.TurnTeamsOnAsync();
        await app.ChooseNoticesAsync(diego, email: true, teams: true);
        return new Scene(app, entra, marina, diego);
    }

    [Fact(DisplayName = "Link do aviso")]
    public async Task TheNotice_LeadsToThePageInTeams_WithTheItem()
    {
        await using var scene = await StartAsync();
        scene.Graph.TeamsApps[DiegoObject] = CatalogId;

        await scene.AssignToDiegoAsync();
        await scene.App.TeamsAsync();

        var body = scene.Graph.Activities.Should().ContainSingle().Which.Body;
        body.GetProperty("activityType").GetString().Should().Be("ticketAssigned");
        var topic = body.GetProperty("topic");
        topic.GetProperty("source").GetString().Should().Be("text");
        topic.GetProperty("value").GetString().Should().Be("ERP-FIN");
        var link = new Uri(topic.GetProperty("webUrl").GetString()!);
        link.Host.Should().Be("teams.microsoft.com");
        link.AbsolutePath.Should().Be($"/l/entity/{CatalogId}/home");
        var context = System.Web.HttpUtility.ParseQueryString(link.Query)["context"]!;
        JsonDocument.Parse(context).RootElement.GetProperty("subEntityId").GetString().Should().Be("/tickets/1");
        body.GetProperty("previewText").GetProperty("content").GetString().Should().Be("Rejeição 539 ao emitir nota de devolução");
        body.GetProperty("templateParameters").EnumerateArray()
            .ToDictionary(parameter => parameter.GetProperty("name").GetString()!, parameter => parameter.GetProperty("value").GetString()!)
            .Should().BeEquivalentTo(new Dictionary<string, string> { ["person"] = "Marina Costa", ["ticket"] = "#1" });
        scene.Graph.Requests.Should().Contain(request =>
            request.AbsolutePath == $"/v1.0/users/{DiegoObject}/teamwork/installedApps"
            && Uri.UnescapeDataString(request.Query).Contains("externalId eq '", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Pessoa sem o Entra ID")]
    public async Task WithoutEntra_TheEmail_AndNothingForTeams()
    {
        await using var scene = await StartAsync(diegoByEntra: false);

        await scene.AssignToDiegoAsync();

        (await scene.App.CountAsync("SELECT COUNT(*) FROM [notifications].[teams_notices]")).Should().Be(0);
        (await scene.App.EmailsToAsync(DiegoEmail)).Select(email => email.Subject).Should().Contain(Assigned);
        using var diego = await scene.App.SignInAsync(scene.Diego);
        (await diego.GetFromJsonAsync<JsonElement>("/api/profile/immediate-notices", Cancellation))
            .GetProperty("teamsAvailable").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "Teams ligado sem o Teams disponível")]
    public async Task OnlyTeams_WithoutEntra_OnlyTheBell()
    {
        await using var scene = await StartAsync(diegoByEntra: false);
        await scene.ChooseAsync(email: false, teams: true);

        await scene.AssignToDiegoAsync();

        (await scene.App.CountAsync("SELECT COUNT(*) FROM [notifications].[teams_notices]")).Should().Be(0);
        (await scene.App.EmailsToAsync(DiegoEmail)).Select(email => email.Subject).Should().NotContain(Assigned);
        using var diego = await scene.App.SignInAsync(scene.Diego);
        var bell = await diego.GetFromJsonAsync<JsonElement>("/api/notifications", Cancellation);
        bell.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("kind").GetString()).Should().Contain("TicketAssigned");
    }

    [Fact(DisplayName = "Aplicativo não instalado pelo Graph")]
    public async Task NotInstalled_ByGraph_FailedWithoutEmail()
    {
        await using var scene = await StartAsync();
        await scene.ChooseAsync(email: false, teams: true);

        await scene.AssignToDiegoAsync();
        await scene.App.TeamsAsync();

        scene.Graph.Activities.Should().BeEmpty();
        (await scene.App.CountAsync("SELECT COUNT(*) FROM [notifications].[teams_notices] WHERE Status = 'Failed'"))
            .Should().Be(1);
        (await scene.App.EmailsToAsync(DiegoEmail)).Select(email => email.Subject).Should().NotContain(Assigned);
    }

    [Fact(DisplayName = "Teams fora do ar")]
    public async Task GraphDown_TheNoticeWaits()
    {
        await using var scene = await StartAsync();
        scene.Graph.TeamsApps[DiegoObject] = CatalogId;
        await scene.AssignToDiegoAsync();
        scene.Graph.Down = true;

        await scene.App.TeamsAsync();

        (await scene.App.CountAsync(
                "SELECT COUNT(*) FROM [notifications].[teams_notices] WHERE Status = 'Pending' AND Attempts = 1 AND LastError LIKE '%503%'"))
            .Should().Be(1);
        scene.Graph.Down = false;
        scene.App.Clock.Advance(TimeSpan.FromMinutes(2));
        // The background process, woken by the clock, may be the one that sends it.
        await scene.App.TeamsAsync(until: _ => scene.Graph.Activities.Count > 0);
        scene.Graph.Activities.Should().ContainSingle();
    }

    [Fact(DisplayName = "Teams sem a entrada pelo Entra")]
    public async Task GraphWithoutEntra_DoesNotStart()
    {
        var start = () => TestApp.ComposedAsync(TestApp.Production, _ => { }, builder =>
        {
            builder.Configuration["Teams:Provider"] = "Graph";
            builder.Configuration["Email:SiteUrl"] = "https://projetos.empresa.example";
        });

        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain("needs the sign-in by Entra ID");
    }

    [Fact(DisplayName = "Implementação local do Teams em produção")]
    public async Task LocalTeams_DoesNotStartInProduction()
    {
        var start = () => TestApp.ComposedAsync(TestApp.Production, _ => { }, builder =>
        {
            builder.Configuration["Teams:Provider"] = "Local";
            builder.Configuration["Email:SiteUrl"] = "https://projetos.empresa.example";
        });

        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain("not allowed in production");
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF", "1");
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request, Cancellation);
    }
}
