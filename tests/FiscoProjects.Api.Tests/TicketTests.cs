using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: chamados (API side): "Número do chamado", "Abertura do chamado", "Quem abre chamados", "Times dos chamados",
// "Quem vê os chamados", "Visões e filtros da lista", "Página do chamado" and "Chamados na auditoria" (ADR-0022,
// ADR-0029, ADR-0044, ADR-0068).
public sealed class TicketTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>
    /// ERP-FIN, managed by Marina, with Rafael as tech lead, Carla as contributor, Diego as developer and Lucas as viewer;
    /// FISC-SP, managed by Marina, only with Diego as developer. Sofia is in no project.
    /// </summary>
    private sealed record Scene(
        SessionApp App,
        HttpClient Admin,
        Guid Marina,
        Guid Rafael,
        Guid Carla,
        Guid Diego,
        Guid Lucas,
        Guid Sofia,
        Guid Erp,
        Guid Fisc) : IAsyncDisposable
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
        var diego = await app.CreateUserAsync("Diego Torres", SystemRole.User);
        var lucas = await app.CreateUserAsync("Lucas Prado", SystemRole.User);
        var sofia = await app.CreateUserAsync("Sofia Martins", SystemRole.User);
        var erp = await IdOfAsync(await SendAsync(admin, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-FIN",
            name = "Implantação do ERP financeiro",
            managerId = marina,
            techLeadId = rafael,
        }));
        await app.AddProjectMemberAsync(erp, carla, "Contributor");
        await app.AddProjectMemberAsync(erp, diego, "Developer");
        await app.AddProjectMemberAsync(erp, lucas, "Viewer");
        var fisc = await IdOfAsync(await SendAsync(admin, HttpMethod.Post, "/api/projects", new
        {
            code = "FISC-SP",
            name = "Revisão fiscal da filial SP",
            managerId = marina,
        }));
        await app.AddProjectMemberAsync(fisc, diego, "Developer");
        return new Scene(app, admin, marina, rafael, carla, diego, lucas, sofia, erp, fisc);
    }

    private static Task<HttpResponseMessage> OpenAsync(
        HttpClient client,
        Guid? projectId,
        string team = "Development",
        string title = "Rejeição 539 ao emitir nota de devolução",
        string priority = "High",
        string description = "A SEFAZ retorna a rejeição 539 na devolução da venda 3381.") =>
        SendAsync(client, HttpMethod.Post, "/api/tickets", new { projectId, team, priority, title, description });

    private static async Task<int> NumberOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
    }

    private static async Task<List<int>> NumbersAsync(HttpClient client, string query = "")
    {
        var page = await client.GetFromJsonAsync<JsonElement>($"/api/tickets?pageSize=100&{query}", Cancellation);
        return [.. page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("number").GetInt32())];
    }

    private static Task<JsonElement> CountsAsync(HttpClient client, Guid? projectId = null) =>
        client.GetFromJsonAsync<JsonElement>(projectId is { } id ? $"/api/tickets/counts?projectId={id}" : "/api/tickets/counts", Cancellation);

    [Fact(DisplayName = "Números em sequência")]
    public async Task Numbers_FollowEachOther_AcrossProjects()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        using var diego = await scene.App.SignInAsync(scene.Diego);

        var first = await NumberOfAsync(await OpenAsync(carla, scene.Erp));
        var second = await NumberOfAsync(await OpenAsync(diego, scene.Fisc, title: "Ajustar layout do DANFE"));

        second.Should().Be(first + 1);
    }

    [Fact(DisplayName = "Aberturas simultâneas")]
    public async Task SimultaneousOpenings_NeverRepeatANumber()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);

        var responses = await Task.WhenAll(Enumerable.Range(1, 10)
            .Select(index => OpenAsync(carla, scene.Erp, title: $"Chamado {index}")));
        var numbers = new List<int>();
        foreach (var response in responses)
        {
            numbers.Add(await NumberOfAsync(response));
        }

        numbers.Should().OnlyHaveUniqueItems().And.HaveCount(10);
        (await NumbersAsync(carla)).Should().BeEquivalentTo(numbers);
    }

    [Fact(DisplayName = "Abrir chamado")]
    public async Task Contributor_OpensATicket_OpenWithoutAssignee()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);

        var number = await NumberOfAsync(await OpenAsync(carla, scene.Erp, description: "  Linha 1\nLinha 2  "));

        var ticket = await carla.GetFromJsonAsync<JsonElement>($"/api/tickets/{number}", Cancellation);
        ticket.GetProperty("status").GetString().Should().Be("Open");
        ticket.GetProperty("assignee").ValueKind.Should().Be(JsonValueKind.Null);
        ticket.GetProperty("requester").GetProperty("name").GetString().Should().Be("Carla Mendes");
        ticket.GetProperty("team").GetString().Should().Be("Development");
        ticket.GetProperty("priority").GetString().Should().Be("High");
        ticket.GetProperty("description").GetString().Should().Be("Linha 1\nLinha 2");
    }

    [Fact(DisplayName = "Chamado sem projeto")]
    public async Task Ticket_WithoutAProject_IsRefused()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);

        var response = await OpenAsync(carla, null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorFieldsAsync(response)).Should().Equal("projectId");
        (await NumbersAsync(scene.Admin, "situation=All")).Should().BeEmpty();
    }

    [Fact(DisplayName = "Campos obrigatórios do chamado")]
    public async Task Ticket_WithoutItsFields_TellsEachOne()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);

        var response = await SendAsync(carla, HttpMethod.Post, "/api/tickets", new { projectId = scene.Erp, title = " ", description = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorFieldsAsync(response)).Should().BeEquivalentTo("team", "priority", "title", "description");
        var tooLong = await OpenAsync(carla, scene.Erp, title: new string('a', 201));
        (await ErrorFieldsAsync(tooLong)).Should().Equal("title");
    }

    public static TheoryData<string, HttpStatusCode> WhoOpens => new()
    {
        { "Administrator", HttpStatusCode.Created },
        { "Manager", HttpStatusCode.Created },
        { "TechLead", HttpStatusCode.Created },
        { "Contributor", HttpStatusCode.Created },
        { "Developer", HttpStatusCode.Created },
        { "Viewer", HttpStatusCode.Forbidden },
        { "Outside", HttpStatusCode.NotFound },
    };

    // The matrix of ADR-0044 for opening and seeing a ticket: everyone in the project sees it; all but the viewer open.
    [Theory(DisplayName = "Quem abre e quem vê os chamados")]
    [MemberData(nameof(WhoOpens))]
    public async Task OpeningAndSeeing_FollowTheRoleInTheProject(string who, HttpStatusCode opening)
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        var number = await NumberOfAsync(await OpenAsync(carla, scene.Erp));
        var person = who switch
        {
            "Manager" => scene.Marina,
            "TechLead" => scene.Rafael,
            "Contributor" => scene.Carla,
            "Developer" => scene.Diego,
            "Viewer" => scene.Lucas,
            _ => scene.Sofia,
        };
        using var client = who == "Administrator" ? null : await scene.App.SignInAsync(person);
        var asker = client ?? scene.Admin;

        (await OpenAsync(asker, scene.Erp)).StatusCode.Should().Be(opening);
        (await asker.GetAsync($"/api/tickets/{number}", Cancellation)).StatusCode
            .Should().Be(who == "Outside" ? HttpStatusCode.NotFound : HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Leitor não abre chamado")]
    public async Task Viewer_DoesNotOpenTickets()
    {
        await using var scene = await StartAsync();
        using var lucas = await scene.App.SignInAsync(scene.Lucas);

        (await OpenAsync(lucas, scene.Erp)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await NumbersAsync(scene.Admin, "situation=All")).Should().BeEmpty();
        var choices = await lucas.GetFromJsonAsync<JsonElement>("/api/tickets/choices", Cancellation);
        choices.EnumerateArray().Single().GetProperty("canOpen").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "Fora do projeto não abre chamado")]
    public async Task Outsider_DoesNotOpenTickets()
    {
        await using var scene = await StartAsync();
        using var sofia = await scene.App.SignInAsync(scene.Sofia);

        (await OpenAsync(sofia, scene.Erp)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await OpenAsync(sofia, Guid.CreateVersion7())).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Projeto encerrado não recebe chamado")]
    public async Task ClosedProject_TakesNoTickets_UntilReopened()
    {
        await using var scene = await StartAsync();
        using var marina = await scene.App.SignInAsync(scene.Marina);
        using var carla = await scene.App.SignInAsync(scene.Carla);
        (await SendAsync(marina, HttpMethod.Put, $"/api/projects/{scene.Erp}/status", new { status = "Completed" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refused = await OpenAsync(carla, scene.Erp);

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await refused.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString()
            .Should().Be("urn:fiscoprojects:problem:project-closed");
        (await carla.GetFromJsonAsync<JsonElement>("/api/tickets/choices", Cancellation)).EnumerateArray()
            .Single(project => project.GetProperty("code").GetString() == "ERP-FIN").GetProperty("canOpen").GetBoolean()
            .Should().BeFalse();

        (await SendAsync(marina, HttpMethod.Put, $"/api/projects/{scene.Erp}/status", new { status = "InProgress" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await OpenAsync(carla, scene.Erp)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact(DisplayName = "Times na abertura")]
    public async Task Opening_OffersOnlyTheTeamsWithSomeoneToServe()
    {
        await using var scene = await StartAsync();
        using var marina = await scene.App.SignInAsync(scene.Marina);

        var choices = (await marina.GetFromJsonAsync<JsonElement>("/api/tickets/choices", Cancellation)).EnumerateArray()
            .ToDictionary(
                project => project.GetProperty("code").GetString()!,
                project => project.GetProperty("teams").EnumerateArray().Select(team => team.GetString()).ToList());

        choices["ERP-FIN"].Should().Equal("Functional", "Development");
        choices["FISC-SP"].Should().Equal("Development");
    }

    [Fact(DisplayName = "Time sem quem atenda")]
    public async Task TeamWithoutPeople_TakesNoTickets()
    {
        await using var scene = await StartAsync();
        using var marina = await scene.App.SignInAsync(scene.Marina);

        var response = await OpenAsync(marina, scene.Fisc, team: "Functional");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorFieldsAsync(response)).Should().Equal("team");
        (await NumbersAsync(scene.Admin, "situation=All")).Should().BeEmpty();
    }

    [Fact(DisplayName = "Leitor vê os chamados do projeto")]
    public async Task Viewer_SeesEveryTicketOfTheProject()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        using var diego = await scene.App.SignInAsync(scene.Diego);
        using var lucas = await scene.App.SignInAsync(scene.Lucas);
        var first = await NumberOfAsync(await OpenAsync(carla, scene.Erp, team: "Functional"));
        var second = await NumberOfAsync(await OpenAsync(diego, scene.Erp));
        var other = await NumberOfAsync(await OpenAsync(diego, scene.Fisc));

        (await NumbersAsync(lucas)).Should().BeEquivalentTo([first, second]);
        (await NumbersAsync(lucas)).Should().NotContain(other);
    }

    [Fact(DisplayName = "Fora do projeto não vê os chamados")]
    public async Task Outsider_DoesNotSeeTheTickets()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        using var sofia = await scene.App.SignInAsync(scene.Sofia);
        var number = await NumberOfAsync(await OpenAsync(carla, scene.Erp));

        (await NumbersAsync(sofia, "situation=All")).Should().BeEmpty();
        (await sofia.GetAsync($"/api/tickets/{number}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await sofia.GetAsync($"/api/tickets/counts?projectId={scene.Erp}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await CountsAsync(sofia)).GetProperty("all").GetInt32().Should().Be(0);
    }

    [Fact(DisplayName = "Administrador vê todos os chamados")]
    public async Task Administrator_SeesTheTicketsOfAllProjects()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        using var diego = await scene.App.SignInAsync(scene.Diego);
        var erp = await NumberOfAsync(await OpenAsync(carla, scene.Erp));
        var fisc = await NumberOfAsync(await OpenAsync(diego, scene.Fisc));

        (await NumbersAsync(scene.Admin)).Should().BeEquivalentTo([erp, fisc]);
        var item = (await scene.Admin.GetFromJsonAsync<JsonElement>($"/api/tickets?search=%23{fisc}", Cancellation))
            .GetProperty("items").EnumerateArray().Single();
        item.GetProperty("project").GetProperty("code").GetString().Should().Be("FISC-SP");
        item.GetProperty("requester").GetProperty("name").GetString().Should().Be("Diego Torres");
    }

    [Fact(DisplayName = "Sem responsável nos meus times")]
    public async Task UnassignedInMyTeams_FollowsTheTeamsOfThePerson()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        using var diego = await scene.App.SignInAsync(scene.Diego);
        using var marina = await scene.App.SignInAsync(scene.Marina);
        using var rafael = await scene.App.SignInAsync(scene.Rafael);
        using var lucas = await scene.App.SignInAsync(scene.Lucas);
        var functional = await NumberOfAsync(await OpenAsync(diego, scene.Erp, team: "Functional"));
        var development = await NumberOfAsync(await OpenAsync(carla, scene.Erp));
        var otherProject = await NumberOfAsync(await OpenAsync(marina, scene.Fisc));

        (await NumbersAsync(diego, "view=UnassignedInMyTeams")).Should().BeEquivalentTo([development, otherProject]);
        (await NumbersAsync(carla, "view=UnassignedInMyTeams")).Should().Equal(functional);
        (await NumbersAsync(marina, "view=UnassignedInMyTeams")).Should().BeEquivalentTo([functional, development, otherProject]);
        (await NumbersAsync(rafael, "view=UnassignedInMyTeams")).Should().BeEquivalentTo([functional, development]);
        (await NumbersAsync(lucas, "view=UnassignedInMyTeams")).Should().BeEmpty();
        (await NumbersAsync(scene.Admin, "view=UnassignedInMyTeams")).Should().BeEmpty();
        (await NumbersAsync(lucas, "view=Unassigned")).Should().BeEquivalentTo([functional, development]);
        (await NumbersAsync(diego, "view=AssignedToMe")).Should().BeEmpty();

        var counts = await CountsAsync(diego);
        (counts.GetProperty("unassignedInMyTeams").GetInt32(), counts.GetProperty("open").GetInt32(), counts.GetProperty("assignedToMe").GetInt32())
            .Should().Be((2, 3, 0));
        (await CountsAsync(diego, scene.Erp)).GetProperty("unassignedInMyTeams").GetInt32().Should().Be(1);
    }

    [Fact(DisplayName = "Abertos por mim")]
    public async Task OpenedByMe_ShowsOnlyThePersonTickets()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        using var diego = await scene.App.SignInAsync(scene.Diego);
        var mine = await NumberOfAsync(await OpenAsync(carla, scene.Erp));
        await NumberOfAsync(await OpenAsync(diego, scene.Erp));

        (await NumbersAsync(carla, "view=OpenedByMe")).Should().Equal(mine);
        (await CountsAsync(carla)).GetProperty("openedByMe").GetInt32().Should().Be(1);
    }

    [Fact(DisplayName = "Busca nos chamados")]
    public async Task Search_FindsByNumberTitleAndDescription()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        var rejection = await NumberOfAsync(await OpenAsync(carla, scene.Erp));
        var layout = await NumberOfAsync(await OpenAsync(
            carla, scene.Erp, title: "Ajustar layout do DANFE simplificado", description: "O código de barras sai cortado."));

        (await NumbersAsync(carla, $"search=%23{layout}")).Should().Equal(layout);
        (await NumbersAsync(carla, $"search={rejection}")).Should().Equal(rejection);
        (await NumbersAsync(carla, "search=REJEICAO")).Should().Equal(rejection);
        (await NumbersAsync(carla, "search=codigo de barras")).Should().Equal(layout);
        (await NumbersAsync(carla, "search=inexistente")).Should().BeEmpty();
    }

    [Fact(DisplayName = "Filtros da lista")]
    public async Task Filters_KeepOnlyTheMatchingTickets()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);
        using var carla = await scene.App.SignInAsync(scene.Carla);
        var low = await NumberOfAsync(await OpenAsync(carla, scene.Erp, team: "Functional", priority: "Low"));
        var critical = await NumberOfAsync(await OpenAsync(carla, scene.Erp, priority: "Critical"));
        var high = await NumberOfAsync(await OpenAsync(diego, scene.Fisc));
        var otherHigh = await NumberOfAsync(await OpenAsync(diego, scene.Erp));

        (await NumbersAsync(diego, $"projectId={scene.Fisc}")).Should().Equal(high);
        (await NumbersAsync(diego, "team=Functional")).Should().Equal(low);
        (await NumbersAsync(diego, "situation=Finished")).Should().BeEmpty();
        (await NumbersAsync(carla, $"projectId={scene.Fisc}")).Should().BeEmpty();
        // By the priority, the highest first and the oldest first in each; or by the number.
        (await NumbersAsync(diego)).Should().Equal(critical, high, otherHigh, low);
        (await NumbersAsync(diego, "sort=Number&descending=true")).Should().Equal(otherHigh, high, critical, low);
        var page = await diego.GetFromJsonAsync<JsonElement>("/api/tickets?pageSize=2&page=2", Cancellation);
        page.GetProperty("total").GetInt32().Should().Be(4);
        page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("number").GetInt32()).Should().Equal(otherHigh, low);
    }

    [Fact(DisplayName = "Página do chamado")]
    public async Task TicketPage_HasTheHeaderAndThePeople()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        using var lucas = await scene.App.SignInAsync(scene.Lucas);
        var number = await NumberOfAsync(await OpenAsync(carla, scene.Erp));

        var ticket = await lucas.GetFromJsonAsync<JsonElement>($"/api/tickets/{number}", Cancellation);

        ticket.GetProperty("number").GetInt32().Should().Be(number);
        ticket.GetProperty("title").GetString().Should().Be("Rejeição 539 ao emitir nota de devolução");
        ticket.GetProperty("project").GetProperty("code").GetString().Should().Be("ERP-FIN");
        ticket.GetProperty("managers").EnumerateArray().Select(person => person.GetProperty("name").GetString()).Should().Equal("Marina Costa");
        ticket.GetProperty("techLeads").EnumerateArray().Select(person => person.GetProperty("name").GetString()).Should().Equal("Rafael Souza");
        ticket.GetProperty("openedAt").GetDateTime().Should().Be(scene.App.Clock.GetUtcNow().UtcDateTime);
        (await lucas.GetAsync("/api/tickets/9999", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Abertura na auditoria")]
    public async Task Opening_IsAudited_InTheProject()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        var number = await NumberOfAsync(await OpenAsync(carla, scene.Erp));

        var entry = (await scene.Admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=Ticket", Cancellation))
            .GetProperty("items").EnumerateArray().Should().ContainSingle().Subject;

        entry.GetProperty("action").GetString().Should().Be("Created");
        entry.GetProperty("itemLabel").GetString().Should().Be("Rejeição 539 ao emitir nota de devolução");
        var changes = entry.GetProperty("changes").EnumerateArray().ToDictionary(
            change => change.GetProperty("field").GetString()!, change => change);
        changes["Number"].GetProperty("after").GetString().Should().Be(number.ToString(System.Globalization.CultureInfo.InvariantCulture));
        changes["ProjectId"].GetProperty("reference").GetString().Should().Be("Project");
        changes.Should().NotContainKeys("OpenedAt", "UpdatedAt");
    }

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<List<string>> ErrorFieldsAsync(HttpResponseMessage response) =>
        [.. (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation))
            .GetProperty("errors").EnumerateObject().Select(field => field.Name)];

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
