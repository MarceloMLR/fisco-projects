using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace FiscoProjects.Projects.Tests;

// The contracts the Projects module offers to the documents (ADR-0092): the new project and the items of the schedule.
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
            new ScheduleItemInfo(homologation, ScheduleItemType.Milestone, "Aceite da homologação"));
    }

    private static async Task<Guid> AddAsync(
        ProjectsDatabase database, Guid project, ScheduleItemKind kind, ScheduleItemForm form, ProjectViewer viewer) =>
        (await database.RunAsync<AddScheduleItem, ScheduleResult>(add => add.ExecuteAsync(project, kind, form, viewer, Cancellation))).Id!.Value;
}
