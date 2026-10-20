using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: cronograma (API side): "Quem vê e quem edita o cronograma", "Tarefas e marcos", "Dependências", "Dependências
// que empurram", "Tarefa atrasada", "Cronograma de um projeto encerrado", "Mudanças do cronograma na auditoria",
// "Progresso das tarefas", "Quem atualiza o progresso", "Marco atingido", "Marco atrasado", "Novas datas pela API",
// "Dias não úteis no cronograma", "Subtarefas", "Tarefa-resumo", "Dependências com subtarefas" and "Subtarefas na
// auditoria"; membros-do-projeto: "Tarefas de quem sai ou passa a leitor"; projetos: "Cronograma na lista de projetos"
// and "Cabeçalho do projeto".
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
        Guid AdminId,
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

    /// <summary>One action on the schedule and its status when allowed (ADR-0043); all but the reads change the schedule.</summary>
    private sealed record Action(string Name, Func<HttpClient, Scene, Task<HttpResponseMessage>> Run, HttpStatusCode Allowed)
    {
        public bool Changes => !Reads.Contains(Name);
    }

    /// <summary>What everyone in the project does with the schedule, without changing it (ADR-0043).</summary>
    private static readonly HashSet<string> Reads = ["See the schedule", "Export to MS Project"];

    private static readonly Action[] Actions =
    [
        new("See the schedule", (client, scene) => client.GetAsync(scene.Path, Cancellation), HttpStatusCode.OK),
        new("Export to MS Project", (client, scene) => client.GetAsync($"{scene.Path}/ms-project", Cancellation), HttpStatusCode.OK),
        new("Add a phase", (client, scene) => SendAsync(client, HttpMethod.Post, $"{scene.Path}/phases", new { name = "Entrada em produção" }),
            HttpStatusCode.Created),
        new("Rename a phase", (client, scene) => SendAsync(client, HttpMethod.Put, $"{scene.Path}/phases/{scene.Phase}", new { name = "Parametrização" }),
            HttpStatusCode.NoContent),
        new("Add a task", (client, scene) => SendAsync(client, HttpMethod.Post, $"{scene.Path}/items",
            Item(scene.Phase, "Configurar plano de contas", "2026-10-14", 3, kind: "Task")), HttpStatusCode.Created),
        new("Change a task", (client, scene) => SendAsync(client, HttpMethod.Put, $"{scene.Path}/items/{scene.Task}",
            Item(scene.Phase, "Parametrizar regras de ICMS", "2026-10-13", 6, progress: 50)), HttpStatusCode.OK),
        new("Move a task in the timeline", (client, scene) => DatesAsync(client, scene, scene.Task, "2026-10-14"),
            HttpStatusCode.OK),
        new("Update the progress of a task of no one", (client, scene) => ProgressAsync(client, scene, scene.Task, 70),
            HttpStatusCode.NoContent),
        new("Mark the milestone as reached", (client, scene) => ReachedAsync(client, scene, scene.Milestone, true),
            HttpStatusCode.NoContent),
        new("Save the baseline", (client, scene) => SendAsync(client, HttpMethod.Put, $"{scene.Path}/baseline"),
            HttpStatusCode.NoContent),
        new("Remove a milestone", (client, scene) => SendAsync(client, HttpMethod.Delete, $"{scene.Path}/items/{scene.Milestone}"),
            HttpStatusCode.NoContent),
        new("Remove a phase", (client, scene) => SendAsync(client, HttpMethod.Delete, $"{scene.Path}/phases/{scene.EmptyPhase}"),
            HttpStatusCode.NoContent),
        new("Undo the last change", (client, scene) => SendAsync(client, HttpMethod.Post, $"{scene.Path}/undo"), HttpStatusCode.OK),
        new("Redo the change", (client, scene) => SendAsync(client, HttpMethod.Post, $"{scene.Path}/redo"), HttpStatusCode.OK),
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

    [Fact(DisplayName = "Gerente atualiza qualquer tarefa")]
    public async Task Manager_UpdatesTheProgressOfAnyTask()
    {
        var scene = await StartAsync(carlasRole: "Developer");
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        var carlas = await AddTaskAsync(scene, "Integrar NF-e", scene.Carla);

        (await ProgressAsync(marina, scene, carlas, 40)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ProgressAsync(marina, scene, scene.Task, 100)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ItemAsync(scene, carlas)).GetProperty("progress").GetInt32().Should().Be(40);
        var task = await ItemAsync(scene, scene.Task);
        (task.GetProperty("progress").GetInt32(), task.GetProperty("completedOn").GetString()).Should().Be((100, "2026-10-19"));
    }

    [Theory(DisplayName = "Responsável atualiza a própria tarefa")]
    [InlineData("TechLead")]
    [InlineData("Contributor")]
    [InlineData("Developer")]
    public async Task Owner_UpdatesTheProgressOfTheirOwnTask(string role)
    {
        var scene = await StartAsync(carlasRole: role == "TechLead" ? "Viewer" : role);
        await using var app = scene.App;
        var person = role == "TechLead" ? scene.Rafael : scene.Carla;
        using var client = await app.SignInAsync(person);
        var own = await AddTaskAsync(scene, "Integrar NF-e", person);
        var others = await AddTaskAsync(scene, "Ajustar relatórios", scene.Marina);

        (await ProgressAsync(client, scene, own, 30)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ProgressAsync(client, scene, others, 30)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var schedule = await client.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation);
        var items = schedule.GetProperty("phases")[0].GetProperty("items").EnumerateArray().ToList();
        items.Where(item => item.GetProperty("canUpdateProgress").GetBoolean()).Select(item => item.GetProperty("id").GetGuid())
            .Should().Equal(own);
        ((await ItemAsync(scene, own)).GetProperty("progress").GetInt32(), (await ItemAsync(scene, others)).GetProperty("progress").GetInt32())
            .Should().Be((30, 0));
    }

    [Fact(DisplayName = "Leitor não atualiza o progresso")]
    public async Task Viewer_DoesNotUpdateTheProgress()
    {
        var scene = await StartAsync(carlasRole: "Viewer");
        await using var app = scene.App;
        using var carla = await app.SignInAsync(scene.Carla);
        var before = await ScheduleOfAsync(scene);

        (await ProgressAsync(carla, scene, scene.Task, 30)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await ScheduleOfAsync(scene)).Should().Be(before);
    }

    [Fact(DisplayName = "Progresso de um marco")]
    public async Task ProgressOfAMilestone_IsABadRequest()
    {
        var scene = await StartAsync();
        await using var app = scene.App;

        var milestone = await ProgressAsync(scene.Admin, scene, scene.Milestone, 50);
        var outOfRange = await ProgressAsync(scene.Admin, scene, scene.Task, 101);

        milestone.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorFieldsAsync(milestone)).Should().Equal("progress");
        (await ErrorFieldsAsync(outOfRange)).Should().Equal("progress");
        (await ProgressAsync(scene.Admin, scene, Guid.CreateVersion7(), 50)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Progresso num projeto encerrado")]
    public async Task ProgressInAClosedProject_IsAConflict()
    {
        var scene = await StartAsync(carlasRole: "Developer");
        await using var app = scene.App;
        using var carla = await app.SignInAsync(scene.Carla);
        var own = await AddTaskAsync(scene, "Integrar NF-e", scene.Carla);
        await SendAsync(scene.Admin, HttpMethod.Put, $"/api/projects/{scene.ProjectId}/status", new { status = "Completed" });

        var response = await ProgressAsync(carla, scene, own, 30);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:project-closed");
        var item = await ItemAsync(scene, own);
        (item.GetProperty("progress").GetInt32(), item.GetProperty("canUpdateProgress").GetBoolean()).Should().Be((0, false));
        (await ReachedAsync(scene.Admin, scene, scene.Milestone, true)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "Alterar a tarefa mantém o progresso")]
    public async Task ChangingTheTask_WithoutTheProgress_KeepsIt()
    {
        var scene = await StartAsync(carlasRole: "Developer");
        await using var app = scene.App;
        using var carla = await app.SignInAsync(scene.Carla);
        var own = await AddTaskAsync(scene, "Integrar NF-e", scene.Carla);

        await ProgressAsync(carla, scene, own, 60);
        var changed = await SendAsync(scene.Admin, HttpMethod.Put, $"{scene.Path}/items/{own}",
            Item(scene.Phase, "Integrar a NF-e", "2026-10-19", 3, progress: null, owner: scene.Carla));

        changed.StatusCode.Should().Be(HttpStatusCode.OK);
        var item = await ItemAsync(scene, own);
        (item.GetProperty("name").GetString(), item.GetProperty("progress").GetInt32()).Should().Be(("Integrar a NF-e", 60));
    }

    [Fact(DisplayName = "Marcar marco como atingido")]
    public async Task ReachedMilestone_KeepsTheDay_AndIsAudited()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        app.Clock.Advance(TimeSpan.FromMinutes(1));

        (await ReachedAsync(marina, scene, scene.Milestone, true)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var milestone = await ItemAsync(scene, scene.Milestone);
        (milestone.GetProperty("reachedOn").GetString(), milestone.GetProperty("late").GetBoolean()).Should().Be(("2026-10-19", false));
        var entry = (await scene.Admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=ScheduleItem&pageSize=100", Cancellation))
            .GetProperty("items")[0];
        (entry.GetProperty("action").GetString(), entry.GetProperty("itemLabel").GetString()).Should().Be(("Updated", "Aceite da configuração"));
        Values(entry, "ReachedOn").Should().Be((null, "2026-10-19"));
        Values(entry, "Kind").Should().Be(("Milestone", "Milestone"));
    }

    [Fact(DisplayName = "Desmarcar marco")]
    public async Task UnmarkedMilestone_IsPendingAgain()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        await ReachedAsync(scene.Admin, scene, scene.Milestone, true);

        (await ReachedAsync(scene.Admin, scene, scene.Milestone, false)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var milestone = await ItemAsync(scene, scene.Milestone);
        milestone.GetProperty("reachedOn").ValueKind.Should().Be(JsonValueKind.Null);
        milestone.GetProperty("late").GetBoolean().Should().BeTrue("its day, 15/10, passed");
    }

    [Fact(DisplayName = "Líder técnico não marca o marco")]
    public async Task TechLead_DoesNotMarkTheMilestone()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var rafael = await app.SignInAsync(scene.Rafael);

        (await ReachedAsync(rafael, scene, scene.Milestone, true)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await ItemAsync(scene, scene.Milestone)).GetProperty("reachedOn").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "Atingir uma tarefa")]
    public async Task ReachingATask_IsABadRequest()
    {
        var scene = await StartAsync();
        await using var app = scene.App;

        var response = await ReachedAsync(scene.Admin, scene, scene.Task, true);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorFieldsAsync(response)).Should().Equal("reached");
    }

    [Fact(DisplayName = "Mudar o início pela API")]
    public async Task NewStart_KeepsTheDuration_AndPushes()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);

        var response = await DatesAsync(marina, scene, scene.Task, "2026-10-16");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("pushed").GetInt32().Should().Be(1);
        var task = await ItemAsync(scene, scene.Task);
        (task.GetProperty("start").GetString(), task.GetProperty("end").GetString(), task.GetProperty("duration").GetInt32())
            .Should().Be(("2026-10-16", "2026-10-20", 3));
        (await ItemAsync(scene, scene.Milestone)).GetProperty("start").GetString().Should().Be("2026-10-20");
    }

    [Fact(DisplayName = "Mudar o fim pela API")]
    public async Task NewEnd_ChangesTheDuration()
    {
        var scene = await StartAsync();
        await using var app = scene.App;

        (await DatesAsync(scene.Admin, scene, scene.Task, "2026-10-13", "2026-10-20")).StatusCode.Should().Be(HttpStatusCode.OK);

        var task = await ItemAsync(scene, scene.Task);
        (task.GetProperty("end").GetString(), task.GetProperty("duration").GetInt32()).Should().Be(("2026-10-20", 6));
    }

    [Fact(DisplayName = "Fim antes do início")]
    public async Task EndBeforeTheStart_IsABadRequest()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        var before = await ScheduleOfAsync(scene);

        var task = await DatesAsync(scene.Admin, scene, scene.Task, "2026-10-14", "2026-10-13");
        var milestone = await DatesAsync(scene.Admin, scene, scene.Milestone, "2026-10-15", "2026-10-16");

        task.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorFieldsAsync(task)).Should().Equal("end");
        (await ErrorFieldsAsync(milestone)).Should().Equal("end");
        (await ScheduleOfAsync(scene)).Should().Be(before);
    }

    [Fact(DisplayName = "Dias não úteis no cronograma")]
    public async Task Schedule_ComesWithTheNonWorkingDays()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var rafael = await app.SignInAsync(scene.Rafael);

        var days = (await rafael.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation))
            .GetProperty("nonWorkingDays").EnumerateArray().Select(day => day.GetString()).ToList();

        // The schedule goes from 13/10 to 15/10/2026: September to November, with the national holidays of 12/10 and 02/11.
        days.Should().Contain(["2026-10-12", "2026-11-02", "2026-10-10", "2026-10-11"]);
        days.Should().NotContain(["2026-10-13", "2026-10-09"]);
        (days[0], days[^1]).Should().Be(("2026-09-05", "2026-11-29"));
    }

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
        schedule.GetProperty("lateMilestones").GetInt32().Should().Be(1);
        var phases = schedule.GetProperty("phases");
        phases.EnumerateArray().Select(phase => phase.GetProperty("name").GetString()).Should().Equal("Configuração", "Homologação");
        var configuration = phases[0];
        (configuration.GetProperty("late").GetBoolean(), configuration.GetProperty("progress").GetInt32()).Should().Be((true, 80));
        var task = configuration.GetProperty("items")[0];
        (task.GetProperty("kind").GetString(), task.GetProperty("end").GetString(), task.GetProperty("late").GetBoolean())
            .Should().Be(("Task", "2026-10-15", true));
        // Late on 16/10 and 19/10, without the weekend, as the milestone of the same day.
        task.GetProperty("lateDays").GetInt32().Should().Be(2);
        task.GetProperty("owner").GetProperty("name").GetString().Should().Be("Carla Mendes");
        task.GetProperty("canUpdateProgress").GetBoolean().Should().BeTrue();
        var milestone = configuration.GetProperty("items")[1];
        (milestone.GetProperty("kind").GetString(), milestone.GetProperty("duration").GetInt32()).Should().Be(("Milestone", 0));
        (milestone.GetProperty("late").GetBoolean(), milestone.GetProperty("canUpdateProgress").GetBoolean()).Should().Be((true, false));
        (milestone.GetProperty("start").GetString(), milestone.GetProperty("lateDays").GetInt32()).Should().Be(("2026-10-15", 2));
        milestone.GetProperty("reachedOn").ValueKind.Should().Be(JsonValueKind.Null);
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

    [Fact(DisplayName = "Subtarefas pela API")]
    public async Task Subtasks_ComeUnderTheirTask_WithTheSummaryComputed()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        var path = $"{scene.Path}/items";

        // The first subtask of "Parametrizar regras de ICMS" takes its dependency: the milestone now waits for it.
        var created = await SendAsync(marina, HttpMethod.Post, path, new
        {
            kind = "Task",
            phaseId = scene.Phase,
            name = "Mapear regras",
            start = "2026-10-13",
            duration = 4,
            ownerId = (Guid?)null,
            progress = 50,
            predecessorIds = Array.Empty<Guid>(),
            parentId = scene.Task,
        });
        var subtask = await IdOfAsync(created);

        var schedule = await scene.Admin.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation);
        var items = schedule.GetProperty("phases")[0].GetProperty("items").EnumerateArray().ToList();
        items.Select(item => item.GetProperty("name").GetString())
            .Should().Equal("Parametrizar regras de ICMS", "Mapear regras", "Aceite da configuração");
        var task = items[0];
        task.GetProperty("hasSubtasks").GetBoolean().Should().BeTrue();
        task.GetProperty("canUpdateProgress").GetBoolean().Should().BeFalse();
        (task.GetProperty("start").GetString(), task.GetProperty("end").GetString(), task.GetProperty("duration").GetInt32(),
            task.GetProperty("progress").GetInt32()).Should().Be(("2026-10-13", "2026-10-16", 4, 50));
        items[1].GetProperty("parentId").GetGuid().Should().Be(scene.Task);
        items[1].GetProperty("hasSubtasks").GetBoolean().Should().BeFalse();
        // The subtask ended on 16/10 at 50%: on 19/10 it and its task are one business day late.
        (items[1].GetProperty("lateDays").GetInt32(), task.GetProperty("lateDays").GetInt32()).Should().Be((1, 1));
        items[2].GetProperty("predecessorIds").EnumerateArray().Select(id => id.GetGuid()).Should().Equal(subtask);
        items[2].GetProperty("start").GetString().Should().Be("2026-10-16");
        task.GetProperty("predecessorIds").EnumerateArray().Should().BeEmpty();

        var progress = await ProgressAsync(marina, scene, scene.Task, 70);
        var dates = await DatesAsync(marina, scene, scene.Task, "2026-10-20");
        var inMilestone = await SendAsync(marina, HttpMethod.Post, path, new
        {
            kind = "Task",
            phaseId = scene.Phase,
            name = "Detalhar",
            start = "2026-10-13",
            duration = 1,
            ownerId = (Guid?)null,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
            parentId = scene.Milestone,
        });
        var onSummary = await SendAsync(marina, HttpMethod.Put, $"{path}/{scene.Milestone}",
            Item(scene.Phase, "Aceite da configuração", "2026-10-16", 0, after: [scene.Task]));

        progress.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorFieldsAsync(progress)).Should().Equal("progress");
        (await ErrorFieldsAsync(dates)).Should().Equal("start");
        (await ErrorFieldsAsync(inMilestone)).Should().Equal("parentId");
        (await ErrorFieldsAsync(onSummary)).Should().Equal("predecessorIds");
    }

    [Fact(DisplayName = "Subtarefa na auditoria")]
    public async Task Subtask_IsAudited_WithItsTaskByName()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        var path = $"{scene.Path}/items";
        var other = await IdOfAsync(await SendAsync(scene.Admin, HttpMethod.Post, path,
            Item(scene.Phase, "Integrar emissão de NF-e", "2026-10-19", 3, kind: "Task")));
        app.Clock.Advance(TimeSpan.FromMinutes(1));

        var subtask = await IdOfAsync(await SendAsync(scene.Admin, HttpMethod.Post, path, new
        {
            kind = "Task",
            phaseId = scene.Phase,
            name = "Roteiro de testes",
            start = "2026-10-19",
            duration = 2,
            ownerId = (Guid?)null,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
            parentId = other,
        }));
        app.Clock.Advance(TimeSpan.FromMinutes(1));
        (await SendAsync(scene.Admin, HttpMethod.Put, $"{path}/{subtask}", new
        {
            phaseId = scene.Phase,
            name = "Roteiro de testes",
            start = "2026-10-19",
            duration = 2,
            ownerId = (Guid?)null,
            progress = (int?)null,
            predecessorIds = Array.Empty<Guid>(),
            parentId = scene.Task,
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        var entries = (await scene.Admin.GetFromJsonAsync<JsonElement>(
                "/api/audit?itemType=ScheduleItem&pageSize=100", Cancellation))
            .GetProperty("items").EnumerateArray()
            .Where(entry => entry.GetProperty("itemLabel").GetString() == "Roteiro de testes")
            .ToList();

        var added = entries.Single(entry => entry.GetProperty("action").GetString() == "Created");
        var moved = entries.Single(entry => entry.GetProperty("action").GetString() == "Updated");
        Values(added, "ParentId").Should().Be((null, "Integrar emissão de NF-e"));
        Values(moved, "ParentId").Should().Be(("Integrar emissão de NF-e", "Parametrizar regras de ICMS"));
    }

    [Fact(DisplayName = "Linha de base e caminho crítico pela API")]
    public async Task Baseline_IsSaved_AndComparedWithTheSchedule_WithTheCriticalPath()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        var before = await ScheduleOfAsync(scene);
        JsonDocument.Parse(before).RootElement.GetProperty("baseline").ValueKind.Should().Be(JsonValueKind.Null);

        (await SendAsync(marina, HttpMethod.Put, $"{scene.Path}/baseline")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        // Moved two business days later, the task pushes the milestone after it, and the end of the project.
        (await DatesAsync(marina, scene, scene.Task, "2026-10-15")).StatusCode.Should().Be(HttpStatusCode.OK);

        var schedule = await scene.Admin.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation);
        var baseline = schedule.GetProperty("baseline");
        (baseline.GetProperty("savedOn").GetString(), baseline.GetProperty("start").GetString(), baseline.GetProperty("end").GetString())
            .Should().Be(("2026-10-19", "2026-10-13", "2026-10-15"));
        (schedule.GetProperty("end").GetString(), baseline.GetProperty("deviation").GetInt32()).Should().Be(("2026-10-19", 2));
        var task = await ItemAsync(scene, scene.Task);
        (task.GetProperty("baselineStart").GetString(), task.GetProperty("baselineEnd").GetString(), task.GetProperty("baselineDeviation").GetInt32())
            .Should().Be(("2026-10-13", "2026-10-15", 2));
        // The task and the milestone after it make the whole schedule: both on the critical path.
        (task.GetProperty("critical").GetBoolean(), (await ItemAsync(scene, scene.Milestone)).GetProperty("critical").GetBoolean())
            .Should().Be((true, true));

        // An item included later has no baseline.
        var later = await AddTaskAsync(scene, "Configurar plano de contas", scene.Rafael);
        var added = await ItemAsync(scene, later);
        (added.GetProperty("baselineStart").ValueKind, added.GetProperty("baselineDeviation").ValueKind)
            .Should().Be((JsonValueKind.Null, JsonValueKind.Null));

        // Without tasks nor milestones there is no baseline to save.
        foreach (var id in new[] { scene.Milestone, scene.Task, later })
        {
            await SendAsync(marina, HttpMethod.Delete, $"{scene.Path}/items/{id}");
        }

        var empty = await SendAsync(marina, HttpMethod.Put, $"{scene.Path}/baseline");
        empty.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorFieldsAsync(empty)).Should().Equal("baseline");
    }

    [Fact(DisplayName = "Linha de base num projeto encerrado")]
    public async Task ClosedProject_KeepsItsBaseline()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        await SendAsync(marina, HttpMethod.Put, $"{scene.Path}/baseline");
        await SendAsync(marina, HttpMethod.Put, $"/api/projects/{scene.ProjectId}/status", new { status = "Completed" });
        var before = await ScheduleOfAsync(scene);

        var response = await SendAsync(marina, HttpMethod.Put, $"{scene.Path}/baseline");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:project-closed");
        (await ScheduleOfAsync(scene)).Should().Be(before);
    }

    [Fact(DisplayName = "Linha de base na auditoria")]
    public async Task Baseline_IsAuditedOnce_ByTheNameOfTheProject()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        await SendAsync(marina, HttpMethod.Put, $"{scene.Path}/baseline");
        // The next day, with sessions of their own, as the others expired.
        app.Clock.Advance(TimeSpan.FromDays(1));
        using var marinaNextDay = await app.SignInAsync(scene.Marina);
        using var admin = await app.SignInAsync(scene.AdminId);
        await DatesAsync(marinaNextDay, scene, scene.Task, "2026-10-15");
        await SendAsync(marinaNextDay, HttpMethod.Put, $"{scene.Path}/baseline");

        var entries = (await admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=ScheduleBaseline&pageSize=100", Cancellation))
            .GetProperty("items").EnumerateArray().ToList();
        var project = await admin.GetFromJsonAsync<JsonElement>($"/api/projects/{scene.ProjectId}", Cancellation);

        entries.Select(entry => entry.GetProperty("action").GetString()).Should().Equal("Updated", "Created");
        entries.Should().AllSatisfy(entry =>
        {
            entry.GetProperty("itemLabel").GetString().Should().Be(project.GetProperty("name").GetString());
            entry.GetProperty("actor").GetProperty("name").GetString().Should().Be("Marina Costa");
        });
        Values(entries[0], "SavedOn").Should().Be(("2026-10-19", "2026-10-20"));
        Values(entries[0], "End").Should().Be(("2026-10-15", "2026-10-19"));
        // The copy of the dates of each item stays out of the audit.
        var all = (await admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation)).GetProperty("items");
        all.EnumerateArray().Select(entry => entry.GetProperty("itemType").GetString()).Should().NotContain("ScheduleBaselineItem");
    }

    [Fact(DisplayName = "Desfazer volta a cadeia")]
    public async Task Undo_PutsTheTaskAndThePushedMilestoneBack()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        var before = await ScheduleOfAsync(scene);
        (await DatesAsync(marina, scene, scene.Task, "2026-10-15")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await StepsAsync(marina, scene)).Should().Be(("DatesChanged:Parametrizar regras de ICMS", null));

        var undone = await SendAsync(marina, HttpMethod.Post, $"{scene.Path}/undo");

        undone.StatusCode.Should().Be(HttpStatusCode.OK);
        var step = await undone.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        (step.GetProperty("action").GetString(), step.GetProperty("name").GetString())
            .Should().Be(("DatesChanged", "Parametrizar regras de ICMS"));
        (await ItemAsync(scene, scene.Task)).GetProperty("start").GetString().Should().Be("2026-10-13");
        (await ItemAsync(scene, scene.Milestone)).GetProperty("start").GetString().Should().Be("2026-10-15");
        (await ScheduleOfAsync(scene)).Should().Be(before);
        // The Administrator built the schedule: Marina has nothing else to undo, and may redo the move.
        (await StepsAsync(marina, scene)).Should().Be((null, "DatesChanged:Parametrizar regras de ICMS"));
    }

    [Fact(DisplayName = "Desfazer uma exclusão")]
    public async Task UndoingARemoval_BringsTheTaskBackWithItsDependency()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        await SendAsync(marina, HttpMethod.Delete, $"{scene.Path}/items/{scene.Task}");

        (await SendAsync(marina, HttpMethod.Post, $"{scene.Path}/undo")).StatusCode.Should().Be(HttpStatusCode.OK);

        var task = await ItemAsync(scene, scene.Task);
        (task.GetProperty("name").GetString(), task.GetProperty("start").GetString(), task.GetProperty("end").GetString())
            .Should().Be(("Parametrizar regras de ICMS", "2026-10-13", "2026-10-15"));
        (await ItemAsync(scene, scene.Milestone)).GetProperty("predecessorIds").EnumerateArray()
            .Select(id => id.GetGuid()).Should().Equal(scene.Task);
    }

    [Fact(DisplayName = "Desfazer bloqueado")]
    public async Task SomeoneChangingTheSameTaskAfter_BlocksTheUndo_AndTheNextUndoGoesToTheChangeBefore()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        await SendAsync(marina, HttpMethod.Put, $"{scene.Path}/phases/{scene.Phase}", new { name = "Parametrização" });
        await DatesAsync(marina, scene, scene.Task, "2026-10-15");
        // The Administrator changes the same task after it.
        await ProgressAsync(scene.Admin, scene, scene.Task, 40);
        var before = await ScheduleOfAsync(scene);

        var blocked = await SendAsync(marina, HttpMethod.Post, $"{scene.Path}/undo");

        blocked.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProblemTypeAsync(blocked)).Should().Be("urn:fiscoprojects:problem:schedule-undo-blocked");
        (await ScheduleOfAsync(scene)).Should().Be(before);
        (await StepsAsync(marina, scene)).Should().Be(("PhaseRenamed:Parametrização", null));
        (await SendAsync(marina, HttpMethod.Post, $"{scene.Path}/undo")).StatusCode.Should().Be(HttpStatusCode.OK);
        var schedule = await scene.Admin.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation);
        schedule.GetProperty("phases").EnumerateArray().Select(phase => phase.GetProperty("name").GetString())
            .Should().Contain("Configuração");
    }

    [Fact(DisplayName = "Só as próprias mudanças")]
    public async Task ThePersonUndoesOnlyTheirOwnChanges()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        var added = await AddTaskAsync(scene, "Configurar plano de contas", scene.Rafael);

        var response = await SendAsync(marina, HttpMethod.Post, $"{scene.Path}/undo");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:schedule-nothing-to-undo");
        (await ItemAsync(scene, added)).GetProperty("name").GetString().Should().Be("Configurar plano de contas");
        (await StepsAsync(marina, scene)).Should().Be((null, null));
    }

    [Fact(DisplayName = "Últimas 20 mudanças")]
    public async Task ThePersonKeepsTheLast20Changes()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        for (var number = 1; number <= 21; number++)
        {
            await SendAsync(marina, HttpMethod.Put, $"{scene.Path}/phases/{scene.EmptyPhase}", new { name = $"Fase {number}" });
        }

        var statuses = new List<HttpStatusCode>();
        for (var undo = 1; undo <= 21; undo++)
        {
            statuses.Add((await SendAsync(marina, HttpMethod.Post, $"{scene.Path}/undo")).StatusCode);
        }

        statuses.Should().Equal([.. Enumerable.Repeat(HttpStatusCode.OK, 20), HttpStatusCode.Conflict]);
        // Back to the name of the first change: the one before it left the history.
        var schedule = await scene.Admin.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation);
        schedule.GetProperty("phases").EnumerateArray().Select(phase => phase.GetProperty("name").GetString())
            .Should().Contain("Fase 1");
    }

    [Fact(DisplayName = "Refazer depois de desfazer")]
    public async Task Redo_AppliesTheUndoneChangeAgain()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        await SendAsync(marina, HttpMethod.Put, $"{scene.Path}/phases/{scene.Phase}", new { name = "Parametrização" });
        await SendAsync(marina, HttpMethod.Post, $"{scene.Path}/undo");

        var redone = await SendAsync(marina, HttpMethod.Post, $"{scene.Path}/redo");

        redone.StatusCode.Should().Be(HttpStatusCode.OK);
        var schedule = await scene.Admin.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation);
        schedule.GetProperty("phases").EnumerateArray().Select(phase => phase.GetProperty("name").GetString())
            .Should().Contain("Parametrização");
        (await StepsAsync(marina, scene)).Should().Be(("PhaseRenamed:Parametrização", null));
    }

    [Fact(DisplayName = "Mudança nova apaga o refazer")]
    public async Task ANewChange_TakesAwayTheRedo()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        await SendAsync(marina, HttpMethod.Put, $"{scene.Path}/phases/{scene.Phase}", new { name = "Parametrização" });
        await SendAsync(marina, HttpMethod.Post, $"{scene.Path}/undo");
        await SendAsync(marina, HttpMethod.Post, $"{scene.Path}/phases", new { name = "Treinamento" });

        var response = await SendAsync(marina, HttpMethod.Post, $"{scene.Path}/redo");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:schedule-nothing-to-undo");
        (await StepsAsync(marina, scene)).Should().Be(("PhaseAdded:Treinamento", null));
    }

    [Fact(DisplayName = "XML do cronograma pela API")]
    public async Task Export_IsAFileNamedAfterTheProject()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var carla = await app.SignInAsync(scene.Carla);

        var response = await carla.GetAsync($"{scene.Path}/ms-project", Cancellation);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/xml");
        response.Content.Headers.ContentDisposition!.FileName.Should().Be("ERP-FIN-cronograma.xml");
        var document = System.Xml.Linq.XDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation));
        document.Descendants().Where(element => element.Name.LocalName == "Name").Select(element => element.Value)
            .Should().Contain(["Configuração", "Parametrizar regras de ICMS", "Aceite da configuração"]);
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
        schedule.GetProperty("lateMilestones").GetInt32().Should().Be(1);
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

    private static Task<HttpResponseMessage> ProgressAsync(HttpClient client, Scene scene, Guid itemId, int progress) =>
        SendAsync(client, HttpMethod.Put, $"{scene.Path}/items/{itemId}/progress", new { progress });

    private static Task<HttpResponseMessage> ReachedAsync(HttpClient client, Scene scene, Guid itemId, bool reached) =>
        SendAsync(client, HttpMethod.Put, $"{scene.Path}/items/{itemId}/reached", new { reached });

    private static Task<HttpResponseMessage> DatesAsync(HttpClient client, Scene scene, Guid itemId, string start, string? end = null) =>
        SendAsync(client, HttpMethod.Put, $"{scene.Path}/items/{itemId}/dates", new { start, end });

    private static async Task<Guid> AddTaskAsync(Scene scene, string name, Guid owner) =>
        await IdOfAsync(await SendAsync(scene.Admin, HttpMethod.Post, $"{scene.Path}/items",
            Item(scene.Phase, name, "2026-10-19", 3, owner: owner, kind: "Task")));

    private static async Task<JsonElement> ItemAsync(Scene scene, Guid itemId) =>
        (await scene.Admin.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation))
            .GetProperty("phases").EnumerateArray()
            .SelectMany(phase => phase.GetProperty("items").EnumerateArray())
            .Single(item => item.GetProperty("id").GetGuid() == itemId);

    /// <summary>What the person would undo and redo, as "Action:name", as the schedule tells the person.</summary>
    private static async Task<(string? Undo, string? Redo)> StepsAsync(HttpClient client, Scene scene)
    {
        var schedule = await client.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation);
        static string? StepOf(JsonElement step) =>
            step.ValueKind == JsonValueKind.Null ? null : $"{step.GetProperty("action").GetString()}:{step.GetProperty("name").GetString()}";
        return (StepOf(schedule.GetProperty("undo")), StepOf(schedule.GetProperty("redo")));
    }

    private static async Task<string> ScheduleOfAsync(Scene scene) =>
        (await scene.Admin.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation)).GetRawText();

    private async Task<Scene> StartAsync(string carlasRole = "Viewer")
    {
        var app = await SessionApp.StartAsync(sqlServer);
        var (admin, adminId) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
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
        return new Scene(app, admin, adminId, projectId, marina, rafael, carla, beatriz, phase, emptyPhase, task, milestone);
    }

    private static object Item(
        Guid phase, string name, string start, int duration, int? progress = 0, Guid? owner = null, string? kind = null, Guid[]? after = null) =>
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
