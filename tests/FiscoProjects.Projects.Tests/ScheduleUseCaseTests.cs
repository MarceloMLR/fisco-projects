using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Projects.Tests;

// Spec: cronograma (module side): "Quem vê e quem edita o cronograma", "Fases do cronograma", "Tarefas e marcos",
// "Responsável da tarefa", "Dependências", "Dependências que empurram", "Cronograma de um projeto encerrado",
// "Progresso das tarefas", "Quem atualiza o progresso", "Marco atingido", "Novas datas pela API", "Dias não úteis
// no cronograma", "Subtarefas", "Tarefa-resumo", "Dependências com subtarefas" and "Excluir tarefa-resumo"; membros-do-projeto: "Tarefas de quem sai ou passa a leitor"; projetos: "Cronograma na lista de
// projetos" and "Cabeçalho do projeto".
public sealed class ScheduleUseCaseTests(SqlServerFixture sqlServer) : IAsyncLifetime
{
    private ProjectsDatabase _database = null!;
    private Guid _paulo;
    private Guid _marina;
    private Guid _rafael;
    private Guid _diego;
    private Guid _carla;
    private Guid _lucas;
    private Guid _beatriz;
    private Guid _project;
    private Guid _phase;

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private ProjectViewer Paulo => new(_paulo, IsAdministrator: true);

    private ProjectViewer Marina => new(_marina, IsAdministrator: false);

    private ProjectViewer Rafael => new(_rafael, IsAdministrator: false);

