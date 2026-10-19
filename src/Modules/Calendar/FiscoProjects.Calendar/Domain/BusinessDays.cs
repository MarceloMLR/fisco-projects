using FiscoProjects.Calendar.Contracts;

namespace FiscoProjects.Calendar.Domain;

/// <summary>
/// The business days of the installation at one moment (ADR-0018): the working days of the week, the keys of the
/// calculated holidays considered and the local holidays. The days that are no business days are found once per year.
/// </summary>
public sealed class BusinessDays : IBusinessDays
{
    /// <summary>A search for a business day longer than this means a broken calendar, never a real one.</summary>
    private const int MaximumSearch = 3660;

    private readonly WorkingDays _workingDays;
    private readonly INationalCalendar _nationalCalendar;
    private readonly IReadOnlySet<string> _consideredKeys;
    private readonly IReadOnlyList<LocalHoliday> _localHolidays;
    private readonly Dictionary<int, HashSet<DateOnly>> _daysOffByYear = [];

    public BusinessDays(
        WorkingDays workingDays,
        INationalCalendar nationalCalendar,
        IEnumerable<string> consideredHolidays,
        IEnumerable<LocalHoliday> localHolidays)
    {
        ArgumentNullException.ThrowIfNull(nationalCalendar);
        ArgumentNullException.ThrowIfNull(consideredHolidays);
        ArgumentNullException.ThrowIfNull(localHolidays);
        if (!WorkCalendar.IsValid(workingDays))
        {
            throw new ArgumentException("The week needs at least one working day.", nameof(workingDays));
        }

        _workingDays = workingDays;
        _nationalCalendar = nationalCalendar;
        _consideredKeys = consideredHolidays.ToHashSet(StringComparer.Ordinal);
        _localHolidays = [.. localHolidays.Where(holiday => holiday.IsConsidered)];
    }

    public bool IsBusinessDay(DateOnly date) =>
        _workingDays.Includes(date.DayOfWeek) && !DaysOffIn(date.Year).Contains(date);

    public DateOnly AddBusinessDays(DateOnly date, int days)
    {
        var step = Math.Sign(days);
        var remaining = Math.Abs(days);
        var current = date;
        for (var walked = 0; remaining > 0; walked++)
        {
            if (walked > MaximumSearch + Math.Abs(days))
            {
                throw new InvalidOperationException("The calendar has no business day ahead.");
            }

            current = current.AddDays(step);
            if (IsBusinessDay(current))
            {
                remaining--;
            }
        }

        return current;
    }

    public int CountBusinessDays(DateOnly first, DateOnly last)
    {
        var count = 0;
        for (var day = first; day <= last; day = day.AddDays(1))
        {
            if (IsBusinessDay(day))
            {
                count++;
            }
        }

        return count;
    }

    private HashSet<DateOnly> DaysOffIn(int year)
    {
        if (!_daysOffByYear.TryGetValue(year, out var daysOff))
        {
            daysOff =
            [
                .. _nationalCalendar.In(year).Where(holiday => _consideredKeys.Contains(holiday.Key)).Select(holiday => holiday.Date),
                .. _localHolidays.Select(holiday => holiday.OccursIn(year)).OfType<DateOnly>(),
            ];
            _daysOffByYear[year] = daysOff;
        }

        return daysOff;
    }
}
