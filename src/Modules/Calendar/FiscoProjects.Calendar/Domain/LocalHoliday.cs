using FiscoProjects.SharedKernel;

namespace FiscoProjects.Calendar.Domain;

/// <summary>
/// A holiday of the city, the state or the company, registered by the Administrator (ADR-0018, ADR-0083). One that
/// repeats every year holds on the same day and month from the year of its date on.
/// </summary>
public sealed class LocalHoliday : Entity
{
    public const int NameMaxLength = 80;

    private LocalHoliday()
    {
        Name = string.Empty;
    }

    public string Name { get; private set; }

    public DateOnly Date { get; private set; }

    public bool RepeatsEveryYear { get; private set; }

    /// <summary>Considered from the start; disregarded, it stays in the calendar as a business day.</summary>
    public bool IsConsidered { get; private set; } = true;

    /// <summary>The name without the spaces at the ends, when it is not empty and has up to 80 characters.</summary>
    public static string? ValidName(string? name)
    {
        var trimmed = name?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > NameMaxLength ? null : trimmed;
    }

    public static bool IsValidDate(DateOnly date) => CalendarYears.IsSupported(date.Year);

    public static LocalHoliday Create(string name, DateOnly date, bool repeatsEveryYear)
    {
        var holiday = new LocalHoliday();
        holiday.Change(name, date, repeatsEveryYear);
        return holiday;
    }

    public void Change(string name, DateOnly date, bool repeatsEveryYear)
    {
        Name = ValidName(name) ?? throw new ArgumentException(
            $"The name of the holiday is needed and has up to {NameMaxLength} characters.", nameof(name));
        Date = IsValidDate(date)
            ? date
            : throw new ArgumentOutOfRangeException(nameof(date), date, "The calendar has the years 1900 to 2199.");
        RepeatsEveryYear = repeatsEveryYear;
    }

    public void Consider(bool considered) => IsConsidered = considered;

    /// <summary>
    /// The date of the holiday in a year: its own date, the same day and month in a later year when it repeats (29
    /// February only in leap years), or none.
    /// </summary>
    public DateOnly? OccursIn(int year)
    {
        if (year == Date.Year)
        {
            return Date;
        }

        if (!RepeatsEveryYear || year < Date.Year || !CalendarYears.IsSupported(year))
        {
            return null;
        }

        return Date is { Month: 2, Day: 29 } && !DateTime.IsLeapYear(year) ? null : new DateOnly(year, Date.Month, Date.Day);
    }
}