    public async ValueTask InitializeAsync()
    {
        _database = await ProjectsDatabase.CreateAsync(sqlServer);
        _paulo = _database.People.Add("Paulo Nogueira", SystemRole.Administrator);
        _marina = _database.People.Add("Marina Costa", SystemRole.ProjectManager);
        _rafael = _database.People.Add("Rafael Souza", SystemRole.User);
        _diego = _database.People.Add("Diego Torres", SystemRole.User);
        _carla = _database.People.Add("Carla Mendes", SystemRole.User);
        _lucas = _database.People.Add("Lucas Prado", SystemRole.User);
        _beatriz = _database.People.Add("Beatriz Lima", SystemRole.ProjectManager);
        var created = await _database.RunAsync<CreateProject, ProjectSaveResult>(create => create.ExecuteAsync(
            new NewProject("ERP-FIN", "Implantação do ERP financeiro", null, null, null, _rafael), _marina, Cancellation));
        _project = created.Id!.Value;
        await AddMemberAsync(_diego, ProjectRole.Developer);
        await AddMemberAsync(_carla, ProjectRole.Contributor);
        await AddMemberAsync(_lucas, ProjectRole.Viewer);
        _phase = (await AddPhaseAsync(Marina, "Configuração")).Id!.Value;
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    private static ScheduleItemForm Form(
        Guid phase, string name, DateOnly start, int duration, Guid? owner = null, int progress = 0, params Guid[] after) =>
        new(phase, name, start, duration, owner, progress, after);

    private ScheduleItemForm TaskForm(string name, DateOnly start, int duration, Guid? owner = null, params Guid[] after) =>
        Form(_phase, name, start, duration, owner, 0, after);

    private Task<MemberAdded> AddMemberAsync(Guid userId, ProjectRole role) =>
        _database.RunAsync<AddProjectMember, MemberAdded>(add => add.ExecuteAsync(
            _project, new NewProjectMember(userId, null, role), Paulo, Cancellation));

    private Task<ScheduleResult> AddPhaseAsync(ProjectViewer viewer, string? name) =>
        _database.RunAsync<AddSchedulePhase, ScheduleResult>(add => add.ExecuteAsync(_project, name, viewer, Cancellation));

    private Task<ScheduleResult> RenamePhaseAsync(ProjectViewer viewer, Guid phaseId, string? name) =>
        _database.RunAsync<RenameSchedulePhase, ScheduleResult>(rename => rename.ExecuteAsync(_project, phaseId, name, viewer, Cancellation));

    private Task<ScheduleResult> RemovePhaseAsync(ProjectViewer viewer, Guid phaseId) =>
        _database.RunAsync<RemoveSchedulePhase, ScheduleResult>(remove => remove.ExecuteAsync(_project, phaseId, viewer, Cancellation));

    private Task<ScheduleResult> AddItemAsync(ProjectViewer viewer, ScheduleItemForm form, ScheduleItemKind kind = ScheduleItemKind.Task) =>
        _database.RunAsync<AddScheduleItem, ScheduleResult>(add => add.ExecuteAsync(_project, kind, form, viewer, Cancellation));

    private Task<ScheduleResult> ChangeItemAsync(ProjectViewer viewer, Guid itemId, ScheduleItemForm form) =>
        _database.RunAsync<ChangeScheduleItem, ScheduleResult>(change => change.ExecuteAsync(_project, itemId, form, viewer, Cancellation));

    private Task<ScheduleResult> RemoveItemAsync(ProjectViewer viewer, Guid itemId) =>
        _database.RunAsync<RemoveScheduleItem, ScheduleResult>(remove => remove.ExecuteAsync(_project, itemId, viewer, Cancellation));

    private Task<ScheduleResult> UpdateProgressAsync(ProjectViewer viewer, Guid itemId, int progress) =>
        _database.RunAsync<UpdateScheduleItemProgress, ScheduleResult>(update =>
            update.ExecuteAsync(_project, itemId, progress, viewer, Cancellation));

    private Task<ScheduleResult> SetReachedAsync(ProjectViewer viewer, Guid itemId, bool reached) =>
        _database.RunAsync<SetMilestoneReached, ScheduleResult>(set => set.ExecuteAsync(_project, itemId, reached, viewer, Cancellation));

    private Task<ScheduleResult> RescheduleAsync(ProjectViewer viewer, Guid itemId, DateOnly start, DateOnly? end = null) =>
        _database.RunAsync<RescheduleScheduleItem, ScheduleResult>(reschedule =>
            reschedule.ExecuteAsync(_project, itemId, new ScheduleItemDates(start, end), viewer, Cancellation));

    private Task<ScheduleView?> GetAsync(ProjectViewer viewer) =>
        _database.RunAsync<GetSchedule, ScheduleView?>(get => get.ExecuteAsync(_project, viewer, Cancellation));

    private async Task<ScheduleItemView> ItemAsync(Guid itemId)
    {
        var view = (await GetAsync(Paulo))!;
        var item = view.Figures.Phases.SelectMany(phase => phase.Items).Single(item => item.Id == itemId);
        return new ScheduleItemView(item, view.Predecessors.TryGetValue(itemId, out var before) ? before : []);
    }

    private sealed record ScheduleItemView(ScheduleItem Item, IReadOnlyList<Guid> Predecessors);

    private async Task<Guid> AddTaskAsync(string name, DateOnly start, int duration, params Guid[] after) =>
        (await AddItemAsync(Marina, TaskForm(name, start, duration, null, after))).Id!.Value;

    private async Task<Guid> AddSubtaskAsync(string name, DateOnly start, int duration, Guid parent, params Guid[] after) =>
        (await AddItemAsync(Marina, SubtaskForm(name, start, duration, parent, after))).Id!.Value;

    private ScheduleItemForm SubtaskForm(string name, DateOnly start, int duration, Guid? parent, params Guid[] after) =>
        new(_phase, name, start, duration, null, null, after, parent);

    private async Task<Guid> AddMilestoneAsync(string name, DateOnly day) =>
        (await AddItemAsync(Marina, Form(_phase, name, day, 0), ScheduleItemKind.Milestone)).Id!.Value;

    private Task<ProjectStatusOutcome> CompleteProjectAsync() =>
        _database.RunAsync<ChangeProjectStatus, ProjectStatusOutcome>(async change =>
            (await change.ExecuteAsync(_project, ProjectStatus.Completed, Marina, Cancellation)).Outcome);

    private ProjectViewer ViewerOf(string person) =>
        new(person switch { "Rafael" => _rafael, "Carla" => _carla, "Diego" => _diego, _ => _lucas }, false);

    [Fact(DisplayName = "Gerente edita o cronograma")]
    public async Task Manager_ChangesTheSchedule()
    {
        var phase = await AddPhaseAsync(Marina, "Levantamento");
        var task = await AddItemAsync(Marina, Form(phase.Id!.Value, "Mapear processos", D(10, 5), 5, _diego));
        var milestone = await AddItemAsync(Marina, Form(phase.Id!.Value, "Processos mapeados", D(10, 9), 0), ScheduleItemKind.Milestone);
        var changed = await ChangeItemAsync(Marina, task.Id!.Value, Form(phase.Id!.Value, "Mapear processos fiscais", D(10, 5), 6, _diego, 50));
        var removed = await RemoveItemAsync(Marina, milestone.Id!.Value);

        new[] { phase, task, milestone, changed, removed }.Should().OnlyContain(result => result.Outcome == ScheduleOutcome.Done);
        var view = (await GetAsync(Marina))!;
        var levantamento = view.Figures.Phases.Single(p => p.Phase.Name == "Levantamento");
        levantamento.Items.Should().ContainSingle().Which.Should().Match<ScheduleItem>(item =>
            item.Name == "Mapear processos fiscais" && item.Duration == 6 && item.End == D(10, 13) && item.Progress == 50);
        view.OwnerNames[_diego].Should().Be("Diego Torres");
    }

    [Fact(DisplayName = "Administrador fora do projeto edita o cronograma")]
    public async Task Administrator_ChangesTheSchedule()
    {
        (await GetAsync(Paulo)).Should().NotBeNull();
        (await AddItemAsync(Paulo, TaskForm("Configurar plano de contas", D(10, 14), 3))).Outcome.Should().Be(ScheduleOutcome.Done);
    }

    [Fact(DisplayName = "Líder técnico só vê o cronograma")]
    public async Task TechLead_OnlySeesTheSchedule()
    {
        var task = await AddTaskAsync("Parametrizar ICMS", D(10, 13), 5);

        (await GetAsync(Rafael)).Should().NotBeNull();
        (await AddPhaseAsync(Rafael, "Homologação")).Outcome.Should().Be(ScheduleOutcome.Forbidden);
        (await RenamePhaseAsync(Rafael, _phase, "Outra")).Outcome.Should().Be(ScheduleOutcome.Forbidden);
        (await RemovePhaseAsync(Rafael, _phase)).Outcome.Should().Be(ScheduleOutcome.Forbidden);
        (await AddItemAsync(Rafael, TaskForm("Outra", D(10, 13), 1))).Outcome.Should().Be(ScheduleOutcome.Forbidden);
        (await ChangeItemAsync(Rafael, task, TaskForm("Outra", D(10, 13), 1))).Outcome.Should().Be(ScheduleOutcome.Forbidden);
        (await RemoveItemAsync(Rafael, task)).Outcome.Should().Be(ScheduleOutcome.Forbidden);

        var view = (await GetAsync(Paulo))!;
        view.Figures.Phases.Should().ContainSingle().Which.Items.Should().ContainSingle().Which.Name.Should().Be("Parametrizar ICMS");
    }

    [Theory(DisplayName = "Colaborador, desenvolvedor e leitor só veem o cronograma")]
    [InlineData("Carla")]
    [InlineData("Diego")]
    [InlineData("Lucas")]
    public async Task OtherRoles_OnlySeeTheSchedule(string person)
    {
        var viewer = new ProjectViewer(person switch { "Carla" => _carla, "Diego" => _diego, _ => _lucas }, false);
        var owned = (await AddItemAsync(Marina, TaskForm("Integrar NF-e", D(10, 19), 5, person == "Lucas" ? null : viewer.UserId))).Id!.Value;

        (await GetAsync(viewer)).Should().NotBeNull();
        (await ChangeItemAsync(viewer, owned, TaskForm("Integrar NF-e", D(10, 19), 5, null))).Outcome.Should().Be(ScheduleOutcome.Forbidden);
        (await AddPhaseAsync(viewer, "Homologação")).Outcome.Should().Be(ScheduleOutcome.Forbidden);
    }

    [Fact(DisplayName = "Cronograma de um projeto de fora")]
    public async Task Outsider_FindsNoSchedule()
    {
        var beatriz = new ProjectViewer(_beatriz, false);
        var task = await AddTaskAsync("Parametrizar ICMS", D(10, 13), 5);

        (await GetAsync(beatriz)).Should().BeNull();
        (await AddPhaseAsync(beatriz, "Homologação")).Outcome.Should().Be(ScheduleOutcome.NotFound);
        (await ChangeItemAsync(beatriz, task, TaskForm("Outra", D(10, 13), 1))).Outcome.Should().Be(ScheduleOutcome.NotFound);
        (await ChangeItemAsync(Marina, Guid.CreateVersion7(), TaskForm("Outra", D(10, 13), 1))).Outcome.Should().Be(ScheduleOutcome.NotFound);
        (await RemovePhaseAsync(Marina, Guid.CreateVersion7())).Outcome.Should().Be(ScheduleOutcome.NotFound);
    }

    [Fact(DisplayName = "Cronograma sem tarefas")]
    public async Task ScheduleWithoutItems_HasNoDates()
    {
        var view = (await GetAsync(Marina))!;

        view.Figures.Start.Should().BeNull();
        view.Figures.Today.Should().Be(D(10, 19));
        view.Figures.Phases.Should().ContainSingle().Which.Phase.Name.Should().Be("Configuração");
    }

    [Fact(DisplayName = "Fase sem nome")]
    public async Task Phase_WithoutAName_IsRefused()
    {
        (await AddPhaseAsync(Marina, "  ")).Should().BeEquivalentTo(ScheduleResult.InvalidFields([ScheduleField.Name]));
        (await AddPhaseAsync(Marina, new string('a', 151))).Outcome.Should().Be(ScheduleOutcome.Invalid);
        (await RenamePhaseAsync(Marina, _phase, null)).Outcome.Should().Be(ScheduleOutcome.Invalid);

        (await GetAsync(Marina))!.Figures.Phases.Should().ContainSingle();
    }

    [Fact(DisplayName = "Criar e renomear fase")]
    public async Task Phase_IsCreated_AndRenamed()
    {
        var phase = (await AddPhaseAsync(Marina, "Levantamento")).Id!.Value;

        (await RenamePhaseAsync(Marina, phase, " Diagnóstico ")).Outcome.Should().Be(ScheduleOutcome.Done);

        (await GetAsync(Marina))!.Figures.Phases.Select(p => p.Phase.Name).Should().Equal("Configuração", "Diagnóstico");
    }

    [Fact(DisplayName = "Excluir fase com tarefas")]
    public async Task RemovingAPhase_RemovesItsItems()
    {
        var survey = (await AddPhaseAsync(Marina, "Levantamento")).Id!.Value;
        var first = (await AddItemAsync(Marina, Form(survey, "Mapear processos", D(10, 5), 5))).Id!.Value;
        var next = await AddTaskAsync("Parametrizar ICMS", D(10, 13), 3, first);

        (await RemovePhaseAsync(Marina, survey)).Outcome.Should().Be(ScheduleOutcome.Done);

        var item = await ItemAsync(next);
        (item.Item.Start, item.Item.End).Should().Be((D(10, 13), D(10, 15)));
        item.Predecessors.Should().BeEmpty();
        (await GetAsync(Marina))!.Figures.Phases.Should().ContainSingle();
    }

    [Fact(DisplayName = "Tarefa inválida")]
    public async Task InvalidItems_AreRefused()
    {
        var other = Guid.CreateVersion7();

        (await AddItemAsync(Marina, Form(other, " ", D(10, 5), 0, progress: 101)))
            .Invalid.Should().BeEquivalentTo([ScheduleField.Name, ScheduleField.Phase, ScheduleField.Duration, ScheduleField.Progress]);
        (await AddItemAsync(Marina, Form(_phase, "Aceite", D(10, 5), 0, _diego), ScheduleItemKind.Milestone))
            .Invalid.Should().Equal(ScheduleField.Owner);
        (await AddItemAsync(Marina, Form(_phase, "Tarefa", new DateOnly(1999, 12, 31), 1, after: Guid.CreateVersion7())))
            .Invalid.Should().Equal(ScheduleField.Start, ScheduleField.Predecessors);

        (await GetAsync(Marina))!.Figures.Phases.Single().Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "Criar tarefa")]
    public async Task Task_IsSavedWithItsEnd()
    {
        var id = await AddTaskAsync(" Levantar cadastros ", D(10, 8), 3);

        var item = (await ItemAsync(id)).Item;
        (item.Name, item.Start, item.End, item.Duration, item.Kind).Should().Be(("Levantar cadastros", D(10, 8), D(10, 13), 3, ScheduleItemKind.Task));
    }

