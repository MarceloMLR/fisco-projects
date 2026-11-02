using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Notifications.Application;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FiscoProjects.Api.Tests;

// Spec: avisos, requirements "Avisos na hora pelo Teams", "Canal dos avisos na hora", "Volta ao e-mail", "Fila dos
// avisos do Teams" and "Aviso na aba Atividade"; perfil, "Avisos no perfil"; configuracao-da-instalacao, "Aplicativo do
// Teams" and "Avisar pelo Teams"; dados-pessoais, "Avisos do Teams na anonimização" (ADR-0149). The local sending of the
// development, with the notices kept in memory by the test.
public sealed class TeamsTests(SqlServerFixture sqlServer)
{
    private const string DiegoEmail = "diego.torres@empresa.example";
    private const string Assigned = "Chamado #1 atribuído a você: Rejeição 539 ao emitir nota de devolução";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>
    /// ERP-FIN, created by Paulo (the Administrator) and managed by Marina, with Rafael as tech lead, Carla as contributor
    /// and Diego as developer; Carla opens #1 for Desenvolvimento.
    /// </summary>
    private sealed record Scene(
        SessionApp App, HttpClient Admin, Guid Marina, Guid Rafael, Guid Carla, Guid Diego, Guid Erp) : IAsyncDisposable
    {
        public async Task AssignToDiegoAsync()
        {
            using var marina = await App.SignInAsync(Marina);
            (await SendAsync(marina, HttpMethod.Put, "/api/tickets/assignee", new { numbers = new[] { 1 }, assigneeId = Diego }))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        public async Task ChooseAsync(Guid person, string channel)
        {
            using var client = await App.SignInAsync(person);
            (await SendAsync(client, HttpMethod.Put, "/api/profile/immediate-notices", new { channel }))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        public async Task<IReadOnlyList<TeamsDelivery>> TeamsToAsync(Guid person) =>
            [.. (await App.TeamsAsync()).Where(delivery => delivery.UserId == person)];

        public ValueTask DisposeAsync() => App.DisposeAsync();
    }

    private async Task<Scene> StartAsync(bool teamsOn = true, Action<WebApplicationBuilder>? configure = null)
    {
        var app = await SessionApp.StartAsync(sqlServer, configure);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        var rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.TechLead);
        var carla = await app.CreateUserAsync("Carla Mendes", SystemRole.User);
        var diego = await app.CreateUserAsync("Diego Torres", SystemRole.User);
        var created = await SendAsync(admin, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-FIN",
            name = "Implantação do ERP financeiro",
            managerId = marina,
            techLeadId = rafael,
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync(Cancellation));
        var erp = (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        await app.AddProjectMemberAsync(erp, carla, "Contributor");
        await app.AddProjectMemberAsync(erp, diego, "Developer");
        using (var asCarla = await app.SignInAsync(carla))
        {
            var opened = await SendAsync(asCarla, HttpMethod.Post, "/api/tickets", new
            {
                projectId = erp,
                team = "Development",
                priority = "High",
                title = "Rejeição 539 ao emitir nota de devolução",
                description = "A SEFAZ retorna a rejeição 539.",
            });
            opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        }

        // The notices of the opening went before Teams was on.
        await app.EmailsAsync();
        if (teamsOn)
        {
            await app.TurnTeamsOnAsync();
        }

        return new Scene(app, admin, marina, rafael, carla, diego, erp);
    }

    [Fact(DisplayName = "Chamado atribuído no Teams")]
    public async Task Assignment_GoesToTheActivityOfTeams()
    {
        await using var scene = await StartAsync();

        await scene.AssignToDiegoAsync();

        (await scene.TeamsToAsync(scene.Diego)).Should().ContainSingle()
            .Which.Message.ActivityType.Should().Be("ticketAssigned");
    }

    [Fact(DisplayName = "Texto do aviso")]
    public async Task TheNotice_SaysWhoDidWhat_WithTheProjectAndTheTitle()
    {
        await using var scene = await StartAsync();

        await scene.AssignToDiegoAsync();

        var message = (await scene.TeamsToAsync(scene.Diego)).Single().Message;
        message.Parameters.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["person"] = "Marina Costa",
            ["ticket"] = "#1",
        });
        message.Topic.Should().Be("ERP-FIN");
        message.Preview.Should().Be("Rejeição 539 ao emitir nota de devolução");
        message.Path.Should().Be("/tickets/1");
    }

