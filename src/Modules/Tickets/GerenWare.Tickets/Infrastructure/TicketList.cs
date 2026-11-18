using GerenWare.Tickets.Contracts;
using GerenWare.Tickets.Domain;
using GerenWare.Tickets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GerenWare.Tickets.Infrastructure;

/// <summary>The tickets of the projects for the other modules, as the hours worked on a ticket (ADR-0128).</summary>
internal sealed class TicketList(TicketsDbContext db) : IProjectTicketList
{
    public async Task<IReadOnlyList<ProjectTicketInfo>> OfProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        [.. (await db.Tickets.AsNoTracking()
                .Where(ticket => ticket.ProjectId == projectId)
                .Select(ticket => new ProjectTicketInfo(ticket.Id, ticket.ProjectId, ticket.Number, ticket.Title, ticket.Status == TicketStatus.Closed))
                .ToListAsync(cancellationToken))
            .OrderBy(ticket => ticket.IsClosed)
            .ThenByDescending(ticket => ticket.Number)];

    public async Task<IReadOnlyList<ProjectTicketInfo>> FindAsync(IReadOnlyCollection<Guid> ticketIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticketIds);
        if (ticketIds.Count == 0)
        {
            return [];
        }

        return await db.Tickets.AsNoTracking()
            .Where(ticket => ticketIds.Contains(ticket.Id))
            .Select(ticket => new ProjectTicketInfo(ticket.Id, ticket.ProjectId, ticket.Number, ticket.Title, ticket.Status == TicketStatus.Closed))
            .ToListAsync(cancellationToken);
    }
}