    [Fact(DisplayName = "Responsável do projeto")]
    public async Task Owner_IsAPersonOfTheProject()
    {
        var id = await AddTaskAsync("Integrar NF-e", D(10, 19), 5);

        var result = await ChangeItemAsync(Marina, id, TaskForm("Integrar NF-e", D(10, 19), 5, _diego));

        result.Outcome.Should().Be(ScheduleOutcome.Done);
        (await ItemAsync(id)).Item.OwnerId.Should().Be(_diego);
    }

    [Fact(DisplayName = "Leitor ou pessoa de fora como responsável")]
    public async Task Viewer_OrOutsider_IsNoOwner()
    {
        var id = await AddTaskAsync("Integrar NF-e", D(10, 19), 5);

        (await ChangeItemAsync(Marina, id, TaskForm("Integrar NF-e", D(10, 19), 5, _lucas))).Invalid.Should().Equal(ScheduleField.Owner);
        (await ChangeItemAsync(Marina, id, TaskForm("Integrar NF-e", D(10, 19), 5, _beatriz))).Invalid.Should().Equal(ScheduleField.Owner);

        (await ItemAsync(id)).Item.OwnerId.Should().BeNull();
    }

    [Fact(DisplayName = "Dependência circular")]
    public async Task Cycle_IsRefused()
    {
        var a = await AddTaskAsync("A", D(10, 5), 1);
        var b = await AddTaskAsync("B", D(10, 6), 1, a);
        var c = await AddTaskAsync("C", D(10, 7), 1, b);

        var result = await ChangeItemAsync(Marina, a, TaskForm("A", D(10, 5), 1, null, c));
        var itself = await ChangeItemAsync(Marina, a, TaskForm("A", D(10, 5), 1, null, a));

        result.Outcome.Should().Be(ScheduleOutcome.Cycle);
        itself.Invalid.Should().Equal(ScheduleField.Predecessors);
        (await ItemAsync(a)).Predecessors.Should().BeEmpty();
    }

