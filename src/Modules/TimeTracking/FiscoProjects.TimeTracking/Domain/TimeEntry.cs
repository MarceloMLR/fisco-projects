using FiscoProjects.SharedKernel;

namespace FiscoProjects.TimeTracking.Domain;

/// <summary>The fields of a time entry, to tell which ones are not valid.</summary>
public enum TimeEntryField
{
    Day = 1,
    Hours = 2,
    Description = 3,
    Project = 4,
    ScheduleItem = 5,
    Ticket = 6,
}

/// <summary>
/// The hours a person worked on a project on a day (ADR-0128): in quarters of an hour, with what was done and, when
/// the person says, the task of the schedule or the ticket of the project. Only hours: no value in money.
/// </summary>
public sealed class TimeEntry : Entity
{
    public const int DescriptionMaxLength = 500;

    private TimeEntry()
    {
        Description = string.Empty;
    }

    public TimeEntry(Guid userId, TimeEntryForm form, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(form);
        UserId = userId;
        CreatedAt = now;
        Apply(form, now);
        Description = form.Description;
    }

    /// <summary>Who worked the hours.</summary>
    public Guid UserId { get; private init; }

    public Guid ProjectId { get; private set; }

    public DateOnly Day { get; private set; }

    public decimal Hours { get; private set; }

    public string Description { get; private set; }

    /// <summary>The task of the schedule of the project, when the person says.</summary>
    public Guid? ScheduleItemId { get; private set; }

    /// <summary>The ticket of the project, when the person says; never together with a task.</summary>
    public Guid? TicketId { get; private set; }

    public DateTime CreatedAt { get; private init; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>Corrects every field, the project included (ADR-0128).</summary>
    public void Change(TimeEntryForm form, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(form);
        Apply(form, now);
    }

    private void Apply(TimeEntryForm form, DateTime now)
    {
        ProjectId = form.ProjectId;
        Day = form.Day;
        Hours = form.Hours;
        Description = form.Description;
        ScheduleItemId = form.ScheduleItemId;
        TicketId = form.TicketId;
        UpdatedAt = now;
    }
}

/// <summary>A time entry as the person filled it in, already checked against the day of today.</summary>
public sealed record TimeEntryForm(Guid ProjectId, DateOnly Day, decimal Hours, string Description, Guid? ScheduleItemId, Guid? TicketId)
{
    /// <summary>The least hours of an entry, a quarter of an hour, which is also the step of the hours.</summary>
    public const decimal Quarter = 0.25m;

    /// <summary>The most hours of an entry and of the entries of a person on a day.</summary>
    public const decimal MaxHours = 24m;

    /// <summary>
    /// The entry, with the description trimmed, or the fields that are not valid: the project needed; the day needed
    /// and not after today; the hours from 0.25 to 24 in quarters of an hour; the description needed, up to 500
    /// characters; and a task or a ticket, never both.
    /// </summary>
    public static (TimeEntryForm? Form, IReadOnlyList<TimeEntryField> Invalid) Of(
        Guid? projectId, DateOnly? day, decimal? hours, string? description, Guid? scheduleItemId, Guid? ticketId, DateOnly today)
    {
        var invalid = new List<TimeEntryField>();
        if (projectId is null || projectId == Guid.Empty)
        {
            invalid.Add(TimeEntryField.Project);
        }

        if (day is not { } validDay || validDay > today)
        {
            invalid.Add(TimeEntryField.Day);
        }

        if (hours is not { } validHours || !IsValidHours(validHours))
        {
            invalid.Add(TimeEntryField.Hours);
        }

        var text = description?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > TimeEntry.DescriptionMaxLength || text.Any(IsForbidden))
        {
            invalid.Add(TimeEntryField.Description);
        }

        if (scheduleItemId is not null && ticketId is not null)
        {
            invalid.Add(TimeEntryField.Ticket);
        }

        return invalid.Count > 0
            ? (null, invalid)
            : (new TimeEntryForm(projectId!.Value, day!.Value, hours!.Value, text!, scheduleItemId, ticketId), invalid);
    }

    /// <summary>From a quarter of an hour to 24 hours, in quarters of an hour.</summary>
    public static bool IsValidHours(decimal hours) => Quarters.IsValid(hours, Quarter, MaxHours);

    /// <summary>Whether the entry still fits in the day of the person, with the other entries of that day.</summary>
    public static bool FitsInDay(decimal hours, decimal otherHoursOfTheDay) => otherHoursOfTheDay + hours <= MaxHours;

    // Line breaks and tabs are text the person typed; the other control characters are not.
    private static bool IsForbidden(char character) => char.IsControl(character) && character is not ('\n' or '\r' or '\t');
}
