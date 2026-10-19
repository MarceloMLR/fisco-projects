namespace FiscoProjects.Calendar.Domain;

/// <summary>The kinds of dates of the calendar.</summary>
public enum CalendarDayKind
{
    NationalHoliday = 1,
    OptionalHoliday = 2,
    LocalHoliday = 3,
}

/// <summary>A holiday the system calculates, and whether it starts considered (ADR-0083).</summary>
public sealed record CalculatedHolidayDefinition(string Key, CalendarDayKind Kind, bool ConsideredByDefault);

/// <summary>The date of a calculated holiday in one year.</summary>
public sealed record CalculatedHolidayDate(string Key, CalendarDayKind Kind, DateOnly Date);

/// <summary>
/// The national holidays and optional holidays of a country, calculated without registration (ADR-0018). Another
/// country is another implementation.
/// </summary>
public interface INationalCalendar
{
    /// <summary>Every holiday the calendar calculates, in a fixed order.</summary>
    IReadOnlyList<CalculatedHolidayDefinition> Holidays { get; }

    /// <summary>The dates of the holidays in a year, in the order of the dates.</summary>
    IReadOnlyList<CalculatedHolidayDate> In(int year);
}
