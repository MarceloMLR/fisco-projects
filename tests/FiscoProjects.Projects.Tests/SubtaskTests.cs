using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: cronograma (domain side): "Subtarefas", "Tarefa-resumo", "Dependências com subtarefas", "Excluir
// tarefa-resumo" and "Datas e progresso das fases e do projeto" (ADR-0088).
public sealed class SubtaskTests
{
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;
    private static readonly DateOnly Today = D(10, 16);

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static ScheduleItem Add(
        Schedule schedule,
        SchedulePhase phase,
        string name,
        DateOnly start,
        int duration,
        ScheduleItem? inside = null,
        params ScheduleItem[] after) =>
        schedule.AddItem(
            duration == 0 ? ScheduleItemKind.Milestone : ScheduleItemKind.Task,
            new ScheduleItemPlan(phase.Id, name, start, duration, null, 0, [.. after.Select(item => item.Id)], inside?.Id),
            Days,
            Today);

    private static ScheduleItemPlan PlanOf(Schedule schedule, ScheduleItem item) => new(
        item.PhaseId, item.Name, item.Start, item.Duration, item.OwnerId, null,
        [.. schedule.PredecessorsOf(item.Id).Select(predecessor => predecessor.Id)], item.ParentId);

    private static int? Change(Schedule schedule, ScheduleItem item, Func<ScheduleItemPlan, ScheduleItemPlan> change) =>
        schedule.ChangeItem(item.Id, change(PlanOf(schedule, item)), Days, Today);

