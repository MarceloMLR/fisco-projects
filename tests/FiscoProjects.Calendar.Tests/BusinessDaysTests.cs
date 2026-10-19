using FiscoProjects.Calendar.Domain;

namespace FiscoProjects.Calendar.Tests;

// Spec: calendario, requirements "Dias de trabalho", "Dias úteis", "Pontos facultativos calculados" and
// "Escolha dos feriados calculados" (module side).
public sealed class BusinessDaysTests
{
    private static readonly BrazilianNationalCalendar National = new();

    [Fact(DisplayName = "Somar dias úteis")]
    public void AddingABusinessDay_SkipsTheWeekendAndTheHoliday() =>
        Days().AddBusinessDays(new DateOnly(2026, 10, 9), 1).Should().Be(new DateOnly(2026, 10, 13));

    [Fact(DisplayName = "Feriado desconsiderado não afeta as datas")]
    public void DisregardedHoliday_IsABusinessDay() =>
        Days(disregarded: [BrazilianNationalCalendar.OurLadyOfAparecida])
            .AddBusinessDays(new DateOnly(2026, 10, 9), 1).Should().Be(new DateOnly(2026, 10, 12));

    [Fact(DisplayName = "Dias de trabalho mudam o cálculo")]
    public void WorkingSaturday_IsABusinessDay() =>
        Days(WorkCalendar.DefaultWorkingDays | WorkingDays.Saturday)
            .AddBusinessDays(new DateOnly(2026, 10, 16), 1).Should().Be(new DateOnly(2026, 10, 17));

    [Fact(DisplayName = "Contar dias úteis")]
    public void November2026_Has19BusinessDays() =>
        Days().CountBusinessDays(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30)).Should().Be(19);

    [Fact(DisplayName = "Ponto facultativo desconsiderado por padrão")]
    public void OptionalHoliday_IsABusinessDay_ByDefault() =>
        Days().IsBusinessDay(new DateOnly(2026, 2, 18)).Should().BeTrue();

    [Fact(DisplayName = "Ponto facultativo considerado")]
    public void ConsideredOptionalHoliday_IsNoBusinessDay() =>
        Days(considered: [BrazilianNationalCalendar.AshWednesday]).IsBusinessDay(new DateOnly(2026, 2, 18)).Should().BeFalse();

    [Fact(DisplayName = "Escolha em todos os anos")]
    public void Choice_HoldsInEveryYear()
    {
        var days = Days(considered: [BrazilianNationalCalendar.CarnivalTuesday]);

        days.IsBusinessDay(new DateOnly(2026, 2, 17)).Should().BeFalse();
        days.IsBusinessDay(new DateOnly(2027, 2, 9)).Should().BeFalse();
    }

    [Fact(DisplayName = "Feriado local considerado e desconsiderado")]
    public void LocalHoliday_CountsOnlyWhenConsidered()
    {
        var recess = LocalHoliday.Create("Recesso da empresa", new DateOnly(2026, 12, 30), repeatsEveryYear: false);
        var magna = LocalHoliday.Create("Data magna do estado", new DateOnly(2026, 7, 9), repeatsEveryYear: true);
        magna.Consider(false);

        var days = Days(local: [recess, magna]);

        days.IsBusinessDay(new DateOnly(2026, 12, 30)).Should().BeFalse();
        days.IsBusinessDay(new DateOnly(2026, 7, 9)).Should().BeTrue();
    }

    [Fact(DisplayName = "Somar para trás e somar zero")]
    public void AddingBackwards_AndAddingZero()
    {
        var days = Days();

        days.AddBusinessDays(new DateOnly(2026, 10, 13), -1).Should().Be(new DateOnly(2026, 10, 9));
        days.AddBusinessDays(new DateOnly(2026, 10, 10), 0).Should().Be(new DateOnly(2026, 10, 10));
        days.AddBusinessDays(new DateOnly(2026, 12, 23), 3).Should().Be(new DateOnly(2026, 12, 29));
    }

    [Fact(DisplayName = "Contar com o fim antes do começo")]
    public void Counting_WithTheLastDateFirst_IsZero() =>
        Days().CountBusinessDays(new DateOnly(2026, 11, 30), new DateOnly(2026, 11, 1)).Should().Be(0);

    [Fact(DisplayName = "Semana sem dia de trabalho")]
    public void WeekWithoutAWorkingDay_IsRefused()
    {
        var create = () => Days(WorkingDays.None);

        create.Should().Throw<ArgumentException>();
        WorkCalendar.IsValid(WorkingDays.None).Should().BeFalse();
        WorkCalendar.IsValid((WorkingDays)128).Should().BeFalse();
        WorkCalendar.IsValid(WorkingDays.Sunday).Should().BeTrue();
    }

    [Fact(DisplayName = "Dias de trabalho padrão")]
    public void DefaultWorkingDays_AreMondayToFriday() =>
        WorkCalendar.DefaultWorkingDays.ToDaysOfWeek().Should().Equal(
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);

    private static BusinessDays Days(
        WorkingDays workingDays = WorkCalendar.DefaultWorkingDays,
        string[]? considered = null,
        string[]? disregarded = null,
        LocalHoliday[]? local = null)
    {
        var keys = National.Holidays
            .Where(definition => (definition.ConsideredByDefault || (considered ?? []).Contains(definition.Key))
                && !(disregarded ?? []).Contains(definition.Key))
            .Select(definition => definition.Key);
        return new BusinessDays(workingDays, National, keys, local ?? []);
    }
}
