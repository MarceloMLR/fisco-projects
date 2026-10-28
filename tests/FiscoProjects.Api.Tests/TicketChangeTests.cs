using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: chamados (API side): "Trocar o responsável", "Trocar o solicitante", "Transferir de time", "Chamados de quem
// sai" and "Visões e filtros da lista" (ADR-0027, ADR-0042, ADR-0044, ADR-0068).
public sealed class TicketChangeTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>
    /// ERP-FIN, managed by Marina, with Rafael as tech lead, Carla and Ana as contributors, Diego and Bruno as developers
    /// and Lucas as viewer.
    /// </summary>
    private sealed record Scene(
        SessionApp App, HttpClient Admin, Guid Marina, Guid Rafael, Guid Carla, Guid Ana, Guid Diego, Guid Bruno, Guid Lucas, Guid Erp)
        : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => App.DisposeAsync();
    }

    private async Task<Scene> StartAsync()
    {
        var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        var rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.TechLead);
        var carla = await app.CreateUserAsync("Carla Mendes", SystemRole.User);
        var ana = await app.CreateUserAsync("Ana Ribeiro", SystemRole.User);
        var diego = await app.CreateUserAsync("Diego Torres", SystemRole.User);
        var bruno = await app.CreateUserAsync("Bruno Alves", SystemRole.User);
        var lucas = await app.CreateUserAsync("Lucas Prado", SystemRole.User);
        var erp = await CreateProjectAsync(admin, "ERP-FIN", marina, rafael);
        await app.AddProjectMemberAsync(erp, carla, "Contributor");
        await app.AddProjectMemberAsync(erp, ana, "Contributor");
        await app.AddProjectMemberAsync(erp, diego, "Developer");
        await app.AddProjectMemberAsync(erp, bruno, "Developer");
        await app.AddProjectMemberAsync(erp, lucas, "Viewer");
        return new Scene(app, admin, marina, rafael, carla, ana, diego, bruno, lucas, erp);
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient admin, string code, Guid manager, Guid? techLead)
    {
        var created = await SendAsync(admin, HttpMethod.Post, "/api/projects", new
        {
            code,
            name = $"Projeto {code}",
            managerId = manager,
            techLeadId = techLead,
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync(Cancellation));
        return (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<int> OpenAsync(SessionApp app, Guid by, Guid project, string team, string title = "Rejeição 539")
    {
        using var client = await app.SignInAsync(by);
        var opened = await SendAsync(client, HttpMethod.Post, "/api/tickets", new
        {
            projectId = project,
            team,
            priority = "High",
            title,
            description = "A SEFAZ retorna a rejeição 539.",
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        return (await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
    }

    private static Task<HttpResponseMessage> AssignAsync(HttpClient client, Guid assignee, params int[] numbers) =>
        SendAsync(client, HttpMethod.Put, "/api/tickets/assignee", new { numbers, assigneeId = assignee });

    private static Task<HttpResponseMessage> ChangeRequesterAsync(HttpClient client, Guid requester, params int[] numbers) =>
        SendAsync(client, HttpMethod.Put, "/api/tickets/requester", new { numbers, requesterId = requester });

    private static Task<JsonElement> TicketAsync(HttpClient client, int number) =>
        client.GetFromJsonAsync<JsonElement>($"/api/tickets/{number}", Cancellation);

    private static Guid? AssigneeOf(JsonElement ticket) =>
        ticket.GetProperty("assignee").ValueKind == JsonValueKind.Null ? null : ticket.GetProperty("assignee").GetProperty("id").GetGuid();

    private static async Task AnswerAsync(HttpClient client, int number, string text)
    {
        using var form = new MultipartFormDataContent();
        var body = JsonSerializer.Serialize(new { type = "doc", content = new[] { new { type = "paragraph", content = new[] { new { type = "text", text } } } } });
        form.Add(new StringContent(body, Encoding.UTF8), "body");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/tickets/{number}/messages") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        var sent = await client.SendAsync(request, Cancellation);
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
    }

    [Fact(DisplayName = "Atribuir um chamado")]
    public async Task Manager_AssignsATicket()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development");
        using var marina = await scene.App.SignInAsync(scene.Marina);

        var assigned = await AssignAsync(marina, scene.Diego, number);

        assigned.StatusCode.Should().Be(HttpStatusCode.NoContent, await assigned.Content.ReadAsStringAsync(Cancellation));
        using var diego = await scene.App.SignInAsync(scene.Diego);
        var ticket = await TicketAsync(diego, number);
        AssigneeOf(ticket).Should().Be(scene.Diego);
        ticket.GetProperty("following").GetBoolean().Should().BeTrue();
        ticket.GetProperty("status").GetString().Should().Be("Open");
        var change = ticket.GetProperty("events").EnumerateArray().Single();
        (change.GetProperty("kind").GetString(), change.GetProperty("actorId").GetGuid(), change.GetProperty("assigneeId").GetGuid())
            .Should().Be(("AssigneeSet", scene.Marina, scene.Diego));
    }

    [Fact(DisplayName = "Atribuir em lote")]
    public async Task TechLead_AssignsThreeTicketsAtOnce()
    {
        await using var scene = await StartAsync();
        int[] numbers =
        [
            await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development", "Primeiro"),
            await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development", "Segundo"),
            await OpenAsync(scene.App, scene.Ana, scene.Erp, "Development", "Terceiro"),
        ];
        using var rafael = await scene.App.SignInAsync(scene.Rafael);

        (await AssignAsync(rafael, scene.Diego, numbers)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        foreach (var number in numbers)
        {
            AssigneeOf(await TicketAsync(rafael, number)).Should().Be(scene.Diego);
        }
    }

    [Fact(DisplayName = "Responsável fora do time")]
    public async Task Contributor_MayNotTakeADevelopmentTicket()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development");
        using var marina = await scene.App.SignInAsync(scene.Marina);

        var assigned = await AssignAsync(marina, scene.Ana, number);

        assigned.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await assigned.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        problem.GetProperty("type").GetString().Should().Be("urn:fiscoprojects:problem:ticket-person-refused");
        problem.GetProperty("numbers").EnumerateArray().Select(item => item.GetInt32()).Should().Equal(number);
        AssigneeOf(await TicketAsync(marina, number)).Should().BeNull();
    }

    [Fact(DisplayName = "Quem atende não troca o responsável")]
    public async Task Developer_MayNotAssign()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development");
        using var diego = await scene.App.SignInAsync(scene.Diego);

        (await AssignAsync(diego, scene.Bruno, number)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        AssigneeOf(await TicketAsync(diego, number)).Should().BeNull();
        var listed = await diego.GetFromJsonAsync<JsonElement>("/api/tickets", Cancellation);
        listed.GetProperty("items")[0].GetProperty("canChangeAssignee").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "Opções de responsável")]
    public async Task Choices_AreThePeopleWhoServeAllTheTickets()
    {
        await using var scene = await StartAsync();
        var development = await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development");
        var functional = await OpenAsync(scene.App, scene.Diego, scene.Erp, "Functional");
        using var marina = await scene.App.SignInAsync(scene.Marina);

        var both = await marina.GetFromJsonAsync<JsonElement>(
            $"/api/tickets/assignee-choices?number={development}&number={functional}", Cancellation);
        var developers = await marina.GetFromJsonAsync<JsonElement>(
            $"/api/tickets/assignee-choices?number={development}", Cancellation);
        var requesters = await marina.GetFromJsonAsync<JsonElement>(
            $"/api/tickets/requester-choices?number={development}", Cancellation);

        both.EnumerateArray().Select(person => person.GetProperty("name").GetString()).Should().Equal("Marina Costa");
        developers.EnumerateArray().Select(person => person.GetProperty("name").GetString())
            .Should().Equal("Bruno Alves", "Diego Torres", "Marina Costa", "Rafael Souza");
        requesters.EnumerateArray().Select(person => person.GetProperty("name").GetString())
            .Should().NotContain("Lucas Prado").And.HaveCount(6);
        (await marina.GetAsync("/api/tickets/assignee-choices?number=999", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Spec: usuarios-e-papeis, "E-mail na escolha de pessoa".
    [Fact(DisplayName = "Responsável com o e-mail")]
    public async Task Choices_BringTheEmail()
    {
        await using var scene = await StartAsync();
        var namesake = await scene.App.CreateUserAsync("Diego Torres", "diego.torres.ti@empresa.example");
        await scene.App.AddProjectMemberAsync(scene.Erp, namesake, "Developer");
        var number = await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development");
        using var marina = await scene.App.SignInAsync(scene.Marina);

        var choices = await marina.GetFromJsonAsync<JsonElement>(
            $"/api/tickets/assignee-choices?number={number}", Cancellation);
        var requesters = await marina.GetFromJsonAsync<JsonElement>(
            $"/api/tickets/requester-choices?number={number}", Cancellation);

        choices.EnumerateArray()
            .Where(person => person.GetProperty("name").GetString() == "Diego Torres")
            .Select(person => (person.GetProperty("id").GetGuid(), person.GetProperty("email").GetString()))
            .Should().BeEquivalentTo([
                (scene.Diego, "diego.torres@empresa.example"),
                (namesake, "diego.torres.ti@empresa.example"),
            ]);
        requesters.EnumerateArray().Should().AllSatisfy(person =>
            person.GetProperty("email").GetString().Should().EndWith("@empresa.example"));
    }

    [Fact(DisplayName = "Chamado encerrado mantém o responsável")]
    public async Task FinishedTicket_KeepsTheAssignee()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development");
        using var diego = await scene.App.SignInAsync(scene.Diego);
        await AnswerAsync(diego, number, "Vou olhar.");
        (await SendAsync(diego, HttpMethod.Post, $"/api/tickets/{number}/transitions", new { action = "Resolve" })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        using var marina = await scene.App.SignInAsync(scene.Marina);

        var assigned = await AssignAsync(marina, scene.Bruno, number);

        assigned.StatusCode.Should().Be(HttpStatusCode.Conflict);
        AssigneeOf(await TicketAsync(marina, number)).Should().Be(scene.Diego);
    }

    [Fact(DisplayName = "Solicitante passa o chamado")]
    public async Task Requester_HandsTheTicketOver()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development");
        using var carla = await scene.App.SignInAsync(scene.Carla);

        (await ChangeRequesterAsync(carla, scene.Ana, number)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var ticket = await TicketAsync(carla, number);
        ticket.GetProperty("requester").GetProperty("id").GetGuid().Should().Be(scene.Ana);
        ticket.GetProperty("following").GetBoolean().Should().BeTrue();
        ticket.GetProperty("followers").EnumerateArray().Select(id => id.GetGuid()).Should().Contain([scene.Carla, scene.Ana]);
        var change = ticket.GetProperty("events").EnumerateArray().Single();
        (change.GetProperty("kind").GetString(), change.GetProperty("fromPersonId").GetGuid(), change.GetProperty("toPersonId").GetGuid())
            .Should().Be(("RequesterChanged", scene.Carla, scene.Ana));
        ticket.GetProperty("permissions").GetProperty("canChangeRequester").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "Solicitante não troca em lote")]
    public async Task Requester_MayNotChangeManyAtOnce()
    {
        await using var scene = await StartAsync();
        var first = await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development", "Primeiro");
        var second = await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development", "Segundo");
        using var carla = await scene.App.SignInAsync(scene.Carla);

        (await ChangeRequesterAsync(carla, scene.Ana, first, second)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await TicketAsync(carla, first)).GetProperty("requester").GetProperty("id").GetGuid().Should().Be(scene.Carla);
    }

    [Fact(DisplayName = "Solicitante leitor")]
    public async Task Viewer_MayNotBeTheRequester()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development");
        using var marina = await scene.App.SignInAsync(scene.Marina);

        (await ChangeRequesterAsync(marina, scene.Lucas, number)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await TicketAsync(marina, number)).GetProperty("requester").GetProperty("id").GetGuid().Should().Be(scene.Carla);
    }

    [Fact(DisplayName = "Transferir para o Funcional")]
    public async Task Assignee_TransfersToFunctional_AndAContributorTakesIt()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development");
        using var diego = await scene.App.SignInAsync(scene.Diego);
        await AnswerAsync(diego, number, "Isso é cadastro, não é erro.");
        scene.App.Clock.Advance(TimeSpan.FromMinutes(5));

        var moved = await SendAsync(diego, HttpMethod.Put, $"/api/tickets/{number}/team", new { team = "Functional" });

        moved.StatusCode.Should().Be(HttpStatusCode.NoContent, await moved.Content.ReadAsStringAsync(Cancellation));
        var ticket = await TicketAsync(diego, number);
        (ticket.GetProperty("team").GetString(), AssigneeOf(ticket)).Should().Be(("Functional", null));
        var change = ticket.GetProperty("events").EnumerateArray().Last();
        (change.GetProperty("kind").GetString(), change.GetProperty("fromTeam").GetString(), change.GetProperty("toTeam").GetString())
            .Should().Be(("TeamChanged", "Development", "Functional"));
        // Diego no longer serves it, and Ana, of Funcional, takes it with her first answer.
        ticket.GetProperty("permissions").GetProperty("canTransfer").GetBoolean().Should().BeFalse();
        using var ana = await scene.App.SignInAsync(scene.Ana);
        await AnswerAsync(ana, number, "Ajusto o cadastro.");
        AssigneeOf(await TicketAsync(ana, number)).Should().Be(scene.Ana);
    }

    [Fact(DisplayName = "Time sem ninguém")]
    public async Task TeamWithoutPeople_TakesNoTransfer()
    {
        await using var scene = await StartAsync();
        var api = await CreateProjectAsync(scene.Admin, "API-X", scene.Marina, null);
        await scene.App.AddProjectMemberAsync(api, scene.Diego, "Developer");
        var number = await OpenAsync(scene.App, scene.Marina, api, "Development");
        using var marina = await scene.App.SignInAsync(scene.Marina);

        var moved = await SendAsync(marina, HttpMethod.Put, $"/api/tickets/{number}/team", new { team = "Functional" });

        moved.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TicketAsync(marina, number)).GetProperty("team").GetString().Should().Be("Development");
    }

    [Fact(DisplayName = "Responsável sai do projeto")]
    public async Task AssigneeWhoLeaves_LeavesTheTicketsWithoutAssignee()
    {
        await using var scene = await StartAsync();
        var first = await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development", "Primeiro");
        var second = await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development", "Segundo");
        var own = await OpenAsync(scene.App, scene.Diego, scene.Erp, "Functional", "Aberto pelo Diego");
        using var marina = await scene.App.SignInAsync(scene.Marina);
        (await AssignAsync(marina, scene.Diego, first, second)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var removed = await SendAsync(marina, HttpMethod.Delete, $"/api/projects/{scene.Erp}/members/{scene.Diego}");

        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        foreach (var number in new[] { first, second })
        {
            var ticket = await TicketAsync(marina, number);
            AssigneeOf(ticket).Should().BeNull();
            var cleared = ticket.GetProperty("events").EnumerateArray().Last();
            (cleared.GetProperty("kind").GetString(), cleared.GetProperty("fromPersonId").GetGuid(), cleared.GetProperty("actorId").GetGuid())
                .Should().Be(("AssigneeCleared", scene.Diego, scene.Marina));
        }

        // What Diego opened stays with him.
        (await TicketAsync(marina, own)).GetProperty("requester").GetProperty("id").GetGuid().Should().Be(scene.Diego);
    }

    [Fact(DisplayName = "Responsável muda de time")]
    public async Task AssigneeWhoChangesTeam_LeavesTheTicket()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development");
        using var marina = await scene.App.SignInAsync(scene.Marina);
        (await AssignAsync(marina, scene.Diego, number)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var changed = await SendAsync(marina, HttpMethod.Put, $"/api/projects/{scene.Erp}/members/{scene.Diego}/role", new { role = "Contributor" });

        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        AssigneeOf(await TicketAsync(marina, number)).Should().BeNull();
    }

    [Fact(DisplayName = "Chamados de uma pessoa")]
    public async Task List_ByPerson_HasTheTicketsOfThePerson()
    {
        await using var scene = await StartAsync();
        var assigned = await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development", "Atribuído ao Diego");
        var opened = await OpenAsync(scene.App, scene.Diego, scene.Erp, "Functional", "Aberto pelo Diego");
        await OpenAsync(scene.App, scene.Carla, scene.Erp, "Development", "De outra pessoa");
        using var marina = await scene.App.SignInAsync(scene.Marina);
        (await AssignAsync(marina, scene.Diego, assigned)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listed = await marina.GetFromJsonAsync<JsonElement>($"/api/tickets?personId={scene.Diego}", Cancellation);

        listed.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("number").GetInt32())
            .Should().BeEquivalentTo([assigned, opened]);
        listed.GetProperty("items")[0].GetProperty("canChangeAssignee").GetBoolean().Should().BeTrue();
        listed.GetProperty("items")[0].GetProperty("canChangeRequester").GetBoolean().Should().BeTrue();
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
