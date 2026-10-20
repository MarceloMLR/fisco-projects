using FiscoProjects.SharedKernel;

namespace FiscoProjects.Tickets.Domain;

/// <summary>
/// The deadlines of a priority in the installation (ADR-0025): the business days for the first answer and for the
/// resolution, as the Administrator sets them; they count for the tickets opened afterwards.
/// </summary>
public sealed class TicketDeadlineSetting : Entity
{
    public const int MinDays = 1;
    public const int MaxDays = 365;

    private TicketDeadlineSetting()
    {
    }

    public TicketDeadlineSetting(TicketPriority priority, int firstResponseDays, int resolutionDays)
    {
        Priority = priority;
        FirstResponseDays = firstResponseDays;
        ResolutionDays = resolutionDays;
    }

    public TicketPriority Priority { get; private init; }

    public int FirstResponseDays { get; private set; }

    public int ResolutionDays { get; private set; }

    public static bool IsValid(int days) => days is >= MinDays and <= MaxDays;

    public void Change(int firstResponseDays, int resolutionDays)
    {
        if (!IsValid(firstResponseDays) || !IsValid(resolutionDays))
        {
            throw new ArgumentOutOfRangeException(nameof(resolutionDays), "A deadline goes from 1 to 365 business days.");
        }

        FirstResponseDays = firstResponseDays;
        ResolutionDays = resolutionDays;
    }

    /// <summary>The deadlines of the MVP (ADR-0025): one business day for the first answer; 10, 5, 2 and 1 to resolve.</summary>
    public static IReadOnlyList<TicketDeadlineSetting> Defaults =>
    [
        new(TicketPriority.Low, 1, 10),
        new(TicketPriority.Medium, 1, 5),
        new(TicketPriority.High, 1, 2),
        new(TicketPriority.Critical, 1, 1),
    ];
}

/// <summary>The two deadlines of a ticket (ADR-0025).</summary>
public enum TicketDeadline
{
    FirstResponse = 1,
    Resolution = 2,
}

/// <summary>
/// The last alert told of a deadline of a ticket (ADR-0037): the state and the day it was due then. Each state is told
/// once, and a deadline that moved, as with a new priority or after waiting for the requester, starts over.
/// </summary>
public sealed class TicketDeadlineAlert
{
    private TicketDeadlineAlert()
    {
    }

    public TicketDeadlineAlert(Guid ticketId, TicketDeadline deadline, TicketDueState state, DateOnly dueOn)
    {
        TicketId = ticketId;
        Deadline = deadline;
        State = state;
        DueOn = dueOn;
    }

    public Guid TicketId { get; private init; }

    public TicketDeadline Deadline { get; private init; }

    public TicketDueState State { get; private set; }

    public DateOnly DueOn { get; private set; }

    /// <summary>Whether a deadline due soon or overdue is news after the alert told, if any.</summary>
    public static bool IsNews(TicketDeadlineAlert? told, TicketDueState state, DateOnly dueOn) =>
        state is TicketDueState.Soon or TicketDueState.Overdue
        && (told is null || told.DueOn != dueOn || state > told.State);

    public void Told(TicketDueState state, DateOnly dueOn)
    {
        State = state;
        DueOn = dueOn;
    }
}

/// <summary>Where a deadline stands today (ADR-0025), for the icon, the color and the text (ADR-0065).</summary>
public enum TicketDueState
{
    OnTrack = 1,

    /// <summary>Due today or on the next business day.</summary>
    Soon = 2,

    Overdue = 3,

    /// <summary>The resolution waits for the requester, and the deadline does not run.</summary>
    Paused = 4,

    /// <summary>The first answer was given, or the ticket was resolved.</summary>
    Met = 5,
}
