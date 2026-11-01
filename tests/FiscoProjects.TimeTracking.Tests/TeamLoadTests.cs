using FiscoProjects.TimeTracking.Domain;

namespace FiscoProjects.TimeTracking.Tests;

// Spec: horas-do-projeto, requirements "Carga da equipe", "Capacidade da semana" and "Estado da carga" (ADR-0142). The
// calendar of 2026 has 12/10, 02/11 and 20/11 as holidays.
public sealed class TeamLoadTests
{
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static decimal Week(LoadedTask task, DateOnly monday) => TeamLoad.HoursIn(task, monday, monday.AddDays(6), Days);

    [Fact(DisplayName = "Semana de segunda a domingo")]
    public void AWeek_StartsOnItsMonday()
    {
        TeamLoad.WeekOf(D(10, 19)).Should().Be(D(10, 19));
        TeamLoad.WeekOf(D(10, 22)).Should().Be(D(10, 19));
        TeamLoad.WeekOf(D(10, 25)).Should().Be(D(10, 19));
        TeamLoad.WeekOf(D(10, 26)).Should().Be(D(10, 26));
    }

    [Fact(DisplayName = "Tarefa espalhada pelas semanas")]
    public void ATask_SpreadsItsPlannedHoursOverItsBusinessDays()
    {
        // From Thursday 22/10 to Wednesday 28/10: five business days of 8 hours.
        var task = new LoadedTask(D(10, 22), D(10, 28), 40m);

        Week(task, D(10, 19)).Should().Be(16m);
        Week(task, D(10, 26)).Should().Be(24m);
        Week(task, D(11, 2)).Should().Be(0m);
    }

    [Fact(DisplayName = "Carga sobre um feriado")]
    public void TheHoliday_DoesNotWeigh()
    {
        // From Friday 30/10 to Wednesday 04/11, with the holiday of 02/11: three business days.
        var task = new LoadedTask(D(10, 30), D(11, 4), 24m);

        Week(task, D(10, 26)).Should().Be(8m);
        Week(task, D(11, 2)).Should().Be(16m);
        TeamLoad.HoursIn(task, D(11, 2), D(11, 2), Days).Should().Be(0m);
    }

    [Fact(DisplayName = "Capacidade com férias")]
    public void TheCapacity_IsTheBusinessDaysNotAway_TimesTheHoursOfADay()
    {
        AbsentPeriod[] away = [new(D(10, 21), D(10, 23))];

        var absent = TeamLoad.AbsentBusinessDays(away, D(10, 19), D(10, 25), Days);

        absent.Should().Be(3);
        TeamLoad.CapacityOf(D(10, 19), D(10, 25), absent, 8m, Days).Should().Be(16m);
        // An absence over the weekend and into the next week counts only its business days in each week.
        TeamLoad.AbsentBusinessDays([new(D(10, 23), D(10, 27))], D(10, 26), D(11, 1), Days).Should().Be(2);
    }

    [Fact(DisplayName = "Capacidade com feriado")]
    public void TheHolidayOfTheWeek_TakesADayOfTheCapacity()
    {
        TeamLoad.CapacityOf(D(11, 2), D(11, 8), 0, 8m, Days).Should().Be(32m);
        TeamLoad.CapacityOf(D(11, 2), D(11, 8), 0, 7.5m, Days).Should().Be(30m);
        // A holiday inside the absence is not taken twice.
        TeamLoad.AbsentBusinessDays([new(D(10, 30), D(11, 3))], D(11, 2), D(11, 8), Days).Should().Be(1);
    }

    [Theory(DisplayName = "Estados da carga")]
    [InlineData(44, 40, LoadState.Over)]
    [InlineData(40, 40, LoadState.NearLimit)]
    [InlineData(38, 40, LoadState.NearLimit)]
    [InlineData(36, 40, LoadState.NearLimit)]
    [InlineData(35.75, 40, LoadState.Within)]
    [InlineData(20, 40, LoadState.Within)]
    [InlineData(0, 40, LoadState.Free)]
    [InlineData(0, 0, LoadState.Free)]
    public void TheState_OfAWeek(double load, double capacity, LoadState state) =>
        TeamLoad.StateOf((decimal)load, (decimal)capacity).Should().Be(state);

    [Fact(DisplayName = "Carga na semana toda de férias")]
    public void AWholeWeekAway_HasNoCapacity_AndAnyLoadIsOver()
    {
        var absent = TeamLoad.AbsentBusinessDays([new(D(10, 17), D(10, 30))], D(10, 19), D(10, 25), Days);
        var capacity = TeamLoad.CapacityOf(D(10, 19), D(10, 25), absent, 8m, Days);

        (absent, capacity).Should().Be((5, 0m));
        TeamLoad.StateOf(4m, capacity).Should().Be(LoadState.Over);
    }
}
