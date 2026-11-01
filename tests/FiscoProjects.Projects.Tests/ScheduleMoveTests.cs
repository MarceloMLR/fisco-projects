using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: cronograma, requirements "Mover o cronograma" and "O que não anda no movimento" (ADR-0140). The calendar of 2026
// has 12/10 and 02/11 as holidays, on Mondays.
public sealed class ScheduleMoveTests
{
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;
    private static readonly DateOnly Today = D(10, 16);

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static ScheduleItem Add(
        Schedule schedule, SchedulePhase phase, ScheduleItemKind kind, string name, DateOnly start, int duration,
        ScheduleItem[]? after = null, ScheduleItem? parent = null, DateOnly? deadline = null) =>
        schedule.AddItem(
            kind,
            new ScheduleItemPlan(
                phase.Id, name, start, duration, null, 0, [.. (after ?? []).Select(item => item.Id)], parent?.Id, Deadline: deadline),
            Days,
            Today);

    private static (DateOnly Start, DateOnly End, int Duration) Dates(ScheduleItem item) => (item.Start, item.End, item.Duration);

    [Fact(DisplayName = "Mover para depois")]
    public void Later_EveryOpenItemGoesTheBusinessDays_OverTheHoliday()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var configuration = schedule.AddPhase("Configuração");
        var training = schedule.AddPhase("Treinamento");
        var rules = Add(schedule, configuration, ScheduleItemKind.Task, "Levantar regras", D(10, 21), 3);
        var tests = Add(schedule, configuration, ScheduleItemKind.Task, "Testes integrados", D(10, 26), 3, [rules]);
        var accepted = Add(schedule, configuration, ScheduleItemKind.Milestone, "Configuração aceita", D(10, 28), 0, [tests]);
        // A task of its own, depending on nothing: the dependencies alone would not move it.
        var users = Add(schedule, training, ScheduleItemKind.Task, "Treinar usuários", D(10, 29), 2);

        var moved = schedule.Move(5, Days);

        moved.Should().Be(4);
        Dates(rules).Should().Be((D(10, 28), D(10, 30), 3));
        // 26/10 plus five business days, past the holiday of 02/11: 03/11.
        Dates(tests).Should().Be((D(11, 3), D(11, 5), 3));
        Dates(accepted).Should().Be((D(11, 5), D(11, 5), 0));
        Dates(users).Should().Be((D(11, 6), D(11, 9), 2));
        schedule.Figures(Today).Phases.Select(phase => phase.End).Should().Equal(D(11, 5), D(11, 9));
    }

    [Fact(DisplayName = "Concluídos parados")]
    public void TheDoneItems_TheDeadlines_AndTheBaseline_Stay()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var phase = schedule.AddPhase("Levantamento");
        var opening = Add(schedule, phase, ScheduleItemKind.Task, "Reunião de abertura", D(10, 13), 1);
        var mapped = Add(schedule, phase, ScheduleItemKind.Milestone, "Processos mapeados", D(10, 14), 0, [opening]);
        var scope = Add(schedule, phase, ScheduleItemKind.Task, "Aprovar o escopo", D(10, 15), 3, deadline: D(10, 23));
        schedule.UpdateProgress(opening.Id, 100, D(10, 13));
        schedule.SetReached(mapped.Id, true, D(10, 14));
        schedule.SaveBaseline(Today);

        schedule.Move(2, Days);

        (Dates(opening), Dates(mapped)).Should().Be(((D(10, 13), D(10, 13), 1), (D(10, 14), D(10, 14), 0)));
        Dates(scope).Should().Be((D(10, 19), D(10, 21), 3));
        scope.Deadline.Should().Be(D(10, 23));
        schedule.Baseline!.ItemOf(scope.Id)!.Start.Should().Be(D(10, 15));
    }

    [Fact(DisplayName = "Mover para antes")]
    public void Earlier_TheOpenItemsComeBack_ButNotBeforeADonePredecessor()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var phase = schedule.AddPhase("Configuração");
        var setup = Add(schedule, phase, ScheduleItemKind.Task, "Preparar o ambiente", D(10, 19), 2);
        var accounts = Add(schedule, phase, ScheduleItemKind.Task, "Configurar plano de contas", D(10, 21), 3, [setup]);
        var report = Add(schedule, phase, ScheduleItemKind.Task, "Relatório fiscal", D(10, 28), 2);
        schedule.UpdateProgress(setup.Id, 100, D(10, 20));

        var moved = schedule.Move(-3, Days);

        // The report goes three business days earlier; the accounts would start on 16/10, but wait for the done setup.
        Dates(report).Should().Be((D(10, 23), D(10, 26), 2));
        Dates(accounts).Should().Be((D(10, 21), D(10, 23), 3));
        Dates(setup).Should().Be((D(10, 19), D(10, 20), 2));
        moved.Should().Be(1);
    }

    [Fact(DisplayName = "Mover com subtarefas")]
    public void ATaskWithSubtasks_FollowsThem()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var phase = schedule.AddPhase("Configuração");
        var tests = Add(schedule, phase, ScheduleItemKind.Task, "Testes integrados", D(10, 21), 2);
        var script = Add(schedule, phase, ScheduleItemKind.Task, "Roteiro de testes", D(10, 21), 2, parent: tests);
        Add(schedule, phase, ScheduleItemKind.Task, "Executar testes", D(10, 23), 2, [script], tests);

        schedule.Move(1, Days);

        Dates(script).Should().Be((D(10, 22), D(10, 23), 2));
        (tests.Start, tests.End).Should().Be((D(10, 22), D(10, 27)));
    }

    [Theory(DisplayName = "Movimento inválido")]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(-1000)]
    public void AMove_IsFrom1To999BusinessDays(int businessDays)
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var phase = schedule.AddPhase("Configuração");
        var task = Add(schedule, phase, ScheduleItemKind.Task, "Levantar regras", D(10, 21), 3);

        schedule.MayMove(businessDays, Days).Should().BeFalse();
        var refused = () => schedule.Move(businessDays, Days);

        refused.Should().Throw<ArgumentOutOfRangeException>();
        Dates(task).Should().Be((D(10, 21), D(10, 23), 3));
        schedule.MayMove(999, Days).Should().BeTrue();
    }
}
