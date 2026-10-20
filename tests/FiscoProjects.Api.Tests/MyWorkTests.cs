using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: meu-trabalho (API side): "Aguardando você", "Itens da visão Para mim", "Visões Abertos por mim e Que
// acompanho", "Só itens da pessoa nos projetos que ela vê", "Ação de cada linha" and "Fila pela API" (ADR-0070,
// ADR-0096). The clock starts on Monday, 19/10/2026, 06:00 in São Paulo; the next business day is Tuesday, 20/10, and
// the window goes up to Monday, 26/10.
public sealed class MyWorkTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static Task<JsonElement> MyWorkAsync(HttpClient client) =>
        client.GetFromJsonAsync<JsonElement>("/api/my-work", Cancellation);

    private static JsonElement ViewOf(JsonElement work, string view) =>
        work.GetProperty("views").EnumerateArray().Single(item => item.GetProperty("view").GetString() == view);

    /// <summary>The items of a view as (group, title, action).</summary>
    private static List<(string Group, string Title, string Action)> LinesOf(JsonElement work, string view) =>
    [
        .. ViewOf(work, view).GetProperty("groups").EnumerateArray().SelectMany(group => group.GetProperty("items").EnumerateArray()
            .Select(item => (group.GetProperty("group").GetString()!, item.GetProperty("title").GetString()!, item.GetProperty("action").GetString()!))),
    ];

    private static async Task<Guid> PhaseAsync(ProjectTeam team, Guid project, string name = "Configuração") =>
        await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{project}/schedule/phases", new { name }));

    private static async Task<Guid> TaskAsync(
        ProjectTeam team, Guid project, Guid phase, string name, string start, int duration, Guid? owner, int progress = 0, Guid? parentId = null) =>
        await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{project}/schedule/items", new
        {
            kind = "Task",
            phaseId = phase,
            name,
            start,
            duration,
            ownerId = owner,
            progress,
            predecessorIds = Array.Empty<Guid>(),
            parentId,
        }));

    private static async Task<Guid> MilestoneAsync(ProjectTeam team, Guid project, Guid phase, string name, string day) =>
        await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{project}/schedule/items", new
        {
            kind = "Milestone",
            phaseId = phase,
            name,
            start = day,
            duration = 0,
            ownerId = (Guid?)null,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
        }));

    private static async Task<string> OpenTicketAsync(
        ProjectTeam team, Guid requester, string title, string priority = "Medium", string teamName = "Development", Guid? project = null)
    {
        var client = await team.ClientOfAsync(requester);
        var opened = await SendAsync(client, HttpMethod.Post, "/api/tickets", new
        {
            projectId = project ?? team.Erp,
            team = teamName,
            priority,
            title,
            description = "A SEFAZ retorna a rejeição 539 ao emitir a nota de devolução.",
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        return $"/api/tickets/{(await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32()}";
    }

    private static string Doc(string text) =>
        JsonSerializer.Serialize(new { type = "doc", content = new[] { new { type = "paragraph", content = new[] { new { type = "text", text } } } } });

    private static async Task AnswerAsync(HttpClient client, string path, string text)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(Doc(text), Encoding.UTF8), "body");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{path}/messages") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        var sent = await client.SendAsync(request, Cancellation);
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
    }

    private static async Task TakeAsync(HttpClient client, string path, string action, string? text = null)
    {
        var taken = await SendAsync(client, HttpMethod.Post, $"{path}/transitions", new { action, body = text is null ? null : Doc(text) });
        taken.StatusCode.Should().Be(HttpStatusCode.NoContent, await taken.Content.ReadAsStringAsync(Cancellation));
    }

    private static Task<HttpResponseMessage> RequestAcceptanceAsync(HttpClient client, Guid project, Guid milestone, Guid recipient, string title)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(milestone.ToString()), "milestoneId" },
            { new StringContent(recipient.ToString()), "recipientId" },
            { new StringContent(title, Encoding.UTF8), "title" },
            { new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.7\ntermo\n%%EOF")), "file", "termo.pdf" },
        };
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/projects/{project}/acceptances") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        return client.SendAsync(request, Cancellation);
    }

    [Fact(DisplayName = "Fila pela API")]
    public async Task Queue_HasTheViews_TheGroupsInOrder_AndTheItems()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp);
        await TaskAsync(team, team.Erp, phase, "Parametrizar regras de ICMS", "2026-10-21", 3, team.Diego, progress: 50);
        await TaskAsync(team, team.Erp, phase, "Validar cadastro de produtos", "2026-10-14", 3, team.Diego, progress: 80);
        await TaskAsync(team, team.Erp, phase, "Revisar cadastro de CFOP", "2026-10-19", 2, team.Diego);
        var diego = await team.ClientOfAsync(team.Diego);

        var work = await MyWorkAsync(diego);

        (work.GetProperty("today").GetString(), work.GetProperty("nextBusinessDay").GetString()).Should().Be(("2026-10-19", "2026-10-20"));
        work.GetProperty("views").EnumerateArray().Select(view => (view.GetProperty("view").GetString(), view.GetProperty("count").GetInt32()))
            .Should().Equal(("ForMe", 3), ("OpenedByMe", 0), ("Following", 0));
        LinesOf(work, "ForMe").Should().Equal(
            ("Late", "Validar cadastro de produtos", "Complete"),
            ("Soon", "Revisar cadastro de CFOP", "Start"),
            ("Week", "Parametrizar regras de ICMS", "Update"));
        var late = ViewOf(work, "ForMe").GetProperty("groups")[0].GetProperty("items")[0];
        (late.GetProperty("kind").GetString(), late.GetProperty("project").GetProperty("code").GetString(),
            late.GetProperty("dueOn").GetString(), late.GetProperty("daysLate").GetInt32()).Should().Be(("Task", "ERP-FIN", "2026-10-16", 1));
        (late.GetProperty("task").GetProperty("phase").GetString(), late.GetProperty("task").GetProperty("progress").GetInt32())
            .Should().Be(("Configuração", 80));
    }

    [Fact(DisplayName = "Fila sem sessão")]
    public async Task Queue_WithoutASession_IsUnauthorized()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        using var client = app.NewClient();

        (await client.GetAsync("/api/my-work", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Chamado aguardando a resposta da pessoa")]
    public async Task TicketWaitingForTheRequester_WaitsForThePerson()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var path = await OpenTicketAsync(team, team.Marina, "Liberar ambiente de homologação ao cliente");
        var diego = await team.ClientOfAsync(team.Diego);
        await AnswerAsync(diego, path, "Vou liberar o ambiente.");
        await TakeAsync(diego, path, "AskForInformation", "Qual usuário do cliente deve ter acesso?");

        var marina = await MyWorkAsync(await team.ClientOfAsync(team.Marina));

        LinesOf(marina, "ForMe").Should().Contain(("Waiting", "Liberar ambiente de homologação ao cliente", "Reply"));
        LinesOf(marina, "OpenedByMe").Should().Contain(("Waiting", "Liberar ambiente de homologação ao cliente", "Reply"));
        var item = ViewOf(marina, "ForMe").GetProperty("groups").EnumerateArray()
            .Single(group => group.GetProperty("group").GetString() == "Waiting").GetProperty("items")[0];
        item.GetProperty("waitingSince").ValueKind.Should().Be(JsonValueKind.String);
        (item.GetProperty("ticket").GetProperty("status").GetString(), item.GetProperty("ticket").GetProperty("assignee").GetProperty("name").GetString())
            .Should().Be(("WaitingForRequester", "Diego Torres"));
    }

    [Fact(DisplayName = "Chamado aguardando o solicitante fica fora para o responsável")]
    public async Task TicketWaitingForTheRequester_LeavesTheQueueOfTheAssignee()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var path = await OpenTicketAsync(team, team.Marina, "Liberar ambiente de homologação ao cliente");
        var diego = await team.ClientOfAsync(team.Diego);
        await AnswerAsync(diego, path, "Vou liberar o ambiente.");
        LinesOf(await MyWorkAsync(diego), "ForMe").Should().ContainSingle();

        await TakeAsync(diego, path, "AskForInformation", "Qual usuário do cliente deve ter acesso?");

        LinesOf(await MyWorkAsync(diego), "ForMe").Should().BeEmpty();
    }

    [Fact(DisplayName = "Pedido de aceite para a pessoa")]
    public async Task RequestForThePerson_WaitsForThePerson()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp, "Homologação");
        var milestone = await MilestoneAsync(team, team.Erp, phase, "Homologação concluída", "2026-11-18");
        var marina = await team.ClientOfAsync(team.Marina);
        await IdOfAsync(await RequestAcceptanceAsync(marina, team.Erp, milestone, team.Carla, "Aceite da homologação"));

        var carla = await MyWorkAsync(await team.ClientOfAsync(team.Carla));

        LinesOf(carla, "ForMe").Should().Equal(("Waiting", "Aceite da homologação", "Reply"));
        var acceptance = ViewOf(carla, "ForMe").GetProperty("groups")[0].GetProperty("items")[0].GetProperty("acceptance");
        (acceptance.GetProperty("forYou").GetBoolean(), acceptance.GetProperty("requestedBy").GetProperty("name").GetString(),
            acceptance.GetProperty("milestone").GetString()).Should().Be((true, "Marina Costa", "Homologação concluída"));
        // Far from the milestone, the request Marina made is out of her queue.
        LinesOf(await MyWorkAsync(marina), "ForMe").Should().BeEmpty();
    }

    [Fact(DisplayName = "Pedido feito pela pessoa")]
    public async Task RequestThePersonMade_IsDueOnTheMilestone()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp, "Homologação");
        var milestone = await MilestoneAsync(team, team.Erp, phase, "Plano de testes aprovado", "2026-10-20");
        var marina = await team.ClientOfAsync(team.Marina);
        await IdOfAsync(await RequestAcceptanceAsync(marina, team.Erp, milestone, team.Carla, "Aceite do plano de testes"));

        var work = await MyWorkAsync(marina);

        LinesOf(work, "ForMe").Should().Equal(("Soon", "Aceite do plano de testes", "Remind"));
        LinesOf(work, "OpenedByMe").Should().Equal(("Soon", "Aceite do plano de testes", "Remind"));
        var acceptance = ViewOf(work, "ForMe").GetProperty("groups")[0].GetProperty("items")[0].GetProperty("acceptance");
        (acceptance.GetProperty("forYou").GetBoolean(), acceptance.GetProperty("recipient").GetProperty("name").GetString())
            .Should().Be((false, "Carla Mendes"));
    }

    [Fact(DisplayName = "Chamado do responsável")]
    public async Task TicketOfTheAssignee_IsForTheAssignee()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var path = await OpenTicketAsync(team, team.Carla, "Rejeição 539 ao emitir nota de devolução", priority: "Critical");
        var diego = await team.ClientOfAsync(team.Diego);
        await AnswerAsync(diego, path, "Estou olhando.");

        var work = await MyWorkAsync(diego);

        LinesOf(work, "ForMe").Should().Equal(("Soon", "Rejeição 539 ao emitir nota de devolução", "Open"));
        var ticket = ViewOf(work, "ForMe").GetProperty("groups")[0].GetProperty("items")[0];
        (ticket.GetProperty("dueOn").GetString(), ticket.GetProperty("ticket").GetProperty("deadline").GetString(),
            ticket.GetProperty("ticket").GetProperty("status").GetString()).Should().Be(("2026-10-20", "Resolution", "InProgress"));
    }

    [Fact(DisplayName = "Chamado sem responsável do time")]
    public async Task UnassignedTicket_IsForTheTeam()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await OpenTicketAsync(team, team.Carla, "Relatório de ICMS-ST sem as notas de devolução");

        foreach (var (person, expected) in new[] { (team.Marina, 1), (team.Rafael, 1), (team.Diego, 1), (team.Carla, 0), (team.Lucas, 0), (team.Paulo, 0) })
        {
            ViewOf(await MyWorkAsync(await team.ClientOfAsync(person)), "ForMe").GetProperty("count").GetInt32().Should().Be(expected);
        }
    }

    [Fact(DisplayName = "Atribuir só para o gerente e o líder técnico")]
    public async Task Assign_IsOnlyForWhoChangesTheAssignee()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await OpenTicketAsync(team, team.Carla, "Relatório de ICMS-ST sem as notas de devolução");

        foreach (var (person, action) in new[] { (team.Marina, "Assign"), (team.Rafael, "Assign"), (team.Diego, "Open") })
        {
            LinesOf(await MyWorkAsync(await team.ClientOfAsync(person)), "ForMe")
                .Should().Equal(("Soon", "Relatório de ICMS-ST sem as notas de devolução", action));
        }
    }

    [Fact(DisplayName = "Primeira resposta e resolução")]
    public async Task Ticket_CountsTheFirstAnswer_ThenTheResolution()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var path = await OpenTicketAsync(team, team.Carla, "Erro ao importar XML de entrada");
        var diego = await team.ClientOfAsync(team.Diego);
        var before = ViewOf(await MyWorkAsync(diego), "ForMe").GetProperty("groups")[0];
        (before.GetProperty("group").GetString(), before.GetProperty("items")[0].GetProperty("dueOn").GetString(),
            before.GetProperty("items")[0].GetProperty("ticket").GetProperty("deadline").GetString()).Should().Be(("Soon", "2026-10-20", "FirstResponse"));

        await AnswerAsync(diego, path, "Pode mandar o XML?");

        var after = ViewOf(await MyWorkAsync(diego), "ForMe").GetProperty("groups")[0];
        (after.GetProperty("group").GetString(), after.GetProperty("items")[0].GetProperty("dueOn").GetString(),
            after.GetProperty("items")[0].GetProperty("ticket").GetProperty("deadline").GetString()).Should().Be(("Week", "2026-10-26", "Resolution"));
    }

    [Fact(DisplayName = "Tarefa concluída e tarefa com subtarefas ficam fora")]
    public async Task CompletedTask_AndSummaryTask_StayOut()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp);
        await TaskAsync(team, team.Erp, phase, "Configurar centros de custo", "2026-10-19", 2, team.Diego, progress: 100);
        var summary = await TaskAsync(team, team.Erp, phase, "Parametrizar impostos", "2026-10-19", 3, team.Diego);
        await TaskAsync(team, team.Erp, phase, "Parametrizar ICMS", "2026-10-21", 1, team.Diego, parentId: summary);

        var lines = LinesOf(await MyWorkAsync(await team.ClientOfAsync(team.Diego)), "ForMe");

        lines.Select(line => line.Title).Should().Equal("Parametrizar ICMS");
    }

    [Fact(DisplayName = "Abertos por mim")]
    public async Task OpenedByMe_HasTheTicketsNotResolved()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await OpenTicketAsync(team, team.Carla, "Erro ao importar XML de entrada");
        await OpenTicketAsync(team, team.Carla, "Dúvida sobre o fechamento do SPED");
        var resolved = await OpenTicketAsync(team, team.Carla, "Ajustar layout do DANFE");
        var diego = await team.ClientOfAsync(team.Diego);
        await AnswerAsync(diego, resolved, "Ajustado.");
        await TakeAsync(diego, resolved, "Resolve", "Layout ajustado na versão 2.3.");

        var opened = ViewOf(await MyWorkAsync(await team.ClientOfAsync(team.Carla)), "OpenedByMe");

        opened.GetProperty("count").GetInt32().Should().Be(2);
        opened.GetProperty("groups").EnumerateArray().SelectMany(group => group.GetProperty("items").EnumerateArray())
            .Select(item => item.GetProperty("title").GetString())
            .Should().BeEquivalentTo("Erro ao importar XML de entrada", "Dúvida sobre o fechamento do SPED");
    }

    [Fact(DisplayName = "Que acompanho")]
    public async Task Following_HasTheTicketsThePersonFollows()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var path = await OpenTicketAsync(team, team.Carla, "Erro ao importar XML de entrada");
        var lucas = await team.ClientOfAsync(team.Lucas);
        (await SendAsync(lucas, HttpMethod.Put, $"{path}/following")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var work = await MyWorkAsync(lucas);

        LinesOf(work, "Following").Should().Equal(("Soon", "Erro ao importar XML de entrada", "Open"));
        LinesOf(work, "ForMe").Should().BeEmpty();
    }

    [Fact(DisplayName = "Item de outra pessoa")]
    public async Task ItemOfSomeoneElse_StaysOut()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp);
        await TaskAsync(team, team.Erp, phase, "Validar cadastro de produtos", "2026-10-14", 3, team.Diego, progress: 80);

        LinesOf(await MyWorkAsync(await team.ClientOfAsync(team.Carla)), "ForMe").Should().BeEmpty();
    }

    [Fact(DisplayName = "Projeto que a pessoa não vê")]
    public async Task ProjectThePersonDoesNotSee_StaysOut()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await team.App.AddProjectMemberAsync(team.Erp, team.Sofia, "Contributor");
        await OpenTicketAsync(team, team.Sofia, "Erro ao importar XML de entrada");
        var sofia = await team.ClientOfAsync(team.Sofia);
        ViewOf(await MyWorkAsync(sofia), "OpenedByMe").GetProperty("count").GetInt32().Should().Be(1);

        (await SendAsync(team.Admin, HttpMethod.Delete, $"/api/projects/{team.Erp}/members/{team.Sofia}")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        var work = await MyWorkAsync(sofia);
        work.GetProperty("views").EnumerateArray().Select(view => view.GetProperty("count").GetInt32()).Should().Equal(0, 0, 0);
    }

    [Fact(DisplayName = "Projeto encerrado")]
    public async Task ClosedProject_LeavesTheQueue()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp);
        await TaskAsync(team, team.Erp, phase, "Validar cadastro de produtos", "2026-10-14", 3, team.Diego, progress: 80);
        var diego = await team.ClientOfAsync(team.Diego);
        LinesOf(await MyWorkAsync(diego), "ForMe").Should().ContainSingle();

        (await SendAsync(await team.ClientOfAsync(team.Marina), HttpMethod.Put, $"/api/projects/{team.Erp}/status", new { status = "Completed" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        LinesOf(await MyWorkAsync(diego), "ForMe").Should().BeEmpty();
    }
}
