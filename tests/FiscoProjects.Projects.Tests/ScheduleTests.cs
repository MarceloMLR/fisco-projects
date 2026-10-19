using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: cronograma (domain side): "Fases do cronograma", "Tarefas e marcos", "Início em dia útil", "Dependências",
// "Dependências que empurram", "Excluir sem mover", "Progresso das tarefas", "Datas e progresso das fases e do
// projeto", "Tarefa atrasada", "Marco atingido", "Marco atrasado", "Novas datas pela API" and "Dias não úteis no
// cronograma".
public sealed class ScheduleTests
{
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;
    private static readonly DateOnly Today = D(10, 16);

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static ScheduleItemPlan Plan(
        SchedulePhase phase, string name, DateOnly start, int duration, int progress = 0, Guid? owner = null, params Guid[] after) =>
        new(phase.Id, name, start, duration, owner, progress, after);

    private static ScheduleItem Task(Schedule schedule, SchedulePhase phase, string name, DateOnly start, int duration, params ScheduleItem[] after) =>
        schedule.AddItem(ScheduleItemKind.Task, Plan(phase, name, start, duration, after: [.. after.Select(item => item.Id)]), Days, Today);

    private static ScheduleItem Milestone(Schedule schedule, SchedulePhase phase, string name, DateOnly day, params ScheduleItem[] after) =>
        schedule.AddItem(ScheduleItemKind.Milestone, Plan(phase, name, day, 0, after: [.. after.Select(item => item.Id)]), Days, Today);

    private static int? Change(Schedule schedule, ScheduleItem item, Func<ScheduleItemPlan, ScheduleItemPlan> change) =>
        schedule.ChangeItem(item.Id, change(PlanOf(schedule, item)), Days, Today);

    private static ScheduleItemPlan PlanOf(Schedule schedule, ScheduleItem item) => new(
        item.PhaseId, item.Name, item.Start, item.Duration, item.OwnerId, item.Progress,
        [.. schedule.PredecessorsOf(item.Id).Select(predecessor => predecessor.Id)]);

