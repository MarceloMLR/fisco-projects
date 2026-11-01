using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: cronograma (domain side): "Linha de base", "Diferença da linha de base" and "Caminho crítico" (ADR-0021). The
// calendar of 2026 has 12/10 and 02/11 as holidays.
public sealed class BaselineAndCriticalPathTests
{
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;
    private static readonly DateOnly Today = D(10, 16);

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static (Schedule Schedule, SchedulePhase Phase) NewSchedule()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        return (schedule, schedule.AddPhase("Configuração"));
    }

    private static ScheduleItem Task(
        Schedule schedule, SchedulePhase phase, string name, DateOnly start, int duration, Guid? parent = null, params ScheduleItem[] after) =>
        schedule.AddItem(
            ScheduleItemKind.Task,
            new ScheduleItemPlan(phase.Id, name, start, duration, null, 0, [.. after.Select(item => item.Id)], parent),
            Days,
            Today);

    private static ScheduleItem Milestone(Schedule schedule, SchedulePhase phase, string name, DateOnly day, params ScheduleItem[] after) =>
        schedule.AddItem(
            ScheduleItemKind.Milestone,
            new ScheduleItemPlan(phase.Id, name, day, 0, null, 0, [.. after.Select(item => item.Id)]),
            Days,
            Today);

    private static void Move(Schedule schedule, ScheduleItem item, DateOnly start, int? duration = null) =>
        schedule.ChangeItem(
            item.Id,
            new ScheduleItemPlan(
                item.PhaseId, item.Name, start, duration ?? item.Duration, item.OwnerId, null,
                [.. schedule.PredecessorsOf(item.Id).Select(predecessor => predecessor.Id)], item.ParentId),
            Days,
            Today);

    private static IReadOnlySet<Guid> CriticalOf(Schedule schedule) =>
        ScheduleCriticalPath.Of(schedule.Items, schedule.Dependencies, Days);

    [Fact(DisplayName = "Cadeia mais longa é crítica")]
    public void LongestChain_IsCritical_AndAParallelTaskWithFloatIsNot()
    {
        var (schedule, phase) = NewSchedule();
        var a = Task(schedule, phase, "A", D(10, 5), 3);
        var b = Task(schedule, phase, "B", D(10, 5), 2, after: a);
        var m = Milestone(schedule, phase, "M", D(10, 5), b);
        var c = Task(schedule, phase, "C", D(10, 5), 2);
        // After the milestone, on the next business day: Tuesday 13/10, past the holiday of 12/10.
        var t = Task(schedule, phase, "T", D(10, 5), 1, after: m);
        // A completed task stays on the path by its dates.
        schedule.UpdateProgress(a.Id, 100, Today);

        var critical = CriticalOf(schedule);

        (a.End, b.Start, b.End, m.Start, t.Start).Should().Be((D(10, 7), D(10, 8), D(10, 9), D(10, 9), D(10, 13)));
        critical.Should().BeEquivalentTo([a.Id, b.Id, m.Id, t.Id]);
        critical.Should().NotContain(c.Id);
    }

    [Fact(DisplayName = "Folga em dias úteis")]
    public void TaskEndingBeforeAHoliday_HasOneBusinessDayOfFloat()
    {
        var (schedule, phase) = NewSchedule();
        var friday = Task(schedule, phase, "Termina na sexta", D(10, 7), 3);
        var tuesday = Task(schedule, phase, "Termina na terça", D(10, 13), 1);

        var critical = CriticalOf(schedule);

        // From Friday 09/10 to Tuesday 13/10 only 13/10 is a business day: one day of float.
        friday.End.Should().Be(D(10, 9));
        critical.Should().BeEquivalentTo([tuesday.Id]);
        var slack = ScheduleCriticalPath.Analyze(schedule.Items, schedule.Dependencies, Days).Slack;
        slack[friday.Id].Should().Be(new ScheduleSlack(1, D(10, 13)));
        slack[tuesday.Id].Should().Be(new ScheduleSlack(0, D(10, 13)));
    }

    [Fact(DisplayName = "Folga da tarefa")]
    public void ShorterParallelTask_HasTheBusinessDaysLeftAsFloat()
    {
        var (schedule, phase) = NewSchedule();
        var longer = Task(schedule, phase, "Parametrizar regras de ICMS", D(10, 13), 5);
        var shorter = Task(schedule, phase, "Integrar emissão de NF-e", D(10, 13), 2);
        var milestone = Milestone(schedule, phase, "Configuração aprovada", D(10, 13), longer, shorter);

        var analysis = ScheduleCriticalPath.Analyze(schedule.Items, schedule.Dependencies, Days);

        analysis.Critical.Should().BeEquivalentTo([longer.Id, milestone.Id]);
        (analysis.Slack[longer.Id].Days, analysis.Slack[milestone.Id].Days, analysis.Slack[shorter.Id].Days).Should().Be((0, 0, 3));
        analysis.Slack[shorter.Id].LatestEnd.Should().Be(longer.End);
    }

    [Fact(DisplayName = "Folga com os tipos")]
    public void TaskEndingWithTheLastOne_HasNoFloat()
    {
        var (schedule, phase) = NewSchedule();
        var longer = Task(schedule, phase, "Migrar saldos", D(10, 13), 5);
        var training = schedule.AddItem(
            ScheduleItemKind.Task,
            new ScheduleItemPlan(phase.Id, "Treinar usuários", D(10, 13), 2, null, 0, [longer.Id],
                Links: [new ScheduleLink(longer.Id, ScheduleLinkType.FinishToFinish)]),
            Days,
            Today);
        Milestone(schedule, phase, "Virada", D(10, 13), longer);

        var analysis = ScheduleCriticalPath.Analyze(schedule.Items, schedule.Dependencies, Days);

        training.End.Should().Be(longer.End);
        analysis.Slack[training.Id].Days.Should().Be(0);
        analysis.Critical.Should().Contain(training.Id);
    }

    [Fact(DisplayName = "Subtarefas no caminho crítico")]
    public void LongestSubtask_IsCritical_AndItsSummaryIsNot()
    {
        var (schedule, phase) = NewSchedule();
        var summary = Task(schedule, phase, "Testes integrados", D(10, 5), 5);
        var script = Task(schedule, phase, "Roteiro de testes", D(10, 5), 2, summary.Id);
        var run = Task(schedule, phase, "Executar testes", D(10, 5), 5, summary.Id);
        var other = Task(schedule, phase, "Configurar plano de contas", D(10, 5), 2);

        var critical = CriticalOf(schedule);

        summary.End.Should().Be(run.End);
        critical.Should().BeEquivalentTo([run.Id]);
        critical.Should().NotContain([summary.Id, script.Id, other.Id]);
        ScheduleCriticalPath.Of([], [], Days).Should().BeEmpty();
    }

    [Fact(DisplayName = "Salvar linha de base")]
    public void SavingTheBaseline_CopiesTheDatesOfTheItemsAndOfTheProject()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Parametrizar regras de ICMS", D(10, 13), 3);
        var milestone = Milestone(schedule, phase, "Aceite", D(10, 16), task);

        schedule.SaveBaseline(D(10, 16)).Should().BeTrue();

        var baseline = schedule.Baseline!;
        (baseline.SavedOn, baseline.Start, baseline.End).Should().Be((D(10, 16), D(10, 13), D(10, 16)));
        (baseline.ItemOf(task.Id)!.Start, baseline.ItemOf(task.Id)!.End).Should().Be((D(10, 13), D(10, 15)));
        (baseline.ItemOf(milestone.Id)!.Start, baseline.ItemOf(milestone.Id)!.End).Should().Be((D(10, 16), D(10, 16)));
        var comparison = baseline.CompareWith(schedule.Items, D(10, 16), Days);
        comparison.Deviation.Should().Be(0);
        comparison.Items.Values.Select(item => item.Deviation).Should().AllBeEquivalentTo(0);
    }

    [Fact(DisplayName = "Atualizar linha de base")]
    public void SavingAgain_ReplacesTheBaseline()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Parametrizar regras de ICMS", D(10, 13), 3);
        schedule.SaveBaseline(D(10, 16));
        var removed = Task(schedule, phase, "Sai antes", D(10, 13), 1);
        schedule.SaveBaseline(D(10, 16));
        schedule.RemoveItem(removed.Id, Days);

        Move(schedule, task, D(10, 19));
        schedule.SaveBaseline(D(10, 19)).Should().BeTrue();

        var baseline = schedule.Baseline!;
        (baseline.SavedOn, baseline.Start, baseline.End).Should().Be((D(10, 19), D(10, 19), D(10, 21)));
        baseline.Items.Should().ContainSingle().Which.ItemId.Should().Be(task.Id);
        (baseline.ItemOf(task.Id)!.Start, baseline.ItemOf(task.Id)!.End).Should().Be((D(10, 19), D(10, 21)));
    }

    [Fact(DisplayName = "Linha de base sem tarefas")]
    public void ScheduleWithoutItems_HasNoBaseline()
    {
        var (schedule, _) = NewSchedule();

        schedule.SaveBaseline(Today).Should().BeFalse();

        schedule.Baseline.Should().BeNull();
    }

    [Fact(DisplayName = "Tarefa depois da linha de base")]
    public void TaskEndingLater_IsBusinessDaysAfterTheBaseline()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Validar cadastro", D(10, 13), 3);
        schedule.SaveBaseline(Today);

        Move(schedule, task, D(10, 15));

        // From 15/10 to 19/10: 16/10 and 19/10, without the weekend.
        task.End.Should().Be(D(10, 19));
        schedule.Baseline!.CompareWith(schedule.Items, task.End, Days).Items[task.Id].Deviation.Should().Be(2);
    }

    [Fact(DisplayName = "Tarefa antes da linha de base")]
    public void TaskEndingEarlier_IsBusinessDaysBeforeTheBaseline()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Validar cadastro", D(10, 13), 2);
        schedule.SaveBaseline(Today);

        Move(schedule, task, D(10, 8));

        // From 09/10 to 14/10: 13/10 and 14/10, without the weekend and the holiday of 12/10.
        (task.End, schedule.Baseline!.ItemOf(task.Id)!.End).Should().Be((D(10, 9), D(10, 14)));
        schedule.Baseline.CompareWith(schedule.Items, task.End, Days).Items[task.Id].Deviation.Should().Be(-2);
    }

    [Fact(DisplayName = "Item incluído depois da linha de base")]
    public void ItemIncludedLater_HasNoBaseline_AndRemovingAnItemKeepsTheEndOfTheProject()
    {
        var (schedule, phase) = NewSchedule();
        var first = Task(schedule, phase, "Mapear processos", D(10, 5), 5);
        var last = Task(schedule, phase, "Treinar usuários", D(10, 19), 3);
        schedule.SaveBaseline(Today);

        var later = Task(schedule, phase, "Configurar impostos", D(10, 13), 2);
        schedule.RemoveItem(last.Id, Days);

        var comparison = schedule.Baseline!.CompareWith(schedule.Items, D(10, 14), Days);
        comparison.Items.Keys.Should().BeEquivalentTo([first.Id]);
        comparison.Items.Should().NotContainKey(later.Id);
        // The end of the project in the baseline stays 21/10, though its last task is gone.
        comparison.End.Should().Be(D(10, 21));
        comparison.Deviation.Should().Be(-5);
    }

    [Fact(DisplayName = "Fim do projeto contra a linha de base")]
    public void EndOfTheProject_IsComparedWithTheBaseline()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Acompanhamento assistido", D(10, 28), 3);
        schedule.SaveBaseline(Today);

        Move(schedule, task, D(10, 28), duration: 5);

        // From Friday 30/10 to Wednesday 04/11: 03/11 and 04/11, without the weekend and the holiday of 02/11.
        var figures = schedule.Figures(Today);
        (schedule.Baseline!.End, figures.End).Should().Be((D(10, 30), D(11, 4)));
        schedule.Baseline.CompareWith(schedule.Items, figures.End, Days).Deviation.Should().Be(2);
        schedule.Baseline.CompareWith([], null, Days).Deviation.Should().BeNull();
    }
}
