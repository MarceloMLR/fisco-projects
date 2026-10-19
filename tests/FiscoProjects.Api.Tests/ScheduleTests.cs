using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: cronograma (API side): "Quem vê e quem edita o cronograma", "Tarefas e marcos", "Dependências", "Dependências
// que empurram", "Cronograma de um projeto encerrado" and "Mudanças do cronograma na auditoria"; membros-do-projeto:
// "Tarefas de quem sai ou passa a leitor"; projetos: "Cronograma na lista de projetos" and "Cabeçalho do projeto".
public sealed class ScheduleTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>
    /// The project of the matrix: Marina is the manager, Rafael the tech lead and Carla in it with the role of the row;
    /// Beatriz (a Project manager) is outside it. The schedule has the phase Configuração, with a task and a milestone,
    /// and the empty phase Homologação.
    /// </summary>
    private sealed record Scene(
        SessionApp App,
        HttpClient Admin,
        Guid ProjectId,
        Guid Marina,
        Guid Rafael,
        Guid Carla,
        Guid Beatriz,
        Guid Phase,
        Guid EmptyPhase,
        Guid Task,
        Guid Milestone)
    {
        public string Path => $"/api/projects/{ProjectId}/schedule";
    }

    /// <summary>One action on the schedule and its status when allowed (ADR-0043); all but the first change the schedule.</summary>
    private sealed record Action(string Name, Func<HttpClient, Scene, Task<HttpResponseMessage>> Run, HttpStatusCode Allowed)
    {
        public bool Changes => Name != Actions[0].Name;
    }

    private static readonly Action[] Actions =
    [
        new("See the schedule", (client, scene) => client.GetAsync(scene.Path, Cancellation), HttpStatusCode.OK),
        new("Add a phase", (client, scene) => SendAsync(client, HttpMethod.Post, $"{scene.Path}/phases", new { name = "Entrada em produção" }),
            HttpStatusCode.Created),
        new("Rename a phase", (client, scene) => SendAsync(client, HttpMethod.Put, $"{scene.Path}/phases/{scene.Phase}", new { name = "Parametrização" }),
            HttpStatusCode.NoContent),
        new("Add a task", (client, scene) => SendAsync(client, HttpMethod.Post, $"{scene.Path}/items",
            Item(scene.Phase, "Configurar plano de contas", "2026-10-14", 3, kind: "Task")), HttpStatusCode.Created),
        new("Change a task", (client, scene) => SendAsync(client, HttpMethod.Put, $"{scene.Path}/items/{scene.Task}",
            Item(scene.Phase, "Parametrizar regras de ICMS", "2026-10-13", 6, progress: 50)), HttpStatusCode.OK),
        new("Remove a milestone", (client, scene) => SendAsync(client, HttpMethod.Delete, $"{scene.Path}/items/{scene.Milestone}"),
            HttpStatusCode.NoContent),
        new("Remove a phase", (client, scene) => SendAsync(client, HttpMethod.Delete, $"{scene.Path}/phases/{scene.EmptyPhase}"),
            HttpStatusCode.NoContent),
    ];

    [Fact(DisplayName = "Gerente edita o cronograma")]
    public Task Manager_ChangesTheSchedule() => AssertRowAsync(scene => scene.App.SignInAsync(scene.Marina), _ => null);

    [Fact(DisplayName = "Administrador fora do projeto edita o cronograma")]
    public Task Administrator_ChangesTheSchedule() => AssertRowAsync(scene => Task.FromResult(scene.Admin), _ => null);

    [Fact(DisplayName = "Líder técnico só vê o cronograma")]
    public Task TechLead_OnlySeesTheSchedule() => AssertRowAsync(
        scene => scene.App.SignInAsync(scene.Rafael), action => action.Changes ? HttpStatusCode.Forbidden : null);

    [Theory(DisplayName = "Colaborador, desenvolvedor e leitor só veem o cronograma")]
    [InlineData("Contributor")]
    [InlineData("Developer")]
    [InlineData("Viewer")]
    public Task OtherRoles_OnlySeeTheSchedule(string role) => AssertRowAsync(
        scene => scene.App.SignInAsync(scene.Carla),
        action => action.Changes ? HttpStatusCode.Forbidden : null,
        role);

    [Fact(DisplayName = "Cronograma de um projeto de fora")]
    public Task Outsider_FindsNoSchedule() => AssertRowAsync(scene => scene.App.SignInAsync(scene.Beatriz), _ => HttpStatusCode.NotFound);

    [Fact(DisplayName = "Cronograma sem sessão")]
    public Task WithoutASession_NothingIsReadNorChanged() => AssertRowAsync(scene => Task.FromResult(scene.App.NewClient()), _ => HttpStatusCode.Unauthorized);

    [Fact(DisplayName = "Cronograma pela API")]
    public async Task Schedule_ComesWithItsComputedNumbers()
    {
        var scene = await StartAsync(carlasRole: "Developer");
        await using var app = scene.App;
        using var carla = await app.SignInAsync(scene.Carla);
        await SendAsync(scene.Admin, HttpMethod.Put, $"{scene.Path}/items/{scene.Task}",
            Item(scene.Phase, "Parametrizar regras de ICMS", "2026-10-13", 3, progress: 80, owner: scene.Carla));

        var schedule = await carla.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation);

        schedule.GetProperty("today").GetString().Should().Be("2026-10-19");
        (schedule.GetProperty("start").GetString(), schedule.GetProperty("end").GetString()).Should().Be(("2026-10-13", "2026-10-15"));
        (schedule.GetProperty("progress").GetInt32(), schedule.GetProperty("lateTasks").GetInt32()).Should().Be((80, 1));
        var phases = schedule.GetProperty("phases");
        phases.EnumerateArray().Select(phase => phase.GetProperty("name").GetString()).Should().Equal("Configuração", "Homologação");
        var configuration = phases[0];
        (configuration.GetProperty("late").GetBoolean(), configuration.GetProperty("progress").GetInt32()).Should().Be((true, 80));
        var task = configuration.GetProperty("items")[0];
        (task.GetProperty("kind").GetString(), task.GetProperty("end").GetString(), task.GetProperty("late").GetBoolean())
            .Should().Be(("Task", "2026-10-15", true));
        task.GetProperty("owner").GetProperty("name").GetString().Should().Be("Carla Mendes");
        var milestone = configuration.GetProperty("items")[1];
        (milestone.GetProperty("kind").GetString(), milestone.GetProperty("duration").GetInt32()).Should().Be(("Milestone", 0));
        milestone.GetProperty("predecessorIds").EnumerateArray().Select(id => id.GetGuid()).Should().Equal(scene.Task);
        phases[1].GetProperty("start").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "Tarefa inválida")]
    public async Task InvalidItem_IsABadRequest()
    {
        var scene = await StartAsync();
        await using var app = scene.App;

        var task = await SendAsync(scene.Admin, HttpMethod.Post, $"{scene.Path}/items",
            Item(Guid.CreateVersion7(), " ", "2026-10-14", 0, progress: 120, kind: "Task"));
        var milestone = await SendAsync(scene.Admin, HttpMethod.Post, $"{scene.Path}/items",
            Item(scene.Phase, "Aceite", "2026-10-14", 0, owner: scene.Carla, kind: "Milestone"));
        var phase = await SendAsync(scene.Admin, HttpMethod.Post, $"{scene.Path}/phases", new { name = "" });

        task.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorFieldsAsync(task)).Should().BeEquivalentTo("name", "phaseId", "duration", "progress");
        (await ErrorFieldsAsync(milestone)).Should().Equal("ownerId");
        (await ErrorFieldsAsync(phase)).Should().Equal("name");
    }

    [Fact(DisplayName = "Dependência circular")]
    public async Task Cycle_IsABadRequest_WithItsProblemType()
    {
        var scene = await StartAsync();
        await using var app = scene.App;

        var response = await SendAsync(scene.Admin, HttpMethod.Put, $"{scene.Path}/items/{scene.Task}",
            Item(scene.Phase, "Parametrizar regras de ICMS", "2026-10-13", 3, after: [scene.Milestone]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:schedule-cycle");
    }

    [Fact(DisplayName = "Atrasar empurra a cadeia")]
    public async Task LaterEnd_PushesTheChain_AndSaysHowMany()
    {
        var scene = await StartAsync();
        await using var app = scene.App;

        var response = await SendAsync(scene.Admin, HttpMethod.Put, $"{scene.Path}/items/{scene.Task}",
            Item(scene.Phase, "Parametrizar regras de ICMS", "2026-10-13", 5));

        (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("pushed").GetInt32().Should().Be(1);
        var milestone = (await scene.Admin.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation))
            .GetProperty("phases")[0].GetProperty("items")[1];
        milestone.GetProperty("start").GetString().Should().Be("2026-10-19");
    }

    [Fact(DisplayName = "Alterar tarefa num projeto concluído")]
    public async Task ClosedProject_IsAConflict()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        await SendAsync(marina, HttpMethod.Put, $"/api/projects/{scene.ProjectId}/status", new { status = "Completed" });

        var response = await SendAsync(marina, HttpMethod.Put, $"{scene.Path}/items/{scene.Task}",
            Item(scene.Phase, "Outro nome", "2026-10-13", 3));
        var project = await marina.GetFromJsonAsync<JsonElement>($"/api/projects/{scene.ProjectId}", Cancellation);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:project-closed");
        project.GetProperty("permissions").GetProperty("canEditSchedule").GetBoolean().Should().BeFalse();
        (await marina.GetAsync(scene.Path, Cancellation)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Tarefa empurrada na auditoria")]
    public async Task PushedItem_IsAudited()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        app.Clock.Advance(TimeSpan.FromMinutes(1));

        await SendAsync(marina, HttpMethod.Put, $"{scene.Path}/items/{scene.Task}",
            Item(scene.Phase, "Parametrizar regras de ICMS", "2026-10-13", 5));

        var entries = (await scene.Admin.GetFromJsonAsync<JsonElement>(
                "/api/audit?itemType=ScheduleItem&pageSize=100", Cancellation))
            .GetProperty("items").EnumerateArray()
            .Where(entry => entry.GetProperty("action").GetString() == "Updated")
            .ToList();

        entries.Select(entry => entry.GetProperty("itemLabel").GetString())
            .Should().BeEquivalentTo("Parametrizar regras de ICMS", "Aceite da configuração");
        entries.Should().OnlyContain(entry => entry.GetProperty("actor").GetProperty("name").GetString() == "Marina Costa");
        var milestone = entries.Single(entry => entry.GetProperty("itemLabel").GetString() == "Aceite da configuração");
        Values(milestone, "Start").Should().Be(("2026-10-15", "2026-10-19"));
        Values(milestone, "ProjectId").Should().Be(("Implantação do ERP financeiro", "Implantação do ERP financeiro"));
        Values(milestone, "Kind").Should().Be(("Milestone", "Milestone"));
    }

    [Fact(DisplayName = "Dependência na auditoria")]
    public async Task Dependency_IsAudited_WithBothItems()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        app.Clock.Advance(TimeSpan.FromMinutes(1));

        await SendAsync(scene.Admin, HttpMethod.Put, $"{scene.Path}/items/{scene.Milestone}",
            Item(scene.Phase, "Aceite da configuração", "2026-10-15", 0));

        var entries = (await scene.Admin.GetFromJsonAsync<JsonElement>(
                "/api/audit?itemType=ScheduleDependency&pageSize=100", Cancellation))
            .GetProperty("items").EnumerateArray().ToList();

        entries.Select(entry => entry.GetProperty("action").GetString()).Should().Equal("Deleted", "Created");
        var removed = entries[0];
        removed.GetProperty("itemLabel").GetString().Should().Be("Aceite da configuração");
        Values(removed, "PredecessorId").Should().Be(("Parametrizar regras de ICMS", null));
        Values(removed, "SuccessorId").Should().Be(("Aceite da configuração", null));
    }

    [Fact(DisplayName = "Remover o responsável de tarefas")]
    public async Task RemovedPerson_LeavesTheTasksWithoutOwner_AndItIsAudited()
    {
        var scene = await StartAsync(carlasRole: "Developer");
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        await SendAsync(marina, HttpMethod.Put, $"{scene.Path}/items/{scene.Task}",
            Item(scene.Phase, "Parametrizar regras de ICMS", "2026-10-13", 3, progress: 40, owner: scene.Carla));
        app.Clock.Advance(TimeSpan.FromMinutes(1));

        (await SendAsync(marina, HttpMethod.Delete, $"/api/projects/{scene.ProjectId}/members/{scene.Carla}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var task = (await marina.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation)).GetProperty("phases")[0].GetProperty("items")[0];
        task.GetProperty("owner").ValueKind.Should().Be(JsonValueKind.Null);
        (task.GetProperty("progress").GetInt32(), task.GetProperty("end").GetString()).Should().Be((40, "2026-10-15"));
        var updated = (await scene.Admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=ScheduleItem&pageSize=100", Cancellation))
            .GetProperty("items")[0];
        updated.GetProperty("action").GetString().Should().Be("Updated");
        Values(updated, "OwnerId").Should().Be(("Carla Mendes", null));
    }

    [Fact(DisplayName = "Responsável passa a leitor")]
    public async Task PersonWhoBecomesAViewer_LeavesTheTasksWithoutOwner()
    {
        var scene = await StartAsync(carlasRole: "Contributor");
        await using var app = scene.App;
        await SendAsync(scene.Admin, HttpMethod.Put, $"{scene.Path}/items/{scene.Task}",
            Item(scene.Phase, "Parametrizar regras de ICMS", "2026-10-13", 3, owner: scene.Carla));

        await SendAsync(scene.Admin, HttpMethod.Put, $"/api/projects/{scene.ProjectId}/members/{scene.Carla}/role", new { role = "Viewer" });

        var task = (await scene.Admin.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation)).GetProperty("phases")[0].GetProperty("items")[0];
        task.GetProperty("owner").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "Cronograma na lista")]
    public async Task List_AndProject_ComeWithTheSchedule()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);

        var page = await marina.GetFromJsonAsync<JsonElement>("/api/projects", Cancellation);
        var project = await marina.GetFromJsonAsync<JsonElement>($"/api/projects/{scene.ProjectId}", Cancellation);

        page.GetProperty("today").GetString().Should().Be("2026-10-19");
        var schedule = page.GetProperty("items")[0].GetProperty("schedule");
        (schedule.GetProperty("start").GetString(), schedule.GetProperty("end").GetString()).Should().Be(("2026-10-13", "2026-10-15"));
        (schedule.GetProperty("lateTasks").GetInt32(), schedule.GetProperty("currentPhase").GetString()).Should().Be((1, "Configuração"));
        schedule.GetProperty("phases").EnumerateArray().Select(phase => phase.GetProperty("name").GetString()).Should().Equal("Configuração");
        schedule.GetProperty("milestones").EnumerateArray().Select(day => day.GetString()).Should().Equal("2026-10-15");
        (project.GetProperty("scheduleStart").GetString(), project.GetProperty("scheduleEnd").GetString())
            .Should().Be(("2026-10-13", "2026-10-15"));
        project.GetProperty("permissions").GetProperty("canEditSchedule").GetBoolean().Should().BeTrue();
    }

    private async Task AssertRowAsync(
        Func<Scene, Task<HttpClient>> clientOf, Func<Action, HttpStatusCode?> refusal, string carlasRole = "Viewer")
    {
        var scene = await StartAsync(carlasRole);
        await using var app = scene.App;
        var client = await clientOf(scene);
        var before = await ScheduleOfAsync(scene);

        var statuses = new List<(string, HttpStatusCode)>();
        foreach (var action in Actions)
        {
            statuses.Add((action.Name, (await action.Run(client, scene)).StatusCode));
        }

        statuses.Should().Equal(Actions.Select(action => (action.Name, refusal(action) ?? action.Allowed)));
        if (Actions.Any(action => action.Changes && refusal(action) is not null))
        {
            // A refused change changes nothing.
            (await ScheduleOfAsync(scene)).Should().Be(before);
        }

        if (client != scene.Admin)
        {
            client.Dispose();
        }
    }

    private static async Task<string> ScheduleOfAsync(Scene scene) =>
        (await scene.Admin.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation)).GetRawText();

    private async Task<Scene> StartAsync(string carlasRole = "Viewer")
    {
        var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        var rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.User);
        var carla = await app.CreateUserAsync("Carla Mendes", SystemRole.User);
        var beatriz = await app.CreateUserAsync("Beatriz Lima", SystemRole.ProjectManager);

        var created = await SendAsync(admin, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-FIN",
            name = "Implantação do ERP financeiro",
            description = (string?)null,
            clientId = (Guid?)null,
            managerId = marina,
            techLeadId = rafael,
        });
        var projectId = await IdOfAsync(created);
        await app.AddProjectMemberAsync(projectId, carla, carlasRole);
        var path = $"/api/projects/{projectId}/schedule";
        var phase = await IdOfAsync(await SendAsync(admin, HttpMethod.Post, $"{path}/phases", new { name = "Configuração" }));
        var emptyPhase = await IdOfAsync(await SendAsync(admin, HttpMethod.Post, $"{path}/phases", new { name = "Homologação" }));
        // 13/10/2026 to 15/10/2026, and the milestone the same day it ends.
        var task = await IdOfAsync(await SendAsync(admin, HttpMethod.Post, $"{path}/items",
            Item(phase, "Parametrizar regras de ICMS", "2026-10-13", 3, kind: "Task")));
        var milestone = await IdOfAsync(await SendAsync(admin, HttpMethod.Post, $"{path}/items",
            Item(phase, "Aceite da configuração", "2026-10-15", 0, kind: "Milestone", after: [task])));
        return new Scene(app, admin, projectId, marina, rafael, carla, beatriz, phase, emptyPhase, task, milestone);
    }

    private static object Item(
        Guid phase, string name, string start, int duration, int progress = 0, Guid? owner = null, string? kind = null, Guid[]? after = null) =>
        kind is null
            ? new { phaseId = phase, name, start, duration, ownerId = owner, progress, predecessorIds = after ?? [] }
            : new { kind, phaseId = phase, name, start, duration, ownerId = owner, progress, predecessorIds = after ?? [] };

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<string?> ProblemTypeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString();

    private static async Task<List<string>> ErrorFieldsAsync(HttpResponseMessage response) =>
        [.. (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation))
            .GetProperty("errors").EnumerateObject().Select(field => field.Name)];

    private static (string? Before, string? After) Values(JsonElement entry, string name)
    {
        var field = entry.GetProperty("changes").EnumerateArray().Single(change => change.GetProperty("field").GetString() == name);
        return (field.GetProperty("before").GetString(), field.GetProperty("after").GetString());
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
