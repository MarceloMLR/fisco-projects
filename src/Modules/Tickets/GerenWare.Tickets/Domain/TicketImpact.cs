using GerenWare.SharedKernel;

namespace GerenWare.Tickets.Domain;

/// <summary>
/// A task (also a summary or a subtask) or a milestone of the schedule of the project that a ticket impacts (ADR-0164),
/// linked by who serves the ticket. It keeps only the id of the item, which is of the Projects module: a removed item
/// keeps its link, which comes back with it when the removal is undone (ADR-0091), and leaves with the project.
/// </summary>
public sealed class TicketImpact : Entity
{
    /// <summary>Up to 10 items for a ticket.</summary>
    public const int MaxItems = 10;

    private TicketImpact()
    {
    }

    public TicketImpact(Guid ticketId, Guid projectId, Guid scheduleItemId)
    {
        TicketId = ticketId;
        ProjectId = projectId;
        ScheduleItemId = scheduleItemId;
    }

    public Guid TicketId { get; private init; }

    /// <summary>The project of the ticket, where the link is in the audit.</summary>
    public Guid ProjectId { get; private init; }

    public Guid ScheduleItemId { get; private init; }

    /// <summary>The ids chosen, from none to 10, none repeated; null otherwise.</summary>
    public static IReadOnlyList<Guid>? ItemIdsOf(IReadOnlyCollection<Guid>? ids) =>
        ids is null || ids.Count > MaxItems || ids.Distinct().Count() != ids.Count ? null : [.. ids];
}
