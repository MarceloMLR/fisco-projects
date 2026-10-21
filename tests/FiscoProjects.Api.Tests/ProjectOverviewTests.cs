using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: visao-geral-do-projeto (API side): "Andamento previsto", "Prazo final contra a linha de base", "Chamados e
// próximo marco na visão geral", "Precisa de atenção", "Equipe na visão geral" and "Visão geral pela API" (ADR-0073).
// The clock starts on Monday, 19/10/2026, 06:00 in São Paulo; 02/11 is a national holiday.
public sealed class ProjectOverviewTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static Task<JsonElement> OverviewAsync(HttpClient client, Guid project) =>
        client.GetFromJsonAsync<JsonElement>($"/api/projects/{project}/overview", Cancellation);

    private static async Task<Guid> PhaseAsync(ProjectTeam team, Guid project, string name = "Configuração") =>
        await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{project}/schedule/phases", new { name }));

    private static async Task<Guid> ItemAsync(
        ProjectTeam team, Guid project, Guid phase, string kind, string name, string start, int duration = 0, Guid? owner = null,
        int progress = 0, Guid[]? after = null) =>
        await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{project}/schedule/items", new
        {
            kind,
            phaseId = phase,
            name,
            start,
            duration,
            ownerId = owner,
            progress,
            predecessorIds = after ?? [],
        }));

    private static async Task<int> OpenTicketAsync(HttpClient client, Guid project, string title, string team = "Development", string priority = "Medium")
    {
        var opened = await SendAsync(client, HttpMethod.Post, "/api/tickets", new
        {
            projectId = project,
            team,
            priority,
            title,
            description = "A SEFAZ retorna a rejeição 539 ao emitir a nota de devolução.",
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        return (await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
    }

    private static string Doc(string text) =>
        JsonSerializer.Serialize(new { type = "doc", content = new[] { new { type = "paragraph", content = new[] { new { type = "text", text } } } } });

    private static async Task AnswerAsync(HttpClient client, int number, string text = "Estou olhando.")
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(Doc(text), Encoding.UTF8), "body");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/tickets/{number}/messages") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        var sent = await client.SendAsync(request, Cancellation);
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
    }

    private static async Task TakeAsync(HttpClient client, int number, string action, string? text = null)
    {
        var taken = await SendAsync(client, HttpMethod.Post, $"/api/tickets/{number}/transitions", new { action, body = text is null ? null : Doc(text) });
        taken.StatusCode.Should().Be(HttpStatusCode.NoContent, await taken.Content.ReadAsStringAsync(Cancellation));
    }

    private static async Task RequestAcceptanceAsync(HttpClient client, Guid project, Guid milestone, Guid recipient, string title)
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent(milestone.ToString()), "milestoneId" },
            { new StringContent(recipient.ToString()), "recipientId" },
            { new StringContent(title, Encoding.UTF8), "title" },
            { new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.7\ntermo\n%%EOF")), "file", "termo.pdf" },
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/projects/{project}/acceptances") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        await IdOfAsync(await client.SendAsync(request, Cancellation));
    }

    [Fact(DisplayName = "Visão geral pela API")]
    public async Task Overview_HasTheNumbers_TheTimeline_TheAttention_AndTheTeam()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var levantamento = await PhaseAsync(team, team.Erp, "Levantamento");
        var configuracao = await PhaseAsync(team, team.Erp);
        var validar = await ItemAsync(team, team.Erp, levantamento, "Task", "Validar cadastro de produtos", "2026-10-13", 3, team.Diego, 80);
        await ItemAsync(team, team.Erp, levantamento, "Milestone", "Levantamento aprovado", "2026-10-16");
        await ItemAsync(team, team.Erp, configuracao, "Task", "Configurar plano de contas", "2026-10-19", 5, team.Carla, after: [validar]);
        await ItemAsync(team, team.Erp, configuracao, "Milestone", "Homologação", "2026-11-03");

        var overview = await OverviewAsync(await team.ClientOfAsync(team.Lucas), team.Erp);

        overview.GetProperty("today").GetString().Should().Be("2026-10-19");
        var progress = overview.GetProperty("progress");
        // Actual: 80% of the 3 days of Validar, of 8 days. Planned: all of Validar and today of Configurar, 4 of 8 days.
        (progress.GetProperty("actual").GetInt32(), progress.GetProperty("planned").GetInt32(), progress.GetProperty("byBaseline").GetBoolean())
            .Should().Be((30, 50, false));
        overview.GetProperty("deadline").GetProperty("baselineEnd").ValueKind.Should().Be(JsonValueKind.Null);
        var timeline = overview.GetProperty("timeline");
        timeline.GetProperty("rows").EnumerateArray().Select(row => (row.GetProperty("kind").GetString(), row.GetProperty("name").GetString()))
            .Should().Equal(
                ("Phase", "Levantamento"),
                ("Milestone", "Levantamento aprovado"),
                ("Phase", "Configuração"),
                ("Milestone", "Homologação"));
        timeline.GetProperty("nonWorkingDays").EnumerateArray().Select(day => day.GetString()).Should().Contain("2026-11-02");
        var late = overview.GetProperty("attention").GetProperty("tasks").EnumerateArray().Single();
        (late.GetProperty("name").GetString(), late.GetProperty("owner").GetProperty("name").GetString(), late.GetProperty("progress").GetInt32(),
            late.GetProperty("daysLate").GetInt32()).Should().Be(("Validar cadastro de produtos", "Diego Torres", 80, 2));
        overview.GetProperty("teams").EnumerateArray().Select(entry => entry.GetProperty("team").GetString())
            .Should().Equal("Management", "Functional", "Development");
    }

    [Fact(DisplayName = "Visão geral de quem não está no projeto")]
    public async Task Overview_OfSomeoneOutside_IsNotFound()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var sofia = await team.ClientOfAsync(team.Sofia);

        (await sofia.GetAsync($"/api/projects/{team.Erp}/overview", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Desvio do prazo final")]
    public async Task Deadline_HasTheDeviationAgainstTheBaseline()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp);
        var task = await ItemAsync(team, team.Erp, phase, "Task", "Entrada em produção", "2026-11-05", 5);
        (await SendAsync(team.Admin, HttpMethod.Put, $"/api/projects/{team.Erp}/schedule/baseline")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SendAsync(team.Admin, HttpMethod.Put, $"/api/projects/{team.Erp}/schedule/items/{task}", new
        {
            phaseId = phase,
            name = "Entrada em produção",
            start = "2026-11-05",
            duration = 7,
            ownerId = (Guid?)null,
            predecessorIds = Array.Empty<Guid>(),
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        var overview = await OverviewAsync(await team.ClientOfAsync(team.Marina), team.Erp);

        var deadline = overview.GetProperty("deadline");
        (deadline.GetProperty("end").GetString(), deadline.GetProperty("baselineEnd").GetString(), deadline.GetProperty("deviation").GetInt32())
            .Should().Be(("2026-11-13", "2026-11-11", 2));
        overview.GetProperty("progress").GetProperty("byBaseline").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "Prazo sem linha de base")]
    public async Task Deadline_WithoutABaseline_HasOnlyTheEnd()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp);
        await ItemAsync(team, team.Erp, phase, "Task", "Entrada em produção", "2026-11-05", 5);

        var deadline = (await OverviewAsync(await team.ClientOfAsync(team.Marina), team.Erp)).GetProperty("deadline");

        (deadline.GetProperty("end").GetString(), deadline.GetProperty("baselineEnd").ValueKind, deadline.GetProperty("deviation").ValueKind)
            .Should().Be(("2026-11-11", JsonValueKind.Null, JsonValueKind.Null));
    }

    [Fact(DisplayName = "Projeto sem cronograma na visão geral")]
    public async Task Overview_WithoutASchedule_HasNoNumbersOfTheSchedule()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        var overview = await OverviewAsync(await team.ClientOfAsync(team.Marina), team.Erp);

        foreach (var name in new[] { "progress", "deadline", "nextMilestone", "timeline" })
        {
            overview.GetProperty(name).ValueKind.Should().Be(JsonValueKind.Null, name);
        }

        overview.GetProperty("tickets").GetProperty("total").GetInt32().Should().Be(0);
    }

    [Fact(DisplayName = "Próximo marco")]
    public async Task NextMilestone_HasTheDay_AndTheBusinessDaysLeft()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp);
        var reached = await ItemAsync(team, team.Erp, phase, "Milestone", "Escopo aprovado", "2026-10-21");
        await ItemAsync(team, team.Erp, phase, "Milestone", "Entrada em produção", "2026-11-13");
        await ItemAsync(team, team.Erp, phase, "Milestone", "Homologação", "2026-11-03");
        (await SendAsync(team.Admin, HttpMethod.Put, $"/api/projects/{team.Erp}/schedule/items/{reached}/reached", new { reached = true }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var milestone = (await OverviewAsync(await team.ClientOfAsync(team.Marina), team.Erp)).GetProperty("nextMilestone");

        (milestone.GetProperty("name").GetString(), milestone.GetProperty("day").GetString(), milestone.GetProperty("businessDaysLeft").GetInt32(),
            milestone.GetProperty("daysLate").ValueKind).Should().Be(("Homologação", "2026-11-03", 10, JsonValueKind.Null));
    }

    [Fact(DisplayName = "Chamados na visão geral")]
    public async Task Tickets_AreOpen_OfAll_Late_AndWaiting()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);
        var diego = await team.ClientOfAsync(team.Diego);
        var numbers = new List<int>();
        for (var index = 1; index <= 8; index++)
        {
            var number = await OpenTicketAsync(carla, team.Erp, $"Chamado {index}");
            await AnswerAsync(diego, number);
            numbers.Add(number);
        }

        // Unanswered and critical: its first answer was due on Tuesday, 20/10.
        await OpenTicketAsync(carla, team.Erp, "Chamado sem resposta", priority: "Critical");
        await TakeAsync(diego, numbers[0], "Resolve", "Resolvido.");
        await TakeAsync(diego, numbers[1], "Resolve", "Resolvido.");
        await TakeAsync(diego, numbers[2], "AskForInformation", "Qual nota?");
        await TakeAsync(diego, numbers[3], "AskForInformation", "Qual série?");

        // Wednesday, 21/10: a new session, as the one of the morning of Monday expired.
        team.App.Clock.Advance(TimeSpan.FromDays(2));
        using var marina = await team.App.SignInAsync(team.Marina);
        var tickets = (await OverviewAsync(marina, team.Erp)).GetProperty("tickets");

        (tickets.GetProperty("open").GetInt32(), tickets.GetProperty("total").GetInt32(), tickets.GetProperty("overdue").GetInt32(),
            tickets.GetProperty("waitingForRequester").GetInt32()).Should().Be((7, 9, 1, 2));
    }

    [Fact(DisplayName = "Tarefa atrasada na atenção")]
    public async Task LateTask_HasTheOwner_TheProgress_AndWhatDependsOnIt()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp);
        var validar = await ItemAsync(team, team.Erp, phase, "Task", "Validar cadastro de produtos", "2026-10-14", 3, team.Diego, 80);
        await ItemAsync(team, team.Erp, phase, "Task", "Configurar plano de contas", "2026-10-19", 5, team.Carla, after: [validar]);

        var task = (await OverviewAsync(await team.ClientOfAsync(team.Marina), team.Erp))
            .GetProperty("attention").GetProperty("tasks").EnumerateArray().Single();

        (task.GetProperty("owner").GetProperty("name").GetString(), task.GetProperty("progress").GetInt32(), task.GetProperty("daysLate").GetInt32())
            .Should().Be(("Diego Torres", 80, 1));
        task.GetProperty("blocks").EnumerateArray().Select(name => name.GetString()).Should().Equal("Configurar plano de contas");
    }

    [Fact(DisplayName = "Chamados na atenção")]
    public async Task Tickets_DueSoon_OrWithoutAnAssignee_NeedAttention()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);
        var diego = await team.ClientOfAsync(team.Diego);
        var tomorrow = await OpenTicketAsync(carla, team.Erp, "Vence amanhã", priority: "Critical");
        await AnswerAsync(diego, tomorrow);
        await OpenTicketAsync(carla, team.Erp, "Sem responsável", priority: "Low");
        var nextWeek = await OpenTicketAsync(carla, team.Erp, "Vence na semana que vem");
        await AnswerAsync(diego, nextWeek);

        var tickets = (await OverviewAsync(await team.ClientOfAsync(team.Marina), team.Erp))
            .GetProperty("attention").GetProperty("tickets").EnumerateArray().ToList();

        tickets.Select(ticket => ticket.GetProperty("title").GetString()).Should().Equal("Vence amanhã", "Sem responsável");
        (tickets[0].GetProperty("deadline").GetString(), tickets[0].GetProperty("dueOn").GetString(),
            tickets[0].GetProperty("assignee").GetProperty("name").GetString()).Should().Be(("Resolution", "2026-10-20", "Diego Torres"));
        (tickets[1].GetProperty("deadline").GetString(), tickets[1].GetProperty("assignee").ValueKind)
            .Should().Be(("FirstResponse", JsonValueKind.Null));
    }

    [Fact(DisplayName = "Aceites e fornecedores na atenção")]
    public async Task Acceptances_ForWhoSeesThem_AndStaleVendorTickets()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp, "Homologação");
        var milestone = await ItemAsync(team, team.Erp, phase, "Milestone", "Homologação", "2026-11-03");
        await RequestAcceptanceAsync(await team.ClientOfAsync(team.Marina), team.Erp, milestone, team.Carla, "Aceite da homologação");
        var vendor = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, "/api/admin/vendors", new { name = "Emissor Exemplo", portalUrl = (string?)null }));
        var number = await OpenTicketAsync(await team.ClientOfAsync(team.Carla), team.Erp, "Rejeição 539");
        var diego = await team.ClientOfAsync(team.Diego);
        await AnswerAsync(diego, number);
        await IdOfAsync(await SendAsync(diego, HttpMethod.Post, $"/api/tickets/{number}/vendor-tickets", new
        {
            vendorId = vendor,
            code = "FX-20931",
            openedOn = "2026-10-19",
            status = "InAnalysis",
        }));

        // Tuesday, 27/10: six business days without an update.
        team.App.Clock.Advance(TimeSpan.FromDays(8));
        using var marina = await team.App.SignInAsync(team.Marina);
        using var developer = await team.App.SignInAsync(team.Diego);
        var managed = (await OverviewAsync(marina, team.Erp)).GetProperty("attention");
        var developed = (await OverviewAsync(developer, team.Erp)).GetProperty("attention");

        var term = managed.GetProperty("acceptances").EnumerateArray().Single();
        (term.GetProperty("title").GetString(), term.GetProperty("recipient").GetProperty("name").GetString(),
            term.GetProperty("milestoneDay").GetString()).Should().Be(("Aceite da homologação", "Carla Mendes", "2026-11-03"));
        var stale = managed.GetProperty("vendors").EnumerateArray().Single();
        (stale.GetProperty("ticketNumber").GetInt32(), stale.GetProperty("vendor").GetString(), stale.GetProperty("code").GetString(),
            stale.GetProperty("staleDays").GetInt32()).Should().Be((number, "Emissor Exemplo", "FX-20931", 6));
        developed.GetProperty("acceptances").GetArrayLength().Should().Be(0);
        developed.GetProperty("vendors").GetArrayLength().Should().Be(1);
    }

    [Fact(DisplayName = "Só itens do projeto")]
    public async Task Attention_HasOnlyItemsOfTheProject()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var other = await team.CreateProjectAsync("FISC-SP", "Revisão fiscal da filial SP");
        await team.App.AddProjectMemberAsync(other, team.Diego, "Developer");
        var phase = await PhaseAsync(team, other);
        await ItemAsync(team, other, phase, "Task", "Revisar cadastro de CFOP", "2026-10-13", 3, team.Diego, 50);
        await OpenTicketAsync(await team.ClientOfAsync(team.Diego), other, "Dúvida sobre o CFOP", team: "Development");

        var attention = (await OverviewAsync(await team.ClientOfAsync(team.Marina), team.Erp)).GetProperty("attention");

        attention.GetProperty("tasks").GetArrayLength().Should().Be(0);
        attention.GetProperty("tickets").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "Equipe na visão geral")]
    public async Task Team_HasThePeople_AndTheOpenTicketsOfEachTeam()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await team.App.AddProjectMemberAsync(team.Erp, team.Sofia, "Contributor");
        var bruno = await team.App.CreateUserAsync("Bruno Alves", SystemRole.User);
        await team.App.AddProjectMemberAsync(team.Erp, bruno, "Developer");
        var carla = await team.ClientOfAsync(team.Carla);
        for (var index = 1; index <= 3; index++)
        {
            await OpenTicketAsync(carla, team.Erp, $"Funcional {index}", team: "Functional");
        }

        for (var index = 1; index <= 4; index++)
        {
            await OpenTicketAsync(carla, team.Erp, $"Desenvolvimento {index}");
        }

        var teams = (await OverviewAsync(await team.ClientOfAsync(team.Lucas), team.Erp)).GetProperty("teams").EnumerateArray().ToList();

        teams.Select(entry => (
                entry.GetProperty("team").GetString(),
                entry.GetProperty("people").GetArrayLength(),
                entry.GetProperty("openTickets").ValueKind == JsonValueKind.Null ? (int?)null : entry.GetProperty("openTickets").GetInt32()))
            .Should().Equal(("Management", 2, null), ("Functional", 2, 3), ("Development", 3, 4));
        teams[0].GetProperty("people").EnumerateArray().Select(person => person.GetProperty("name").GetString())
            .Should().Equal("Marina Costa", "Rafael Souza");
    }
}
