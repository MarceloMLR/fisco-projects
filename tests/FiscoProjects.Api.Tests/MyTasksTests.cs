using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using static FiscoProjects.Api.Tests.ProjectTeam;

namespace FiscoProjects.Api.Tests;

// Spec: minhas-tarefas, requirements "Página Minhas tarefas", "Concluídas em Minhas tarefas" and "Visibilidade nos
// projetos" (ADR-0158).
public sealed class MyTasksTests(SqlServerFixture sqlServer)
{
    /// <summary>The team of ERP-FIN (<see cref="ProjectTeam"/>), on Monday 19/10/2026, with the phase Configuração.</summary>
    private sealed record Scene(ProjectTeam Team, Guid Phase) : IAsyncDisposable
    {
        /// <summary>Marina includes a task in the phase of the project, with the owner and the task it is in.</summary>
        public async Task<Guid> TaskAsync(Guid project, Guid phase, string name, Guid? owner, int progress = 0, Guid? parent = null)
        {
            var marina = await Team.ClientOfAsync(Team.Marina);
            var added = await SendAsync(marina, HttpMethod.Post, $"/api/projects/{project}/schedule/items", new
            {
                kind = "Task",
                phaseId = phase,
                name,
                start = "2026-10-26",
                duration = 3,
                ownerId = owner,
                progress,
                parentId = parent,
                predecessorIds = Array.Empty<Guid>(),
            });
            return await IdOfAsync(added);
        }