    private static (Schedule Schedule, SchedulePhase Phase) NewSchedule()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        return (schedule, schedule.AddPhase("Configuração"));
    }

    [Fact(DisplayName = "Criar e renomear fase")]
    public void Phase_IsCreated_AndRenamed()
    {
        var schedule = new Schedule(Guid.CreateVersion7());

        var phase = schedule.AddPhase("  Levantamento ");
        schedule.RenamePhase(phase.Id, "Diagnóstico").Should().BeTrue();

        phase.Name.Should().Be("Diagnóstico");
        phase.ProjectId.Should().Be(schedule.ProjectId);
        schedule.AddPhase("Configuração").Position.Should().Be(phase.Position + 1);
        var figures = schedule.Figures(Today).Phases.Single(p => p.Phase == phase);
        figures.Start.Should().BeNull();
        figures.End.Should().BeNull();
        figures.Items.Should().BeEmpty();
        schedule.RenamePhase(Guid.CreateVersion7(), "Outra").Should().BeFalse();
    }

    [Theory(DisplayName = "Fase sem nome")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData(151)]
    public void Phase_WithoutAValidName_IsRefused(object? name)
    {
        var text = name is int length ? new string('a', length) : (string?)name;

        Schedule.ValidPhaseName(text).Should().BeNull();
        Schedule.ValidPhaseName(new string('a', 150)).Should().HaveLength(150);
        var add = () => new Schedule(Guid.CreateVersion7()).AddPhase(text!);
        add.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Excluir fase com tarefas")]
    public void RemovingAPhase_RemovesItsItems_AndKeepsTheOthers()
    {
        var (schedule, configuration) = NewSchedule();
        var survey = schedule.AddPhase("Levantamento");
        var first = Task(schedule, survey, "Mapear processos", D(10, 5), 5);
        Task(schedule, survey, "Levantar cadastros", D(10, 6), 2);
        var next = Task(schedule, configuration, "Parametrizar ICMS", D(10, 13), 3, first);

        schedule.RemovePhase(survey.Id).Should().BeTrue();

        schedule.Phases.Should().ContainSingle().Which.Should().Be(configuration);
        schedule.Items.Should().ContainSingle().Which.Should().Be(next);
        schedule.Dependencies.Should().BeEmpty();
        (next.Start, next.End).Should().Be((D(10, 13), D(10, 15)));
    }

    [Fact(DisplayName = "Criar tarefa")]
    public void Task_EndsAfterItsBusinessDays()
    {
        var (schedule, phase) = NewSchedule();

        var task = Task(schedule, phase, " Levantar cadastros ", D(10, 8), 3);

        task.Name.Should().Be("Levantar cadastros");
        task.Kind.Should().Be(ScheduleItemKind.Task);
        (task.Start, task.Duration, task.End).Should().Be((D(10, 8), 3, D(10, 13)));
        task.Progress.Should().Be(0);
        task.CompletedOn.Should().BeNull();
    }

    [Fact(DisplayName = "Marco sem duração")]
    public void Milestone_IsOneDay()
    {
        var (schedule, phase) = NewSchedule();

        var milestone = Milestone(schedule, phase, "Aceite da homologação", D(11, 3));

        (milestone.Start, milestone.End, milestone.Duration).Should().Be((D(11, 3), D(11, 3), 0));
        milestone.Kind.Should().Be(ScheduleItemKind.Milestone);
    }

    [Fact(DisplayName = "Tarefa inválida")]
    public void InvalidPlans_AreRefused()
    {
        var (schedule, phase) = NewSchedule();
        var other = new Schedule(Guid.CreateVersion7()).AddPhase("Outra");

        var withoutName = () => schedule.AddItem(ScheduleItemKind.Task, Plan(phase, " ", D(10, 5), 1), Days, Today);
        var withoutDuration = () => schedule.AddItem(ScheduleItemKind.Task, Plan(phase, "Tarefa", D(10, 5), 0), Days, Today);
        var otherPhase = () => schedule.AddItem(ScheduleItemKind.Task, Plan(other, "Tarefa", D(10, 5), 1), Days, Today);
        var milestoneWithOwner = () => schedule.AddItem(
            ScheduleItemKind.Milestone, Plan(phase, "Marco", D(10, 5), 0, owner: Guid.CreateVersion7()), Days, Today);
        var tooLong = () => schedule.AddItem(ScheduleItemKind.Task, Plan(phase, "Tarefa", D(10, 5), 1000), Days, Today);
        var outOfRange = () => schedule.AddItem(ScheduleItemKind.Task, Plan(phase, "Tarefa", new DateOnly(2100, 1, 4), 1), Days, Today);

        withoutName.Should().Throw<ArgumentException>();
        withoutDuration.Should().Throw<ArgumentException>();
        otherPhase.Should().Throw<ArgumentException>();
        milestoneWithOwner.Should().Throw<ArgumentException>();
        tooLong.Should().Throw<ArgumentException>();
        outOfRange.Should().Throw<ArgumentException>();
        schedule.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "Início num sábado")]
    public void Start_OnADayOff_GoesToTheNextBusinessDay()
    {
        var (schedule, phase) = NewSchedule();

        var task = Task(schedule, phase, "Configurar plano de contas", D(10, 10), 2);
        var milestone = Milestone(schedule, phase, "Virada", D(11, 1));

        (task.Start, task.End).Should().Be((D(10, 13), D(10, 14)));
        milestone.Start.Should().Be(D(11, 3));
    }

    [Fact(DisplayName = "Tarefa depois da anterior")]
    public void Item_StartsAfterTheItemsBeforeIt()
    {
        var (schedule, phase) = NewSchedule();
        var before = Task(schedule, phase, "Mapear processos", D(10, 5), 5);
        var task = Task(schedule, phase, "Validar cadastro", D(10, 8), 2);

        var pushed = Change(schedule, task, plan => plan with { PredecessorIds = [before.Id] });
        var milestone = Milestone(schedule, phase, "Processos mapeados", D(10, 5), before);

        pushed.Should().Be(0);
        (before.End, task.Start, task.End).Should().Be((D(10, 9), D(10, 13), D(10, 14)));
        milestone.Start.Should().Be(D(10, 9));
        schedule.PredecessorsOf(task.Id).Should().ContainSingle().Which.Should().Be(before);
    }

    [Fact(DisplayName = "Dependência circular")]
    public void Cycle_IsRefused()
    {
        var (schedule, phase) = NewSchedule();
        var a = Task(schedule, phase, "A", D(10, 5), 1);
        var b = Task(schedule, phase, "B", D(10, 6), 1, a);
        var c = Task(schedule, phase, "C", D(10, 7), 1, b);

        var cycle = () => Change(schedule, a, plan => plan with { PredecessorIds = [c.Id] });
        var itself = () => Change(schedule, a, plan => plan with { PredecessorIds = [a.Id] });

        cycle.Should().Throw<ScheduleCycleException>();
        itself.Should().Throw<ArgumentException>();
        schedule.Dependencies.Select(d => (d.PredecessorId, d.SuccessorId)).Should().BeEquivalentTo([(a.Id, b.Id), (b.Id, c.Id)]);
        (a.Start, a.End).Should().Be((D(10, 5), D(10, 5)));
    }

    [Fact(DisplayName = "Atrasar empurra a cadeia")]
    public void LaterEnd_PushesTheChain()
    {
        var (schedule, phase) = NewSchedule();
        var a = Task(schedule, phase, "A", D(10, 5), 5);
        var b = Task(schedule, phase, "B", D(10, 13), 2, a);
        var c = Task(schedule, phase, "C", D(10, 15), 1, b);
        var unrelated = Task(schedule, phase, "Outra", D(10, 13), 1);

        var pushed = Change(schedule, a, plan => plan with { Duration = 6 });

        pushed.Should().Be(2);
        a.End.Should().Be(D(10, 13));
        (b.Start, b.End).Should().Be((D(10, 14), D(10, 15)));
        (c.Start, c.End).Should().Be((D(10, 16), D(10, 16)));
        unrelated.Start.Should().Be(D(10, 13));
    }

    [Fact(DisplayName = "Empurrar por dois caminhos")]
    public void Push_ThroughTwoPaths_MovesEachItemOnce()
    {
        var (schedule, phase) = NewSchedule();
        var a = Task(schedule, phase, "A", D(10, 5), 1);
        var b = Task(schedule, phase, "B", D(10, 6), 1, a);
        var c = Task(schedule, phase, "C", D(10, 6), 3, a);
        var d = Task(schedule, phase, "D", D(10, 9), 1, b, c);
        var end = Milestone(schedule, phase, "Fim", D(10, 9), d);

        var pushed = Change(schedule, a, plan => plan with { Duration = 2 });

        pushed.Should().Be(4);
        (b.Start, c.Start, c.End).Should().Be((D(10, 7), D(10, 7), D(10, 9)));
        (d.Start, end.Start).Should().Be((D(10, 13), D(10, 13)));
    }

    [Fact(DisplayName = "Antecipar não puxa")]
    public void EarlierStart_PullsNobody()
    {
        var (schedule, phase) = NewSchedule();
        var a = Task(schedule, phase, "A", D(10, 7), 2);
        var b = Task(schedule, phase, "B", D(10, 13), 2, a);

        var pushed = Change(schedule, a, plan => plan with { Start = D(10, 5) });

        pushed.Should().Be(0);
        (a.Start, a.End).Should().Be((D(10, 5), D(10, 6)));
        (b.Start, b.End).Should().Be((D(10, 13), D(10, 14)));
    }

    [Fact(DisplayName = "Excluir a tarefa anterior")]
    public void RemovingAnItem_RemovesItsDependencies_AndMovesNothing()
    {
        var (schedule, phase) = NewSchedule();
        var a = Task(schedule, phase, "A", D(10, 5), 5);
        var b = Task(schedule, phase, "B", D(10, 13), 2, a);

        schedule.RemoveItem(a.Id, Days).Should().BeTrue();
        var noDependency = Change(schedule, b, plan => plan with { PredecessorIds = [] });

        schedule.Items.Should().ContainSingle().Which.Should().Be(b);
        schedule.Dependencies.Should().BeEmpty();
        noDependency.Should().Be(0);
        (b.Start, b.End).Should().Be((D(10, 13), D(10, 14)));
        schedule.RemoveItem(a.Id, Days).Should().BeFalse();
    }

    [Fact(DisplayName = "Concluir tarefa guarda a data")]
    public void CompletedTask_KeepsTheDay()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Mapear processos", D(10, 5), 5);

        schedule.ChangeItem(task.Id, PlanOf(schedule, task) with { Progress = 100 }, Days, D(10, 16));
        schedule.ChangeItem(task.Id, PlanOf(schedule, task) with { Progress = 100 }, Days, D(10, 19));

        task.Progress.Should().Be(100);
        task.CompletedOn.Should().Be(D(10, 16));
    }

    [Fact(DisplayName = "Voltar abaixo de 100%")]
    public void TaskBelow100_HasNoCompletionDay()
    {
        var (schedule, phase) = NewSchedule();
        var task = schedule.AddItem(ScheduleItemKind.Task, Plan(phase, "Mapear processos", D(10, 5), 5, progress: 100), Days, D(10, 9));

        Change(schedule, task, plan => plan with { Progress = 80 });

        task.CompletedOn.Should().BeNull();
        task.Progress.Should().Be(80);
    }

    [Theory(DisplayName = "Progresso fora de 0 a 100%")]
    [InlineData(ScheduleItemKind.Task, -1, false)]
    [InlineData(ScheduleItemKind.Task, 101, false)]
    [InlineData(ScheduleItemKind.Task, 0, true)]
    [InlineData(ScheduleItemKind.Task, 100, true)]
    [InlineData(ScheduleItemKind.Milestone, 50, false)]
    [InlineData(ScheduleItemKind.Milestone, 0, true)]
    public void Progress_GoesFrom0To100(ScheduleItemKind kind, int progress, bool valid) =>
        Schedule.IsValidProgress(kind, progress).Should().Be(valid);

    [Fact(DisplayName = "Progresso ponderado pela duração")]
    public void Progress_IsWeightedByTheDuration()
    {
        var (schedule, phase) = NewSchedule();
        var other = schedule.AddPhase("Homologação");
        var shortTask = Task(schedule, phase, "Curta", D(10, 5), 2);
        var longTask = Task(schedule, phase, "Longa", D(10, 7), 8);
        Change(schedule, shortTask, plan => plan with { Progress = 100 });
        Change(schedule, longTask, plan => plan with { Progress = 25 });
        var milestone = Milestone(schedule, phase, "Aceite", D(10, 20), longTask);
        Task(schedule, other, "Testes", D(10, 21), 10);

        var figures = schedule.Figures(Today);

        var phaseFigures = figures.Phases[0];
        phaseFigures.Phase.Should().Be(phase);
        phaseFigures.Progress.Should().Be(40);
        (phaseFigures.Start, phaseFigures.End).Should().Be((D(10, 5), milestone.End));
        phaseFigures.Items.Should().Equal(shortTask, longTask, milestone);
        // (2 × 100 + 8 × 25 + 10 × 0) / 20 = 20%.
        figures.Progress.Should().Be(20);
        (figures.Start, figures.End).Should().Be((D(10, 5), D(11, 4)));
        figures.Milestones.Should().Equal(D(10, 20));
    }

    [Fact(DisplayName = "Progresso arredondado")]
    public void Progress_IsRoundedToTheNearestWholeNumber()
    {
        var (schedule, phase) = NewSchedule();
        var a = Task(schedule, phase, "A", D(10, 5), 1);
        var b = Task(schedule, phase, "B", D(10, 5), 1);
        var c = Task(schedule, phase, "C", D(10, 5), 1);
        Change(schedule, a, plan => plan with { Progress = 100 });
        Change(schedule, b, plan => plan with { Progress = 1 });

        ScheduleFigures.WeightedProgress([a, b, c]).Should().Be(34);
        ScheduleFigures.WeightedProgress([b, c]).Should().Be(1);
        ScheduleFigures.WeightedProgress([]).Should().Be(0);
    }

    [Fact(DisplayName = "Tarefa vencida")]
    public void OverdueTask_IsLate()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Validar cadastro", D(10, 13), 3);
        Change(schedule, task, plan => plan with { Progress = 80 });

        var figures = schedule.Figures(D(10, 16));

        task.End.Should().Be(D(10, 15));
        task.IsLate(D(10, 16)).Should().BeTrue();
        figures.Phases.Single().IsLate.Should().BeTrue();
        figures.LateTasks.Should().Be(1);
    }

    [Fact(DisplayName = "Tarefa concluída ou do dia não fica atrasada")]
    public void CompletedTask_AndTaskEndingToday_AreNotLate()
    {
        var (schedule, phase) = NewSchedule();
        var done = Task(schedule, phase, "Concluída", D(10, 13), 3);
        Change(schedule, done, plan => plan with { Progress = 100 });
        var today = Task(schedule, phase, "Do dia", D(10, 14), 3);
        Change(schedule, today, plan => plan with { Progress = 50 });

        var figures = schedule.Figures(D(10, 16));

        today.End.Should().Be(D(10, 16));
        done.IsLate(D(10, 16)).Should().BeFalse();
        today.IsLate(D(10, 16)).Should().BeFalse();
        figures.LateTasks.Should().Be(0);
        figures.Phases.Single().IsLate.Should().BeFalse();
    }

    [Fact(DisplayName = "Alterar a tarefa mantém o progresso")]
    public void ChangingTheTask_WithoutTheProgress_KeepsIt()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Mapear processos", D(10, 5), 5);

        schedule.UpdateProgress(task.Id, 60, Today).Should().BeTrue();
        Change(schedule, task, plan => plan with { Name = "Mapear os processos", Progress = null });

        task.Name.Should().Be("Mapear os processos");
        task.Progress.Should().Be(60);
        task.CompletedOn.Should().BeNull();
    }

    [Fact(DisplayName = "Atualizar o progresso no domínio")]
    public void UpdateProgress_KeepsTheCompletionDay_AndOnlyFitsATask()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Mapear processos", D(10, 5), 5);
        var milestone = Milestone(schedule, phase, "Aceite", D(10, 20));

        schedule.UpdateProgress(task.Id, 100, D(10, 16)).Should().BeTrue();
        schedule.UpdateProgress(task.Id, 100, D(10, 19));
        task.CompletedOn.Should().Be(D(10, 16));
        schedule.UpdateProgress(task.Id, 30, D(10, 19));

        (task.Progress, task.CompletedOn).Should().Be((30, (DateOnly?)null));
        schedule.UpdateProgress(Guid.CreateVersion7(), 50, Today).Should().BeFalse();
        schedule.Invoking(s => s.UpdateProgress(milestone.Id, 0, Today)).Should().Throw<ArgumentException>();
        schedule.Invoking(s => s.UpdateProgress(task.Id, 101, Today)).Should().Throw<ArgumentException>();
        schedule.Invoking(s => s.UpdateProgress(task.Id, -1, Today)).Should().Throw<ArgumentException>();
        schedule.Invoking(s => s.SetReached(task.Id, true, Today)).Should().Throw<ArgumentException>();
        task.ReachedOn.Should().BeNull();
    }

    [Fact(DisplayName = "Marcar marco como atingido")]
    public void Milestone_IsReached_AndKeepsTheDay()
    {
        var (schedule, phase) = NewSchedule();
        var milestone = Milestone(schedule, phase, "Aceite da homologação", D(10, 20));

        schedule.SetReached(milestone.Id, true, D(10, 16)).Should().BeTrue();
        schedule.SetReached(milestone.Id, true, D(10, 19));
        Change(schedule, milestone, plan => plan with { Start = D(10, 27) });

        milestone.ReachedOn.Should().Be(D(10, 16));
        milestone.Start.Should().Be(D(10, 27));
        milestone.IsLate(D(10, 28)).Should().BeFalse();
        schedule.SetReached(Guid.CreateVersion7(), true, Today).Should().BeFalse();
    }

    [Fact(DisplayName = "Desmarcar marco")]
    public void Milestone_IsUnmarked_WithoutTheDay()
    {
        var (schedule, phase) = NewSchedule();
        var milestone = Milestone(schedule, phase, "Aceite da homologação", D(10, 20));
        schedule.SetReached(milestone.Id, true, D(10, 16));

        schedule.SetReached(milestone.Id, false, D(10, 19)).Should().BeTrue();

        milestone.ReachedOn.Should().BeNull();
    }

    [Fact(DisplayName = "Marco vencido")]
    public void OverdueMilestone_IsLate()
    {
        var (schedule, phase) = NewSchedule();
        var milestone = Milestone(schedule, phase, "Aceite da configuração", D(10, 15));
        Task(schedule, phase, "Parametrizar", D(10, 19), 3);

        var figures = schedule.Figures(D(10, 16));

        milestone.IsLate(D(10, 16)).Should().BeTrue();
        figures.Phases.Single().IsLate.Should().BeTrue();
        figures.LateMilestones.Should().Be(1);
        figures.LateTasks.Should().Be(0);
        figures.CurrentPhase.Should().Be(phase);
    }

    [Fact(DisplayName = "Marco atingido ou do dia não fica atrasado")]
    public void ReachedMilestone_AndMilestoneOfToday_AreNotLate()
    {
        var (schedule, phase) = NewSchedule();
        var reached = Milestone(schedule, phase, "Aceite do levantamento", D(10, 15));
        schedule.SetReached(reached.Id, true, D(10, 15));
        var ofToday = Milestone(schedule, phase, "Aceite da configuração", D(10, 16));

        var figures = schedule.Figures(D(10, 16));

        reached.IsLate(D(10, 16)).Should().BeFalse();
        ofToday.IsLate(D(10, 16)).Should().BeFalse();
        figures.LateMilestones.Should().Be(0);
        figures.Phases.Single().IsLate.Should().BeFalse();
    }

    [Fact(DisplayName = "Fase atual com marco atrasado")]
    public void CurrentPhase_ConsidersTheLateMilestones()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var survey = schedule.AddPhase("Levantamento");
        var configuration = schedule.AddPhase("Configuração");
        var surveyTask = Task(schedule, survey, "Mapear", D(10, 5), 5);
        Change(schedule, surveyTask, plan => plan with { Progress = 100 });
        var acceptance = Milestone(schedule, survey, "Aceite do levantamento", D(10, 9), surveyTask);
        Task(schedule, configuration, "Parametrizar", D(10, 13), 8);

        schedule.Figures(D(10, 16)).CurrentPhase.Should().Be(survey, "the milestone of Levantamento passed without being reached");
        schedule.SetReached(acceptance.Id, true, D(10, 16));
        schedule.Figures(D(10, 16)).CurrentPhase.Should().Be(configuration);
    }

    [Fact(DisplayName = "Mudar o início pela API")]
    public void Reschedule_WithTheStart_KeepsTheDuration_AndPushes()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Parametrizar ICMS", D(10, 13), 3);
        var next = Task(schedule, phase, "Testes", D(10, 16), 2, task);
        Change(schedule, task, plan => plan with { Progress = 40 });

        var pushed = schedule.Reschedule(task.Id, D(10, 16), end: null, Days, Today);

        (task.Start, task.End, task.Duration, task.Progress).Should().Be((D(10, 16), D(10, 20), 3, 40));
        pushed.Should().Be(1);
        next.Start.Should().Be(D(10, 21));
        schedule.Reschedule(Guid.CreateVersion7(), D(10, 16), null, Days, Today).Should().BeNull();
    }

    [Fact(DisplayName = "Mudar o fim pela API")]
    public void Reschedule_WithTheEnd_CountsTheBusinessDays()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Parametrizar ICMS", D(10, 13), 3);
        var milestone = Milestone(schedule, phase, "Aceite", D(10, 20));

        schedule.Reschedule(task.Id, D(10, 13), D(10, 20), Days, Today);
        schedule.Reschedule(milestone.Id, D(10, 23), null, Days, Today);

        (task.Start, task.End, task.Duration).Should().Be((D(10, 13), D(10, 20), 6));
        (milestone.Start, milestone.End).Should().Be((D(10, 23), D(10, 23)));
        // From Saturday 10/10, moved to the next business day: Tuesday 13/10, after the holiday of 12/10.
        Schedule.DurationBetween(D(10, 10), D(10, 13), Days).Should().Be(1);
    }

    [Fact(DisplayName = "Fim antes do início")]
    public void Reschedule_RefusesAnEndBeforeTheStart_OrForAMilestone()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Parametrizar ICMS", D(10, 13), 3);
        var milestone = Milestone(schedule, phase, "Aceite", D(10, 20));

        Schedule.DurationBetween(D(10, 14), D(10, 13), Days).Should().BeNull();
        Schedule.DurationBetween(D(10, 13), D(10, 13).AddYears(5), Days).Should().BeNull("it would pass 999 business days");
        schedule.Invoking(s => s.Reschedule(task.Id, D(10, 14), D(10, 13), Days, Today)).Should().Throw<ArgumentException>();
        schedule.Invoking(s => s.Reschedule(milestone.Id, D(10, 20), D(10, 21), Days, Today)).Should().Throw<ArgumentException>();
        (task.Start, task.End).Should().Be((D(10, 13), D(10, 15)));
    }

    [Fact(DisplayName = "Dias não úteis no cronograma")]
    public void NonWorkingDays_GoFromTheMonthBefore_ToTheMonthAfter()
    {
        var days = Schedule.NonWorkingDaysAround(D(10, 5), D(11, 13), Days);

        days.Should().Contain([D(10, 12), D(11, 2), D(10, 10), D(10, 11), new DateOnly(2026, 9, 5), new DateOnly(2026, 12, 27)]);
        days.Should().OnlyContain(day => !Days.IsBusinessDay(day));
        (days[0], days[^1]).Should().Be((new DateOnly(2026, 9, 5), new DateOnly(2026, 12, 27)));
        // September to December 2026: 34 days of weekends, and the holidays of 12/10 and 02/11.
        days.Should().HaveCount(36);
    }

    [Fact(DisplayName = "Fase atual")]
    public void CurrentPhase_IsTheFirstLate_OrTheLastStarted()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var survey = schedule.AddPhase("Levantamento");
        var configuration = schedule.AddPhase("Configuração");
        var production = schedule.AddPhase("Entrada em produção");
        schedule.AddPhase("Sem tarefas");
        var surveyTask = Task(schedule, survey, "Mapear", D(10, 5), 5);
        Change(schedule, surveyTask, plan => plan with { Progress = 100 });
        var configurationTask = Task(schedule, configuration, "Parametrizar", D(10, 13), 8);
        Task(schedule, production, "Migrar", D(11, 4), 2);

        schedule.Figures(D(10, 2)).CurrentPhase.Should().BeNull();
        schedule.Figures(D(10, 7)).CurrentPhase.Should().Be(survey);
        schedule.Figures(D(10, 16)).CurrentPhase.Should().Be(configuration);
        schedule.Figures(D(11, 5)).CurrentPhase.Should().Be(configuration, "the task of Configuração ended on 22/10 below 100%");
        Change(schedule, configurationTask, plan => plan with { Progress = 100 });
        schedule.Figures(D(11, 5)).CurrentPhase.Should().Be(production);
        schedule.Figures(D(11, 5)).Phases.Select(p => p.Phase.Name)
            .Should().Equal("Levantamento", "Configuração", "Entrada em produção", "Sem tarefas");
    }

    [Fact(DisplayName = "Cronograma concluído")]
    public void CompletedSchedule_SaysWhen()
    {
        var (schedule, phase) = NewSchedule();
        var a = Task(schedule, phase, "A", D(10, 5), 2);
        var b = Task(schedule, phase, "B", D(10, 7), 2);
        schedule.ChangeItem(a.Id, PlanOf(schedule, a) with { Progress = 100 }, Days, D(10, 6));

        schedule.Figures(Today).CompletedOn.Should().BeNull();
        schedule.ChangeItem(b.Id, PlanOf(schedule, b) with { Progress = 100 }, Days, D(10, 8));

        var figures = schedule.Figures(Today);
        figures.CompletedOn.Should().Be(D(10, 8));
        figures.Progress.Should().Be(100);
        new Schedule(Guid.CreateVersion7()).Figures(Today).Should().Match<ScheduleFigures>(empty =>
            empty.Start == null && empty.End == null && empty.Progress == 0 && empty.CompletedOn == null);
    }

    [Fact(DisplayName = "Tarefas de quem sai ficam sem responsável")]
    public void Unassign_ClearsTheOwner_AndKeepsTheRest()
    {
        var (schedule, phase) = NewSchedule();
        var diego = Guid.CreateVersion7();
        var carla = Guid.CreateVersion7();
        var first = schedule.AddItem(ScheduleItemKind.Task, Plan(phase, "A", D(10, 5), 2, 50, diego), Days, Today);
        var second = schedule.AddItem(ScheduleItemKind.Task, Plan(phase, "B", D(10, 7), 2, 0, diego), Days, Today);
        var other = schedule.AddItem(ScheduleItemKind.Task, Plan(phase, "C", D(10, 7), 2, 0, carla), Days, Today);

        schedule.Unassign(diego).Should().Be(2);

        first.OwnerId.Should().BeNull();
        second.OwnerId.Should().BeNull();
        other.OwnerId.Should().Be(carla);
        (first.Start, first.End, first.Progress).Should().Be((D(10, 5), D(10, 6), 50));
    }

    [Fact(DisplayName = "Itens da fase em ordem")]
    public void Items_ComeByStart_ThenEnd_ThenTasksBeforeMilestones()
    {
        var (schedule, phase) = NewSchedule();
        var milestone = Milestone(schedule, phase, "Aceite", D(10, 13));
        var longer = Task(schedule, phase, "Longa", D(10, 13), 3);
        var shorter = Task(schedule, phase, "Curta", D(10, 13), 1);
        var first = Task(schedule, phase, "Primeira", D(10, 5), 1);

        schedule.Figures(Today).Phases.Single().Items.Should().Equal(first, shorter, milestone, longer);
    }
}
