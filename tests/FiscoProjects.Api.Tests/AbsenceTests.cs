using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Projects.Application;
using FiscoProjects.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FiscoProjects.Api.Tests;

// Spec: membros-do-projeto, requirements "Ausências das pessoas", "Quem cadastra as ausências", "Ausências na lista de
// Membros" and "Ausências na auditoria"; cronograma, "Responsável ausente durante a tarefa"; dados-pessoais, "Retenção
// das ausências" (ADR-0141).
public sealed class AbsenceTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    /// <summary>Today in America/Sao_Paulo, the time zone of the installation, by the clock of the test.</summary>
    private static DateOnly TodayOf(ProjectTeam team) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
        team.App.Clock.GetUtcNow().UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")));

    private static string DayText(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string PathOf(ProjectTeam team, Guid person, Guid? project = null) =>
        $"/api/projects/{project ?? team.Erp}/members/{person}/absences";

    private static Task<HttpResponseMessage> AddAsync(HttpClient client, string path, DateOnly start, DateOnly end) =>
        SendAsync(client, HttpMethod.Post, path, new { start = DayText(start), end = DayText(end) });

    private static async Task<Guid> AddedAsync(ProjectTeam team, Guid person, DateOnly start, DateOnly end)
    {
        var added = await AddAsync(await team.ClientOfAsync(team.Marina), PathOf(team, person), start, end);
        added.StatusCode.Should().Be(HttpStatusCode.Created, await added.Content.ReadAsStringAsync(Cancellation));
        return (await added.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<List<(string? Start, string? End)>> PeriodsAsync(HttpClient client, string path) =>
        [.. (await client.GetFromJsonAsync<JsonElement>(path, Cancellation)).GetProperty("absences").EnumerateArray()
            .Select(absence => (absence.GetProperty("start").GetString(), absence.GetProperty("end").GetString()))];

    [Fact(DisplayName = "Incluir ausência")]
    public async Task TheManager_IncludesAnAbsence_SeenInEveryProjectOfThePerson()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var sped = await team.CreateProjectAsync("SPED-27", "Adequação do SPED Fiscal 2027");
        await team.App.AddProjectMemberAsync(sped, team.Carla, "Contributor");
        var today = TodayOf(team);

        await AddedAsync(team, team.Carla, today.AddDays(15), today.AddDays(26));

        var expected = (DayText(today.AddDays(15)), DayText(today.AddDays(26)));
        (await PeriodsAsync(await team.ClientOfAsync(team.Diego), PathOf(team, team.Carla))).Should().Equal(expected);
        (await PeriodsAsync(team.Admin, PathOf(team, team.Carla, sped))).Should().Equal(expected);
    }

    [Fact(DisplayName = "Ausência inválida")]
    public async Task TheEndBeforeTheStart_OrMoreThan366Days_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        var today = TodayOf(team);

        foreach (var (start, end) in new[] { (today.AddDays(5), today), (today, today.AddDays(366)) })
        {
            var refused = await AddAsync(marina, PathOf(team, team.Carla), start, end);
            refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await refused.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").EnumerateObject()
                .Select(error => error.Name).Should().Equal("end");
        }

        (await PeriodsAsync(marina, PathOf(team, team.Carla))).Should().BeEmpty();
    }

    [Fact(DisplayName = "Ausência sobreposta")]
    public async Task AnAbsenceOverAnother_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var today = TodayOf(team);
        await AddedAsync(team, team.Carla, today.AddDays(10), today.AddDays(20));

        var refused = await AddAsync(await team.ClientOfAsync(team.Marina), PathOf(team, team.Carla), today.AddDays(20), today.AddDays(22));

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(refused)).Should().Be("urn:fiscoprojects:problem:absence-overlap");
        // Another person on the same days is no overlap.
        await AddedAsync(team, team.Diego, today.AddDays(20), today.AddDays(22));
    }

    [Fact(DisplayName = "Apagar ausência")]
    public async Task TheManager_RemovesAnAbsence()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var today = TodayOf(team);
        var absence = await AddedAsync(team, team.Carla, today.AddDays(10), today.AddDays(20));
        var marina = await team.ClientOfAsync(team.Marina);

        (await SendAsync(marina, HttpMethod.Delete, $"{PathOf(team, team.Carla)}/{absence}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await PeriodsAsync(marina, PathOf(team, team.Carla))).Should().BeEmpty();
        // Not of the person in the path, or gone: not found.
        (await SendAsync(marina, HttpMethod.Delete, $"{PathOf(team, team.Carla)}/{absence}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory(DisplayName = "Permissões nas ausências por papel")]
    [InlineData("Administrator", HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent)]
    [InlineData("Manager", HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent)]
    [InlineData("TechLead", HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden)]
    [InlineData("Contributor", HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden)]
    [InlineData("Developer", HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden)]
    [InlineData("Viewer", HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden)]
    [InlineData("Outside", HttpStatusCode.NotFound, HttpStatusCode.NotFound, HttpStatusCode.NotFound)]
    public async Task Permissions_TheAdministratorAndTheManager(string role, HttpStatusCode see, HttpStatusCode add, HttpStatusCode remove)
    {
        // Diego is the person of the absences; each role acts on them.
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var today = TodayOf(team);
        var existing = await AddedAsync(team, team.Diego, today.AddDays(30), today.AddDays(31));
        var client = await team.ClientOfAsync(role);
        var path = PathOf(team, team.Diego);

        (await client.GetAsync(path, Cancellation)).StatusCode.Should().Be(see);
        (await AddAsync(client, path, today.AddDays(40), today.AddDays(41))).StatusCode.Should().Be(add);
        (await SendAsync(client, HttpMethod.Delete, $"{path}/{existing}")).StatusCode.Should().Be(remove);

        var left = await PeriodsAsync(team.Admin, path);
        left.Should().HaveCount(add == HttpStatusCode.Created ? 1 : 1);
        left.Single().Start.Should().Be(DayText(add == HttpStatusCode.Created ? today.AddDays(40) : today.AddDays(30)));
        if (see == HttpStatusCode.OK)
        {
            var canManage = (await client.GetFromJsonAsync<JsonElement>(path, Cancellation)).GetProperty("canManage").GetBoolean();
            canManage.Should().Be(add == HttpStatusCode.Created);
        }

        // A person not in the project is not found, for anyone.
        (await client.GetAsync(PathOf(team, team.Sofia), Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Ausência pelo projeto encerrado")]
    public async Task AClosedProject_ShowsTheAbsences_ButTakesNoChange()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var today = TodayOf(team);
        var absence = await AddedAsync(team, team.Carla, today.AddDays(10), today.AddDays(20));
        (await SendAsync(team.Admin, HttpMethod.Put, $"/api/projects/{team.Erp}/status", new { status = "Cancelled" }))
            .IsSuccessStatusCode.Should().BeTrue();
        var marina = await team.ClientOfAsync(team.Marina);

        var added = await AddAsync(marina, PathOf(team, team.Carla), today.AddDays(30), today.AddDays(31));
        var removed = await SendAsync(marina, HttpMethod.Delete, $"{PathOf(team, team.Carla)}/{absence}");

        added.StatusCode.Should().Be(HttpStatusCode.Conflict);
        removed.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(added)).Should().Be("urn:fiscoprojects:problem:project-closed");
        var list = await marina.GetFromJsonAsync<JsonElement>(PathOf(team, team.Carla), Cancellation);
        (list.GetProperty("absences").GetArrayLength(), list.GetProperty("canManage").GetBoolean()).Should().Be((1, false));
    }

    [Fact(DisplayName = "Ausência na auditoria")]
    public async Task TheAudit_HasTheAbsence_WithoutTheDates()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var today = TodayOf(team);
        await AddedAsync(team, team.Carla, today.AddDays(10), today.AddDays(20));

        var audit = await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation);
        var record = audit.GetProperty("items").EnumerateArray()
            .Single(entry => entry.GetProperty("itemType").GetString() == "PersonAbsence");

        (record.GetProperty("action").GetString(), record.GetProperty("itemLabel").GetString(), record.GetProperty("actor").GetProperty("id").GetGuid())
            .Should().Be(("Created", "Carla Mendes", team.Marina));
        var dates = record.GetProperty("changes").EnumerateArray()
            .Where(change => change.GetProperty("field").GetString() is "Start" or "End")
            .ToList();
        dates.Should().HaveCount(2).And.AllSatisfy(change =>
        {
            change.GetProperty("secret").GetBoolean().Should().BeTrue();
            change.GetProperty("after").ValueKind.Should().Be(JsonValueKind.Null);
        });
        record.ToString().Should().NotContain(DayText(today.AddDays(10)));
    }

    [Fact(DisplayName = "Ausente hoje na lista")]
    public async Task TheMembers_HaveTheAbsenceOfToday_OrTheNextOne()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var today = TodayOf(team);
        await AddedAsync(team, team.Carla, today.AddDays(-2), today.AddDays(5));
        await AddedAsync(team, team.Carla, today.AddDays(20), today.AddDays(25));
        await AddedAsync(team, team.Diego, today.AddDays(7), today.AddDays(11));
        await AddedAsync(team, team.Rafael, today.AddDays(-20), today.AddDays(-10));

        var members = (await team.Admin.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}/members", Cancellation))
            .EnumerateArray()
            .ToDictionary(member => member.GetProperty("id").GetGuid(), member => member.GetProperty("nextAbsence"));

        static (string?, string?) Period(JsonElement absence) => (absence.GetProperty("start").GetString(), absence.GetProperty("end").GetString());
        Period(members[team.Carla]).Should().Be((DayText(today.AddDays(-2)), DayText(today.AddDays(5))));
        Period(members[team.Diego]).Should().Be((DayText(today.AddDays(7)), DayText(today.AddDays(11))));
        // An absence already over is not shown.
        members[team.Rafael].ValueKind.Should().Be(JsonValueKind.Null);
        members[team.Marina].ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "Tarefa na ausência do responsável")]
    public async Task AnOpenTask_HasTheAbsencesOfItsOwner_OnItsDays()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (task, other) = await TasksOfCarlaAsync(team);
        var today = TodayOf(team);
        await AddedAsync(team, team.Carla, today.AddDays(2), today.AddDays(12));

        var items = await ItemsAsync(team);

        items[task].Select(Period).Should().Equal((DayText(today.AddDays(2)), DayText(today.AddDays(12))));
        // The task after the absence has none.
        items[other].Should().BeEmpty();
    }

    [Fact(DisplayName = "Sem aviso na concluída")]
    public async Task ADoneTask_HasNoAbsences()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (task, _) = await TasksOfCarlaAsync(team);
        var today = TodayOf(team);
        await AddedAsync(team, team.Carla, today.AddDays(2), today.AddDays(12));

        (await SendAsync(team.Admin, HttpMethod.Put, $"/api/projects/{team.Erp}/schedule/items/{task}/progress", new { progress = 100 }))
            .IsSuccessStatusCode.Should().BeTrue();

        (await ItemsAsync(team))[task].Should().BeEmpty();
    }

    [Fact(DisplayName = "Ausência apagada depois de um ano")]
    public async Task TheCleanup_RemovesTheAbsencesPastAYearAfterTheirEnd()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var today = TodayOf(team);
        await AddedAsync(team, team.Carla, today.AddDays(-400), today.AddDays(-370));
        await AddedAsync(team, team.Carla, today.AddDays(-340), today.AddDays(-330));

        int removed;
        await using (var scope = team.App.Services.CreateAsyncScope())
        {
            removed = await scope.ServiceProvider.GetRequiredService<CleanUpAbsences>().ExecuteAsync(Cancellation);
        }

        removed.Should().Be(1);
        (await PeriodsAsync(team.Admin, PathOf(team, team.Carla))).Should().Equal((DayText(today.AddDays(-340)), DayText(today.AddDays(-330))));
    }

    [Fact(DisplayName = "Ausências na anonimização")]
    public async Task Anonymizing_RemovesTheAbsences()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var today = TodayOf(team);
        await AddedAsync(team, team.Carla, today.AddDays(10), today.AddDays(20));
        await AddedAsync(team, team.Diego, today.AddDays(10), today.AddDays(20));

        (await SendAsync(team.Admin, HttpMethod.Post, $"/api/users/{team.Carla}/deactivate")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SendAsync(team.Admin, HttpMethod.Post, $"/api/users/{team.Carla}/anonymization")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await team.App.CountAsync("SELECT COUNT(*) FROM [projects].[person_absences] WHERE UserId = @id", ("@id", team.Carla))).Should().Be(0);
        (await team.App.CountAsync("SELECT COUNT(*) FROM [projects].[person_absences] WHERE UserId = @id", ("@id", team.Diego))).Should().Be(1);
    }

    private static (string?, string?) Period(JsonElement absence) =>
        (absence.GetProperty("start").GetString(), absence.GetProperty("end").GetString());

    /// <summary>Two tasks of Carla: one from today for 5 business days, another starting in 30 days.</summary>
    private static async Task<(Guid Task, Guid Other)> TasksOfCarlaAsync(ProjectTeam team)
    {
        var path = $"/api/projects/{team.Erp}/schedule";
        var phase = (await (await SendAsync(team.Admin, HttpMethod.Post, $"{path}/phases", new { name = "Configuração" }))
            .Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        async Task<Guid> TaskAsync(string name, int days)
        {
            var created = await SendAsync(team.Admin, HttpMethod.Post, $"{path}/items", new
            {
                kind = "Task",
                phaseId = phase,
                name,
                start = DayText(TodayOf(team).AddDays(days)),
                duration = 5,
                ownerId = team.Carla,
                progress = 0,
                predecessorIds = Array.Empty<Guid>(),
            });
            created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync(Cancellation));
            return (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        }

        return (await TaskAsync("Configurar plano de contas", 0), await TaskAsync("Treinar usuários", 30));
    }

    private static async Task<Dictionary<Guid, List<JsonElement>>> ItemsAsync(ProjectTeam team) =>
        (await team.Admin.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}/schedule", Cancellation))
            .GetProperty("phases").EnumerateArray()
            .SelectMany(phase => phase.GetProperty("items").EnumerateArray())
            .ToDictionary(item => item.GetProperty("id").GetGuid(), item => item.GetProperty("ownerAbsences").EnumerateArray().ToList());
}