    private static (Schedule Schedule, SchedulePhase Phase) NewSchedule()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        return (schedule, schedule.AddPhase("Configuração"));
    }

    private static IEnumerable<string> Predecessors(Schedule schedule, ScheduleItem item) =>
        schedule.PredecessorsOf(item.Id).Select(predecessor => predecessor.Name);

    private static IEnumerable<string> Successors(Schedule schedule, ScheduleItem item) =>
        schedule.Dependencies.Where(d => d.PredecessorId == item.Id).Select(d => schedule.ItemOf(d.SuccessorId)!.Name);

    [Fact(DisplayName = "Incluir subtarefa")]
    public void Subtasks_GoUnderTheTask_InItsPhase()
    {
        var (schedule, phase) = NewSchedule();
        var rules = Add(schedule, phase, "Parametrizar regras fiscais", D(10, 13), 5);
        var after = Add(schedule, phase, "Integrar NF-e", D(10, 13), 2);

        var mapping = Add(schedule, phase, "Mapear regras de ICMS", D(10, 14), 3, rules);
        var approved = Add(schedule, phase, "Regras aprovadas", D(10, 16), 0, rules, mapping);

        mapping.ParentId.Should().Be(rules.Id);
        approved.ParentId.Should().Be(rules.Id);
        approved.PhaseId.Should().Be(phase.Id);
        schedule.IsSummary(rules.Id).Should().BeTrue();
        schedule.SubtasksOf(rules.Id).Should().BeEquivalentTo([mapping, approved]);
        // The task takes the dates of its subtasks (14/10 to 16/10), so it comes after "Integrar NF-e" (13/10).
        schedule.Figures(Today).Phases.Single().Items.Should().Equal(after, rules, mapping, approved);
    }

    [Fact(DisplayName = "Subtarefa fora do nível")]
    public void Subtasks_HaveOneLevel_InTheSamePhase()
    {
        var (schedule, phase) = NewSchedule();
        var other = schedule.AddPhase("Homologação");
        var rules = Add(schedule, phase, "Parametrizar regras fiscais", D(10, 13), 5);
        var mapping = Add(schedule, phase, "Mapear regras de ICMS", D(10, 14), 3, rules);
        var milestone = Add(schedule, phase, "Regras aprovadas", D(10, 20), 0);
        var loose = Add(schedule, phase, "Integrar NF-e", D(10, 13), 2);

        schedule.MayHold(mapping.Id, null, phase.Id).Should().BeFalse("a subtask holds no subtasks");
        schedule.MayHold(milestone.Id, null, phase.Id).Should().BeFalse("a milestone holds no subtasks");
        schedule.MayHold(loose.Id, rules.Id, phase.Id).Should().BeFalse("a task with subtasks goes into no other");
        schedule.MayHold(rules.Id, null, other.Id).Should().BeFalse("a subtask is in the phase of its task");
        schedule.MayHold(rules.Id, rules.Id, phase.Id).Should().BeFalse();
        var inSubtask = () => Add(schedule, phase, "Detalhar", D(10, 14), 1, mapping);
        var inMilestone = () => Add(schedule, phase, "Detalhar", D(10, 14), 1, milestone);
        var summaryIn = () => Change(schedule, rules, plan => plan with { ParentId = loose.Id });
        inSubtask.Should().Throw<ArgumentException>();
        inMilestone.Should().Throw<ArgumentException>();
        summaryIn.Should().Throw<ArgumentException>();
        rules.ParentId.Should().BeNull();
        schedule.Items.Should().HaveCount(4);
    }

    [Fact(DisplayName = "Mudar a fase da tarefa-resumo")]
    public void MovingTheSummary_TakesItsSubtasks()
    {
        var (schedule, configuration) = NewSchedule();
        var approval = schedule.AddPhase("Homologação");
        var tests = Add(schedule, configuration, "Testes integrados", D(10, 13), 5);
        var script = Add(schedule, configuration, "Roteiro de testes", D(10, 13), 2, tests);
        var run = Add(schedule, configuration, "Executar testes", D(10, 15), 3, tests, script);

        Change(schedule, tests, plan => plan with { PhaseId = approval.Id, Name = "Testes da homologação" });

        (tests.PhaseId, script.PhaseId, run.PhaseId).Should().Be((approval.Id, approval.Id, approval.Id));
        tests.Name.Should().Be("Testes da homologação");
        (tests.Start, tests.End).Should().Be((D(10, 13), D(10, 19)));
    }

    [Fact(DisplayName = "Datas e progresso da tarefa-resumo")]
    public void Summary_TakesItsDatesAndProgress_FromItsSubtasks()
    {
        var (schedule, phase) = NewSchedule();
        var task = Add(schedule, phase, "Parametrizar regras fiscais", D(10, 5), 1);
        var first = Add(schedule, phase, "Mapear", D(10, 5), 2, task);
        var second = Add(schedule, phase, "Configurar", D(10, 7), 8, task);
        schedule.UpdateProgress(first.Id, 100, Today);
        schedule.UpdateProgress(second.Id, 25, Today);

        // 12/10/2026 is a holiday: 05/10 to 19/10 has 10 business days; (2 × 100 + 8 × 25) / 10 = 40%.
        second.End.Should().Be(D(10, 19));
        (task.Start, task.End, task.Duration, task.Progress).Should().Be((D(10, 5), D(10, 19), 10, 40));
        task.CompletedOn.Should().BeNull();

        schedule.UpdateProgress(second.Id, 100, D(10, 20));

        (task.Progress, task.CompletedOn).Should().Be((100, D(10, 20)));
    }

    [Fact(DisplayName = "Tarefa-resumo só com marcos")]
    public void Summary_WithMilestonesOnly_IsDoneWhenAllAreReached()
    {
        var (schedule, phase) = NewSchedule();
        var task = Add(schedule, phase, "Aprovações", D(10, 13), 3);
        var layout = Add(schedule, phase, "Layout aprovado", D(10, 13), 0, task);
        var rules = Add(schedule, phase, "Regras aprovadas", D(10, 15), 0, task);

        schedule.SetReached(layout.Id, true, D(10, 13));
        task.Progress.Should().Be(0);
        schedule.SetReached(rules.Id, true, D(10, 15));

        (task.Start, task.End, task.Duration, task.Progress, task.CompletedOn).Should().Be((D(10, 13), D(10, 15), 3, 100, D(10, 15)));
    }

    [Fact(DisplayName = "Tarefa-resumo atrasada")]
    public void Summary_IsLate_WhenASubtaskIs()
    {
        var (schedule, phase) = NewSchedule();
        var task = Add(schedule, phase, "Validar cadastros", D(10, 13), 1);
        var late = Add(schedule, phase, "Validar produtos", D(10, 13), 3, task);
        Add(schedule, phase, "Validar clientes", D(10, 16), 5, task);
        schedule.UpdateProgress(late.Id, 80, Today);

        var figures = schedule.Figures(D(10, 16));

        figures.LateItems.Should().BeEquivalentTo([late.Id, task.Id]);
        figures.Phases.Single().IsLate.Should().BeTrue();
        figures.LateTasks.Should().Be(1);
        figures.Summaries.Should().BeEquivalentTo([task.Id]);
    }

    [Fact(DisplayName = "Progresso de uma tarefa-resumo")]
    public void Summary_RefusesProgressAndDates_OfItsOwn()
    {
        var (schedule, phase) = NewSchedule();
        var task = Add(schedule, phase, "Parametrizar regras fiscais", D(10, 13), 5);
        Add(schedule, phase, "Mapear", D(10, 13), 2, task);
        var before = (task.Start, task.End, task.Progress);

        var progress = () => schedule.UpdateProgress(task.Id, 50, Today);
        var dates = () => schedule.Reschedule(task.Id, D(10, 20), null, Days, Today);
        var withProgress = () => Change(schedule, task, plan => plan with { Progress = 50 });
        progress.Should().Throw<ArgumentException>();
        dates.Should().Throw<ArgumentException>();
        withProgress.Should().Throw<ArgumentException>();

        // The start and the duration of a summary are not looked at: they come from the subtasks.
        Change(schedule, task, plan => plan with { Start = D(11, 2), Duration = 30 }).Should().Be(0);
        (task.Start, task.End, task.Progress).Should().Be(before);
    }

    [Fact(DisplayName = "Primeira subtarefa herda as dependências")]
    public void FirstSubtask_TakesTheDependenciesOfTheTask()
    {
        var (schedule, phase) = NewSchedule();
        var icms = Add(schedule, phase, "Parametrizar regras de ICMS", D(10, 13), 8);
        var tests = Add(schedule, phase, "Testes integrados", D(10, 23), 5, null, icms);
        var clientTests = Add(schedule, phase, "Testes com a equipe do cliente", D(10, 30), 2, null, tests);

        var script = Add(schedule, phase, "Roteiro de testes", D(10, 13), 7, tests);

        Predecessors(schedule, script).Should().Equal("Parametrizar regras de ICMS");
        Successors(schedule, script).Should().Equal("Testes com a equipe do cliente");
        Predecessors(schedule, tests).Should().BeEmpty();
        Successors(schedule, tests).Should().BeEmpty();
        // It starts after ICMS ends (22/10) and lasts 7 days, so the tests with the client are pushed.
        (script.Start, script.End).Should().Be((D(10, 23), D(11, 3)));
        clientTests.Start.Should().Be(D(11, 4));
        (tests.Start, tests.End, tests.Duration).Should().Be((D(10, 23), D(11, 3), 7));
    }

    [Fact(DisplayName = "Ciclo pela tarefa que recebe a subtarefa")]
    public void FirstSubtask_RefusesACycle_ThroughTheTask()
    {
        var (schedule, phase) = NewSchedule();
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 5);
        var clientTests = Add(schedule, phase, "Testes com a equipe do cliente", D(10, 20), 2, null, tests);
        var loose = Add(schedule, phase, "Roteiro", D(10, 13), 2, null, clientTests);

        var add = () => Add(schedule, phase, "Roteiro de testes", D(10, 13), 2, tests, clientTests);
        var move = () => Change(schedule, loose, plan => plan with { ParentId = tests.Id });

        add.Should().Throw<ScheduleCycleException>();
        move.Should().Throw<ScheduleCycleException>();
        schedule.IsSummary(tests.Id).Should().BeFalse();
        Successors(schedule, tests).Should().Equal("Testes com a equipe do cliente");
    }

    [Fact(DisplayName = "Depender de uma tarefa-resumo")]
    public void Summary_HasNoDependencies()
    {
        var (schedule, phase) = NewSchedule();
        var task = Add(schedule, phase, "Parametrizar regras fiscais", D(10, 13), 5);
        var subtask = Add(schedule, phase, "Mapear", D(10, 13), 2, task);
        var other = Add(schedule, phase, "Integrar NF-e", D(10, 13), 2);

        var onSummary = () => Change(schedule, other, plan => plan with { PredecessorIds = [task.Id] });
        var summaryOn = () => Change(schedule, task, plan => plan with { PredecessorIds = [other.Id] });
        var onItsTask = () => Add(schedule, phase, "Detalhar", D(10, 13), 1, task, task);

        onSummary.Should().Throw<ArgumentException>();
        summaryOn.Should().Throw<ArgumentException>();
        onItsTask.Should().Throw<ArgumentException>();
        schedule.Dependencies.Should().BeEmpty();
        subtask.ParentId.Should().Be(task.Id);
    }

    [Fact(DisplayName = "Empurrar subtarefas")]
    public void Subtasks_OfDifferentTasks_PushEachOther()
    {
        var (schedule, phase) = NewSchedule();
        var rules = Add(schedule, phase, "Parametrizar regras fiscais", D(10, 5), 1);
        var mapping = Add(schedule, phase, "Mapear", D(10, 5), 4, rules);
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 1);
        var script = Add(schedule, phase, "Roteiro de testes", D(10, 13), 2, tests, mapping);

        var pushed = Change(schedule, mapping, plan => plan with { Duration = 6 });

        pushed.Should().Be(1);
        (mapping.End, script.Start, script.End).Should().Be((D(10, 13), D(10, 14), D(10, 15)));
        (rules.Start, rules.End).Should().Be((D(10, 5), D(10, 13)));
        (tests.Start, tests.End).Should().Be((D(10, 14), D(10, 15)));
    }

    [Fact(DisplayName = "Excluir a última subtarefa")]
    public void RemovingTheLastSubtask_GivesItsDependenciesBack()
    {
        var (schedule, phase) = NewSchedule();
        var icms = Add(schedule, phase, "Parametrizar regras de ICMS", D(10, 5), 4);
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 1);
        var script = Add(schedule, phase, "Roteiro de testes", D(10, 13), 3, tests, icms);
        schedule.UpdateProgress(script.Id, 50, Today);

        schedule.RemoveItem(script.Id, Days).Should().BeTrue();

        schedule.IsSummary(tests.Id).Should().BeFalse();
        Predecessors(schedule, tests).Should().Equal("Parametrizar regras de ICMS");
        (tests.Start, tests.End, tests.Duration, tests.Progress).Should().Be((D(10, 13), D(10, 15), 3, 50));
    }

    [Fact(DisplayName = "Sair da tarefa leva as dependências")]
    public void LeavingTheTask_TakesItsOwnDependencies()
    {
        var (schedule, phase) = NewSchedule();
        var icms = Add(schedule, phase, "Parametrizar regras de ICMS", D(10, 5), 4);
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 1);
        var script = Add(schedule, phase, "Roteiro de testes", D(10, 13), 3, tests, icms);

        Change(schedule, script, plan => plan with { ParentId = null });

        script.ParentId.Should().BeNull();
        Predecessors(schedule, script).Should().Equal("Parametrizar regras de ICMS");
        Predecessors(schedule, tests).Should().BeEmpty();
        (tests.Start, tests.End, tests.Duration).Should().Be((D(10, 13), D(10, 15), 3));
    }

    [Fact(DisplayName = "Excluir tarefa com subtarefas")]
    public void RemovingTheSummary_RemovesItsSubtasks_AndMovesNothing()
    {
        var (schedule, configuration) = NewSchedule();
        var approval = schedule.AddPhase("Homologação");
        var tests = Add(schedule, configuration, "Testes integrados", D(10, 13), 1);
        var script = Add(schedule, configuration, "Roteiro de testes", D(10, 13), 2, tests);
        Add(schedule, configuration, "Executar testes", D(10, 15), 3, tests, script);
        var clientTests = Add(schedule, approval, "Testes com a equipe do cliente", D(10, 20), 2, null, script);
        var before = (clientTests.Start, clientTests.End);

        schedule.RemoveItem(tests.Id, Days).Should().BeTrue();

        schedule.Items.Should().Equal(clientTests);
        schedule.Dependencies.Should().BeEmpty();
        (clientTests.Start, clientTests.End).Should().Be(before);
    }

    [Fact(DisplayName = "Excluir fase com subtarefas")]
    public void RemovingThePhase_RemovesTheSummaryAndItsSubtasks()
    {
        var (schedule, configuration) = NewSchedule();
        var approval = schedule.AddPhase("Homologação");
        var tests = Add(schedule, configuration, "Testes integrados", D(10, 13), 1);
        var script = Add(schedule, configuration, "Roteiro de testes", D(10, 13), 2, tests);
        var clientTests = Add(schedule, approval, "Testes com a equipe do cliente", D(10, 20), 2, null, script);

        schedule.RemovePhase(configuration.Id).Should().BeTrue();

        schedule.Items.Should().Equal(clientTests);
        schedule.Dependencies.Should().BeEmpty();
    }

    [Fact(DisplayName = "Progresso sem contar a tarefa-resumo")]
    public void PhaseAndProject_CountTheSubtasks_NotTheSummary()
    {
        var (schedule, phase) = NewSchedule();
        var task = Add(schedule, phase, "Parametrizar regras fiscais", D(10, 5), 1);
        var first = Add(schedule, phase, "Mapear", D(10, 5), 2, task);
        var second = Add(schedule, phase, "Configurar", D(10, 7), 8, task);
        schedule.UpdateProgress(first.Id, 100, Today);
        schedule.UpdateProgress(second.Id, 25, Today);

        var figures = schedule.Figures(Today);

        task.Progress.Should().Be(40);
        figures.Phases.Single().Progress.Should().Be(40);
        figures.Progress.Should().Be(40);
        figures.CompletedOn.Should().BeNull();
    }
}
