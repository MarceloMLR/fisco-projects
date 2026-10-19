using FiscoProjects.Calendar.Domain;

namespace FiscoProjects.Calendar.Tests;

// Spec: calendario, requirements "Feriados locais" and "Repetição dos feriados locais".
public sealed class LocalHolidayTests
{
    [Theory(DisplayName = "Nome válido sem os espaços das pontas")]
    [InlineData("  Aniversário da cidade ", "Aniversário da cidade")]
    [InlineData("A", "A")]
    public void ValidName_TrimsTheEnds(string name, string expected) =>
        LocalHoliday.ValidName(name).Should().Be(expected);

    [Theory(DisplayName = "Nome inválido")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidName_IsNull_ForAnEmptyName(string? name) =>
        LocalHoliday.ValidName(name).Should().BeNull();

    [Fact(DisplayName = "Nome com até 80 caracteres")]
    public void ValidName_AcceptsUpTo80Characters()
    {
        LocalHoliday.ValidName(new string('a', 80)).Should().HaveLength(80);
        LocalHoliday.ValidName(new string('a', 81)).Should().BeNull();
    }

    [Fact(DisplayName = "Feriado local começa considerado")]
    public void NewHoliday_IsConsidered()
    {
        var holiday = LocalHoliday.Create(" Recesso da empresa ", new DateOnly(2026, 12, 30), repeatsEveryYear: false);

        holiday.Name.Should().Be("Recesso da empresa");
        holiday.IsConsidered.Should().BeTrue();
    }

    [Fact(DisplayName = "Data fora do calendário")]
    public void DateOutsideTheCalendar_IsRefused()
    {
        var create = () => LocalHoliday.Create("Feriado", new DateOnly(2200, 1, 1), repeatsEveryYear: false);

        create.Should().Throw<ArgumentOutOfRangeException>();
        LocalHoliday.IsValidDate(new DateOnly(1899, 12, 31)).Should().BeFalse();
        LocalHoliday.IsValidDate(new DateOnly(1900, 1, 1)).Should().BeTrue();
    }

    [Fact(DisplayName = "Feriado que repete todo ano")]
    public void RepeatingHoliday_HoldsFromTheYearOfItsDateOn()
    {
        var holiday = LocalHoliday.Create("Aniversário da cidade", new DateOnly(2026, 1, 25), repeatsEveryYear: true);

        holiday.OccursIn(2026).Should().Be(new DateOnly(2026, 1, 25));
        holiday.OccursIn(2027).Should().Be(new DateOnly(2027, 1, 25));
        holiday.OccursIn(2030).Should().Be(new DateOnly(2030, 1, 25));
        holiday.OccursIn(2025).Should().BeNull();
    }

    [Fact(DisplayName = "Feriado de um ano só")]
    public void HolidayOfOneYear_HoldsOnlyOnItsDate()
    {
        var holiday = LocalHoliday.Create("Emenda de Tiradentes", new DateOnly(2026, 4, 20), repeatsEveryYear: false);

        holiday.OccursIn(2026).Should().Be(new DateOnly(2026, 4, 20));
        holiday.OccursIn(2027).Should().BeNull();
    }

    [Fact(DisplayName = "Feriado em 29 de fevereiro")]
    public void HolidayOnTheTwentyNinthOfFebruary_HoldsOnlyInLeapYears()
    {
        var holiday = LocalHoliday.Create("Dia bissexto", new DateOnly(2028, 2, 29), repeatsEveryYear: true);

        holiday.OccursIn(2032).Should().Be(new DateOnly(2032, 2, 29));
        holiday.OccursIn(2029).Should().BeNull();
    }
}
