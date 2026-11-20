using GerenWare.Tickets.Application;
using GerenWare.Tickets.Domain;
using Microsoft.EntityFrameworkCore;

namespace GerenWare.Tickets.Infrastructure.Persistence;

internal sealed class TicketImpactStore(TicketsDbContext db) : ITicketImpactStore
{
    public async Task<IReadOnlyList<TicketImpact>> OfTicketAsync(Guid ticketId, CancellationToken cancellationToken) =>
        await db.TicketImpacts.Where(impact => impact.TicketId == ticketId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OpenTicketImpacts>> OpenOfProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var rows = await db.TicketImpacts.AsNoTracking()
            .Where(impact => impact.ProjectId == projectId)
            .Join(
                db.Tickets.AsNoTracking().Where(ticket => ticket.Status != TicketStatus.Resolved && ticket.Status != TicketStatus.Closed),
                impact => impact.TicketId,
                ticket => ticket.Id,
                (impact, ticket) => new { ticket, impact.ScheduleItemId })
            .ToListAsync(cancellationToken);
        return [.. rows
            .GroupBy(row => row.ticket.Id)
            .Select(group => new OpenTicketImpacts(group.First().ticket, [.. group.Select(row => row.ScheduleItemId)]))
            .OrderBy(item => item.Ticket.Number)];
    }

    public void Add(TicketImpact impact) => db.TicketImpacts.Add(impact);

    public void Remove(TicketImpact impact) => db.TicketImpacts.Remove(impact);
}
