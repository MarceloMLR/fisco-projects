namespace FiscoProjects.Calendar.Domain;

/// <summary>
/// The national holidays and the optional holidays of Brazil that the federal ordinance repeats every year (ADR-0083,
/// Portaria MGI nº 11.460/2025). The movable ones follow Easter of the Gregorian calendar.
/// </summary>
public sealed class BrazilianNationalCalendar : INationalCalendar
{
    public const string NewYearsDay = "NewYearsDay";
    public const string CarnivalMonday = "CarnivalMonday";
    public const string CarnivalTuesday = "CarnivalTuesday";
    public const string AshWednesday = "AshWednesday";
    public const string GoodFriday = "GoodFriday";
    public const string Tiradentes = "Tiradentes";
    public const string LabourDay = "LabourDay";
    public const string CorpusChristi = "CorpusChristi";
    public const string IndependenceDay = "IndependenceDay";
    public const string OurLadyOfAparecida = "OurLadyOfAparecida";
    public const string PublicServantsDay = "PublicServantsDay";
    public const string AllSoulsDay = "AllSoulsDay";
    public const string RepublicDay = "RepublicDay";
    public const string BlackConsciousnessDay = "BlackConsciousnessDay";
    public const string ChristmasEve = "ChristmasEve";
    public const string ChristmasDay = "ChristmasDay";
    public const string NewYearsEve = "NewYearsEve";

    /// <summary>The Black Consciousness Day is a national holiday from 2024 on (Lei nº 14.759/2023).</summary>
    public const int BlackConsciousnessDaySince = 2024;

    private static readonly IReadOnlyList<Rule> Rules =
    [
        Fixed(NewYearsDay, CalendarDayKind.NationalHoliday, 1, 1),
        FromEaster(CarnivalMonday, CalendarDayKind.OptionalHoliday, -48),
        FromEaster(CarnivalTuesday, CalendarDayKind.OptionalHoliday, -47),
        FromEaster(AshWednesday, CalendarDayKind.OptionalHoliday, -46),
        FromEaster(GoodFriday, CalendarDayKind.NationalHoliday, -2),
        Fixed(Tiradentes, CalendarDayKind.NationalHoliday, 4, 21),
        Fixed(LabourDay, CalendarDayKind.NationalHoliday, 5, 1),
        FromEaster(CorpusChristi, CalendarDayKind.OptionalHoliday, 60),
        Fixed(IndependenceDay, CalendarDayKind.NationalHoliday, 9, 7),
        Fixed(OurLadyOfAparecida, CalendarDayKind.NationalHoliday, 10, 12),
        Fixed(PublicServantsDay, CalendarDayKind.OptionalHoliday, 10, 28),
        Fixed(AllSoulsDay, CalendarDayKind.NationalHoliday, 11, 2),
        Fixed(RepublicDay, CalendarDayKind.NationalHoliday, 11, 15),
        Fixed(BlackConsciousnessDay, CalendarDayKind.NationalHoliday, 11, 20, BlackConsciousnessDaySince),
        Fixed(ChristmasEve, CalendarDayKind.OptionalHoliday, 12, 24),
        Fixed(ChristmasDay, CalendarDayKind.NationalHoliday, 12, 25),
        Fixed(NewYearsEve, CalendarDayKind.OptionalHoliday, 12, 31),
    ];

    /// <summary>National holidays start considered and optional holidays disregarded, as business days (ADR-0083).</summary>
    public IReadOnlyList<CalculatedHolidayDefinition> Holidays { get; } =
        [.. Rules.Select(rule => new CalculatedHolidayDefinition(rule.Key, rule.Kind, rule.Kind == CalendarDayKind.NationalHoliday))];

    public IReadOnlyList<CalculatedHolidayDate> In(int year)
    {
        var easter = EasterSunday(year);
        return
        [
            .. Rules
                .Where(rule => year >= rule.Since)
                .Select(rule => new CalculatedHolidayDate(rule.Key, rule.Kind, rule.DateIn(year, easter)))
                .OrderBy(holiday => holiday.Date),
        ];
    }

    /// <summary>Easter Sunday of the Gregorian calendar (the anonymous algorithm of Meeus, Jones and Butcher).</summary>
    public static DateOnly EasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = ((19 * a) + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + (2 * e) + (2 * i) - h - k) % 7;
        var m = (a + (11 * h) + (22 * l)) / 451;
        var month = (h + l - (7 * m) + 114) / 31;
        var day = ((h + l - (7 * m) + 114) % 31) + 1;
        return new DateOnly(year, month, day);
    }

    private static Rule Fixed(string key, CalendarDayKind kind, int month, int day, int since = int.MinValue) =>
        new(key, kind, since, (year, _) => new DateOnly(year, month, day));

    private static Rule FromEaster(string key, CalendarDayKind kind, int days) =>
        new(key, kind, int.MinValue, (_, easter) => easter.AddDays(days));

    private sealed record Rule(string Key, CalendarDayKind Kind, int Since, Func<int, DateOnly, DateOnly> DateIn);
}
