using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: cronograma (API side): "Recálculo pelo calendário" (ADR-0126).
public sealed class ScheduleRecalculationTests(SqlServerFixture sqlServer)
{
    private const string Recalculation = "/api/admin/schedule-recalculation";
    private const string PublicServantsDay = "/api/calendar/calculated-holidays/PublicServantsDay";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>
    /// The open project ERP-FIN, managed by Marina, with the task of Carla from 26/10 to 30/10/2026, and the completed
    /// project ERP-RH with the same task: both made by the Administrator, before the Dia do Servidor Público is considered.
    /// </summary>
    private sealed record Scene(
        SessionApp App, HttpClient Admin, Guid Marina, Guid Carla, Guid Project, Guid Task, Guid ClosedProject, Guid ClosedTask) : IAsyncDisposable
    {
        public string Path => $"/api/projects/{Project}/schedule";

        public ValueTask DisposeAsync() => App.DisposeAsync();
    }

    [Fact(DisplayName = "Ver antes de recalcular")]
    public async Task ThePreview_SaysWhatChanges_WithoutChangingIt()
    {
        await using var scene = await StartAsync();
        await ConsiderPublicServantsDayAsync(scene);

        var preview = await scene.Admin.GetFromJsonAsync<JsonElement>(Recalculation, Cancellation);

        preview.GetProperty("items").GetInt32().Should().Be(1);
        var project = preview.GetProperty("projects").EnumerateArray().Should().ContainSingle().Subject;
        (project.GetProperty("id").GetGuid(), project.GetProperty("code").GetString(), project.GetProperty("items").GetInt32())
            .Should().Be((scene.Project, "ERP-FIN", 1));
        project.GetProperty("name").GetString().Should().Be("Implantação do ERP financeiro");
        (await DatesAsync(scene, scene.Project, scene.Task)).Should().Be(("2026-10-26", "2026-10-30"));
    }

    [Fact(DisplayName = "Projeto encerrado fora do recálculo")]
    public async Task AClosedProject_StaysOutOfTheRecalculation()
    {
        await using var scene = await StartAsync();
        await ConsiderPublicServantsDayAsync(scene);

        var recalculated = await RecalculateAsync(scene.Admin);

        recalculated.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await recalculated.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        body.GetProperty("projects").EnumerateArray().Select(project => project.GetProperty("code").GetString())
            .Should().Equal("ERP-FIN");
        (await DatesAsync(scene, scene.Project, scene.Task)).Should().Be(("2026-10-26", "2026-11-03"));
        (await DatesAsync(scene, scene.ClosedProject, scene.ClosedTask)).Should().Be(("2026-10-26", "2026-10-30"));
    }

