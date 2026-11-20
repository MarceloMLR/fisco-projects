using GerenWare.Projects.Domain;

namespace GerenWare.Projects.Tests;

// Spec: cronograma, requirements "Tarefa atrasada para a projeção", "Projeção do atraso" and "O que a tarefa atrasada
// empurra" (ADR-0164). The calendar of 2026 has 12/10 and 02/11 as holidays, on Mondays; 19/10/2026 is a Monday.
public sealed class ScheduleProjectionTests
{
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;
    private static readonly DateOnly Today = D(10, 19);

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static (Schedule Schedule, SchedulePhase Phase) Empty()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        return (schedule, schedule.AddPhase("Homologação"));
    }

    private static ScheduleItem Add(
        Schedule schedule, SchedulePhase phase, string name, DateOnly start, int duration, int progress = 0, params ScheduleLink[] links) =>
        schedule.AddItem(
            duration == 0 ? ScheduleItemKind.Milestone : ScheduleItemKind.Task,
            new ScheduleItemPlan(
                phase.Id, name, start, duration, null, duration == 0 ? 0 : progress, [.. links.Select(link => link.PredecessorId)], Links: links),
            Days,
            Today);

    private static ScheduleLink After(ScheduleItem before, ScheduleLinkType type = ScheduleLinkType.FinishToStart) => new(before.Id, type, 0);

    private static ScheduleProjection Projection(Schedule schedule, ScheduleItem task, DateOnly? today = null) =>
        ScheduleProjections.Of(schedule, task.Id, today ?? Today, Days) ?? throw new InvalidOperationException("No projection.");

    [Fact(DisplayName = "Tarefa vencida tem projeção")]
    public void TaskPastItsEnd_HasAProjection()
    {
        var (schedule, phase) = Empty();
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 3, 40);

        ScheduleProjections.IsBehind(schedule, tests, Today).Should().BeTrue();
        ScheduleProjections.Of(schedule, tests.Id, Today, Days).Should().NotBeNull();
    }

    [Fact(DisplayName = "Tarefa que devia ter começado tem projeção")]
    public void TaskPastItsStart_AtZero_HasAProjection_ButIsNotCountedLate()
    {
        var (schedule, phase) = Empty();
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 9);

        tests.End.Should().Be(D(10, 23));
        ScheduleProjections.AllOf(schedule, Today, Days).Keys.Should().Equal(tests.Id);
        schedule.Figures(Today).LateTasks.Should().Be(0, "the rest of the screens count the delay by the end (ADR-0020)");
    }

    [Fact(DisplayName = "Tarefa em dia não tem projeção")]
    public void TaskOnTime_StartedOrStartingToday_HasNoProjection()
    {
        var (schedule, phase) = Empty();
        Add(schedule, phase, "Testes integrados", D(10, 13), 9, 30);
        Add(schedule, phase, "Treinar usuários", Today, 3);
        var done = Add(schedule, phase, "Levantar regras", D(10, 5), 3, 100);
        var milestone = Add(schedule, phase, "Aceite da homologação", D(10, 14), 0);

        ScheduleProjections.AllOf(schedule, Today, Days).Should().BeEmpty();
        ScheduleProjections.IsBehind(schedule, done, Today).Should().BeFalse();
        ScheduleProjections.IsBehind(schedule, milestone, Today).Should().BeFalse("a milestone has no projection");
    }

    [Fact(DisplayName = "Tarefa-resumo não tem projeção")]
    public void SummaryTask_HasNoProjection_ItsSubtasksDo()
    {
        var (schedule, phase) = Empty();
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 3, 40);
        var script = schedule.AddItem(
            ScheduleItemKind.Task, new ScheduleItemPlan(phase.Id, "Roteiro de testes", D(10, 13), 3, null, 40, [], tests.Id), Days, Today);

        ScheduleProjections.AllOf(schedule, Today, Days).Keys.Should().Equal(script.Id);
    }

    [Fact(DisplayName = "Tarefa atrasada com progresso")]
    public void LateTask_WithProgress_EndsWhatIsLeft_FromToday()
    {
        var (schedule, phase) = Empty();
        // 13/10 to 15/10, at 40%: 1.8 business days left, rounded up to 2.
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 3, 40);

        var projection = Projection(schedule, tests);

        (projection.Start, projection.End, projection.Days).Should().Be((D(10, 13), D(10, 20), 3));
        tests.End.Should().Be(D(10, 15), "nothing is saved");
    }

    [Fact(DisplayName = "Tarefa atrasada com quase tudo feito")]
    public void LateTask_AlmostDone_TakesAtLeastOneDay()
    {
        var (schedule, phase) = Empty();
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 3, 99);

        Projection(schedule, tests).End.Should().Be(Today);
    }

    [Fact(DisplayName = "Tarefa que devia ter começado")]
    public void TaskNotStarted_StartsToday_WithItsWholeDuration()
    {
        var (schedule, phase) = Empty();
        // 13/10 to 19/10, five business days, at 0%.
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 5);

        var projection = Projection(schedule, tests);

        (projection.Start, projection.End, projection.Days).Should().Be((D(10, 19), D(10, 23), 4));
    }

    [Fact(DisplayName = "Hoje não é dia útil")]
    public void OnADayOff_TheProjectionStartsOnTheNextBusinessDay()
    {
        var (schedule, phase) = Empty();
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 3, 40);

        var projection = Projection(schedule, tests, D(10, 17));

        projection.End.Should().Be(D(10, 20));
    }

    [Fact(DisplayName = "Marco empurrado pelas dependências")]
    public void MilestonesAfterTheTask_MoveByTheDependencies_ByTheirDay()
    {
        var (schedule, phase) = Empty();
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 5);
        var acceptance = Add(schedule, phase, "Aceite da homologação", D(10, 21), 0, links: After(tests));
        var goLive = Add(schedule, phase, "Go-live", D(10, 22), 0, links: After(acceptance));

        var projection = Projection(schedule, tests);

        projection.Milestones.Should().Equal(
            new ProjectedMilestone(acceptance.Id, "Aceite da homologação", D(10, 21), D(10, 23), 2),
            new ProjectedMilestone(goLive.Id, "Go-live", D(10, 22), D(10, 23), 1));
        (acceptance.Start, goLive.Start).Should().Be((D(10, 21), D(10, 22)), "nothing is saved");
    }

    [Fact(DisplayName = "Dependência que absorve o atraso pela folga")]
    public void Dependency_WithFloat_AbsorbsTheDelay()
    {
        var (schedule, phase) = Empty();
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 5);
        Add(schedule, phase, "Aceite da homologação", D(10, 28), 0, links: After(tests));

        var projection = Projection(schedule, tests);

        projection.End.Should().Be(D(10, 23));
        projection.Milestones.Should().BeEmpty();
        projection.ProjectEnd.Should().BeNull();
    }

    [Fact(DisplayName = "Fim do projeto que muda")]
    public void LastTask_MovesTheEndOfTheProject()
    {
        var (schedule, phase) = Empty();
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 5);
        Add(schedule, phase, "Levantar regras", D(10, 5), 3, 100);

        Projection(schedule, tests).ProjectEnd.Should().Be(new ProjectEndProjection(D(10, 19), D(10, 23), 4));
    }

    [Fact(DisplayName = "Fim do projeto pelo que vem depois")]
    public void ItemsAfterTheTask_MoveTheEndOfTheProject_ThroughTasks()
    {
        var (schedule, phase) = Empty();
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 5);
        var migration = Add(schedule, phase, "Migrar saldos", D(10, 20), 2, links: After(tests));
        var goLive = Add(schedule, phase, "Go-live", D(10, 21), 0, links: After(migration));

        var projection = Projection(schedule, tests);

        // Migrar saldos goes to 26/10 and 27/10, and the go-live to 27/10.
        projection.Milestones.Should().ContainSingle().Which.Should().Be(new ProjectedMilestone(goLive.Id, "Go-live", D(10, 21), D(10, 27), 4));
        projection.ProjectEnd.Should().Be(new ProjectEndProjection(D(10, 21), D(10, 27), 4));
    }

    [Fact(DisplayName = "Item concluído não anda")]
    public void DoneItems_AfterTheTask_StayWhereTheyAre()
    {
        var (schedule, phase) = Empty();
        var tests = Add(schedule, phase, "Testes integrados", D(10, 13), 5);
        var acceptance = Add(schedule, phase, "Aceite da homologação", D(10, 19), 0, links: After(tests));
        var report = Add(schedule, phase, "Relatório dos testes", D(10, 20), 1, 100, After(tests));
        schedule.SetReached(acceptance.Id, reached: true, Today);

        var projection = Projection(schedule, tests);

        projection.Milestones.Should().BeEmpty();
        report.End.Should().Be(D(10, 20));
        projection.ProjectEnd.Should().Be(new ProjectEndProjection(D(10, 20), D(10, 23), 3), "only the task moves");
    }

    [Fact(DisplayName = "Começa junto anda só antes de a tarefa começar")]
    public void StartToStart_MovesOnlyWhileTheTaskHasNotStarted()
    {
        var (schedule, phase) = Empty();
        var started = Add(schedule, phase, "Carga dos saldos", D(10, 13), 3, 40);
        var check = Add(schedule, phase, "Conferência", D(10, 13), 0, links: After(started, ScheduleLinkType.StartToStart));
        var notStarted = Add(schedule, phase, "Carga dos estoques", D(10, 14), 3);
        var count = Add(schedule, phase, "Contagem", D(10, 14), 0, links: After(notStarted, ScheduleLinkType.StartToStart));

        Projection(schedule, started).Milestones.Should().BeEmpty("the task keeps its start");
        Projection(schedule, notStarted).Milestones.Should().ContainSingle()
            .Which.Should().Be(new ProjectedMilestone(count.Id, "Contagem", D(10, 14), D(10, 19), 3));
        check.Start.Should().Be(D(10, 13));
    }

    [Fact(DisplayName = "Termina junto anda com o fim")]
    public void FinishToFinish_MovesWithTheEnd()
    {
        var (schedule, phase) = Empty();
        var load = Add(schedule, phase, "Carga dos saldos", D(10, 13), 3, 40);
        var report = Add(schedule, phase, "Relatório", D(10, 13), 2, links: After(load, ScheduleLinkType.FinishToFinish));
        var delivered = Add(schedule, phase, "Relatório entregue", D(10, 15), 0, links: After(report));

        // The report ends with the load, on 20/10, and the milestone after it too.
        report.End.Should().Be(D(10, 15));
        Projection(schedule, load).Milestones.Should().ContainSingle()
            .Which.Should().Be(new ProjectedMilestone(delivered.Id, "Relatório entregue", D(10, 15), D(10, 20), 3));
    }
}
