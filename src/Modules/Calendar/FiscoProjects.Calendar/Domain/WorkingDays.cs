namespace FiscoProjects.Calendar.Domain;

/// <summary>The days of the week the installation works (ADR-0018).</summary>
[Flags]
public enum WorkingDays
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,
}

/// <summary>Conversions between <see cref="WorkingDays"/> and the days of the week of .NET.</summary>
public static class Weekdays
{
    public const WorkingDays All = WorkingDays.Monday | WorkingDays.Tuesday | WorkingDays.Wednesday
        | WorkingDays.Thursday | WorkingDays.Friday | WorkingDays.Saturday | WorkingDays.Sunday;

    /// <summary>The days of the week from Monday to Sunday, the order of the screens.</summary>
    public static readonly IReadOnlyList<DayOfWeek> MondayFirst =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
    ];

    public static WorkingDays Of(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => WorkingDays.Monday,
        DayOfWeek.Tuesday => WorkingDays.Tuesday,
        DayOfWeek.Wednesday => WorkingDays.Wednesday,
        DayOfWeek.Thursday => WorkingDays.Thursday,
        DayOfWeek.Friday => WorkingDays.Friday,
        DayOfWeek.Saturday => WorkingDays.Saturday,
        DayOfWeek.Sunday => WorkingDays.Sunday,
        _ => throw new ArgumentOutOfRangeException(nameof(day), day, "Not a day of the week."),
    };

    public static WorkingDays Of(IEnumerable<DayOfWeek> days)
    {
        ArgumentNullException.ThrowIfNull(days);
        return days.Aggregate(WorkingDays.None, (all, day) => all | Of(day));
    }

    public static bool Includes(this WorkingDays days, DayOfWeek day) => (days & Of(day)) != WorkingDays.None;

    /// <summary>The days of the week marked, from Monday to Sunday.</summary>
    public static IReadOnlyList<DayOfWeek> ToDaysOfWeek(this WorkingDays days) =>
        [.. MondayFirst.Where(day => days.Includes(day))];
}
