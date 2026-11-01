using FiscoProjects.Calendar.Contracts;

namespace FiscoProjects.TimeTracking.Domain;

/// <summary>The state of the load of a person in a week (ADR-0142), from the freest to the most loaded.</summary>
public enum LoadState
{
    /// <summary>No task weighs on the week.</summary>
    Free = 1,

    /// <summary>Below 90% of the capacity.</summary>
    Within = 2,

    /// <summary>From 90% of the capacity up to it, the limit of the balance of the hours (ADR-0128).</summary>
    NearLimit = 3,

    /// <summary>Past the capacity, also with no capacity at all.</summary>
    Over = 4,
}

/// <summary>A task that weighs on the load of its owner: its own dates and its planned hours (ADR-0138).</summary>
public sealed record LoadedTask(DateOnly Start, DateOnly End, decimal Planned);

/// <summary>A period a person does not work, both days included (ADR-0141).</summary>
public sealed record AbsentPeriod(DateOnly Start, DateOnly End);

/// <summary>
/// The rule of the load of the team, in one place (ADR-0142): the planned hours of a task in equal parts over its
/// business days, the capacity of a week by its business days less the days of absence, and the state of a week.
/// </summary>
public static class TeamLoad
{
    /// <summary>How many weeks the load shows at once.</summary>
    public const int Weeks = 8;

    /// <summary>The Monday of the week of the day; weeks go from Monday to Sunday, as Minhas horas.</summary>
    public static DateOnly WeekOf(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));

    /// <summary>
    /// The planned hours of the task that fall between the days (both included): its hours divided by its business
    /// days, times its business days among them; nothing for a task with no business day.
    /// </summary>
    public static decimal HoursIn(LoadedTask task, DateOnly first, DateOnly last, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(days);
        var total = days.CountBusinessDays(task.Start, task.End);
        var from = task.Start > first ? task.Start : first;
        var to = task.End < last ? task.End : last;
        return total == 0 || from > to ? 0m : task.Planned * days.CountBusinessDays(from, to) / total;
    }

    /// <summary>The business days between the days (both included) in which the person is away, each day once.</summary>
    public static int AbsentBusinessDays(IEnumerable<AbsentPeriod> absences, DateOnly first, DateOnly last, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(absences);
        ArgumentNullException.ThrowIfNull(days);
        var away = new HashSet<DateOnly>();
        foreach (var absence in absences)
        {
            for (var day = absence.Start > first ? absence.Start : first; day <= absence.End && day <= last; day = day.AddDays(1))
            {
                if (days.IsBusinessDay(day))
                {
                    away.Add(day);
                }
            }
        }

        return away.Count;
    }

    /// <summary>The hours that fit between the days: the business days the person is not away, times the hours of a day.</summary>
    public static decimal CapacityOf(DateOnly first, DateOnly last, int absentBusinessDays, decimal hoursPerDay, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(days);
        return Math.Max(0, days.CountBusinessDays(first, last) - absentBusinessDays) * hoursPerDay;
    }

    /// <summary>Free without load; past the capacity, also with no capacity; near the limit from 90% of it; within below.</summary>
    public static LoadState StateOf(decimal load, decimal capacity) =>
        load <= 0m ? LoadState.Free
            : load > capacity ? LoadState.Over
            : load * 100m >= capacity * HoursBalance.NearLimitShare ? LoadState.NearLimit
            : LoadState.Within;
}
