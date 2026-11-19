using GerenWare.Projects.Contracts;
using GerenWare.Tickets.Domain;

namespace GerenWare.Tickets.Application;

/// <summary>An open ticket of a project and the items of the schedule it is linked to (ADR-0164).</summary>
public sealed record OpenTicketImpacts(Ticket Ticket, IReadOnlyList<Guid> ItemIds);

/// <summary>Where the links of the tickets to the items of the schedule are kept (ADR-0164).</summary>
public interface ITicketImpactStore
{
    /// <summary>The links of a ticket, tracked, also to items removed from the schedule.</summary>
    Task<IReadOnlyList<TicketImpact>> OfTicketAsync(Guid ticketId, CancellationToken cancellationToken);

    /// <summary>The open tickets (not resolved nor closed) of a project with links, by the number.</summary>
    Task<IReadOnlyList<OpenTicketImpacts>> OpenOfProjectAsync(Guid projectId, CancellationToken cancellationToken);

    void Add(TicketImpact impact);

    void Remove(TicketImpact impact);
}

/// <summary>
/// What a ticket reaches in the schedule (ADR-0164), as its page shows it: the items linked, the milestones after them
/// and the critical path, computed by the schedule now, and whether the person may change the items.
/// </summary>
public sealed record TicketSchedule(ScheduleImpact Impact, bool CanChange);

public enum ImpactChangeOutcome
{
    Done = 1,
    NotFound = 2,
    Forbidden = 3,

    /// <summary>More than 10 items, one repeated, or one that is not a task or milestone of the project.</summary>
    Invalid = 4,

    /// <summary>The ticket is closed.</summary>
    Closed = 5,

    /// <summary>Someone changed the ticket at the same time; nothing was saved.</summary>
    Changed = 6,
}

/// <summary>
/// Who serves the ticket changes the tasks and milestones of the schedule of its project the ticket impacts (ADR-0164),
/// the whole list at once, from none to 10, until the ticket is closed. Nothing of the schedule nor of the deadlines
/// changes. The conversation records what was linked and unlinked, by the names of then, and the audit, each link.
/// </summary>
public sealed class ChangeTicketImpacts(
    IProjectAccess projects, ITicketStore store, ITicketImpactStore impacts, IScheduleItems schedule, TimeProvider clock)
{
    public async Task<ImpactChangeOutcome> ExecuteAsync(
        int number, IReadOnlyCollection<Guid>? itemIds, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var ticket = await store.FindAsync(number, cancellationToken);
        var project = ticket is null
            ? null
            : await projects.FindAsync(ticket.ProjectId, viewer.UserId, viewer.IsAdministrator, cancellationToken);
        if (ticket is null || project is null)
        {
            return ImpactChangeOutcome.NotFound;
        }

        if (!TicketAccess.MayLinkToSchedule(viewer, project.Role, ticket))
        {
            return ImpactChangeOutcome.Forbidden;
        }

        if (ticket.Status == TicketStatus.Closed)
        {
            return ImpactChangeOutcome.Closed;
        }

        var items = await schedule.ItemsAsync(ticket.ProjectId, cancellationToken);
        var linkable = items.Where(item => item.Type is ScheduleItemType.Task or ScheduleItemType.Milestone).Select(item => item.Id).ToHashSet();
        if (TicketImpact.ItemIdsOf(itemIds) is not { } chosen || !chosen.All(linkable.Contains))
        {
            return ImpactChangeOutcome.Invalid;
        }

        var linked = await impacts.OfTicketAsync(ticket.Id, cancellationToken);
        var added = chosen.Where(id => linked.All(link => link.ScheduleItemId != id)).ToHashSet();
        // A link to an item removed from the schedule is not shown, so the person never chose to leave it: it stays for
        // the undo of the removal to bring it back (ADR-0091).
        var removed = linked.Where(link => linkable.Contains(link.ScheduleItemId) && !chosen.Contains(link.ScheduleItemId)).ToList();
        if (added.Count == 0 && removed.Count == 0)
        {
            return ImpactChangeOutcome.Done;
        }

        foreach (var id in added)
        {
            impacts.Add(new TicketImpact(ticket.Id, ticket.ProjectId, id));
        }

        foreach (var link in removed)
        {
            impacts.Remove(link);
        }

        var unlinked = removed.Select(link => link.ScheduleItemId).ToHashSet();
        var now = clock.GetUtcNow().UtcDateTime;
        store.AddEvent(TicketEvent.ImpactsChanged(
            ticket.Id,
            viewer.UserId,
            [.. items.Where(item => added.Contains(item.Id)).Select(item => item.Name)],
            [.. items.Where(item => unlinked.Contains(item.Id)).Select(item => item.Name)],
            now));
        ticket.Touch(now);
        try
        {
            await store.SaveChangesAsync(cancellationToken);
            return ImpactChangeOutcome.Done;
        }
        catch (TicketChangedException)
        {
            return ImpactChangeOutcome.Changed;
        }
    }
}
