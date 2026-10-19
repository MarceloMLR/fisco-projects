using FiscoProjects.Calendar.Application;
using FiscoProjects.Calendar.Contracts;
using FiscoProjects.Calendar.Domain;
using FiscoProjects.Testing;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Calendar.Tests;

// Spec: calendario (module side): "Dias de trabalho", "Feriados nacionais calculados", "Pontos facultativos calculados",
// "Escolha dos feriados calculados", "Feriados locais", "Alterar e excluir feriados locais" and "Dias úteis".
public sealed class CalendarUseCaseTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Tabelas do módulo")]
    public async Task Migration_CreatesTheTablesOfTheModule()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);

        (await database.TablesAsync()).Where(table => table.Schema == "calendar").Select(table => table.Table)
            .Should().BeEquivalentTo("work_calendar", "calculated_holidays", "local_holidays", "__EFMigrationsHistory");
    }

    [Fact(DisplayName = "Dias de trabalho padrão")]
    public async Task NewInstallation_WorksFromMondayToFriday()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);

        var year = await YearAsync(database, 2026);

        year.WorkingDays.Should().Be(WorkCalendar.DefaultWorkingDays);
    }

    [Fact(DisplayName = "Feriados nacionais de 2026")]
    public async Task Year2026_HasTheTenNationalHolidays_Considered()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);

        var year = await YearAsync(database, 2026);

        var national = year.Days.Where(day => day.Kind == CalendarDayKind.NationalHoliday).ToList();
        national.Should().HaveCount(10).And.OnlyContain(day => day.IsConsidered && day.RepeatsEveryYear);
        national.Should().Contain(day => day.Key == BrazilianNationalCalendar.GoodFriday && day.Date == new DateOnly(2026, 4, 3));
    }

    [Fact(DisplayName = "Pontos facultativos de 2026")]
    public async Task Year2026_HasTheSevenOptionalHolidays_Disregarded()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);

        var year = await YearAsync(database, 2026);

        year.Days.Where(day => day.Kind == CalendarDayKind.OptionalHoliday).Should().HaveCount(7)
            .And.OnlyContain(day => !day.IsConsidered);
        year.Days.Select(day => day.Date).Should().BeInAscendingOrder();
    }

    [Fact(DisplayName = "Ano fora do calendário")]
    public async Task YearOutsideTheCalendar_IsNotAnswered()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);

        (await database.RunAsync<GetCalendarYear, CalendarYear?>(get => get.ExecuteAsync(1899, Cancellation))).Should().BeNull();
        (await database.RunAsync<GetCalendarYear, CalendarYear?>(get => get.ExecuteAsync(2200, Cancellation))).Should().BeNull();
    }

    [Fact(DisplayName = "Trocar os dias de trabalho")]
    public async Task WorkingDays_AreChanged()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);

        var changed = await database.RunAsync<ChangeWorkingDays, bool>(change => change.ExecuteAsync(
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday],
            Cancellation));

        changed.Should().BeTrue();
        (await YearAsync(database, 2026)).WorkingDays.Should().Be(WorkCalendar.DefaultWorkingDays | WorkingDays.Saturday);
        (await BusinessDaysAsync(database)).AddBusinessDays(new DateOnly(2026, 10, 16), 1).Should().Be(new DateOnly(2026, 10, 17));
    }

    [Fact(DisplayName = "Semana sem dia de trabalho")]
    public async Task WeekWithoutAWorkingDay_IsRefused()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);

        var changed = await database.RunAsync<ChangeWorkingDays, bool>(change => change.ExecuteAsync([], Cancellation));

        changed.Should().BeFalse();
        (await YearAsync(database, 2026)).WorkingDays.Should().Be(WorkCalendar.DefaultWorkingDays);
    }

    [Fact(DisplayName = "Escolha em todos os anos")]
    public async Task Choice_HoldsInEveryYear()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);

        (await ChooseAsync(database, BrazilianNationalCalendar.CarnivalTuesday, true)).Should().BeTrue();

        (await YearAsync(database, 2027)).Days.Should().ContainSingle(day => day.Key == BrazilianNationalCalendar.CarnivalTuesday)
            .Which.Should().Match<CalendarDay>(day => day.IsConsidered && day.Date == new DateOnly(2027, 2, 9));
        (await BusinessDaysAsync(database)).IsBusinessDay(new DateOnly(2026, 2, 17)).Should().BeFalse();
    }

    [Fact(DisplayName = "Desconsiderar um feriado nacional")]
    public async Task DisregardedNationalHoliday_StaysInTheList_AsABusinessDay()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);

        await ChooseAsync(database, BrazilianNationalCalendar.Tiradentes, false);

        (await YearAsync(database, 2026)).Days.Should().ContainSingle(day => day.Key == BrazilianNationalCalendar.Tiradentes)
            .Which.IsConsidered.Should().BeFalse();
        (await BusinessDaysAsync(database)).IsBusinessDay(new DateOnly(2026, 4, 21)).Should().BeTrue();
    }

    [Fact(DisplayName = "Feriado calculado desconhecido")]
    public async Task UnknownCalculatedHoliday_IsNotFound()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);

        (await ChooseAsync(database, "FoundationDay", true)).Should().BeFalse();
    }

    [Fact(DisplayName = "Incluir feriado local")]
    public async Task LocalHoliday_IsIncluded_Considered()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);

        var added = await AddAsync(database, " Recesso da empresa ", new DateOnly(2026, 12, 30), repeats: false);

        added.Invalid.Should().BeEmpty();
        (await YearAsync(database, 2026)).Days.Should().ContainSingle(day => day.Kind == CalendarDayKind.LocalHoliday)
            .Which.Should().Be(new CalendarDay(
                new DateOnly(2026, 12, 30), CalendarDayKind.LocalHoliday, null, added.Id, "Recesso da empresa", false, true));
        (await BusinessDaysAsync(database)).IsBusinessDay(new DateOnly(2026, 12, 30)).Should().BeFalse();
    }

    [Fact(DisplayName = "Feriado local inválido")]
    public async Task InvalidLocalHoliday_IsNotIncluded()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);

        var empty = await database.RunAsync<AddLocalHoliday, AddedLocalHoliday>(add =>
            add.ExecuteAsync(new LocalHolidayChanges("  ", null, false), Cancellation));
        var tooLong = await AddAsync(database, new string('a', 81), new DateOnly(2026, 1, 1), repeats: false);
        var outside = await AddAsync(database, "Feriado", new DateOnly(2200, 1, 1), repeats: false);

        empty.Invalid.Should().Equal(LocalHolidayField.Name, LocalHolidayField.Date);
        tooLong.Invalid.Should().Equal(LocalHolidayField.Name);
        outside.Invalid.Should().Equal(LocalHolidayField.Date);
        await using var db = database.NewContext();
        (await db.LocalHolidays.CountAsync(Cancellation)).Should().Be(0);
    }

    [Fact(DisplayName = "Alterar feriado local")]
    public async Task LocalHoliday_IsChanged()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);
        var added = await AddAsync(database, "Emenda de Tiradentes", new DateOnly(2026, 4, 20), repeats: false);

        var changed = await ChangeAsync(database, added.Id!.Value, "Emenda de Tiradentes", new DateOnly(2026, 4, 22), true);

        changed.Should().BeEquivalentTo(new ChangedLocalHoliday(true, []));
        var days = await BusinessDaysAsync(database);
        days.IsBusinessDay(new DateOnly(2026, 4, 22)).Should().BeFalse();
        days.IsBusinessDay(new DateOnly(2026, 4, 20)).Should().BeTrue();
    }

    [Fact(DisplayName = "Desconsiderar feriado local")]
    public async Task DisregardedLocalHoliday_StaysInTheList()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);
        var added = await AddAsync(database, "Data magna do estado", new DateOnly(2026, 7, 9), repeats: true);

        await ChangeAsync(database, added.Id!.Value, "Data magna do estado", new DateOnly(2026, 7, 9), false, repeats: true);

        (await YearAsync(database, 2027)).Days.Should().ContainSingle(day => day.LocalHolidayId == added.Id)
            .Which.IsConsidered.Should().BeFalse();
        (await BusinessDaysAsync(database)).IsBusinessDay(new DateOnly(2027, 7, 9)).Should().BeTrue();
    }

    [Fact(DisplayName = "Alterar feriado local que não existe")]
    public async Task ChangingAMissingLocalHoliday_IsNotFound()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);

        var changed = await ChangeAsync(database, Guid.CreateVersion7(), "Feriado", new DateOnly(2026, 1, 2), true);

        changed.Found.Should().BeFalse();
    }

    [Fact(DisplayName = "Alterar feriado local com dados inválidos")]
    public async Task ChangingWithInvalidFields_ChangesNothing()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);
        var added = await AddAsync(database, "Emenda de Tiradentes", new DateOnly(2026, 4, 20), repeats: false);

        var changed = await ChangeAsync(database, added.Id!.Value, "", new DateOnly(2026, 4, 22), false);

        changed.Should().BeEquivalentTo(new ChangedLocalHoliday(true, [LocalHolidayField.Name]));
        (await YearAsync(database, 2026)).Days.Should().ContainSingle(day => day.LocalHolidayId == added.Id)
            .Which.Should().Match<CalendarDay>(day => day.Name == "Emenda de Tiradentes" && day.IsConsidered);
    }

    [Fact(DisplayName = "Excluir feriado local")]
    public async Task LocalHoliday_IsRemoved()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);
        var added = await AddAsync(database, "Emenda de Tiradentes", new DateOnly(2026, 4, 20), repeats: false);

        var removed = await database.RunAsync<RemoveLocalHoliday, bool>(remove => remove.ExecuteAsync(added.Id!.Value, Cancellation));
        var again = await database.RunAsync<RemoveLocalHoliday, bool>(remove => remove.ExecuteAsync(added.Id!.Value, Cancellation));

        removed.Should().BeTrue();
        again.Should().BeFalse();
        (await YearAsync(database, 2026)).Days.Should().NotContain(day => day.Kind == CalendarDayKind.LocalHoliday);
        (await BusinessDaysAsync(database)).IsBusinessDay(new DateOnly(2026, 4, 20)).Should().BeTrue();
    }

    [Fact(DisplayName = "Contar dias úteis pelo contrato")]
    public async Task Contract_CountsTheBusinessDays()
    {
        await using var database = await CalendarDatabase.CreateAsync(sqlServer);

        var days = await BusinessDaysAsync(database);

        days.CountBusinessDays(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30)).Should().Be(19);
    }

    private static Task<CalendarYear> YearAsync(CalendarDatabase database, int year) =>
        database.RunAsync<GetCalendarYear, CalendarYear>(async get => (await get.ExecuteAsync(year, Cancellation))!);

    private static Task<IBusinessDays> BusinessDaysAsync(CalendarDatabase database) =>
        database.RunAsync<IWorkCalendar, IBusinessDays>(calendar => calendar.GetAsync(Cancellation));

    private static Task<bool> ChooseAsync(CalendarDatabase database, string key, bool considered) =>
        database.RunAsync<ChooseCalculatedHoliday, bool>(choose => choose.ExecuteAsync(key, considered, Cancellation));

    private static Task<AddedLocalHoliday> AddAsync(CalendarDatabase database, string name, DateOnly date, bool repeats) =>
        database.RunAsync<AddLocalHoliday, AddedLocalHoliday>(add =>
            add.ExecuteAsync(new LocalHolidayChanges(name, date, repeats), Cancellation));

    private static Task<ChangedLocalHoliday> ChangeAsync(
        CalendarDatabase database, Guid id, string name, DateOnly date, bool considered, bool repeats = false) =>
        database.RunAsync<ChangeLocalHoliday, ChangedLocalHoliday>(change =>
            change.ExecuteAsync(id, new LocalHolidayChanges(name, date, repeats), considered, Cancellation));
}
