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
