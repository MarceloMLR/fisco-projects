using FiscoProjects.Calendar.Contracts;

namespace FiscoProjects.Projects.Tests;

/// <summary>Monday to Friday, but the holidays given: the calendar of the tests of the schedule.</summary>
internal sealed class TestBusinessDays(params DateOnly[] holidays) : IBusinessDays
{
    /// <summary>The calendar of October and November 2026, with 12/10 and 02/11 as holidays.</summary>
    public static TestBusinessDays Of2026 { get; } = new(new DateOnly(2026, 10, 12), new DateOnly(2026, 11, 2));

    private readonly HashSet<DateOnly> _holidays = [.. holidays];

    public bool IsBusinessDay(DateOnly date) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !_holidays.Contains(date);

    public DateOnly AddBusinessDays(DateOnly date, int days)
    {
        var step = Math.Sign(days);
        var current = date;
        for (var remaining = Math.Abs(days); remaining > 0;)
        {
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
}