    [Fact(DisplayName = "Atrasar empurra a cadeia")]
    public async Task LaterEnd_PushesTheChain()
    {
        var a = await AddTaskAsync("A", D(10, 5), 5);
        var b = await AddTaskAsync("B", D(10, 13), 2, a);
        var c = await AddTaskAsync("C", D(10, 15), 1, b);

        var result = await ChangeItemAsync(Marina, a, TaskForm("A", D(10, 5), 6));

        result.Should().BeEquivalentTo(ScheduleResult.Done(pushed: 2).Labeled(ScheduleAction.TaskChanged, "A"));
        (await ItemAsync(a)).Predecessors.Should().BeEmpty();
        ((await ItemAsync(b)).Item.Start, (await ItemAsync(c)).Item.Start).Should().Be((D(10, 14), D(10, 16)));
    }

    [Fact(DisplayName = "Duas edições ao mesmo tempo")]
    public async Task TwoChangesAtOnce_KeepTheChain()
    {
        var a = await AddTaskAsync("A", D(10, 5), 5);
        var b = await AddTaskAsync("B", D(10, 13), 2, a);
        var c = await AddTaskAsync("C", D(10, 15), 1, b);

        var results = await System.Threading.Tasks.Task.WhenAll(
            ChangeItemAsync(Marina, a, TaskForm("A", D(10, 5), 8)),
            ChangeItemAsync(Paulo, b, TaskForm("B", D(10, 13), 4, null, a)));

        results.Should().OnlyContain(result => result.Outcome == ScheduleOutcome.Done);
        var (itemA, itemB, itemC) = ((await ItemAsync(a)).Item, (await ItemAsync(b)).Item, (await ItemAsync(c)).Item);
        (itemA.End, itemB.Duration).Should().Be((D(10, 15), 4));
        itemB.Start.Should().Be(D(10, 16), "B starts the next business day after A ends");
        itemC.Start.Should().Be(TestBusinessDays.Of2026.AddBusinessDays(itemB.End, 1));
    }

    [Fact(DisplayName = "Alterar tarefa num projeto concluído")]
    public async Task ClosedProject_KeepsItsSchedule()
    {
        var task = await AddTaskAsync("Parametrizar ICMS", D(10, 13), 5);
        await CompleteProjectAsync();

        (await ChangeItemAsync(Marina, task, TaskForm("Outra", D(10, 13), 1))).Outcome.Should().Be(ScheduleOutcome.Closed);
        (await AddPhaseAsync(Paulo, "Homologação")).Outcome.Should().Be(ScheduleOutcome.Closed);

        (await ItemAsync(task)).Item.Name.Should().Be("Parametrizar ICMS");
        (await GetAsync(Rafael)).Should().NotBeNull();
    }

    [Fact(DisplayName = "Remover o responsável de tarefas")]
    public async Task RemovedPerson_LeavesTheTasksWithoutOwner()
    {
        var first = (await AddItemAsync(Marina, Form(_phase, "Integrar NF-e", D(10, 19), 5, _diego, 40))).Id!.Value;
        var second = (await AddItemAsync(Marina, TaskForm("Ajustar relatórios", D(10, 26), 3, _diego))).Id!.Value;
        var other = (await AddItemAsync(Marina, TaskForm("Testes integrados", D(10, 26), 3, _carla))).Id!.Value;

        (await _database.RunAsync<RemoveProjectMember, MemberOutcome>(remove =>
            remove.ExecuteAsync(_project, _diego, Marina, Cancellation))).Should().Be(MemberOutcome.Done);

        var firstItem = (await ItemAsync(first)).Item;
        (firstItem.OwnerId, firstItem.Start, firstItem.End, firstItem.Progress).Should().Be(((Guid?)null, D(10, 19), D(10, 23), 40));
        (await ItemAsync(second)).Item.OwnerId.Should().BeNull();
        (await ItemAsync(other)).Item.OwnerId.Should().Be(_carla);
    }

    [Fact(DisplayName = "Responsável passa a leitor")]
    public async Task PersonWhoBecomesAViewer_LeavesTheTasksWithoutOwner()
    {
        var task = (await AddItemAsync(Marina, TaskForm("Testes integrados", D(10, 26), 3, _carla))).Id!.Value;
        var other = (await AddItemAsync(Marina, TaskForm("Integrar NF-e", D(10, 19), 5, _diego))).Id!.Value;

        (await _database.RunAsync<ChangeProjectMemberRole, MemberOutcome>(change =>
            change.ExecuteAsync(_project, _carla, ProjectRole.Viewer, Marina, Cancellation))).Should().Be(MemberOutcome.Done);
        (await _database.RunAsync<ChangeProjectMemberRole, MemberOutcome>(change =>
            change.ExecuteAsync(_project, _diego, ProjectRole.Contributor, Marina, Cancellation))).Should().Be(MemberOutcome.Done);

        (await ItemAsync(task)).Item.OwnerId.Should().BeNull();
        (await ItemAsync(other)).Item.OwnerId.Should().Be(_diego);
    }