        public async Task<Guid> PhaseAsync(Guid project, string name)
        {
            var marina = await Team.ClientOfAsync(Team.Marina);
            return await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"/api/projects/{project}/schedule/phases", new { name }));
        }

        /// <summary>
        /// The person in charge takes the task to 100%, on today of the installation, in a session of its own: moving the
        /// clock ends the sessions left without use.
        /// </summary>
        public async Task CompleteAsync(Guid person, Guid project, Guid task)
        {
            using var client = await Team.App.SignInAsync(person);
            (await SendAsync(client, HttpMethod.Put, $"/api/projects/{project}/schedule/items/{task}/progress", new { progress = 100 }))
                .IsSuccessStatusCode.Should().BeTrue();
        }

        /// <summary>Minhas tarefas, as the person reads it, in a session of its own.</summary>
        public async Task<JsonElement> MyTasksAsync(Guid person, bool done = false)
        {
            using var client = await Team.App.SignInAsync(person);
            return await client.GetFromJsonAsync<JsonElement>(done ? "/api/my-tasks?done=true" : "/api/my-tasks", Cancellation);
        }

        public ValueTask DisposeAsync() => Team.DisposeAsync();
    }

    private async Task<Scene> StartAsync()
    {
        var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        var phase = await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"/api/projects/{team.Erp}/schedule/phases", new { name = "Configuração" }));
        return new Scene(team, phase);
    }

    /// <summary>The names of the tasks of each project, by the code.</summary>
    private static IReadOnlyList<(string Code, string[] Tasks)> ByProject(JsonElement tasks) =>
        [.. tasks.GetProperty("projects").EnumerateArray().Select(project => (
            project.GetProperty("code").GetString()!,
            project.GetProperty("tasks").EnumerateArray()
                .Select(task => task.GetProperty("item").GetProperty("name").GetString()!).ToArray()))];

    [Fact(DisplayName = "Tarefas de dois projetos")]
    public async Task TheTasksOfThePerson_ComeByProject()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        var portal = await team.CreateProjectAsync("PORTAL-NF", "Portal de notas");
        await team.App.AddProjectMemberAsync(portal, team.Diego, "Developer");
        var portalPhase = await scene.PhaseAsync(portal, "Desenvolvimento");
        await scene.TaskAsync(team.Erp, scene.Phase, "Integrar emissão de NF-e", team.Diego, progress: 30);
        await scene.TaskAsync(portal, portalPhase, "Tela de consulta", team.Diego);

        var tasks = await scene.MyTasksAsync(team.Diego);

        ByProject(tasks).Should().BeEquivalentTo(
            [("ERP-FIN", new[] { "Integrar emissão de NF-e" }), ("PORTAL-NF", new[] { "Tela de consulta" })],
            options => options.WithStrictOrdering());
        tasks.GetProperty("today").GetString().Should().Be("2026-10-19");
        var erp = tasks.GetProperty("projects")[0];
        erp.GetProperty("name").GetString().Should().Be("Implantação do ERP financeiro");
        var task = erp.GetProperty("tasks")[0];
        task.GetProperty("phase").GetString().Should().Be("Configuração");
        // The item as the Schedule tab shows it: the person updates the progress of the own task.
        (task.GetProperty("item").GetProperty("progress").GetInt32(), task.GetProperty("item").GetProperty("canUpdateProgress").GetBoolean())
            .Should().Be((30, true));
    }

    [Fact(DisplayName = "Só as tarefas da pessoa")]
    public async Task OnlyTheTasksOfThePerson_Come()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        await scene.TaskAsync(team.Erp, scene.Phase, "Parametrizar regras de ICMS", team.Diego);
        await scene.TaskAsync(team.Erp, scene.Phase, "Configurar plano de contas", team.Carla);
        await scene.TaskAsync(team.Erp, scene.Phase, "Revisar parâmetros", owner: null);

        ByProject(await scene.MyTasksAsync(team.Diego)).Should().BeEquivalentTo([("ERP-FIN", new[] { "Parametrizar regras de ICMS" })]);
        // Without tasks, no project.
        (await scene.MyTasksAsync(team.Lucas)).GetProperty("projects").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "Projeto encerrado e tarefa-resumo")]
    public async Task AClosedProjectAndASummaryTask_StayOut()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        var summary = await scene.TaskAsync(team.Erp, scene.Phase, "Testes integrados", team.Diego);
        await scene.TaskAsync(team.Erp, scene.Phase, "Roteiro de testes", team.Diego, parent: summary);
        var old = await team.CreateProjectAsync("TMS-OLD", "Projeto cancelado");
        await team.App.AddProjectMemberAsync(old, team.Diego, "Developer");
        await scene.TaskAsync(old, await scene.PhaseAsync(old, "Única"), "Tarefa do cancelado", team.Diego);
        (await SendAsync(team.Admin, HttpMethod.Put, $"/api/projects/{old}/status", new { status = "Cancelled" }))
            .IsSuccessStatusCode.Should().BeTrue();

        var tasks = await scene.MyTasksAsync(team.Diego);

        ByProject(tasks).Should().BeEquivalentTo([("ERP-FIN", new[] { "Roteiro de testes" })]);
        tasks.GetProperty("projects")[0].GetProperty("tasks")[0].GetProperty("parent").GetString().Should().Be("Testes integrados");
    }

    [Fact(DisplayName = "Mostrar concluídas")]
    public async Task TheCompletedTasks_OfTheLast30Days_ComeWhenAsked()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        var older = await scene.TaskAsync(team.Erp, scene.Phase, "Concluída há 40 dias", team.Diego);
        var recent = await scene.TaskAsync(team.Erp, scene.Phase, "Concluída há 10 dias", team.Diego);
        await scene.TaskAsync(team.Erp, scene.Phase, "Em aberto", team.Diego);
        await scene.CompleteAsync(team.Diego, team.Erp, older);
        team.App.Clock.Advance(TimeSpan.FromDays(30));
        await scene.CompleteAsync(team.Diego, team.Erp, recent);
        team.App.Clock.Advance(TimeSpan.FromDays(10));

        ByProject(await scene.MyTasksAsync(team.Diego)).Should().BeEquivalentTo([("ERP-FIN", new[] { "Em aberto" })]);
        ByProject(await scene.MyTasksAsync(team.Diego, done: true)).Should().BeEquivalentTo(
            [("ERP-FIN", new[] { "Em aberto", "Concluída há 10 dias" })],
            options => options.WithStrictOrdering());
    }

    [Fact(DisplayName = "Tarefas de outra pessoa")]
    public async Task TheManager_SeesOnlyTheOwnTasks_AndTheScheduleStaysWhole()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        await scene.TaskAsync(team.Erp, scene.Phase, "Revisar com o cliente", team.Marina);
        await scene.TaskAsync(team.Erp, scene.Phase, "Configurar plano de contas", team.Carla);

        ByProject(await scene.MyTasksAsync(team.Marina)).Should().BeEquivalentTo([("ERP-FIN", new[] { "Revisar com o cliente" })]);
        var schedule = await (await team.ClientOfAsync(team.Lucas)).GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}/schedule", Cancellation);
        schedule.GetProperty("phases")[0].GetProperty("items").GetArrayLength().Should().Be(2);
    }

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }
}
