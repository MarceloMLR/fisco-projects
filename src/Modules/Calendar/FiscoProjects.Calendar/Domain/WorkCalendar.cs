using FiscoProjects.SharedKernel;

namespace FiscoProjects.Calendar.Domain;

/// <summary>
/// The working days of the installation (ADR-0018): one row, created with the database, Monday to Friday while nobody
/// changes them, and never without a working day.
/// </summary>
public sealed class WorkCalendar : Entity
{
    public const WorkingDays DefaultWorkingDays = WorkingDays.Monday | WorkingDays.Tuesday | WorkingDays.Wednesday
        | WorkingDays.Thursday | WorkingDays.Friday;

    /// <summary>Id of the one row, created by the first migration of the module.</summary>
    public static readonly Guid SingleId = new("019a0127-3c40-7b6e-8f21-5d9c4a7e3b10");

    private WorkCalendar()
    {
    }

    public WorkingDays WorkingDays { get; private set; } = DefaultWorkingDays;

    /// <summary>At least one day of the week, and nothing but days of the week.</summary>
    public static bool IsValid(WorkingDays days) => days != WorkingDays.None && (days & ~Weekdays.All) == WorkingDays.None;

    public void ChangeWorkingDays(WorkingDays days) =>
        WorkingDays = IsValid(days)
            ? days
            : throw new ArgumentException("The week needs at least one working day.", nameof(days));
}
