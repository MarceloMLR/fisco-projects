using FiscoProjects.Calendar.Contracts;

namespace FiscoProjects.MyWork.Tests;

/// <summary>Monday to Friday, but the holidays of October and November 2026 (12/10, 02/11 and 20/11).</summary>
internal sealed class TestBusinessDays : IBusinessDays
{
    private static readonly HashSet<DateOnly> Holidays = [new(2026, 10, 12), new(2026, 11, 2), new(2026, 11, 20)];

    public static TestBusinessDays Of2026 { get; } = new();

    public bool IsBusinessDay(DateOnly date) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !Holidays.Contains(date);

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
