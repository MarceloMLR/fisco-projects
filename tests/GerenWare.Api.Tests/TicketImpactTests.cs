using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GerenWare.Testing;
using static GerenWare.Api.Tests.ProjectTeam;

namespace GerenWare.Api.Tests;

// Spec: chamados (API side), requirements "Chamado no cronograma", "Quem liga o chamado ao cronograma", "Itens que podem
// ser ligados", "O que depende do chamado" and "Ligação na conversa e na auditoria" (ADR-0044, ADR-0164).
public sealed class TicketImpactTests(SqlServerFixture sqlServer)
{
    /// <summary>
    /// The team of ERP-FIN (<see cref="ProjectTeam"/>) with the phase Homologação, included by Marina: Testes integrados
    /// (26/10, 5 business days), then the milestones Homologação concluída and Go-live, one after the other; Treinamento,
    /// with nothing after it; and Integração do emissor, a summary whose subtask Ajustar o XML leads to the milestone
    /// Emissor no ar.
    /// </summary>
    private sealed record Scene(
        ProjectTeam Team, Guid Phase, Guid Tests, Guid Acceptance, Guid GoLive, Guid Training, Guid Integration, Guid Ready)
        : IAsyncDisposable
    {
        public string Schedule => $"/api/projects/{Team.Erp}/schedule";

        public ValueTask DisposeAsync() => Team.DisposeAsync();
    }