    [Fact(DisplayName = "Gerente atualiza qualquer tarefa")]
    public async Task Manager_UpdatesTheProgressOfAnyTask()
    {
        var diegos = (await AddItemAsync(Marina, TaskForm("Integrar NF-e", D(10, 19), 5, _diego))).Id!.Value;
        var nobodys = await AddTaskAsync("Ajustar relatórios", D(10, 26), 3);

        (await UpdateProgressAsync(Marina, diegos, 40)).Outcome.Should().Be(ScheduleOutcome.Done);
        (await UpdateProgressAsync(Paulo, nobodys, 100)).Outcome.Should().Be(ScheduleOutcome.Done);

        (await ItemAsync(diegos)).Item.Progress.Should().Be(40);
        (await ItemAsync(nobodys)).Item.Should().Match<ScheduleItem>(item => item.Progress == 100 && item.CompletedOn == D(10, 19));
        (await GetAsync(Marina))!.ProgressUpdatable.Should().BeEquivalentTo([diegos, nobodys]);
    }

    [Theory(DisplayName = "Responsável atualiza a própria tarefa")]
    [InlineData("Rafael")]
    [InlineData("Carla")]
    [InlineData("Diego")]
    public async Task Owner_UpdatesTheProgressOfTheirOwnTask(string person)
    {
        var viewer = ViewerOf(person);
        var own = (await AddItemAsync(Marina, TaskForm("Integrar NF-e", D(10, 19), 5, viewer.UserId))).Id!.Value;
        var others = (await AddItemAsync(Marina, TaskForm("Testes integrados", D(10, 26), 3, person == "Diego" ? _carla : _diego))).Id!.Value;

        (await UpdateProgressAsync(viewer, own, 30)).Outcome.Should().Be(ScheduleOutcome.Done);
        (await UpdateProgressAsync(viewer, others, 30)).Outcome.Should().Be(ScheduleOutcome.Forbidden);

        ((await ItemAsync(own)).Item.Progress, (await ItemAsync(others)).Item.Progress).Should().Be((30, 0));
        (await GetAsync(viewer))!.ProgressUpdatable.Should().Equal(own);
        (await UpdateProgressAsync(viewer, Guid.CreateVersion7(), 30)).Outcome.Should().Be(ScheduleOutcome.NotFound);
    }

    [Fact(DisplayName = "Leitor não atualiza o progresso")]
    public async Task Viewer_DoesNotUpdateTheProgress()
    {
        var task = (await AddItemAsync(Marina, Form(_phase, "Integrar NF-e", D(10, 19), 5, _diego, 20))).Id!.Value;
        var lucas = ViewerOf("Lucas");

        (await UpdateProgressAsync(lucas, task, 50)).Outcome.Should().Be(ScheduleOutcome.Forbidden);
        (await UpdateProgressAsync(new ProjectViewer(_beatriz, false), task, 50)).Outcome.Should().Be(ScheduleOutcome.NotFound);

        (await ItemAsync(task)).Item.Progress.Should().Be(20);
        (await GetAsync(lucas))!.ProgressUpdatable.Should().BeEmpty();
    }

    [Fact(DisplayName = "Progresso de um marco")]
    public async Task Milestone_HasNoProgress()
    {
        var milestone = await AddMilestoneAsync("Aceite da configuração", D(10, 30));
        var task = await AddTaskAsync("Integrar NF-e", D(10, 19), 5);

        (await UpdateProgressAsync(Marina, milestone, 0)).Should().BeEquivalentTo(ScheduleResult.InvalidFields([ScheduleField.Progress]));
        (await UpdateProgressAsync(Marina, task, 101)).Invalid.Should().Equal(ScheduleField.Progress);
        (await UpdateProgressAsync(Marina, task, -1)).Invalid.Should().Equal(ScheduleField.Progress);

        (await ItemAsync(task)).Item.Progress.Should().Be(0);
        (await GetAsync(Marina))!.ProgressUpdatable.Should().Equal(task);
    }

    [Fact(DisplayName = "Progresso num projeto encerrado")]
    public async Task ClosedProject_KeepsTheProgress()
    {
        var task = (await AddItemAsync(Marina, Form(_phase, "Integrar NF-e", D(10, 19), 5, _diego, 20))).Id!.Value;
        var milestone = await AddMilestoneAsync("Aceite da configuração", D(10, 30));
        await CompleteProjectAsync();

        (await UpdateProgressAsync(ViewerOf("Diego"), task, 50)).Outcome.Should().Be(ScheduleOutcome.Closed);
        (await UpdateProgressAsync(ViewerOf("Carla"), task, 50)).Outcome.Should().Be(ScheduleOutcome.Forbidden);
        (await SetReachedAsync(Marina, milestone, true)).Outcome.Should().Be(ScheduleOutcome.Closed);

        (await ItemAsync(task)).Item.Progress.Should().Be(20);
        (await ItemAsync(milestone)).Item.ReachedOn.Should().BeNull();
        (await GetAsync(Marina))!.ProgressUpdatable.Should().BeEmpty();
    }

    [Fact(DisplayName = "Alterar a tarefa mantém o progresso")]
    public async Task ChangingTheTask_WithoutTheProgress_KeepsIt()
    {
        var task = (await AddItemAsync(Marina, TaskForm("Integrar NF-e", D(10, 19), 5, _diego))).Id!.Value;

        (await UpdateProgressAsync(ViewerOf("Diego"), task, 60)).Outcome.Should().Be(ScheduleOutcome.Done);
        (await ChangeItemAsync(Marina, task, TaskForm("Integrar a NF-e", D(10, 19), 5, _diego) with { Progress = null }))
            .Outcome.Should().Be(ScheduleOutcome.Done);

        (await ItemAsync(task)).Item.Should().Match<ScheduleItem>(item => item.Name == "Integrar a NF-e" && item.Progress == 60);
    }

    [Fact(DisplayName = "Gerente e responsável mudam a tarefa ao mesmo tempo")]
    public async Task ManagerAndOwner_ChangeTheTaskAtOnce()
    {
        var task = (await AddItemAsync(Marina, TaskForm("Integrar NF-e", D(10, 19), 5, _diego))).Id!.Value;

        var results = await System.Threading.Tasks.Task.WhenAll(
            ChangeItemAsync(Marina, task, TaskForm("Integrar a NF-e", D(10, 19), 6, _diego) with { Progress = null }),
            UpdateProgressAsync(ViewerOf("Diego"), task, 60));

        results.Should().OnlyContain(result => result.Outcome == ScheduleOutcome.Done);
        (await ItemAsync(task)).Item.Should().Match<ScheduleItem>(item =>
            item.Name == "Integrar a NF-e" && item.Duration == 6 && item.Progress == 60);
    }

