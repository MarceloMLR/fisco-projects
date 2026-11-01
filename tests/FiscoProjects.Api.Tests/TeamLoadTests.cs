using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: horas-do-projeto (API side): "Carga da equipe", "Capacidade da semana", "Estado da carga", "Pessoas da carga"
// and "Permissões na carga" (ADR-0142). Today is Monday 19/10/2026 in the clock of the tests; 02/11 is a holiday.
public sealed class TeamLoadTests(SqlServerFixture sqlServer)
{
    private const string Path = "/api/admin/team-load";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> PhaseAsync(ProjectTeam team, Guid project) =>
        await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{project}/schedule/phases", new { name = "Configuração" }));

    /// <summary>A task of 5 business days of the person, from the day, with the planned hours when given.</summary>
    private static async Task<Guid> TaskAsync(
        ProjectTeam team, Guid project, Guid phase, string name, string start, Guid? owner, decimal? planned, int progress = 0, bool byClient = false)
    {
        var task = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{project}/schedule/items", new
        {
            kind = "Task",
            phaseId = phase,
            name,
            start,
            duration = 5,
            ownerId = owner,
            ownedByClient = byClient,
            progress,
            predecessorIds = Array.Empty<Guid>(),
        }));
        if (planned is { } hours)
        {
            (await SendAsync(team.Admin, HttpMethod.Put, $"/api/admin/project-hours/{project}/tasks/{task}", new { hours }))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        return task;
    }

    private static async Task<JsonElement> LoadAsync(ProjectTeam team, string? week = null) =>
        await team.Admin.GetFromJsonAsync<JsonElement>(week is null ? Path : $"{Path}?week={week}", Cancellation);

    private static JsonElement PersonOf(JsonElement load, Guid person) =>
        load.GetProperty("people").EnumerateArray().Single(row => row.GetProperty("id").GetGuid() == person);

    private static (decimal Load, decimal Capacity, int AbsenceDays, string? State) Week(JsonElement week) => (
        week.GetProperty("load").GetDecimal(),
        week.GetProperty("capacity").GetDecimal(),
        week.GetProperty("absenceDays").GetInt32(),
        week.GetProperty("state").GetString());

    [Fact(DisplayName = "Carga entre projetos")]
    public async Task TheLoad_SumsTheTasksOfEveryOpenProject_AgainstTheCapacityWithTheAbsences()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var sped = await team.CreateProjectAsync("SPED-27", "Adequação do SPED Fiscal 2027");
        await team.App.AddProjectMemberAsync(sped, team.Diego, "Developer");
        var nfe = await TaskAsync(team, team.Erp, await PhaseAsync(team, team.Erp), "Integrar emissão de NF-e", "2026-10-26", team.Diego, 20m);
        var file = await TaskAsync(team, sped, await PhaseAsync(team, sped), "Gerar o arquivo do SPED", "2026-10-26", team.Diego, 30m);
        (await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{team.Erp}/members/{team.Diego}/absences", new { start = "2026-10-29", end = "2026-10-30" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var load = await LoadAsync(team, "2026-10-28");

        load.GetProperty("hoursPerDay").GetDecimal().Should().Be(8m);
        load.GetProperty("today").GetString().Should().Be("2026-10-19");
        var weeks = load.GetProperty("weeks").EnumerateArray().ToList();
        weeks.Should().HaveCount(8);
        (weeks[0].GetProperty("start").GetString(), weeks[0].GetProperty("end").GetString(), weeks[1].GetProperty("businessDays").GetInt32())
            .Should().Be(("2026-10-26", "2026-11-01", 4));
        var diego = PersonOf(load, team.Diego);
        diego.GetProperty("overloaded").GetBoolean().Should().BeTrue();
        var first = diego.GetProperty("weeks")[0];
        Week(first).Should().Be((50m, 24m, 2, "Over"));
        first.GetProperty("tasks").EnumerateArray()
            .Select(task => (task.GetProperty("itemId").GetGuid(), task.GetProperty("projectCode").GetString(), task.GetProperty("hours").GetDecimal()))
            .Should().Equal((file, "SPED-27", 30m), (nfe, "ERP-FIN", 20m));
        first.GetProperty("absences").EnumerateArray()
            .Select(absence => (absence.GetProperty("start").GetString(), absence.GetProperty("end").GetString()))
            .Should().Equal(("2026-10-29", "2026-10-30"));
        // The week of the holiday of 02/11 has 32 hours and nothing to do.
        Week(diego.GetProperty("weeks")[1]).Should().Be((0m, 32m, 0, "Free"));
    }

    [Fact(DisplayName = "O que não pesa")]
    public async Task TheDoneTasks_TheOnesWithoutPlannedHours_OfTheClient_AndOfClosedProjects_DoNotWeigh()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var client = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, "/api/clients", new { name = "Cliente Fictício" }));
        (await SendAsync(team.Admin, HttpMethod.Put, $"/api/projects/{team.Erp}", new
        {
            code = "ERP-FIN",
            name = "Implantação do ERP financeiro",
            description = (string?)null,
            clientId = client,
        })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var phase = await PhaseAsync(team, team.Erp);
        await TaskAsync(team, team.Erp, phase, "Mapear processos fiscais", "2026-10-19", team.Carla, 40m, progress: 100);
        await TaskAsync(team, team.Erp, phase, "Configurar plano de contas", "2026-10-19", team.Carla, null);
        await TaskAsync(team, team.Erp, phase, "Testes com a equipe do cliente", "2026-10-19", null, 40m, byClient: true);
        var trib = await team.CreateProjectAsync("TRIB-RS", "Revisão tributária da filial RS");
        await team.App.AddProjectMemberAsync(trib, team.Carla, "Contributor");
        await TaskAsync(team, trib, await PhaseAsync(team, trib), "Revisar créditos de ICMS", "2026-10-19", team.Carla, 40m);
        foreach (var status in new[] { "InProgress", "Completed" })
        {
            (await SendAsync(team.Admin, HttpMethod.Put, $"/api/projects/{trib}/status", new { status }))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        var load = await LoadAsync(team);

        load.GetProperty("weeks")[0].GetProperty("start").GetString().Should().Be("2026-10-19");
        load.GetProperty("people").EnumerateArray()
            .SelectMany(person => person.GetProperty("weeks").EnumerateArray())
            .Should().AllSatisfy(week => week.GetProperty("load").GetDecimal().Should().Be(0m));
        PersonOf(load, team.Carla).GetProperty("weeks")[0].GetProperty("state").GetString().Should().Be("Free");
    }

    [Fact(DisplayName = "Pessoa livre na carga")]
    public async Task ThePeopleOfTheOpenProjects_ShowEvenWithoutLoad_ByName()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        var load = await LoadAsync(team);

        load.GetProperty("people").EnumerateArray().Select(person => person.GetProperty("name").GetString())
            .Should().Equal("Carla Mendes", "Diego Torres", "Lucas Prado", "Marina Costa", "Rafael Souza");
        var lucas = PersonOf(load, team.Lucas);
        lucas.GetProperty("overloaded").GetBoolean().Should().BeFalse();
        lucas.GetProperty("weeks").EnumerateArray().Select(Week).Should().AllSatisfy(week => week.State.Should().Be("Free"));
        Week(lucas.GetProperty("weeks")[0]).Should().Be((0m, 40m, 0, "Free"));
    }

    [Fact(DisplayName = "Pessoa desativada fora da carga")]
    public async Task ADeactivatedPerson_IsNotInTheLoad()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        (await SendAsync(team.Admin, HttpMethod.Post, $"/api/users/{team.Lucas}/deactivate")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var load = await LoadAsync(team);

        load.GetProperty("people").EnumerateArray().Select(person => person.GetProperty("id").GetGuid())
            .Should().NotContain(team.Lucas).And.Contain(team.Carla);
    }

    [Fact(DisplayName = "Semana inválida na carga")]
    public async Task AWeekOutOfTheCentury_IsABadRequest()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        var response = await team.Admin.GetAsync($"{Path}?week=1999-12-31", Cancellation);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        problem.GetProperty("errors").EnumerateObject().Select(field => field.Name).Should().Equal("week");
    }

    [Theory(DisplayName = "Permissões na carga por papel")]
    [InlineData("Administrator", HttpStatusCode.OK)]
    [InlineData("Manager", HttpStatusCode.Forbidden)]
    [InlineData("TechLead", HttpStatusCode.Forbidden)]
    [InlineData("Contributor", HttpStatusCode.Forbidden)]
    [InlineData("Developer", HttpStatusCode.Forbidden)]
    [InlineData("Viewer", HttpStatusCode.Forbidden)]
    [InlineData("Outside", HttpStatusCode.Forbidden)]
    public async Task OnlyTheAdministrator_SeesTheLoad(string role, HttpStatusCode expected)
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var client = await team.ClientOfAsync(role);

        (await client.GetAsync(Path, Cancellation)).StatusCode.Should().Be(expected);
    }
}