    private async Task<Scene> StartAsync()
    {
        var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        var schedule = $"/api/projects/{team.Erp}/schedule";
        var phase = await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"{schedule}/phases", new { name = "Homologação" }));
        async Task<Guid> AddAsync(string kind, string name, string start, int duration, Guid[] after, Guid? parentId = null) =>
            await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"{schedule}/items", new
            {
                kind,
                phaseId = phase,
                name,
                start,
                duration,
                ownerId = (Guid?)null,
                progress = 0,
                predecessorIds = after,
                parentId,
            }));
        var tests = await AddAsync("Task", "Testes integrados", "2026-10-26", 5, []);
        var acceptance = await AddAsync("Milestone", "Homologação concluída", "2026-10-26", 0, [tests]);
        var goLive = await AddAsync("Milestone", "Go-live", "2026-10-26", 0, [acceptance]);
        var training = await AddAsync("Task", "Treinamento", "2026-10-26", 2, []);
        var integration = await AddAsync("Task", "Integração do emissor", "2026-10-26", 3, []);
        var xml = await AddAsync("Task", "Ajustar o XML", "2026-10-26", 3, [], integration);
        var ready = await AddAsync("Milestone", "Emissor no ar", "2026-10-26", 0, [xml]);
        return new Scene(team, phase, tests, acceptance, goLive, training, integration, ready);
    }

    /// <summary>A ticket Carla opens in ERP-FIN, for the team.</summary>
    private static async Task<int> OpenAsync(ProjectTeam team, string ticketTeam = "Development", Guid? projectId = null)
    {
        var carla = await team.ClientOfAsync(team.Carla);
        var opened = await SendAsync(carla, HttpMethod.Post, "/api/tickets", new
        {
            projectId = projectId ?? team.Erp,
            team = ticketTeam,
            priority = "High",
            title = "Rejeição 539 ao emitir nota de devolução",
            description = "A SEFAZ retorna a rejeição 539.",
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        return (await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
    }

    private static Task<HttpResponseMessage> LinkAsync(HttpClient client, int number, params Guid[] itemIds) =>
        SendAsync(client, HttpMethod.Put, $"/api/tickets/{number}/impacts", new { itemIds });

    private static async Task LinkedAsync(HttpClient client, int number, params Guid[] itemIds)
    {
        var linked = await LinkAsync(client, number, itemIds);
        linked.StatusCode.Should().Be(HttpStatusCode.NoContent, await linked.Content.ReadAsStringAsync(Cancellation));
    }

    private static async Task<JsonElement> ScheduleOfTicketAsync(HttpClient client, int number) =>
        (await client.GetFromJsonAsync<JsonElement>($"/api/tickets/{number}", Cancellation)).GetProperty("schedule");

    private static IEnumerable<string?> NamesOf(JsonElement schedule, string list) =>
        schedule.GetProperty(list).EnumerateArray().Select(item => item.GetProperty("name").GetString());

    /// <summary>The start and the end of each task and milestone of the schedule, in its order.</summary>
    private static async Task<IReadOnlyList<(Guid Id, string? Start, string? End)>> DatesAsync(HttpClient client, Scene scene) =>
        [.. (await client.GetFromJsonAsync<JsonElement>(scene.Schedule, Cancellation)).GetProperty("phases").EnumerateArray()
            .SelectMany(phase => phase.GetProperty("items").EnumerateArray())
            .Select(item => (item.GetProperty("id").GetGuid(), item.GetProperty("start").GetString(), item.GetProperty("end").GetString()))];

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    [Fact(DisplayName = "Ligar ao cronograma")]
    public async Task Developer_LinksTheTicket_WithoutChangingDatesOrDeadlines()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.Team);
        var diego = await scene.Team.ClientOfAsync(scene.Team.Diego);
        var datesBefore = await DatesAsync(diego, scene);
        var deadlinesBefore = (await diego.GetFromJsonAsync<JsonElement>($"/api/tickets/{number}", Cancellation)).GetProperty("deadlines").GetRawText();

        await LinkedAsync(diego, number, scene.Training);

        var ticket = await diego.GetFromJsonAsync<JsonElement>($"/api/tickets/{number}", Cancellation);
        var item = ticket.GetProperty("schedule").GetProperty("items").EnumerateArray().Single();
        (item.GetProperty("id").GetGuid(), item.GetProperty("type").GetString(), item.GetProperty("name").GetString(),
                item.GetProperty("start").GetString(), item.GetProperty("progress").GetInt32(), item.GetProperty("lateDays").GetInt32())
            .Should().Be((scene.Training, "Task", "Treinamento", "2026-10-26", 0, 0));
        ticket.GetProperty("schedule").GetProperty("canChange").GetBoolean().Should().BeTrue();
        ticket.GetProperty("deadlines").GetRawText().Should().Be(deadlinesBefore);
        ticket.GetProperty("status").GetString().Should().Be("Open");
        (await DatesAsync(diego, scene)).Should().Equal(datesBefore);
    }

    [Fact(DisplayName = "Gestão liga ao cronograma")]
    public async Task Manager_AndAdministrator_LinkAFunctionalTicket()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.Team, "Functional");
        var marina = await scene.Team.ClientOfAsync(scene.Team.Marina);

        await LinkedAsync(marina, number, scene.Training);
        var byManager = await ScheduleOfTicketAsync(marina, number);
        await LinkedAsync(scene.Team.Admin, number, scene.Training, scene.GoLive);
        var byAdministrator = await ScheduleOfTicketAsync(scene.Team.Admin, number);

        NamesOf(byManager, "items").Should().Equal("Treinamento");
        NamesOf(byAdministrator, "items").Should().Equal("Treinamento", "Go-live");
        byAdministrator.GetProperty("items").EnumerateArray().Last().GetProperty("type").GetString().Should().Be("Milestone");
    }

    [Fact(DisplayName = "Desligar do cronograma")]
    public async Task TheItems_AreUnlinked_WithNone()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.Team);
        var rafael = await scene.Team.ClientOfAsync(scene.Team.Rafael);
        await LinkedAsync(rafael, number, scene.Tests, scene.Training);

        await LinkedAsync(rafael, number);

        var schedule = await ScheduleOfTicketAsync(rafael, number);
        schedule.GetProperty("items").GetArrayLength().Should().Be(0);
        schedule.GetProperty("milestones").GetArrayLength().Should().Be(0);
        schedule.GetProperty("onCriticalPath").GetBoolean().Should().BeFalse();
        (await scene.Team.App.CountAsync("SELECT COUNT(*) FROM [tickets].[ticket_impacts]")).Should().Be(0);
    }

    [Fact(DisplayName = "Quem não atende não liga")]
    public async Task WhoDoesNotServeTheTicket_IsForbidden()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.Team);
        var lucas = await scene.Team.ClientOfAsync(scene.Team.Lucas);
        var carla = await scene.Team.ClientOfAsync(scene.Team.Carla);

        var byViewer = await LinkAsync(lucas, number, scene.Tests);
        var byRequester = await LinkAsync(carla, number, scene.Tests);

        byViewer.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        byRequester.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await scene.Team.App.CountAsync("SELECT COUNT(*) FROM [tickets].[ticket_impacts]")).Should().Be(0);
        var ticket = await carla.GetFromJsonAsync<JsonElement>($"/api/tickets/{number}", Cancellation);
        ticket.GetProperty("events").GetArrayLength().Should().Be(0);
        ticket.GetProperty("schedule").GetProperty("canChange").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "Chamado fechado não liga")]
    public async Task ClosedTicket_IsNotLinked()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.Team);
        var diego = await scene.Team.ClientOfAsync(scene.Team.Diego);
        await LinkedAsync(diego, number, scene.Training);
        var body = JsonSerializer.Serialize(new { type = "doc", content = new[] { new { type = "paragraph", content = new[] { new { type = "text", text = "Aberto por engano." } } } } });
        (await SendAsync(await scene.Team.ClientOfAsync(scene.Team.Carla), HttpMethod.Post, $"/api/tickets/{number}/transitions", new { action = "Cancel", body }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var linked = await LinkAsync(diego, number, scene.Tests);

        linked.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProblemTypeAsync(linked)).Should().Be("urn:gerenware:problem:ticket-closed");
        var schedule = await ScheduleOfTicketAsync(diego, number);
        NamesOf(schedule, "items").Should().Equal("Treinamento");
        schedule.GetProperty("canChange").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "Fora do projeto não liga")]
    public async Task SomeoneOutsideTheProject_DoesNotFindTheTicket()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.Team);

        var linked = await LinkAsync(await scene.Team.ClientOfAsync(scene.Team.Sofia), number, scene.Tests);

        linked.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await scene.Team.App.CountAsync("SELECT COUNT(*) FROM [tickets].[ticket_impacts]")).Should().Be(0);
    }

    [Fact(DisplayName = "Item que não pode ser ligado")]
    public async Task APhase_AnItemOfAnotherProject_OrElevenItems_AreRefused()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.Team);
        var sped = await scene.Team.CreateProjectAsync("SPED-27", "Entrega do SPED");
        var marina = await scene.Team.ClientOfAsync(scene.Team.Marina);
        var spedPhase = await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"/api/projects/{sped}/schedule/phases", new { name = "Bloco K" }));
        var spedTask = await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"/api/projects/{sped}/schedule/items", new
        {
            kind = "Task",
            phaseId = spedPhase,
            name = "Gerar o bloco K",
            start = "2026-10-26",
            duration = 2,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
        }));
        var eleven = Enumerable.Repeat(0, 5).Aggregate(
            new List<Guid> { scene.Tests, scene.Acceptance, scene.GoLive, scene.Training, scene.Integration, scene.Ready },
            (ids, _) => [.. ids, Guid.CreateVersion7()]);
        var rafael = await scene.Team.ClientOfAsync(scene.Team.Rafael);

        foreach (var refused in new[]
        {
            await LinkAsync(rafael, number, scene.Phase),
            await LinkAsync(rafael, number, spedTask),
            await LinkAsync(rafael, number, scene.Tests, scene.Tests),
            await LinkAsync(rafael, number, [.. eleven]),
        })
        {
            refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var problem = await refused.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
            problem.GetProperty("type").GetString().Should().Be("urn:gerenware:problem:ticket-impact-refused");
            problem.GetProperty("errors").TryGetProperty("itemIds", out _).Should().BeTrue();
        }

        (await scene.Team.App.CountAsync("SELECT COUNT(*) FROM [tickets].[ticket_impacts]")).Should().Be(0);
    }

    [Fact(DisplayName = "Marcos ameaçados pelas dependências")]
    public async Task LinkedItem_ThreatensTheMilestonesAfterIt()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.Team);
        var diego = await scene.Team.ClientOfAsync(scene.Team.Diego);

        await LinkedAsync(diego, number, scene.Tests);

        var schedule = await ScheduleOfTicketAsync(await scene.Team.ClientOfAsync(scene.Team.Lucas), number);
        var days = (await DatesAsync(diego, scene)).ToDictionary(item => item.Id, item => item.Start);
        schedule.GetProperty("milestones").EnumerateArray()
            .Select(milestone => (milestone.GetProperty("id").GetGuid(), milestone.GetProperty("name").GetString(), milestone.GetProperty("day").GetString()))
            .Should().Equal(
                (scene.Acceptance, "Homologação concluída", days[scene.Acceptance]),
                (scene.GoLive, "Go-live", days[scene.GoLive]));
    }

    [Fact(DisplayName = "Marco atingido não é ameaçado")]
    public async Task ReachedMilestone_IsNotThreatened()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.Team);
        await LinkedAsync(await scene.Team.ClientOfAsync(scene.Team.Diego), number, scene.Tests);
        var marina = await scene.Team.ClientOfAsync(scene.Team.Marina);

        (await SendAsync(marina, HttpMethod.Put, $"{scene.Schedule}/items/{scene.Acceptance}/reached", new { reached = true }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        NamesOf(await ScheduleOfTicketAsync(marina, number), "milestones").Should().Equal("Go-live");
    }

    [Fact(DisplayName = "Item no caminho crítico")]
    public async Task LinkedItem_OnTheCriticalPath_IsSaid()
    {
        await using var scene = await StartAsync();
        var critical = await OpenAsync(scene.Team);
        var other = await OpenAsync(scene.Team);
        var diego = await scene.Team.ClientOfAsync(scene.Team.Diego);

        await LinkedAsync(diego, critical, scene.Tests);
        await LinkedAsync(diego, other, scene.Training);

        (await ScheduleOfTicketAsync(diego, critical)).GetProperty("onCriticalPath").GetBoolean().Should().BeTrue();
        var training = await ScheduleOfTicketAsync(diego, other);
        training.GetProperty("onCriticalPath").GetBoolean().Should().BeFalse();
        training.GetProperty("milestones").GetArrayLength().Should().Be(0, "nothing depends on the training");
    }

    [Fact(DisplayName = "Tarefa-resumo ligada")]
    public async Task LinkedSummary_ThreatensWhatItsSubtasksLeadTo()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.Team);
        var diego = await scene.Team.ClientOfAsync(scene.Team.Diego);

        await LinkedAsync(diego, number, scene.Integration);

        var schedule = await ScheduleOfTicketAsync(diego, number);
        NamesOf(schedule, "items").Should().Equal("Integração do emissor");
        NamesOf(schedule, "milestones").Should().Equal("Emissor no ar");
    }

    [Fact(DisplayName = "Item ligado excluído")]
    public async Task RemovedItem_LeavesTheTicket_AndComesBackWithTheUndo()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.Team);
        var diego = await scene.Team.ClientOfAsync(scene.Team.Diego);
        await LinkedAsync(diego, number, scene.Tests, scene.Training);
        var marina = await scene.Team.ClientOfAsync(scene.Team.Marina);

        (await SendAsync(marina, HttpMethod.Delete, $"{scene.Schedule}/items/{scene.Training}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var removed = await ScheduleOfTicketAsync(diego, number);
        // Changing the list while the item does not exist keeps its link for the undo.
        await LinkedAsync(diego, number, scene.Tests, scene.GoLive);
        (await SendAsync(marina, HttpMethod.Post, $"{scene.Schedule}/undo")).StatusCode.Should().Be(HttpStatusCode.OK);
        var undone = await ScheduleOfTicketAsync(diego, number);

        NamesOf(removed, "items").Should().Equal("Testes integrados");
        NamesOf(undone, "items").Should().Equal("Testes integrados", "Treinamento", "Go-live");
    }

    [Fact(DisplayName = "Ligação na conversa")]
    public async Task Linking_IsAnEventOfTheConversation_WithTheNamesOfThen()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.Team);
        var diego = await scene.Team.ClientOfAsync(scene.Team.Diego);

        await LinkedAsync(diego, number, scene.Tests);
        scene.Team.App.Clock.Advance(TimeSpan.FromMinutes(5));
        await LinkedAsync(diego, number, scene.Tests);
        await LinkedAsync(diego, number, scene.Training);

        var events = (await diego.GetFromJsonAsync<JsonElement>($"/api/tickets/{number}", Cancellation)).GetProperty("events")
            .EnumerateArray().ToList();
        events.Should().HaveCount(2, "the same list again saves nothing");
        events.Select(item => (
                item.GetProperty("kind").GetString(),
                item.GetProperty("actorId").GetGuid(),
                string.Join(", ", item.GetProperty("linkedItems").EnumerateArray().Select(name => name.GetString())),
                string.Join(", ", item.GetProperty("unlinkedItems").EnumerateArray().Select(name => name.GetString()))))
            .Should().Equal(
                ("ImpactsChanged", scene.Team.Diego, "Testes integrados", ""),
                ("ImpactsChanged", scene.Team.Diego, "Treinamento", "Testes integrados"));
    }

    [Fact(DisplayName = "Ligação na auditoria pelo nome atual")]
    public async Task Linking_IsInTheAudit_ByTheCurrentNames()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene.Team);
        await LinkedAsync(await scene.Team.ClientOfAsync(scene.Team.Diego), number, scene.Training);
        var marina = await scene.Team.ClientOfAsync(scene.Team.Marina);
        (await SendAsync(marina, HttpMethod.Put, $"{scene.Schedule}/items/{scene.Training}", new
        {
            phaseId = scene.Phase,
            name = "Treinamento dos usuários",
            start = "2026-10-26",
            duration = 2,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
        })).StatusCode.Should().Be(HttpStatusCode.OK);
        await LinkedAsync(await scene.Team.ClientOfAsync(scene.Team.Diego), number);

        var audit = await scene.Team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=TicketImpact", Cancellation);

        var entries = audit.GetProperty("items").EnumerateArray().ToList();
        entries.Select(entry => (entry.GetProperty("action").GetString(), entry.GetProperty("itemLabel").GetString()))
            .Should().BeEquivalentTo(new[]
            {
                ("Created", $"#{number} Rejeição 539 ao emitir nota de devolução"),
                ("Deleted", $"#{number} Rejeição 539 ao emitir nota de devolução"),
            });
        var created = entries.Single(entry => entry.GetProperty("action").GetString() == "Created");
        var fields = created.GetProperty("changes").EnumerateArray().ToDictionary(field => field.GetProperty("field").GetString()!);
        fields["ScheduleItemId"].GetProperty("after").GetString().Should().Be("Treinamento dos usuários");
        created.GetProperty("actor").GetProperty("id").GetGuid().Should().Be(scene.Team.Diego);
    }
}