    [Fact(DisplayName = "Marcar marco como atingido")]
    public async Task Manager_MarksTheMilestoneAsReached()
    {
        var milestone = await AddMilestoneAsync("Aceite da homologação", D(10, 30));

        (await SetReachedAsync(Marina, milestone, true)).Outcome.Should().Be(ScheduleOutcome.Done);

        (await ItemAsync(milestone)).Item.ReachedOn.Should().Be(D(10, 19));
        (await SetReachedAsync(Paulo, milestone, false)).Outcome.Should().Be(ScheduleOutcome.Done);
        (await ItemAsync(milestone)).Item.ReachedOn.Should().BeNull();
    }

    [Theory(DisplayName = "Líder técnico não marca o marco")]
    [InlineData("Rafael")]
    [InlineData("Carla")]
    [InlineData("Diego")]
    [InlineData("Lucas")]
    public async Task OtherRoles_DoNotMarkTheMilestone(string person)
    {
        var milestone = await AddMilestoneAsync("Aceite da homologação", D(10, 30));

        (await SetReachedAsync(ViewerOf(person), milestone, true)).Outcome.Should().Be(ScheduleOutcome.Forbidden);
        (await SetReachedAsync(new ProjectViewer(_beatriz, false), milestone, true)).Outcome.Should().Be(ScheduleOutcome.NotFound);

        (await ItemAsync(milestone)).Item.ReachedOn.Should().BeNull();
    }

    [Fact(DisplayName = "Atingir uma tarefa")]
    public async Task Task_IsNotReached()
    {
        var task = await AddTaskAsync("Integrar NF-e", D(10, 19), 5);

        (await SetReachedAsync(Marina, task, true)).Should().BeEquivalentTo(ScheduleResult.InvalidFields([ScheduleField.Reached]));
        (await SetReachedAsync(Marina, Guid.CreateVersion7(), true)).Outcome.Should().Be(ScheduleOutcome.NotFound);

        (await ItemAsync(task)).Item.ReachedOn.Should().BeNull();
    }

    [Fact(DisplayName = "Marco atrasado na lista")]
    public async Task List_CountsTheLateMilestones()
    {
        var late = await AddTaskAsync("Validar cadastro", D(10, 13), 3);
        await UpdateProgressAsync(Marina, late, 80);
        await AddMilestoneAsync("Aceite da configuração", D(10, 16));
        var reached = await AddMilestoneAsync("Aceite do levantamento", D(10, 9));
        await SetReachedAsync(Marina, reached, true);

        var page = await _database.RunAsync<ListProjects, ProjectListPage>(list => list.ExecuteAsync(
            new ProjectListQuery(null, null, ProjectSituation.Active, false, 1, 25), Marina, Cancellation));

        var erp = page.Items.Single(item => item.Code == "ERP-FIN").Schedule!;
        (erp.LateTasks, erp.LateMilestones).Should().Be((1, 1));
    }

    [Fact(DisplayName = "Mudar o início pela API")]
    public async Task Manager_MovesATask_KeepingItsDuration()
    {
        var task = await AddTaskAsync("Parametrizar ICMS", D(10, 13), 3);
        var next = await AddTaskAsync("Testes", D(10, 16), 2, task);
        await UpdateProgressAsync(Marina, task, 40);

        var result = await RescheduleAsync(Marina, task, D(10, 16));

        result.Should().BeEquivalentTo(ScheduleResult.Done(pushed: 1).Labeled(ScheduleAction.DatesChanged, "Parametrizar ICMS"));
        (await ItemAsync(task)).Item.Should().Match<ScheduleItem>(item =>
            item.Start == D(10, 16) && item.End == D(10, 20) && item.Duration == 3 && item.Progress == 40);
        (await ItemAsync(next)).Item.Start.Should().Be(D(10, 21));
        (await ItemAsync(next)).Predecessors.Should().Equal(task);
    }

    [Fact(DisplayName = "Mudar o fim pela API")]
    public async Task Manager_ChangesTheEnd_OfATask()
    {
        var task = await AddTaskAsync("Parametrizar ICMS", D(10, 13), 3);

        (await RescheduleAsync(Paulo, task, D(10, 13), D(10, 20))).Outcome.Should().Be(ScheduleOutcome.Done);

        (await ItemAsync(task)).Item.Should().Match<ScheduleItem>(item => item.Duration == 6 && item.End == D(10, 20));
    }

    [Fact(DisplayName = "Fim antes do início")]
    public async Task InvalidDates_AreRefused()
    {
        var task = await AddTaskAsync("Parametrizar ICMS", D(10, 13), 3);
        var milestone = await AddMilestoneAsync("Aceite", D(10, 20));

        (await RescheduleAsync(Marina, task, D(10, 14), D(10, 13))).Should().BeEquivalentTo(ScheduleResult.InvalidFields([ScheduleField.End]));
        (await RescheduleAsync(Marina, milestone, D(10, 20), D(10, 21))).Invalid.Should().Equal(ScheduleField.End);
        (await RescheduleAsync(Marina, task, new DateOnly(1999, 12, 31))).Invalid.Should().Equal(ScheduleField.Start);
        (await RescheduleAsync(Marina, Guid.CreateVersion7(), D(10, 14))).Outcome.Should().Be(ScheduleOutcome.NotFound);
        (await RescheduleAsync(Rafael, task, D(10, 14))).Outcome.Should().Be(ScheduleOutcome.Forbidden);
        (await RescheduleAsync(new ProjectViewer(_beatriz, false), task, D(10, 14))).Outcome.Should().Be(ScheduleOutcome.NotFound);

        (await ItemAsync(task)).Item.Should().Match<ScheduleItem>(item => item.Start == D(10, 13) && item.End == D(10, 15));
        await CompleteProjectAsync();
        (await RescheduleAsync(Marina, task, D(10, 14))).Outcome.Should().Be(ScheduleOutcome.Closed);
    }

