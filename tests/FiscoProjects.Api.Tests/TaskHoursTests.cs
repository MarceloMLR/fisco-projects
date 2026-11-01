using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: horas-do-projeto (API side): "Horas previstas da tarefa", "Horas previstas fora do cronograma", "Horas das
// tarefas", "Estado das horas da tarefa", "Permissões nas horas previstas" and "Horas previstas na auditoria" (ADR-0138).
public sealed class TaskHoursTests(SqlServerFixture sqlServer)
{
    private const string Path = "/api/admin/project-hours";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    /// <summary>Today in America/Sao_Paulo, the time zone of the installation, by the clock of the test.</summary>
    private static DateOnly TodayOf(ProjectTeam team) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
        team.App.Clock.GetUtcNow().UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")));

    private static string DayText(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static Task<HttpResponseMessage> PlanAsync(HttpClient client, Guid project, Guid item, decimal? hours) =>
        SendAsync(client, HttpMethod.Put, $"{Path}/{project}/tasks/{item}", new { hours });

    private static async Task PlannedAsync(ProjectTeam team, Guid project, Guid item, decimal? hours)
    {
        var planned = await PlanAsync(team.Admin, project, item, hours);
        planned.StatusCode.Should().Be(HttpStatusCode.NoContent, await planned.Content.ReadAsStringAsync(Cancellation));
    }

    private static async Task<JsonElement> TasksAsync(ProjectTeam team, Guid project) =>
        await team.Admin.GetFromJsonAsync<JsonElement>($"{Path}/{project}/tasks", Cancellation);

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> ItemAsync(ProjectTeam team, Guid project, Guid phase, string kind, string name, int days = 0) =>
        await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{project}/schedule/items", new
        {
            kind,
            phaseId = phase,
            name,
            start = DayText(TodayOf(team).AddDays(days)),
            duration = kind == "Task" ? 5 : 0,
            ownerId = (Guid?)null,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
        }));

    /// <summary>A phase of the schedule of the project, with two tasks and a milestone, made by the Administrator.</summary>
    private static async Task<(Guid Phase, Guid Task, Guid Other, Guid Milestone)> ScheduleOfAsync(ProjectTeam team, Guid project)
    {
        var phase = await IdOfAsync(await SendAsync(
            team.Admin, HttpMethod.Post, $"/api/projects/{project}/schedule/phases", new { name = "Configuração" }));
        var task = await ItemAsync(team, project, phase, "Task", "Parametrizar regras de ICMS");
        var other = await ItemAsync(team, project, phase, "Task", "Integrar emissão de NF-e", 1);
        var milestone = await ItemAsync(team, project, phase, "Milestone", "Ambiente pronto", 10);
        return (phase, task, other, milestone);
    }

    /// <summary>Hours logged by the person in the project, in the task when one is given, today or days before.</summary>
    private static async Task LogAsync(ProjectTeam team, Guid person, Guid project, decimal hours, Guid? task, int daysAgo = 0)
    {
        var client = await team.ClientOfAsync(person);
        var logged = await SendAsync(client, HttpMethod.Post, "/api/time-entries", new
        {
            projectId = project,
            day = DayText(TodayOf(team).AddDays(-daysAgo)),
            hours,
            description = "Trabalho na tarefa",
            scheduleItemId = task,
            ticketId = (Guid?)null,
        });
        logged.StatusCode.Should().Be(HttpStatusCode.Created, await logged.Content.ReadAsStringAsync(Cancellation));
    }

    private static (Guid Item, string? Name, decimal? Planned, decimal Logged, decimal? Balance, int? Consumption, string? State) Row(JsonElement item)
    {
        static decimal? Optional(JsonElement value) => value.ValueKind == JsonValueKind.Null ? null : value.GetDecimal();
        var balance = item.GetProperty("balance");
        return (
            item.GetProperty("itemId").GetGuid(),
            item.GetProperty("name").GetString(),
            Optional(item.GetProperty("planned")),
            item.GetProperty("logged").GetDecimal(),
            Optional(balance.GetProperty("balance")),
            balance.GetProperty("consumption").ValueKind == JsonValueKind.Null ? null : balance.GetProperty("consumption").GetInt32(),
            balance.GetProperty("state").GetString());
    }

    private static (decimal Planned, decimal Logged, decimal Outside) Totals(JsonElement tasks) => (
        tasks.GetProperty("planned").GetDecimal(), tasks.GetProperty("logged").GetDecimal(), tasks.GetProperty("outside").GetDecimal());

    [Fact(DisplayName = "Pôr as horas previstas")]
    public async Task TheAdministrator_PutsAndChangesThePlannedHours()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task, _, _) = await ScheduleOfAsync(team, team.Erp);

        await PlannedAsync(team, team.Erp, task, 40m);
        await PlannedAsync(team, team.Erp, task, 48m);

        var tasks = await TasksAsync(team, team.Erp);
        tasks.GetProperty("items").EnumerateArray().Select(Row).Should().Equal(
            (task, "Parametrizar regras de ICMS", 48m, 0m, 48m, 0, "Within"));
        Totals(tasks).Should().Be((48m, 0m, 0m));
    }

    [Fact(DisplayName = "Cronograma igual com as previstas")]
    public async Task ThePlannedHours_DoNotChangeTheSchedule()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task, _, _) = await ScheduleOfAsync(team, team.Erp);
        var before = await team.Admin.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}/schedule", Cancellation);

        await PlannedAsync(team, team.Erp, task, 40m);

        var after = await team.Admin.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}/schedule", Cancellation);
        after.GetProperty("phases").ToString().Should().Be(before.GetProperty("phases").ToString());
        after.GetProperty("undo").ToString().Should().Be(before.GetProperty("undo").ToString(), "the planned hours are out of the undo");
    }

    [Fact(DisplayName = "Tirar as horas previstas")]
    public async Task TakingOutThePlannedHours_TakesTheTaskOutOfTheList()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task, _, _) = await ScheduleOfAsync(team, team.Erp);
        await PlannedAsync(team, team.Erp, task, 8m);

        await PlannedAsync(team, team.Erp, task, null);
        // Taking out what is not there changes nothing.
        await PlannedAsync(team, team.Erp, task, null);

        var tasks = await TasksAsync(team, team.Erp);
        tasks.GetProperty("items").GetArrayLength().Should().Be(0);
        Totals(tasks).Should().Be((0m, 0m, 0m));
    }

    [Theory(DisplayName = "Horas previstas inválidas")]
    [InlineData(0)]
    [InlineData(10.1)]
    [InlineData(10000)]
    public async Task InvalidPlannedHours_AreRefused(double hours)
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task, _, _) = await ScheduleOfAsync(team, team.Erp);

        var refused = await PlanAsync(team.Admin, team.Erp, task, (decimal)hours);

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await refused.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").EnumerateObject()
            .Select(error => error.Name).Should().Equal("hours");
        (await TasksAsync(team, team.Erp)).GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "Horas previstas fora de uma tarefa")]
    public async Task AMilestone_APhase_OrATaskOfAnotherProject_AreRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (phase, _, _, milestone) = await ScheduleOfAsync(team, team.Erp);
        var sped = await team.CreateProjectAsync("SPED-27", "Adequação do SPED Fiscal 2027");
        var (_, spedTask, _, _) = await ScheduleOfAsync(team, sped);

        foreach (var item in new[] { milestone, phase, spedTask })
        {
            (await PlanAsync(team.Admin, team.Erp, item, 8m)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        (await PlanAsync(team.Admin, Guid.CreateVersion7(), spedTask, 8m)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await team.Admin.GetAsync($"{Path}/{Guid.CreateVersion7()}/tasks", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await TasksAsync(team, team.Erp)).GetProperty("items").GetArrayLength().Should().Be(0);
        (await TasksAsync(team, sped)).GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "Horas previstas na exclusão do projeto")]
    public async Task ThePlannedHours_GoWithTheDeletedProject()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task, other, _) = await ScheduleOfAsync(team, team.Erp);
        await PlannedAsync(team, team.Erp, task, 40m);
        await PlannedAsync(team, team.Erp, other, 16m);

        var deleted = await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{team.Erp}/deletion", new { code = "ERP-FIN" });

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleted.Content.ReadAsStringAsync(Cancellation));
        (await team.App.CountAsync("SELECT COUNT(*) FROM [timetracking].[planned_hours]")).Should().Be(0);
    }

    [Fact(DisplayName = "Horas fora das tarefas")]
    public async Task HoursWithoutATask_OrInARemovedTask_AreOutside()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task, other, _) = await ScheduleOfAsync(team, team.Erp);
        await LogAsync(team, team.Carla, team.Erp, 3m, null);
        await LogAsync(team, team.Diego, team.Erp, 2m, other);
        await LogAsync(team, team.Carla, team.Erp, 4m, task);
        await PlannedAsync(team, team.Erp, other, 16m);
        (await SendAsync(team.Admin, HttpMethod.Delete, $"/api/projects/{team.Erp}/schedule/items/{other}")).IsSuccessStatusCode.Should().BeTrue();

        var tasks = await TasksAsync(team, team.Erp);

        // The task without planned hours shows with the logged ones; the removed task is out, with its planned hours.
        tasks.GetProperty("items").EnumerateArray().Select(Row).Should().Equal(
            (task, "Parametrizar regras de ICMS", (decimal?)null, 4m, (decimal?)null, (int?)null, "NoContract"));
        Totals(tasks).Should().Be((0m, 4m, 5m));
    }

    [Fact(DisplayName = "Tarefa acima das previstas")]
    public async Task TheLoggedOfEveryone_GoPastThePlanned()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task, _, _) = await ScheduleOfAsync(team, team.Erp);
        await PlannedAsync(team, team.Erp, task, 10m);
        await LogAsync(team, team.Carla, team.Erp, 8m, task);
        await LogAsync(team, team.Diego, team.Erp, 5m, task);

        Row((await TasksAsync(team, team.Erp)).GetProperty("items")[0])
            .Should().Be((task, "Parametrizar regras de ICMS", 10m, 13m, -3m, 130, "Over"));
    }

    [Fact(DisplayName = "Tarefa concluída perto das previstas")]
    public async Task ATaskAt100Percent_IsWithin_NearThePlanned()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task, other, _) = await ScheduleOfAsync(team, team.Erp);
        await PlannedAsync(team, team.Erp, task, 20m);
        await PlannedAsync(team, team.Erp, other, 20m);
        await LogAsync(team, team.Carla, team.Erp, 12m, task, 1);
        await LogAsync(team, team.Carla, team.Erp, 7m, task);
        await LogAsync(team, team.Diego, team.Erp, 18m, other);
        (await SendAsync(team.Admin, HttpMethod.Put, $"/api/projects/{team.Erp}/schedule/items/{task}/progress", new { progress = 100 }))
            .IsSuccessStatusCode.Should().BeTrue();

        var rows = (await TasksAsync(team, team.Erp)).GetProperty("items").EnumerateArray().Select(Row).ToList();

        rows.Select(row => (row.Item, row.State)).Should().Equal((task, "Within"), (other, "NearLimit"));
    }

    [Theory(DisplayName = "Permissões nas horas previstas por papel")]
    [InlineData("Administrator", true)]
    [InlineData("Manager", false)]
    [InlineData("TechLead", false)]
    [InlineData("Contributor", false)]
    public async Task Permissions_OnlyTheAdministrator(string role, bool allowed)
    {
        // Paulo is the Administrator, Marina a project manager, Rafael a tech lead and Carla a user in the system, the
        // three of them in the project.
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task, _, _) = await ScheduleOfAsync(team, team.Erp);
        var client = await team.ClientOfAsync(role);

        var responses = new[]
        {
            await client.GetAsync($"{Path}/{team.Erp}/tasks", Cancellation),
            await PlanAsync(client, team.Erp, task, 12m),
        };

        responses.Select(response => response.IsSuccessStatusCode).Should().AllBeEquivalentTo(allowed);
        if (!allowed)
        {
            responses.Select(response => response.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.Forbidden);
            (await TasksAsync(team, team.Erp)).GetProperty("items").GetArrayLength().Should().Be(0);
        }
    }

    [Fact(DisplayName = "Horas previstas na auditoria")]
    public async Task ThePlannedHours_AreInTheAudit_WithTheTaskByName()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task, _, _) = await ScheduleOfAsync(team, team.Erp);
        await PlannedAsync(team, team.Erp, task, 40m);
        await PlannedAsync(team, team.Erp, task, 48m);

        var audit = await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation);
        var records = audit.GetProperty("items").EnumerateArray()
            .Where(record => record.GetProperty("itemType").GetString() == "PlannedHours")
            .ToList();

        records.Select(record => record.GetProperty("action").GetString()).Should().BeEquivalentTo(["Created", "Updated"]);
        records.Should().AllSatisfy(record =>
        {
            record.GetProperty("actor").GetProperty("id").GetGuid().Should().Be(team.Paulo);
            record.GetProperty("itemLabel").GetString().Should().Be("Parametrizar regras de ICMS");
            record.ToString().Should().Contain("Implantação do ERP financeiro");
        });
    }
}
