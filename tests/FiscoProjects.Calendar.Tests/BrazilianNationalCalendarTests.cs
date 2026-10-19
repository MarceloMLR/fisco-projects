using FiscoProjects.Calendar.Domain;

namespace FiscoProjects.Calendar.Tests;

// Spec: calendario, requirements "Feriados nacionais calculados" and "Pontos facultativos calculados" (ADR-0083).
public sealed class BrazilianNationalCalendarTests
{
    private static readonly BrazilianNationalCalendar Calendar = new();

    [Theory(DisplayName = "Páscoa do calendário gregoriano")]
    [InlineData(2019, 4, 21)]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    [InlineData(2038, 4, 25)]
    [InlineData(2285, 3, 22)]
    public void EasterSunday_FollowsTheGregorianCalendar(int year, int month, int day) =>
        BrazilianNationalCalendar.EasterSunday(year).Should().Be(new DateOnly(year, month, day));

    [Theory(DisplayName = "Feriados móveis pela Páscoa")]
    [InlineData(2024, "2024-03-29", "2024-02-12", "2024-05-30")]
    [InlineData(2025, "2025-04-18", "2025-03-03", "2025-06-19")]
    [InlineData(2026, "2026-04-03", "2026-02-16", "2026-06-04")]
    [InlineData(2027, "2027-03-26", "2027-02-08", "2027-05-27")]
    public void MovableHolidays_FollowEaster(int year, string goodFriday, string carnivalMonday, string corpusChristi)
    {
        var dates = Calendar.In(year).ToDictionary(holiday => holiday.Key, holiday => holiday.Date);

        dates[BrazilianNationalCalendar.GoodFriday].Should().Be(DateOnly.Parse(goodFriday, System.Globalization.CultureInfo.InvariantCulture));
        dates[BrazilianNationalCalendar.CarnivalMonday].Should().Be(DateOnly.Parse(carnivalMonday, System.Globalization.CultureInfo.InvariantCulture));
        dates[BrazilianNationalCalendar.CarnivalTuesday].Should().Be(dates[BrazilianNationalCalendar.CarnivalMonday].AddDays(1));
        dates[BrazilianNationalCalendar.AshWednesday].Should().Be(dates[BrazilianNationalCalendar.CarnivalMonday].AddDays(2));
        dates[BrazilianNationalCalendar.CorpusChristi].Should().Be(DateOnly.Parse(corpusChristi, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact(DisplayName = "Feriados nacionais de 2026")]
    public void NationalHolidaysOf2026_AreTheTenOfTheOrdinance()
    {
        var holidays = Calendar.In(2026).Where(holiday => holiday.Kind == CalendarDayKind.NationalHoliday);

        holidays.Select(holiday => (holiday.Key, holiday.Date)).Should().Equal(
            (BrazilianNationalCalendar.NewYearsDay, new DateOnly(2026, 1, 1)),
            (BrazilianNationalCalendar.GoodFriday, new DateOnly(2026, 4, 3)),
            (BrazilianNationalCalendar.Tiradentes, new DateOnly(2026, 4, 21)),
            (BrazilianNationalCalendar.LabourDay, new DateOnly(2026, 5, 1)),
            (BrazilianNationalCalendar.IndependenceDay, new DateOnly(2026, 9, 7)),
            (BrazilianNationalCalendar.OurLadyOfAparecida, new DateOnly(2026, 10, 12)),
            (BrazilianNationalCalendar.AllSoulsDay, new DateOnly(2026, 11, 2)),
            (BrazilianNationalCalendar.RepublicDay, new DateOnly(2026, 11, 15)),
            (BrazilianNationalCalendar.BlackConsciousnessDay, new DateOnly(2026, 11, 20)),
            (BrazilianNationalCalendar.ChristmasDay, new DateOnly(2026, 12, 25)));
    }

    [Fact(DisplayName = "Pontos facultativos de 2026")]
    public void OptionalHolidaysOf2026_AreTheSevenTheOrdinanceRepeats()
    {
        var holidays = Calendar.In(2026).Where(holiday => holiday.Kind == CalendarDayKind.OptionalHoliday);

        holidays.Select(holiday => (holiday.Key, holiday.Date)).Should().Equal(
            (BrazilianNationalCalendar.CarnivalMonday, new DateOnly(2026, 2, 16)),
            (BrazilianNationalCalendar.CarnivalTuesday, new DateOnly(2026, 2, 17)),
            (BrazilianNationalCalendar.AshWednesday, new DateOnly(2026, 2, 18)),
            (BrazilianNationalCalendar.CorpusChristi, new DateOnly(2026, 6, 4)),
            (BrazilianNationalCalendar.PublicServantsDay, new DateOnly(2026, 10, 28)),
            (BrazilianNationalCalendar.ChristmasEve, new DateOnly(2026, 12, 24)),
            (BrazilianNationalCalendar.NewYearsEve, new DateOnly(2026, 12, 31)));
    }

    [Fact(DisplayName = "Consciência Negra a partir de 2024")]
    public void BlackConsciousnessDay_IsAHolidayFrom2024On()
    {
        Calendar.In(2023).Should().NotContain(holiday => holiday.Key == BrazilianNationalCalendar.BlackConsciousnessDay);
        Calendar.In(2024).Should().ContainSingle(holiday => holiday.Key == BrazilianNationalCalendar.BlackConsciousnessDay)
            .Which.Date.Should().Be(new DateOnly(2024, 11, 20));
    }

    [Fact(DisplayName = "Padrões do ADR-0083")]
    public void NationalHolidays_StartConsidered_AndOptionalHolidaysDisregarded()
    {
        Calendar.Holidays.Should().HaveCount(17);
        Calendar.Holidays.Should().OnlyContain(holiday =>
            holiday.ConsideredByDefault == (holiday.Kind == CalendarDayKind.NationalHoliday));
        Calendar.Holidays.Select(holiday => holiday.Key).Should().OnlyHaveUniqueItems();
    }

    [Fact(DisplayName = "Datas do ano em ordem")]
    public void DatesOfAYear_AreInOrder() =>
        Calendar.In(2026).Select(holiday => holiday.Date).Should().BeInAscendingOrder();
}