    [Fact(DisplayName = "Dias não úteis no cronograma")]
    public async Task Schedule_ComesWithTheNonWorkingDaysAround()
    {
        (await GetAsync(Marina))!.NonWorkingDays.Should().BeEmpty("the schedule has no items yet");
        await AddTaskAsync("Parametrizar ICMS", D(10, 13), 3);

        var days = (await GetAsync(Rafael))!.NonWorkingDays;

        days.Should().Contain([D(10, 12), D(10, 10), D(11, 2)]);
        (days[0], days[^1]).Should().Be((new DateOnly(2026, 9, 5), new DateOnly(2026, 11, 29)));
    }

    [Fact(DisplayName = "Cronograma na lista")]
    public async Task List_ShowsTheNumbersOfTheSchedules()
    {
        var late = await AddTaskAsync("Validar cadastro", D(10, 13), 3);
        await ChangeItemAsync(Marina, late, Form(_phase, "Validar cadastro", D(10, 13), 3, progress: 80));
        await AddItemAsync(Marina, Form(_phase, "Aceite", D(11, 3), 0), ScheduleItemKind.Milestone);
        await _database.RunAsync<CreateProject, ProjectSaveResult>(create => create.ExecuteAsync(
            new NewProject("SPED-27", "Adequação do SPED Fiscal 2027", null, null, null, null), _marina, Cancellation));

        var page = await _database.RunAsync<ListProjects, ProjectListPage>(list => list.ExecuteAsync(
            new ProjectListQuery(null, null, ProjectSituation.Active, false, 1, 25), Marina, Cancellation));

        page.Today.Should().Be(D(10, 19));
        var erp = page.Items.Single(item => item.Code == "ERP-FIN").Schedule!;
        (erp.Start, erp.End, erp.Progress, erp.LateTasks, erp.CurrentPhase!.Name).Should().Be((D(10, 13), D(11, 3), 80, 1, "Configuração"));
        erp.Milestones.Should().Equal(D(11, 3));
        page.Items.Single(item => item.Code == "SPED-27").Schedule.Should().BeNull();
    }

    [Fact(DisplayName = "Datas do cronograma no cabeçalho")]
    public async Task Project_ComesWithTheDatesOfItsSchedule()
    {
        await AddTaskAsync("Parametrizar ICMS", D(10, 13), 5);
        await AddItemAsync(Marina, Form(_phase, "Aceite", D(11, 13), 0), ScheduleItemKind.Milestone);

        var details = await _database.RunAsync<GetProject, ProjectDetails?>(get => get.ExecuteAsync(_project, Rafael, Cancellation));

        details!.Schedule.Should().Be(new SchedulePeriod(D(10, 13), D(11, 13)));
        details.Permissions.CanEditSchedule.Should().BeFalse();
    }

    [Fact(DisplayName = "Incluir subtarefa")]
    public async Task Subtask_IsSaved_UnderItsTask()
    {
        var rules = await AddTaskAsync("Parametrizar regras fiscais", D(10, 13), 5);
        var mapping = await AddSubtaskAsync("Mapear regras de ICMS", D(10, 13), 3, rules);
        var approved = (await AddItemAsync(Marina, SubtaskForm("Regras aprovadas", D(10, 15), 0, rules, mapping), ScheduleItemKind.Milestone)).Id!.Value;

        var view = (await GetAsync(Marina))!;

        view.Figures.Phases.Single().Items.Select(item => item.Id).Should().Equal(rules, mapping, approved);
        view.Figures.Summaries.Should().BeEquivalentTo([rules]);
        view.ProgressUpdatable.Should().BeEquivalentTo([mapping], "a task with subtasks takes its progress from them");
        (await ItemAsync(approved)).Item.ParentId.Should().Be(rules);
        (await ItemAsync(rules)).Item.Should().Match<ScheduleItem>(item => item.Start == D(10, 13) && item.End == D(10, 15) && item.Duration == 3);
    }

    [Fact(DisplayName = "Subtarefa fora do nível")]
    public async Task Subtask_OutOfTheLevel_IsRefused()
    {
        var other = (await AddPhaseAsync(Marina, "Homologação")).Id!.Value;
        var rules = await AddTaskAsync("Parametrizar regras fiscais", D(10, 13), 5);
        var mapping = await AddSubtaskAsync("Mapear regras de ICMS", D(10, 13), 3, rules);
        var loose = await AddTaskAsync("Integrar NF-e", D(10, 13), 2);

        (await AddItemAsync(Marina, SubtaskForm("Detalhar", D(10, 13), 1, mapping))).Invalid.Should().Equal(ScheduleField.Parent);
        (await AddItemAsync(Marina, new ScheduleItemForm(other, "Detalhar", D(10, 13), 1, null, 0, [], rules))).Invalid.Should().Equal(ScheduleField.Parent);
        (await ChangeItemAsync(Marina, rules, SubtaskForm("Parametrizar regras fiscais", D(10, 13), 5, loose))).Invalid.Should().Equal(ScheduleField.Parent);
        (await AddItemAsync(Marina, SubtaskForm("Detalhar", D(10, 13), 1, Guid.CreateVersion7()))).Invalid.Should().Equal(ScheduleField.Parent);
        (await GetAsync(Marina))!.Figures.Summaries.Should().BeEquivalentTo([rules]);
    }

    [Fact(DisplayName = "Progresso de uma tarefa-resumo")]
    public async Task Summary_RefusesProgressAndDates()
    {
        var rules = await AddTaskAsync("Parametrizar regras fiscais", D(10, 13), 5);
        await AddSubtaskAsync("Mapear regras de ICMS", D(10, 13), 3, rules);

        (await UpdateProgressAsync(Marina, rules, 50)).Invalid.Should().Equal(ScheduleField.Progress);
        (await RescheduleAsync(Marina, rules, D(10, 20))).Invalid.Should().Equal(ScheduleField.Start);
        (await ChangeItemAsync(Marina, rules, Form(_phase, "Parametrizar regras fiscais", D(10, 13), 5, progress: 50))).Invalid
            .Should().Equal(ScheduleField.Progress);
        // A summary takes only the name, the phase and the owner: its start and duration are not looked at.
        (await ChangeItemAsync(Marina, rules, SubtaskForm("Regras fiscais", new DateOnly(1999, 1, 1), 0, null))).Outcome
            .Should().Be(ScheduleOutcome.Done);

        (await ItemAsync(rules)).Item.Should().Match<ScheduleItem>(item =>
            item.Name == "Regras fiscais" && item.Start == D(10, 13) && item.End == D(10, 15) && item.Progress == 0);
    }