    [Fact(DisplayName = "Responsável avisado do recálculo")]
    public async Task TheOwner_HearsOfTheNewDates_AndTheAuditKeepsTheChange()
    {
        await using var scene = await StartAsync();
        await ConsiderPublicServantsDayAsync(scene);
        scene.App.Clock.Advance(TimeSpan.FromMinutes(1));

        (await RecalculateAsync(scene.Admin)).StatusCode.Should().Be(HttpStatusCode.OK);

        using var carla = await scene.App.SignInAsync(scene.Carla);
        var notice = (await carla.GetFromJsonAsync<JsonElement>("/api/notifications", Cancellation))
            .GetProperty("items").EnumerateArray()
            .Where(item => item.GetProperty("kind").GetString() == "ScheduleDatesChanged")
            .Should().ContainSingle("only the task of the open project changed").Subject;
        notice.GetProperty("actor").GetString().Should().Be("Paulo Nogueira");
        var values = notice.GetProperty("values");
        (values.GetProperty("item").GetString(), values.GetProperty("start").GetString(), values.GetProperty("end").GetString())
            .Should().Be(("Parametrizar regras de ICMS", "2026-10-26", "2026-11-03"));
        var entry = (await scene.Admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=ScheduleItem&pageSize=100", Cancellation))
            .GetProperty("items")[0];
        (entry.GetProperty("action").GetString(), entry.GetProperty("itemLabel").GetString(), entry.GetProperty("actor").GetProperty("name").GetString())
            .Should().Be(("Updated", "Parametrizar regras de ICMS", "Paulo Nogueira"));
        var end = entry.GetProperty("changes").EnumerateArray().Single(change => change.GetProperty("field").GetString() == "End");
        (end.GetProperty("before").GetString(), end.GetProperty("after").GetString()).Should().Be(("2026-10-30", "2026-11-03"));
    }

    [Fact(DisplayName = "Recálculo só do administrador")]
    public async Task OnlyTheAdministrator_SeesOrRecalculates()
    {
        await using var scene = await StartAsync();
        await ConsiderPublicServantsDayAsync(scene);
        using var marina = await scene.App.SignInAsync(scene.Marina);

        (await marina.GetAsync(Recalculation, Cancellation)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await RecalculateAsync(marina)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await scene.App.NewClient().GetAsync(Recalculation, Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await DatesAsync(scene, scene.Project, scene.Task)).Should().Be(("2026-10-26", "2026-10-30"));
    }

    [Fact(DisplayName = "Recalcular de novo")]
    public async Task RecalculatingAgain_ChangesNothing()
    {
        await using var scene = await StartAsync();
        await ConsiderPublicServantsDayAsync(scene);
        (await RecalculateAsync(scene.Admin)).StatusCode.Should().Be(HttpStatusCode.OK);

        var again = await (await RecalculateAsync(scene.Admin)).Content.ReadFromJsonAsync<JsonElement>(Cancellation);

        (again.GetProperty("items").GetInt32(), again.GetProperty("projects").GetArrayLength()).Should().Be((0, 0));
        var preview = await scene.Admin.GetFromJsonAsync<JsonElement>(Recalculation, Cancellation);
        preview.GetProperty("items").GetInt32().Should().Be(0);
        (await DatesAsync(scene, scene.Project, scene.Task)).Should().Be(("2026-10-26", "2026-11-03"));
    }

    [Fact(DisplayName = "Desfazer depois do recálculo")]
    public async Task AChangeTheRecalculationChangedAfter_CannotBeUndone()
    {
        await using var scene = await StartAsync();
        using var marina = await scene.App.SignInAsync(scene.Marina);
        (await SendAsync(marina, HttpMethod.Put, $"{scene.Path}/items/{scene.Task}/dates", new { start = "2026-10-27", end = (string?)null }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        await ConsiderPublicServantsDayAsync(scene);
        (await RecalculateAsync(scene.Admin)).StatusCode.Should().Be(HttpStatusCode.OK);

        var undo = await SendAsync(marina, HttpMethod.Post, $"{scene.Path}/undo");

        undo.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await undo.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString()
            .Should().Be("urn:fiscoprojects:problem:schedule-undo-blocked");
        (await DatesAsync(scene, scene.Project, scene.Task)).Should().Be(("2026-10-27", "2026-11-04"));
        var schedule = await marina.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation);
        schedule.GetProperty("undo").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private async Task<Scene> StartAsync()
    {
        var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        var rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.TechLead);
        var carla = await app.CreateUserAsync("Carla Mendes", SystemRole.User);
        var (project, task) = await ProjectAsync(app, admin, "ERP-FIN", "Implantação do ERP financeiro", marina, rafael, carla);
        var (closed, closedTask) = await ProjectAsync(app, admin, "ERP-RH", "Implantação do ERP de RH", marina, rafael, carla);
        (await SendAsync(admin, HttpMethod.Put, $"/api/projects/{closed}/status", new { status = "Completed" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        return new Scene(app, admin, marina, carla, project, task, closed, closedTask);
    }

    /// <summary>A project with the phase Configuração and the task of Carla from 26/10 to 30/10/2026.</summary>
    private static async Task<(Guid Project, Guid Task)> ProjectAsync(
        SessionApp app, HttpClient admin, string code, string name, Guid marina, Guid rafael, Guid carla)
    {
        var project = await IdOfAsync(await SendAsync(admin, HttpMethod.Post, "/api/projects", new
        {
            code,
            name,
            description = (string?)null,
            clientId = (Guid?)null,
            managerId = marina,
            techLeadId = rafael,
        }));
        await app.AddProjectMemberAsync(project, carla, "Developer");
        var path = $"/api/projects/{project}/schedule";
        var phase = await IdOfAsync(await SendAsync(admin, HttpMethod.Post, $"{path}/phases", new { name = "Configuração" }));
        var task = await IdOfAsync(await SendAsync(admin, HttpMethod.Post, $"{path}/items", new
        {
            kind = "Task",
            phaseId = phase,
            name = "Parametrizar regras de ICMS",
            start = "2026-10-26",
            duration = 5,
            ownerId = carla,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
        }));
        return (project, task);
    }

    private static async Task ConsiderPublicServantsDayAsync(Scene scene) =>
        (await SendAsync(scene.Admin, HttpMethod.Put, PublicServantsDay, new { isConsidered = true }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

    private static Task<HttpResponseMessage> RecalculateAsync(HttpClient client) => SendAsync(client, HttpMethod.Post, Recalculation);

    private static async Task<(string? Start, string? End)> DatesAsync(Scene scene, Guid project, Guid task)
    {
        var item = (await scene.Admin.GetFromJsonAsync<JsonElement>($"/api/projects/{project}/schedule", Cancellation))
            .GetProperty("phases").EnumerateArray()
            .SelectMany(phase => phase.GetProperty("items").EnumerateArray())
            .Single(item => item.GetProperty("id").GetGuid() == task);
        return (item.GetProperty("start").GetString(), item.GetProperty("end").GetString());
    }

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
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
