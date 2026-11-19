using GerenWare.Projects.Application;
using GerenWare.Projects.Contracts;
using GerenWare.Projects.Domain;
using GerenWare.Testing;
using GerenWare.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace GerenWare.Projects.Tests;

// The contracts the Projects module offers to the documents (ADR-0092): the new project, the items of the schedule and the
// milestone reached by a signed acceptance term (ADR-0035).
public sealed class ProjectContractTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private sealed class RecordingSetup(bool fails) : IProjectSetup
    {
        public List<Guid> Created { get; } = [];

        public Task ProjectCreatedAsync(Guid projectId, CancellationToken cancellationToken)
        {
            Created.Add(projectId);
            return fails ? throw new InvalidOperationException("The storage is down.") : Task.CompletedTask;
        }
    }

    [Fact(DisplayName = "Projeto novo avisa os outros módulos")]
    public async Task NewProject_IsToldToTheOtherModules()
    {
        var failing = new RecordingSetup(fails: true);
        var working = new RecordingSetup(fails: false);
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer, services =>
        {
            services.AddSingleton<IProjectSetup>(failing);
            services.AddSingleton<IProjectSetup>(working);
        });
        var marina = database.People.Add("Marina Costa", SystemRole.ProjectManager);

        var created = await database.RunAsync<CreateProject, ProjectSaveResult>(create => create.ExecuteAsync(
            new NewProject("ERP-FIN", "Implantação do ERP financeiro", null, null, null, null), marina, Cancellation));

        created.Outcome.Should().Be(ProjectSaveOutcome.Saved, "a module that fails does not undo the project");
        failing.Created.Should().Equal(created.Id!.Value);
        working.Created.Should().Equal(created.Id!.Value);
    }

    [Fact(DisplayName = "Itens do cronograma para outros módulos")]
    public async Task ScheduleItems_ArePhasesThenTasksAndMilestones()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);
        var marina = database.People.Add("Marina Costa", SystemRole.ProjectManager);
        var viewer = new ProjectViewer(marina, IsAdministrator: false);
        var project = (await database.RunAsync<CreateProject, ProjectSaveResult>(create => create.ExecuteAsync(
            new NewProject("ERP-FIN", "Implantação do ERP financeiro", null, null, null, null), marina, Cancellation))).Id!.Value;
        var other = (await database.RunAsync<CreateProject, ProjectSaveResult>(create => create.ExecuteAsync(
            new NewProject("FISC-SP", "Revisão fiscal da filial SP", null, null, null, null), marina, Cancellation))).Id!.Value;
        var setup = (await database.RunAsync<AddSchedulePhase, ScheduleResult>(add =>
            add.ExecuteAsync(project, "Configuração", viewer, Cancellation))).Id!.Value;
        var tests = (await database.RunAsync<AddSchedulePhase, ScheduleResult>(add =>
            add.ExecuteAsync(project, "Testes", viewer, Cancellation))).Id!.Value;
        await database.RunAsync<AddSchedulePhase, ScheduleResult>(add => add.ExecuteAsync(other, "Levantamento", viewer, Cancellation));
        var homologation = await AddAsync(
            database, project, ScheduleItemKind.Milestone, new ScheduleItemForm(tests, "Aceite da homologação", new DateOnly(2026, 11, 3), 0, null, 0, []), viewer);
        var icms = await AddAsync(
            database, project, ScheduleItemKind.Task, new ScheduleItemForm(setup, "Parametrizar regras de ICMS", new DateOnly(2026, 10, 20), 3, null, 0, []), viewer);

        var items = await database.RunAsync<IScheduleItems, IReadOnlyList<ScheduleItemInfo>>(directory =>
            directory.ItemsAsync(project, Cancellation));

        items.Should().Equal(
            new ScheduleItemInfo(setup, ScheduleItemType.Phase, "Configuração"),
            new ScheduleItemInfo(tests, ScheduleItemType.Phase, "Testes"),
            new ScheduleItemInfo(icms, ScheduleItemType.Task, "Parametrizar regras de ICMS"),
            new ScheduleItemInfo(homologation, ScheduleItemType.Milestone, "Aceite da homologação", new DateOnly(2026, 11, 3)));
    }

    [Fact(DisplayName = "Marco atingido por outro módulo")]
    public async Task Milestone_IsReached_ForAnotherModule_OutOfTheUndo()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);
        var marina = database.People.Add("Marina Costa", SystemRole.ProjectManager);
        var viewer = new ProjectViewer(marina, IsAdministrator: false);
        var project = (await database.RunAsync<CreateProject, ProjectSaveResult>(create => create.ExecuteAsync(
            new NewProject("ERP-FIN", "Implantação do ERP financeiro", null, null, null, null), marina, Cancellation))).Id!.Value;
        var tests = (await database.RunAsync<AddSchedulePhase, ScheduleResult>(add =>
            add.ExecuteAsync(project, "Testes", viewer, Cancellation))).Id!.Value;
        var homologation = await AddAsync(
            database, project, ScheduleItemKind.Milestone, new ScheduleItemForm(tests, "Aceite da homologação", new DateOnly(2026, 11, 3), 0, null, 0, []), viewer);
        var icms = await AddAsync(
            database, project, ScheduleItemKind.Task, new ScheduleItemForm(tests, "Testes de ICMS", new DateOnly(2026, 10, 20), 3, null, 0, []), viewer);
        database.Clock.Advance(TimeSpan.FromDays(1));

        var reached = await database.RunAsync<IScheduleMilestones, bool>(milestones => milestones.ReachAsync(project, homologation, Cancellation));
        var again = await database.RunAsync<IScheduleMilestones, bool>(milestones => milestones.ReachAsync(project, homologation, Cancellation));
        var ofATask = await database.RunAsync<IScheduleMilestones, bool>(milestones => milestones.ReachAsync(project, icms, Cancellation));

        (reached, again, ofATask).Should().Be((true, true, false));
        var items = await database.RunAsync<IScheduleItems, IReadOnlyList<ScheduleItemInfo>>(directory => directory.ItemsAsync(project, Cancellation));
        items.Single(item => item.Id == homologation).ReachedOn.Should().Be(new DateOnly(2026, 10, 20));
        items.Single(item => item.Id == icms).ReachedOn.Should().BeNull();
        var schedule = await database.RunAsync<GetSchedule, ScheduleView?>(get => get.ExecuteAsync(project, viewer, Cancellation));
        schedule!.Undo!.Action.Should().Be(ScheduleAction.TaskAdded, "the milestone reached is of no person (ADR-0091)");
    }

    // Spec: chamados, requirement "O que depende do chamado" (ADR-0164).
    [Fact(DisplayName = "O que os itens de um chamado alcançam no cronograma")]
    public async Task ScheduleImpact_TellsTheItems_TheMilestonesAfter_AndTheCriticalPath()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);
        var marina = database.People.Add("Marina Costa", SystemRole.ProjectManager);
        var viewer = new ProjectViewer(marina, IsAdministrator: false);
        var project = (await database.RunAsync<CreateProject, ProjectSaveResult>(create => create.ExecuteAsync(
            new NewProject("ERP-FIN", "Implantação do ERP financeiro", null, null, null, null), marina, Cancellation))).Id!.Value;
        var phase = (await database.RunAsync<AddSchedulePhase, ScheduleResult>(add =>
            add.ExecuteAsync(project, "Testes", viewer, Cancellation))).Id!.Value;
        // Today is Monday 19/10/2026: the tests ended on Thursday 15/10, below 100%.
        var tests = await AddAsync(
            database, project, ScheduleItemKind.Task, new ScheduleItemForm(phase, "Testes integrados", new DateOnly(2026, 10, 13), 3, null, 40, []), viewer);
        var acceptance = await AddAsync(
            database, project, ScheduleItemKind.Milestone, new ScheduleItemForm(phase, "Aceite da homologação", new DateOnly(2026, 10, 13), 0, null, 0, [tests]), viewer);
        var goLive = await AddAsync(
            database, project, ScheduleItemKind.Milestone, new ScheduleItemForm(phase, "Virada para produção", new DateOnly(2026, 10, 13), 0, null, 0, [acceptance]), viewer);

        var impact = await database.RunAsync<IScheduleImpacts, ScheduleImpact>(impacts =>
            impacts.ImpactOfAsync(project, [tests, Guid.CreateVersion7()], Cancellation));
        var none = await database.RunAsync<IScheduleImpacts, ScheduleImpact>(impacts =>
            impacts.ImpactOfAsync(project, [phase], Cancellation));

        var item = impact.Items.Should().ContainSingle().Subject;
        (item.Id, item.Type, item.Name, item.Start, item.End, item.Progress, item.IsDone)
            .Should().Be((tests, ScheduleItemType.Task, "Testes integrados", new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 15), 40, false));
        item.LateDays.Should().BePositive();
        impact.Milestones.Select(milestone => milestone.Id).Should().Equal(acceptance, goLive);
        impact.OnCriticalPath.Should().BeTrue();
        none.Should().Be(ScheduleImpact.None, "a phase is no task nor milestone");
    }

    private static async Task<Guid> AddAsync(
        ProjectsDatabase database, Guid project, ScheduleItemKind kind, ScheduleItemForm form, ProjectViewer viewer) =>
        (await database.RunAsync<AddScheduleItem, ScheduleResult>(add => add.ExecuteAsync(project, kind, form, viewer, Cancellation))).Id!.Value;
}