    [Fact(DisplayName = "Depender de uma tarefa-resumo")]
    public async Task Summary_InADependency_IsRefused()
    {
        var rules = await AddTaskAsync("Parametrizar regras fiscais", D(10, 13), 5);
        await AddSubtaskAsync("Mapear regras de ICMS", D(10, 13), 3, rules);
        var loose = await AddTaskAsync("Integrar NF-e", D(10, 13), 2);

        (await ChangeItemAsync(Marina, loose, TaskForm("Integrar NF-e", D(10, 13), 2, null, rules))).Invalid.Should().Equal(ScheduleField.Predecessors);
        (await ChangeItemAsync(Marina, rules, SubtaskForm("Parametrizar regras fiscais", D(10, 13), 5, null, loose))).Invalid
            .Should().Equal(ScheduleField.Predecessors);
        (await AddItemAsync(Marina, SubtaskForm("Detalhar", D(10, 13), 1, loose, loose))).Invalid.Should().Equal(ScheduleField.Predecessors);
        (await ItemAsync(loose)).Predecessors.Should().BeEmpty();
    }

    [Fact(DisplayName = "Primeira subtarefa herda as dependências")]
    public async Task FirstSubtask_TakesTheDependencies_AndPushes()
    {
        var icms = await AddTaskAsync("Parametrizar regras de ICMS", D(10, 13), 8);
        var tests = await AddTaskAsync("Testes integrados", D(10, 23), 5, icms);
        var clientTests = await AddTaskAsync("Testes com a equipe do cliente", D(10, 30), 2, tests);

        var script = await AddSubtaskAsync("Roteiro de testes", D(10, 13), 7, tests);

        (await ItemAsync(script)).Predecessors.Should().Equal(icms);
        var after = await ItemAsync(clientTests);
        after.Predecessors.Should().Equal(script);
        after.Item.Start.Should().Be(D(11, 4));
        (await ItemAsync(tests)).Predecessors.Should().BeEmpty();
    }

    [Fact(DisplayName = "Ciclo pela tarefa que recebe a subtarefa")]
    public async Task FirstSubtask_RefusesACycle()
    {
        var tests = await AddTaskAsync("Testes integrados", D(10, 13), 5);
        var clientTests = await AddTaskAsync("Testes com a equipe do cliente", D(10, 20), 2, tests);

        var result = await AddItemAsync(Marina, SubtaskForm("Roteiro de testes", D(10, 13), 2, tests, clientTests));

        result.Outcome.Should().Be(ScheduleOutcome.Cycle);
        (await GetAsync(Marina))!.Figures.Summaries.Should().BeEmpty();
    }

    [Fact(DisplayName = "Excluir a última subtarefa")]
    public async Task RemovingTheLastSubtask_GivesItsDependenciesBack()
    {
        var icms = await AddTaskAsync("Parametrizar regras de ICMS", D(10, 5), 4);
        var tests = await AddTaskAsync("Testes integrados", D(10, 13), 1);
        var script = await AddSubtaskAsync("Roteiro de testes", D(10, 13), 3, tests, icms);

        (await RemoveItemAsync(Marina, script)).Outcome.Should().Be(ScheduleOutcome.Done);

        var view = await ItemAsync(tests);
        view.Predecessors.Should().Equal(icms);
        view.Item.Should().Match<ScheduleItem>(item => item.Start == D(10, 13) && item.End == D(10, 15) && item.Duration == 3);
        (await GetAsync(Marina))!.ProgressUpdatable.Should().Contain(tests);
    }

    [Fact(DisplayName = "Excluir tarefa com subtarefas")]
    public async Task RemovingTheSummary_RemovesItsSubtasks()
    {
        var approval = (await AddPhaseAsync(Marina, "Homologação")).Id!.Value;
        var tests = await AddTaskAsync("Testes integrados", D(10, 13), 1);
        var script = await AddSubtaskAsync("Roteiro de testes", D(10, 13), 2, tests);
        await AddSubtaskAsync("Executar testes", D(10, 15), 3, tests, script);
        var clientTests = (await AddItemAsync(Marina, new ScheduleItemForm(approval, "Testes com a equipe do cliente", D(10, 20), 2, null, 0, [script]))).Id!.Value;

        (await RemoveItemAsync(Marina, tests)).Outcome.Should().Be(ScheduleOutcome.Done);

        var view = (await GetAsync(Marina))!;
        view.Figures.Phases.SelectMany(phase => phase.Items).Select(item => item.Id).Should().Equal(clientTests);
        view.Predecessors.Should().BeEmpty();
        (await ItemAsync(clientTests)).Item.Start.Should().Be(D(10, 20));
    }

    [Fact(DisplayName = "Mudar a fase da tarefa-resumo")]
    public async Task MovingTheSummary_TakesItsSubtasks()
    {
        var approval = (await AddPhaseAsync(Marina, "Homologação")).Id!.Value;
        var tests = await AddTaskAsync("Testes integrados", D(10, 13), 1);
        var script = await AddSubtaskAsync("Roteiro de testes", D(10, 13), 2, tests);

        (await ChangeItemAsync(Marina, tests, new ScheduleItemForm(approval, "Testes integrados", D(10, 13), 1, null, null, []))).Outcome
            .Should().Be(ScheduleOutcome.Done);

        ((await ItemAsync(tests)).Item.PhaseId, (await ItemAsync(script)).Item.PhaseId).Should().Be((approval, approval));
    }
}