    [Fact(DisplayName = "Quem fez a ação não recebe no Teams")]
    public async Task WhoActs_HearsNothingInTeams()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);

        await AnswerAsync(diego, "Vou olhar o XML.");

        var sent = await scene.App.TeamsAsync();
        sent.Should().Contain(delivery => delivery.UserId == scene.Carla && delivery.Message.ActivityType == "ticketMessage");
        sent.Should().NotContain(delivery => delivery.UserId == scene.Diego);
    }

    [Fact(DisplayName = "Aviso do resumo fora do Teams")]
    public async Task NoticeOfTheSummary_NeverGoesToTeams()
    {
        await using var scene = await StartAsync();
        using var rafael = await scene.App.SignInAsync(scene.Rafael);

        (await SendAsync(rafael, HttpMethod.Post, "/api/tickets/1/transitions", new { action = "StartTriage" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await scene.App.TeamsAsync()).Should().BeEmpty();
        (await scene.App.CountAsync("SELECT COUNT(*) FROM [notifications].[digest_items] WHERE Kind = 'TicketStatusChanged'"))
            .Should().BePositive();
    }

    [Fact(DisplayName = "Teams desligado na instalação")]
    public async Task TeamsOff_OnlyTheEmail()
    {
        await using var scene = await StartAsync(teamsOn: false);
        await scene.ChooseAsync(scene.Diego, "Teams");

        await scene.AssignToDiegoAsync();

        (await scene.App.TeamsAsync()).Should().BeEmpty();
        (await scene.App.EmailsToAsync(DiegoEmail)).Select(email => email.Subject).Should().Contain(Assigned);
    }

    [Fact(DisplayName = "Só por e-mail")]
    public async Task EmailChosen_NothingInTeams()
    {
        await using var scene = await StartAsync();
        await scene.ChooseAsync(scene.Diego, "Email");

        await scene.AssignToDiegoAsync();

        (await scene.TeamsToAsync(scene.Diego)).Should().BeEmpty();
        (await scene.App.EmailsToAsync(DiegoEmail)).Select(email => email.Subject).Should().Contain(Assigned);
    }

    [Fact(DisplayName = "Só pelo Teams")]
    public async Task TeamsChosen_NoEmail()
    {
        await using var scene = await StartAsync();
        await scene.ChooseAsync(scene.Diego, "Teams");

        await scene.AssignToDiegoAsync();

        (await scene.TeamsToAsync(scene.Diego)).Should().ContainSingle();
        (await scene.App.EmailsToAsync(DiegoEmail)).Select(email => email.Subject).Should().NotContain(Assigned);
    }

    [Fact(DisplayName = "Pelos dois")]
    public async Task NeverChose_BothChannels()
    {
        await using var scene = await StartAsync();

        await scene.AssignToDiegoAsync();

        (await scene.TeamsToAsync(scene.Diego)).Should().ContainSingle();
        (await scene.App.EmailsToAsync(DiegoEmail)).Select(email => email.Subject).Should().Contain(Assigned);
    }

    [Fact(DisplayName = "Sino com qualquer canal")]
    public async Task TeamsChosen_TheBellStill()
    {
        await using var scene = await StartAsync();
        await scene.ChooseAsync(scene.Diego, "Teams");

        await scene.AssignToDiegoAsync();

        using var diego = await scene.App.SignInAsync(scene.Diego);
        var bell = await diego.GetFromJsonAsync<JsonElement>("/api/notifications", Cancellation);
        bell.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("kind").GetString()).Should().Contain("TicketAssigned");
    }

    [Fact(DisplayName = "Ação sem esperar o Teams")]
    public async Task TeamsDown_TheActionIsSaved_AndTheNoticeWaits()
    {
        await using var scene = await StartAsync();
        scene.App.Teams.Down = true;

        await scene.AssignToDiegoAsync();

        (await scene.App.TeamsAsync()).Should().BeEmpty();
        (await scene.App.CountAsync(
                "SELECT COUNT(*) FROM [notifications].[teams_notices] WHERE UserId = @user AND Status = 'Pending' AND Attempts = 1",
                ("@user", scene.Diego)))
            .Should().Be(1);
    }

    [Fact(DisplayName = "Aplicativo não instalado para a pessoa")]
    public async Task NotInstalled_TheEmailInstead()
    {
        await using var scene = await StartAsync();
        await scene.ChooseAsync(scene.Diego, "Teams");
        scene.App.Teams.NotInstalled[scene.Diego] = true;

        await scene.AssignToDiegoAsync();
        await scene.App.TeamsAsync();

        (await scene.App.EmailsToAsync(DiegoEmail)).Select(email => email.Subject).Should().Contain(Assigned);
        (await scene.App.CountAsync(
                "SELECT COUNT(*) FROM [notifications].[teams_notices] WHERE Status = 'Failed' AND EmailedInstead = 1"))
            .Should().Be(1);
    }

    [Fact(DisplayName = "Teams recusa o envio")]
    public async Task Refused_FailedAndTheEmailInstead()
    {
        await using var scene = await StartAsync();
        await scene.ChooseAsync(scene.Diego, "Teams");
        scene.App.Teams.Refusing = true;

        await scene.AssignToDiegoAsync();
        await scene.App.TeamsAsync();

        (await scene.App.CountAsync("SELECT COUNT(*) FROM [notifications].[teams_notices] WHERE Status = 'Failed'")).Should().Be(1);
        (await scene.App.EmailsToAsync(DiegoEmail)).Select(email => email.Subject).Should().Contain(Assigned);
    }

    [Fact(DisplayName = "Recusa com os dois canais")]
    public async Task RefusedWithBoth_OnlyTheEmailThatWent()
    {
        await using var scene = await StartAsync();
        scene.App.Teams.Refusing = true;

        await scene.AssignToDiegoAsync();
        await scene.App.TeamsAsync();

        (await scene.App.CountAsync(
                "SELECT COUNT(*) FROM [notifications].[teams_notices] WHERE Status = 'Failed' AND EmailedInstead = 0"))
            .Should().Be(1);
        (await scene.App.EmailsToAsync(DiegoEmail)).Count(email => email.Subject == Assigned).Should().Be(1);
    }

    [Fact(DisplayName = "Fila limpa com o tempo")]
    public async Task OldNotices_LeaveTheQueue()
    {
        await using var scene = await StartAsync();
        await scene.AssignToDiegoAsync();
        (await scene.App.TeamsAsync()).Should().ContainSingle();

        scene.App.Clock.Advance(TimeSpan.FromDays(31));
        await using (var scope = scene.App.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ForgetOldTeamsNotices>().ExecuteAsync(Cancellation);
        }

        (await scene.App.CountAsync("SELECT COUNT(*) FROM [notifications].[teams_notices]")).Should().Be(0);
    }

    [Fact(DisplayName = "Projeto excluído")]
    public async Task DeletedProject_TakesItsNotices()
    {
        await using var scene = await StartAsync();
        scene.App.Teams.Down = true;
        await scene.AssignToDiegoAsync();
        (await scene.App.CountAsync("SELECT COUNT(*) FROM [notifications].[teams_notices]")).Should().Be(1);

        var deleted = await SendAsync(scene.Admin, HttpMethod.Post, $"/api/projects/{scene.Erp}/deletion", new { code = "ERP-FIN" });

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleted.Content.ReadAsStringAsync(Cancellation));
        (await scene.App.CountAsync("SELECT COUNT(*) FROM [notifications].[teams_notices]")).Should().Be(0);
    }

    [Fact(DisplayName = "Escolher o Teams")]
    public async Task ChoosingTeams_IsKept()
    {
        await using var scene = await StartAsync();

        await scene.ChooseAsync(scene.Diego, "Teams");

        using var diego = await scene.App.SignInAsync(scene.Diego);
        var notices = await diego.GetFromJsonAsync<JsonElement>("/api/profile/immediate-notices", Cancellation);
        notices.GetProperty("channel").GetString().Should().Be("Teams");
        notices.GetProperty("teamsAvailable").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "Canal de quem nunca escolheu")]
    public async Task NeverChose_Both()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);

        var notices = await diego.GetFromJsonAsync<JsonElement>("/api/profile/immediate-notices", Cancellation);

        notices.GetProperty("channel").GetString().Should().Be("Both");
        notices.GetProperty("teamsAvailable").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "Canal fora da lista")]
    public async Task UnknownChannel_IsRefused()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);

        var chosen = await SendAsync(diego, HttpMethod.Put, "/api/profile/immediate-notices", new { channel = 7 });

        chosen.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Ligar os avisos pelo Teams")]
    public async Task TurningOn_IsKept_AndAudited()
    {
        await using var scene = await StartAsync(teamsOn: false);

        (await SendAsync(scene.Admin, HttpMethod.Put, "/api/admin/teams-app", new { noticesOn = true }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var app = await scene.Admin.GetFromJsonAsync<JsonElement>("/api/admin/teams-app", Cancellation);
        app.GetProperty("available").GetBoolean().Should().BeTrue();
        app.GetProperty("noticesOn").GetBoolean().Should().BeTrue();
        (await scene.App.CountAsync("SELECT COUNT(*) FROM [audit].[entries] WHERE ItemType = 'TeamsApp' AND Action = 'Updated'"))
            .Should().Be(1);
        await scene.AssignToDiegoAsync();
        (await scene.TeamsToAsync(scene.Diego)).Should().ContainSingle();
    }

    [Fact(DisplayName = "Ligar sem o Teams na instalação")]
    public async Task TurningOn_WithoutTeams_IsRefused()
    {
        await using var scene = await StartAsync(teamsOn: false, configure: builder => builder.Configuration["Teams:Provider"] = string.Empty);

        var changed = await SendAsync(scene.Admin, HttpMethod.Put, "/api/admin/teams-app", new { noticesOn = true });

        changed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await changed.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").TryGetProperty("noticesOn", out _).Should().BeTrue();
        var app = await scene.Admin.GetFromJsonAsync<JsonElement>("/api/admin/teams-app", Cancellation);
        app.GetProperty("available").GetBoolean().Should().BeFalse();
        app.GetProperty("noticesOn").GetBoolean().Should().BeFalse();
    }

    [Theory(DisplayName = "Opção de outro papel")]
    [InlineData(SystemRole.ProjectManager)]
    [InlineData(SystemRole.TechLead)]
    [InlineData(SystemRole.User)]
    public async Task OtherRoles_DoNotChangeTheOption(SystemRole role)
    {
        await using var scene = await StartAsync(teamsOn: false);
        var (client, _) = await scene.App.SignedInClientAsync("Helena Dias", role);

        var changed = await SendAsync(client, HttpMethod.Put, "/api/admin/teams-app", new { noticesOn = true });

        changed.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await scene.App.CountAsync("SELECT COUNT(*) FROM [notifications].[teams_app] WHERE NoticesOn = 1")).Should().Be(0);
    }

    [Fact(DisplayName = "Conteúdo do pacote")]
    public async Task ThePackage_NamesTheInstallation()
    {
        var entra = new FakeEntra();
        await using var scene = await StartAsync(teamsOn: false, configure: entra.Configure);

        var package = await PackageAsync(scene.Admin, "#1F5FBF");

        var manifest = JsonDocument.Parse(package.GetProperty("manifest").GetString()!).RootElement;
        var manifestId = await ManifestIdAsync(scene.App);
        manifest.GetProperty("id").GetString().Should().Be(manifestId.ToString());
        manifest.GetProperty("version").GetString().Should().Be("1.0.1");
        manifest.GetProperty("webApplicationInfo").GetProperty("id").GetString().Should().Be(FakeEntra.ClientId);
        var page = manifest.GetProperty("staticTabs")[0];
        page.GetProperty("entityId").GetString().Should().Be("home");
        page.GetProperty("contentUrl").GetString().Should().Be("http://localhost:5173/teams?item={page.subPageId}&theme={app.theme}");
        manifest.GetProperty("validDomains")[0].GetString().Should().Be("localhost");
        manifest.GetProperty("authorization").GetProperty("permissions").GetProperty("resourceSpecific").EnumerateArray()
            .Select(permission => permission.GetProperty("name").GetString())
            .Should().BeEquivalentTo(["TeamsActivity.Send.User", "TeamsAppInstallation.Read.User"]);
        var types = manifest.GetProperty("activities").GetProperty("activityTypes").EnumerateArray().ToList();
        types.Select(type => type.GetProperty("type").GetString()).Should().HaveCount(EmailChannel.Immediate.Count).And.Contain("ticketAssigned");
        types.Single(type => type.GetProperty("type").GetString() == "ticketAssigned").GetProperty("templateText").GetString()
            .Should().Be("{person} atribuiu o chamado {ticket} a você");
        manifest.GetProperty("name").GetProperty("short").GetString().Should().Be("FiscoProjects");
    }

    [Theory(DisplayName = "Pacote de outro papel")]
    [InlineData(SystemRole.ProjectManager)]
    [InlineData(SystemRole.TechLead)]
    [InlineData(SystemRole.User)]
    public async Task OtherRoles_DoNotGetThePackage(SystemRole role)
    {
        await using var scene = await StartAsync(teamsOn: false);
        var (client, _) = await scene.App.SignedInClientAsync("Helena Dias", role);

        var package = await SendAsync(client, HttpMethod.Post, "/api/admin/teams-app/package", new { iconSource = "#1F5FBF" });

        package.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Versão nova do pacote")]
    public async Task ChangedContent_NewVersion_SameId()
    {
        await using var scene = await StartAsync(teamsOn: false);

        var first = await PackageAsync(scene.Admin, "/api/installation/logo?v=1");
        var again = await PackageAsync(scene.Admin, "/api/installation/logo?v=1");
        var changed = await PackageAsync(scene.Admin, "/api/installation/logo?v=2");

        first.GetProperty("version").GetString().Should().Be("1.0.1");
        again.GetProperty("version").GetString().Should().Be("1.0.1");
        changed.GetProperty("version").GetString().Should().Be("1.0.2");
        IdOf(changed).Should().Be(IdOf(first));
        (await scene.Admin.GetFromJsonAsync<JsonElement>("/api/admin/teams-app", Cancellation)).GetProperty("version").GetString()
            .Should().Be("1.0.2");
    }

    [Fact(DisplayName = "Pacote sem o Teams na instalação")]
    public async Task NoTeams_NoPackage()
    {
        await using var scene = await StartAsync(teamsOn: false, configure: builder => builder.Configuration["Teams:Provider"] = string.Empty);

        var package = await SendAsync(scene.Admin, HttpMethod.Post, "/api/admin/teams-app/package", new { iconSource = "#1F5FBF" });

        package.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await package.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString()
            .Should().Be("urn:fiscoprojects:problem:teams-unavailable");
    }

    [Fact(DisplayName = "Aviso do Teams esperando envio")]
    public async Task Anonymized_TheWaitingNoticeLeaves()
    {
        await using var scene = await StartAsync();
        scene.App.Teams.Down = true;
        await scene.AssignToDiegoAsync();
        await scene.App.DeactivateAsync(scene.Diego);

        (await SendAsync(scene.Admin, HttpMethod.Post, $"/api/users/{scene.Diego}/anonymization")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await scene.App.CountAsync("SELECT COUNT(*) FROM [notifications].[teams_notices] WHERE UserId = @user", ("@user", scene.Diego)))
            .Should().Be(0);
        scene.App.Teams.Down = false;
        scene.App.Clock.Advance(TimeSpan.FromMinutes(2));
        (await scene.App.TeamsAsync()).Should().BeEmpty();
    }

    [Fact(DisplayName = "Nome em avisos de outras pessoas")]
    public async Task AnonymizedActor_TheIdentifierInTheNotice()
    {
        await using var scene = await StartAsync();
        scene.App.Teams.Down = true;
        await scene.AssignToDiegoAsync();
        await scene.App.DeactivateAsync(scene.Marina);
        (await SendAsync(scene.Admin, HttpMethod.Post, $"/api/users/{scene.Marina}/anonymization")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        scene.App.Teams.Down = false;
        scene.App.Clock.Advance(TimeSpan.FromMinutes(2));

        var sent = await scene.App.TeamsAsync(until: sent => sent.Any(delivery => delivery.UserId == scene.Diego));
        var person = sent.Single(delivery => delivery.UserId == scene.Diego).Message.Parameters.Single(parameter => parameter.Key == "person").Value;
        person.Should().Be("Pessoa removida nº 1");
        (await scene.App.CountAsync("SELECT COUNT(*) FROM [notifications].[teams_notices] WHERE [Values] LIKE '%Marina%'")).Should().Be(0);
    }

    private static async Task<JsonElement> PackageAsync(HttpClient admin, string iconSource)
    {
        var package = await SendAsync(admin, HttpMethod.Post, "/api/admin/teams-app/package", new { iconSource });
        package.StatusCode.Should().Be(HttpStatusCode.OK, await package.Content.ReadAsStringAsync(Cancellation));
        return await package.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
    }

    private static string IdOf(JsonElement package) =>
        JsonDocument.Parse(package.GetProperty("manifest").GetString()!).RootElement.GetProperty("id").GetString()!;

    private static async Task<Guid> ManifestIdAsync(SessionApp app)
    {
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(app.ConnectionString);
        await connection.OpenAsync(Cancellation);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT ManifestId FROM [notifications].[teams_app]";
        return (Guid)(await command.ExecuteScalarAsync(Cancellation))!;
    }

    private static async Task AnswerAsync(HttpClient client, string text)
    {
        using var form = new MultipartFormDataContent();
        var body = JsonSerializer.Serialize(new
        {
            type = "doc",
            content = new[] { new { type = "paragraph", content = new[] { new { type = "text", text } } } },
        });
        form.Add(new StringContent(body, Encoding.UTF8), "body");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/tickets/1/messages") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        var sent = await client.SendAsync(request, Cancellation);
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
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
