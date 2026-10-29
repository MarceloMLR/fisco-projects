using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: cronograma (domain side): "Recálculo pelo calendário" (ADR-0126).
public sealed class ScheduleRecalculationTests
{
    /// <summary>The calendar the schedules were made in: 12/10 and 02/11 as holidays.</summary>
    private static readonly TestBusinessDays Before = TestBusinessDays.Of2026;

    /// <summary>The calendar with the Dia do Servidor Público (28/10) considered.</summary>
    private static readonly TestBusinessDays WithPublicServantsDay = new(D(10, 12), D(10, 28), D(11, 2));

    /// <summary>The calendar with Finados (02/11) disregarded.</summary>
    private static readonly TestBusinessDays WithoutAllSoulsDay = new(D(10, 12));

    private static readonly DateOnly Today = D(10, 19);

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static (Schedule Schedule, SchedulePhase Phase) NewSchedule()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        return (schedule, schedule.AddPhase("Configuração"));
    }

    private static ScheduleItem Add(
        Schedule schedule,
        SchedulePhase phase,
        string name,
        DateOnly start,
        int duration,
        int progress = 0,
        ScheduleItem? inside = null,
        params ScheduleItem[] after) =>
        schedule.AddItem(
            duration == 0 ? ScheduleItemKind.Milestone : ScheduleItemKind.Task,
            new ScheduleItemPlan(phase.Id, name, start, duration, null, progress, [.. after.Select(item => item.Id)], inside?.Id),
            Before,
            Today);

    private static (DateOnly Start, DateOnly End) Dates(ScheduleItem item) => (item.Start, item.End);

    [Fact(DisplayName = "Feriado novo no meio de uma tarefa")]
    public void ANewHoliday_InATask_PushesItsEnd_AndTheNextItems()
    {
        var (schedule, phase) = NewSchedule();
        var first = Add(schedule, phase, "Parametrizar regras de ICMS", D(10, 26), 5);
        var next = Add(schedule, phase, "Integrar NF-e", D(11, 3), 2, after: first);
        Dates(first).Should().Be((D(10, 26), D(10, 30)));
        Dates(next).Should().Be((D(11, 3), D(11, 4)));

        schedule.Recalculate(WithPublicServantsDay).Should().Be(2);

        Dates(first).Should().Be((D(10, 26), D(11, 3)));
        first.Duration.Should().Be(5);
        Dates(next).Should().Be((D(11, 4), D(11, 5)));
    }

    [Fact(DisplayName = "Marco num feriado novo")]
    public void AMilestone_OnANewHoliday_GoesToTheNextBusinessDay()
    {
        var (schedule, phase) = NewSchedule();
        var milestone = Add(schedule, phase, "Aceite da configuração", D(10, 28), 0);

        schedule.Recalculate(WithPublicServantsDay).Should().Be(1);

        Dates(milestone).Should().Be((D(10, 29), D(10, 29)));
    }

    [Fact(DisplayName = "Feriado desconsiderado não puxa as seguintes")]
    public void ADisregardedHoliday_EndsTheTaskEarlier_WithoutPullingTheNextItems()
    {
        var (schedule, phase) = NewSchedule();
        var first = Add(schedule, phase, "Parametrizar regras de ICMS", D(10, 29), 3);
        var next = Add(schedule, phase, "Integrar NF-e", D(10, 29), 1, after: first);
        Dates(first).Should().Be((D(10, 29), D(11, 3)));
        Dates(next).Should().Be((D(11, 4), D(11, 4)));

        schedule.Recalculate(WithoutAllSoulsDay).Should().Be(1);

        Dates(first).Should().Be((D(10, 29), D(11, 2)));
        Dates(next).Should().Be((D(11, 4), D(11, 4)));
    }

    [Fact(DisplayName = "Concluídos não mudam")]
    public void DoneTasks_AndReachedMilestones_KeepTheirDates()
    {
        var (schedule, phase) = NewSchedule();
        var done = Add(schedule, phase, "Parametrizar regras de ICMS", D(10, 26), 5, progress: 100);
        var reached = Add(schedule, phase, "Aceite da configuração", D(10, 28), 0);
        schedule.SetReached(reached.Id, true, Today).Should().BeTrue();

        schedule.Recalculate(WithPublicServantsDay).Should().Be(0);

        Dates(done).Should().Be((D(10, 26), D(10, 30)));
        Dates(reached).Should().Be((D(10, 28), D(10, 28)));
    }

    [Fact(DisplayName = "Tarefa-resumo recalculada")]
    public void ATaskWithSubtasks_TakesItsDatesFromThemAgain()
    {
        var (schedule, phase) = NewSchedule();
        var summary = Add(schedule, phase, "Parametrizar regras fiscais", D(10, 26), 5);
        Add(schedule, phase, "Mapear regras de ICMS", D(10, 26), 2, inside: summary);
        var second = Add(schedule, phase, "Revisar regras de ISS", D(10, 28), 2, inside: summary);
        (Dates(summary), summary.Duration).Should().Be(((D(10, 26), D(10, 29)), 4));

        schedule.Recalculate(WithPublicServantsDay).Should().Be(2);

        Dates(second).Should().Be((D(10, 29), D(10, 30)));
        (Dates(summary), summary.Duration).Should().Be(((D(10, 26), D(10, 30)), 4));
    }

    [Fact(DisplayName = "Cronograma em dia não muda")]
    public void ASchedule_InTheCalendarAsItIs_DoesNotChange()
    {
        var (schedule, phase) = NewSchedule();
        var first = Add(schedule, phase, "Parametrizar regras de ICMS", D(10, 26), 5);
        var next = Add(schedule, phase, "Integrar NF-e", D(11, 6), 2, after: first);
        var milestone = Add(schedule, phase, "Aceite da configuração", D(10, 30), 0, after: first);

        schedule.Recalculate(Before).Should().Be(0);

        Dates(first).Should().Be((D(10, 26), D(10, 30)));
        Dates(next).Should().Be((D(11, 6), D(11, 9)));
        Dates(milestone).Should().Be((D(10, 30), D(10, 30)));
    }
}
