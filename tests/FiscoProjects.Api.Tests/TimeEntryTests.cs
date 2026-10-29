using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: horas-do-projeto (API side): "Apontamento de horas", "Tarefa ou chamado do apontamento", "Horas de cada pessoa",
// "Horas de outra pessoa pelo administrador", "Horas de quem saiu do projeto", "Horas na auditoria", "Permissões nas
// horas" and "Opções para apontar" (ADR-0128).
public sealed class TimeEntryTests(SqlServerFixture sqlServer)
{
    private const string Path = "/api/time-entries";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    /// <summary>Today in America/Sao_Paulo, the time zone of the installation, by the clock of the test.</summary>
    private static DateOnly TodayOf(ProjectTeam team) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
        team.App.Clock.GetUtcNow().UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")));

    private static string DayText(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static Task<HttpResponseMessage> LogAsync(
        HttpClient client, Guid project, DateOnly day, decimal hours, string description = "Reunião de levantamento",
        Guid? scheduleItemId = null, Guid? ticketId = null) =>
        SendAsync(client, HttpMethod.Post, Path, new { projectId = project, day = DayText(day), hours, description, scheduleItemId, ticketId });

    private static async Task<Guid> LoggedAsync(
        HttpClient client, Guid project, DateOnly day, decimal hours, string description = "Reunião de levantamento",
        Guid? scheduleItemId = null, Guid? ticketId = null)
    {
        var logged = await LogAsync(client, project, day, hours, description, scheduleItemId, ticketId);
        logged.StatusCode.Should().Be(HttpStatusCode.Created, await logged.Content.ReadAsStringAsync(Cancellation));
        return (await logged.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> ChangeAsync(
        HttpClient client, Guid entry, Guid project, DateOnly day, decimal hours, string description = "Reunião de levantamento",
        Guid? scheduleItemId = null, Guid? ticketId = null) =>
        SendAsync(client, HttpMethod.Put, $"{Path}/{entry}", new { projectId = project, day = DayText(day), hours, description, scheduleItemId, ticketId });

    private static async Task<JsonElement> WeekAsync(HttpClient client, ProjectTeam team, Guid? personId = null)
    {
        var today = TodayOf(team);
        var person = personId is { } id ? $"&personId={id}" : string.Empty;
        return await client.GetFromJsonAsync<JsonElement>($"{Path}?from={DayText(today.AddDays(-6))}&to={DayText(today)}{person}", Cancellation);
    }

    private static async Task<string?> FirstErrorFieldAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").EnumerateObject().First().Name;
    }

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    /// <summary>A phase of the schedule of the project, with a task and a milestone, made by the Administrator.</summary>
    private static async Task<(Guid Phase, Guid Task, Guid Milestone)> ScheduleOfAsync(ProjectTeam team, Guid project, string task = "Configurar o ambiente")
    {
        var path = $"/api/projects/{project}/schedule";
        var phase = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{path}/phases", new { name = "Configuração" }));
        var taskId = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{path}/items", new
        {
            kind = "Task",
            phaseId = phase,
            name = task,
            start = DayText(TodayOf(team)),
            duration = 5,
            ownerId = (Guid?)null,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
        }));
        var milestone = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{path}/items", new
        {
            kind = "Milestone",
            phaseId = phase,
            name = "Ambiente pronto",
            start = DayText(TodayOf(team).AddDays(10)),
            duration = 0,
            ownerId = (Guid?)null,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
        }));
        return (phase, taskId, milestone);
    }

    /// <summary>A ticket of the project opened by Carla, with its id and number.</summary>
    private static async Task<(Guid Id, int Number)> TicketOfAsync(ProjectTeam team, Guid project, string title = "Rejeição 539 na emissão")
    {
        var carla = await team.ClientOfAsync(team.Carla);
        var opened = await SendAsync(carla, HttpMethod.Post, "/api/tickets", new
        {
            projectId = project,
            team = "Functional",
            priority = "Medium",
            title,
            description = "Descrição do chamado.",
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        var number = (await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
        var id = (await team.Admin.GetFromJsonAsync<JsonElement>($"/api/tickets/{number}", Cancellation)).GetProperty("id").GetGuid();
        return (id, number);
    }

    /// <summary>Another project, SPED-27, with Carla as contributor.</summary>
    private static async Task<Guid> SpedAsync(ProjectTeam team)
    {
        var sped = await team.CreateProjectAsync("SPED-27", "Adequação do SPED Fiscal 2027");
        await team.App.AddProjectMemberAsync(sped, team.Carla, "Contributor");
        return sped;
    }

    [Fact(DisplayName = "Apontar horas no projeto")]
    public async Task Logging_ShowsInTheListOfThePerson()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);

        var id = await LoggedAsync(carla, team.Erp, TodayOf(team).AddDays(-1), 2.5m, "  Reunião de levantamento  ");

        var week = await WeekAsync(carla, team);
        var item = week.GetProperty("items").EnumerateArray().Single();
        item.GetProperty("id").GetGuid().Should().Be(id);
        (item.GetProperty("day").GetString(), item.GetProperty("hours").GetDecimal(), item.GetProperty("description").GetString())
            .Should().Be((DayText(TodayOf(team).AddDays(-1)), 2.5m, "Reunião de levantamento"));
        item.GetProperty("project").GetProperty("code").GetString().Should().Be("ERP-FIN");
        item.GetProperty("canChange").GetBoolean().Should().BeTrue();
        (item.GetProperty("task").ValueKind, item.GetProperty("ticket").ValueKind).Should().Be((JsonValueKind.Null, JsonValueKind.Null));
        week.GetProperty("total").GetDecimal().Should().Be(2.5m);
        (await team.App.CountAsync(
            "SELECT COUNT(*) FROM [timetracking].[time_entries] WHERE UserId = @carla AND CreatedAt = @at",
            ("@carla", team.Carla), ("@at", team.App.Clock.GetUtcNow().UtcDateTime))).Should().Be(1, "who logged and when are kept");
    }

    [Fact(DisplayName = "Dia depois de hoje")]
    public async Task Tomorrow_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);

        (await FirstErrorFieldAsync(await LogAsync(carla, team.Erp, TodayOf(team).AddDays(1), 2m))).Should().Be("day");
        (await team.App.CountAsync("SELECT COUNT(*) FROM [timetracking].[time_entries]")).Should().Be(0);
    }

    [Theory(DisplayName = "Horas fora dos quartos de hora")]
    [InlineData(1.1)]
    [InlineData(0)]
    [InlineData(25)]
    public async Task HoursOutOfQuarters_AreRefused(double hours)
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);

        (await FirstErrorFieldAsync(await LogAsync(carla, team.Erp, TodayOf(team), (decimal)hours))).Should().Be("hours");
    }

    [Fact(DisplayName = "Mais de 24 horas no dia")]
    public async Task MoreThan24HoursInADay_AreRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var sped = await SpedAsync(team);
        var carla = await team.ClientOfAsync(team.Carla);
        await LoggedAsync(carla, team.Erp, TodayOf(team), 20m);

        (await FirstErrorFieldAsync(await LogAsync(carla, sped, TodayOf(team), 5m))).Should().Be("hours");
        await LoggedAsync(carla, sped, TodayOf(team), 4m);
        (await WeekAsync(carla, team)).GetProperty("total").GetDecimal().Should().Be(24m);
    }

    [Fact(DisplayName = "Apontamento sem descrição")]
    public async Task NoDescription_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);

        (await FirstErrorFieldAsync(await LogAsync(carla, team.Erp, TodayOf(team), 2m, "   "))).Should().Be("description");
    }

    [Fact(DisplayName = "Apontar numa tarefa")]
    public async Task LoggingOnATask_ShowsTheTask()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task, _) = await ScheduleOfAsync(team, team.Erp);
        var diego = await team.ClientOfAsync(team.Diego);

        await LoggedAsync(diego, team.Erp, TodayOf(team), 3m, "Instalação", scheduleItemId: task);

        var item = (await WeekAsync(diego, team)).GetProperty("items").EnumerateArray().Single();
        (item.GetProperty("task").GetProperty("id").GetGuid(), item.GetProperty("task").GetProperty("name").GetString())
            .Should().Be((task, "Configurar o ambiente"));
    }

    [Fact(DisplayName = "Apontar num chamado")]
    public async Task LoggingOnATicket_ShowsTheTicket()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (ticket, number) = await TicketOfAsync(team, team.Erp);
        var carla = await team.ClientOfAsync(team.Carla);

        await LoggedAsync(carla, team.Erp, TodayOf(team), 1m, "Análise da rejeição", ticketId: ticket);

        var item = (await WeekAsync(carla, team)).GetProperty("items").EnumerateArray().Single().GetProperty("ticket");
        (item.GetProperty("number").GetInt32(), item.GetProperty("title").GetString()).Should().Be((number, "Rejeição 539 na emissão"));
    }

    [Fact(DisplayName = "Tarefa de outro projeto")]
    public async Task ATaskOfAnotherProject_OrNotATask_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var sped = await SpedAsync(team);
        var (phase, _, milestone) = await ScheduleOfAsync(team, team.Erp);
        var (_, spedTask, _) = await ScheduleOfAsync(team, sped, "Levantar mudanças do leiaute");
        var carla = await team.ClientOfAsync(team.Carla);

        foreach (var item in new[] { spedTask, milestone, phase })
        {
            (await FirstErrorFieldAsync(await LogAsync(carla, team.Erp, TodayOf(team), 1m, scheduleItemId: item))).Should().Be("scheduleItemId");
        }
    }

    [Fact(DisplayName = "Chamado de outro projeto")]
    public async Task ATicketOfAnotherProject_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var sped = await SpedAsync(team);
        var (ticket, _) = await TicketOfAsync(team, sped);
        var carla = await team.ClientOfAsync(team.Carla);

        (await FirstErrorFieldAsync(await LogAsync(carla, team.Erp, TodayOf(team), 1m, ticketId: ticket))).Should().Be("ticketId");
    }

    [Fact(DisplayName = "Tarefa e chamado juntos")]
    public async Task ATaskAndATicket_AreRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task, _) = await ScheduleOfAsync(team, team.Erp);
        var (ticket, _) = await TicketOfAsync(team, team.Erp);
        var carla = await team.ClientOfAsync(team.Carla);

        (await LogAsync(carla, team.Erp, TodayOf(team), 1m, scheduleItemId: task, ticketId: ticket)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Tarefa excluída depois")]
    public async Task ADeletedTask_KeepsTheEntry()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task, _) = await ScheduleOfAsync(team, team.Erp);
        var diego = await team.ClientOfAsync(team.Diego);
        var entry = await LoggedAsync(diego, team.Erp, TodayOf(team), 3m, "Instalação", scheduleItemId: task);

        (await SendAsync(team.Admin, HttpMethod.Delete, $"/api/projects/{team.Erp}/schedule/items/{task}")).IsSuccessStatusCode.Should().BeTrue();

        var item = (await WeekAsync(diego, team)).GetProperty("items").EnumerateArray().Single();
        item.GetProperty("hours").GetDecimal().Should().Be(3m);
        item.GetProperty("task").GetProperty("id").GetGuid().Should().Be(task);
        item.GetProperty("task").GetProperty("name").ValueKind.Should().Be(JsonValueKind.Null, "the task left the schedule");
        (await ChangeAsync(diego, entry, team.Erp, TodayOf(team), 2m, "Instalação", scheduleItemId: task))
            .StatusCode.Should().Be(HttpStatusCode.NoContent, "the task kept from the entry stays");
    }

    [Fact(DisplayName = "Só as próprias horas")]
    public async Task ThePerson_SeesOnlyTheOwnHours()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var sped = await SpedAsync(team);
        var carla = await team.ClientOfAsync(team.Carla);
        var diego = await team.ClientOfAsync(team.Diego);
        await LoggedAsync(carla, team.Erp, TodayOf(team).AddDays(-2), 4m);
        await LoggedAsync(carla, sped, TodayOf(team), 2.5m, "Reunião semanal com o cliente");
        await LoggedAsync(diego, team.Erp, TodayOf(team), 6m, "Integração");

        var week = await WeekAsync(carla, team);

        week.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("project").GetProperty("code").GetString())
            .Should().Equal("SPED-27", "ERP-FIN");
        week.GetProperty("total").GetDecimal().Should().Be(6.5m);
    }

    [Fact(DisplayName = "Horas de outra pessoa")]
    public async Task TheHoursOfAnotherPerson_AreNotSeen()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);
        var diego = await team.ClientOfAsync(team.Diego);
        var entry = await LoggedAsync(diego, team.Erp, TodayOf(team), 6m, "Integração");

        var list = await carla.GetAsync($"{Path}?from={DayText(TodayOf(team))}&to={DayText(TodayOf(team))}&personId={team.Diego}", Cancellation);
        var changed = await ChangeAsync(carla, entry, team.Erp, TodayOf(team), 1m);
        var removed = await SendAsync(carla, HttpMethod.Delete, $"{Path}/{entry}");

        (list.StatusCode, changed.StatusCode, removed.StatusCode)
            .Should().Be((HttpStatusCode.Forbidden, HttpStatusCode.NotFound, HttpStatusCode.NotFound));
        (await WeekAsync(diego, team)).GetProperty("total").GetDecimal().Should().Be(6m);
    }

    [Fact(DisplayName = "Período longo demais")]
    public async Task APeriodLongerThan93Days_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);
        var today = TodayOf(team);

        var longer = await carla.GetAsync($"{Path}?from={DayText(today.AddDays(-93))}&to={DayText(today)}", Cancellation);
        var longest = await carla.GetAsync($"{Path}?from={DayText(today.AddDays(-92))}&to={DayText(today)}", Cancellation);

        (longer.StatusCode, longest.StatusCode).Should().Be((HttpStatusCode.BadRequest, HttpStatusCode.OK));
    }

    [Fact(DisplayName = "Administrador corrige as horas de outra pessoa")]
    public async Task TheAdministrator_CorrectsTheHoursOfAnyone()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var diego = await team.ClientOfAsync(team.Diego);
        var entry = await LoggedAsync(diego, team.Erp, TodayOf(team), 8m, "Integração");

        var seen = await WeekAsync(team.Admin, team, team.Diego);
        var changed = await ChangeAsync(team.Admin, entry, team.Erp, TodayOf(team), 6m, "Integração");

        seen.GetProperty("items").EnumerateArray().Single().GetProperty("canChange").GetBoolean().Should().BeTrue();
        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await WeekAsync(diego, team)).GetProperty("items")[0].GetProperty("hours").GetDecimal().Should().Be(6m);
        var audit = await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation);
        audit.GetProperty("items").EnumerateArray()
            .Should().Contain(record => record.GetProperty("itemType").GetString() == "TimeEntry"
                && record.GetProperty("action").GetString() == "Updated"
                && record.GetProperty("actor").GetProperty("id").GetGuid() == team.Paulo);
    }

    [Fact(DisplayName = "Quem saiu do projeto")]
    public async Task WhoLeftTheProject_NoLongerLogsOrChanges_AndTheHoursStay()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);
        var entry = await LoggedAsync(carla, team.Erp, TodayOf(team), 4m);
        var marina = await team.ClientOfAsync(team.Marina);

        (await SendAsync(marina, HttpMethod.Delete, $"/api/projects/{team.Erp}/members/{team.Carla}")).IsSuccessStatusCode.Should().BeTrue();

        (await LogAsync(carla, team.Erp, TodayOf(team), 1m)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ChangeAsync(carla, entry, team.Erp, TodayOf(team), 1m)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(carla, HttpMethod.Delete, $"{Path}/{entry}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var item = (await WeekAsync(carla, team)).GetProperty("items").EnumerateArray().Single();
        (item.GetProperty("hours").GetDecimal(), item.GetProperty("canChange").GetBoolean()).Should().Be((4m, false));
    }

    [Fact(DisplayName = "Horas na auditoria")]
    public async Task Changes_AreInTheAudit()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);
        var entry = await LoggedAsync(carla, team.Erp, TodayOf(team), 4m, "Levantamento");
        (await ChangeAsync(carla, entry, team.Erp, TodayOf(team), 3m, "Levantamento")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SendAsync(carla, HttpMethod.Delete, $"{Path}/{entry}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var audit = await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation);
        var records = audit.GetProperty("items").EnumerateArray()
            .Where(record => record.GetProperty("itemType").GetString() == "TimeEntry")
            .ToList();

        records.Select(record => record.GetProperty("action").GetString()).Should().BeEquivalentTo(["Created", "Updated", "Deleted"]);
        audit.ToString().Should().Contain("ERP-FIN").And.Contain("Carla Mendes");
    }

    [Theory(DisplayName = "Permissões nas horas por papel")]
    [InlineData("Administrator", true, true)]
    [InlineData("Manager", true, false)]
    [InlineData("TechLead", true, false)]
    [InlineData("Contributor", true, false)]
    [InlineData("Developer", true, false)]
    [InlineData("Viewer", false, false)]
    public async Task Permissions_FollowTheMatrix(string role, bool logs, bool changesOthers)
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var client = await team.ClientOfAsync(role);
        var other = role == "Contributor" ? team.Diego : team.Carla;
        var othersEntry = await LoggedAsync(await team.ClientOfAsync(other), team.Erp, TodayOf(team), 2m, "Do colega");

        var logged = await LogAsync(client, team.Erp, TodayOf(team), 1m, $"Horas de {role}");
        var own = logged.IsSuccessStatusCode ? (await logged.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid() : (Guid?)null;
        var changedOwn = own is { } id ? await ChangeAsync(client, id, team.Erp, TodayOf(team), 1.5m) : null;
        var removedOwn = own is { } removable ? await SendAsync(client, HttpMethod.Delete, $"{Path}/{removable}") : null;
        var changedOthers = await ChangeAsync(client, othersEntry, team.Erp, TodayOf(team), 3m, "Do colega");
        var removedOthers = await SendAsync(client, HttpMethod.Delete, $"{Path}/{othersEntry}");

        logged.StatusCode.Should().Be(logs ? HttpStatusCode.Created : HttpStatusCode.Forbidden);
        if (logs)
        {
            (changedOwn!.StatusCode, removedOwn!.StatusCode).Should().Be((HttpStatusCode.NoContent, HttpStatusCode.NoContent));
        }

        (changedOthers.StatusCode, removedOthers.StatusCode).Should().Be(changesOthers
            ? (HttpStatusCode.NoContent, HttpStatusCode.NoContent)
            : (HttpStatusCode.NotFound, HttpStatusCode.NotFound));
    }

    [Fact(DisplayName = "Administrador fora do projeto")]
    public async Task TheAdministrator_LogsWithoutBeingInTheProject()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        await LoggedAsync(team.Admin, team.Erp, TodayOf(team), 1m, "Revisão do contrato");

        (await WeekAsync(team.Admin, team)).GetProperty("total").GetDecimal().Should().Be(1m);
    }

    [Fact(DisplayName = "Horas de um projeto de fora")]
    public async Task OutsideTheProject_IsNotFound()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var sofia = await team.ClientOfAsync(team.Sofia);

        (await LogAsync(sofia, team.Erp, TodayOf(team), 1m)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await sofia.GetAsync($"{Path}/projects/{team.Erp}/work", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Horas de projeto encerrado")]
    public async Task AClosedProject_TakesNoHours()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);
        var entry = await LoggedAsync(carla, team.Erp, TodayOf(team), 4m);
        var marina = await team.ClientOfAsync(team.Marina);
        (await SendAsync(marina, HttpMethod.Put, $"/api/projects/{team.Erp}/status", new { status = "Completed" })).IsSuccessStatusCode.Should().BeTrue();

        var logged = await LogAsync(carla, team.Erp, TodayOf(team), 1m);
        var changed = await ChangeAsync(carla, entry, team.Erp, TodayOf(team), 2m);
        var removed = await SendAsync(carla, HttpMethod.Delete, $"{Path}/{entry}");

        foreach (var response in new[] { logged, changed, removed })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ProjectTeam.ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:project-closed");
        }

        (await WeekAsync(carla, team)).GetProperty("total").GetDecimal().Should().Be(4m);
    }

    [Fact(DisplayName = "Projetos para apontar")]
    public async Task TheProjectsToLog_AreTheOpenOnesOfThePerson_ButAsViewer()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var sped = await team.CreateProjectAsync("SPED-27", "Adequação do SPED Fiscal 2027");
        await team.App.AddProjectMemberAsync(sped, team.Carla, "Viewer");
        var trib = await team.CreateProjectAsync("TRIB-RS", "Revisão tributária de filial");
        await team.App.AddProjectMemberAsync(trib, team.Carla, "Contributor");
        var marina = await team.ClientOfAsync(team.Marina);
        (await SendAsync(marina, HttpMethod.Put, $"/api/projects/{trib}/status", new { status = "Completed" })).IsSuccessStatusCode.Should().BeTrue();
        var carla = await team.ClientOfAsync(team.Carla);

        var projects = await carla.GetFromJsonAsync<JsonElement>($"{Path}/projects", Cancellation);
        var all = await team.Admin.GetFromJsonAsync<JsonElement>($"{Path}/projects", Cancellation);

        projects.EnumerateArray().Select(project => project.GetProperty("code").GetString()).Should().Equal("ERP-FIN");
        all.EnumerateArray().Select(project => project.GetProperty("code").GetString()).Should().Equal("ERP-FIN", "SPED-27");
    }

    [Fact(DisplayName = "Tarefas e chamados do projeto")]
    public async Task TheWorkOfTheProject_IsItsTasksAndTickets()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task, _) = await ScheduleOfAsync(team, team.Erp);
        var (ticket, number) = await TicketOfAsync(team, team.Erp);
        var carla = await team.ClientOfAsync(team.Carla);

        var work = await carla.GetFromJsonAsync<JsonElement>($"{Path}/projects/{team.Erp}/work", Cancellation);

        work.GetProperty("tasks").EnumerateArray().Select(item => (item.GetProperty("id").GetGuid(), item.GetProperty("name").GetString()))
            .Should().Equal((task, "Configurar o ambiente"));
        var only = work.GetProperty("tickets").EnumerateArray().Single();
        (only.GetProperty("id").GetGuid(), only.GetProperty("number").GetInt32(), only.GetProperty("isClosed").GetBoolean())
            .Should().Be((ticket, number, false));
    }
}
