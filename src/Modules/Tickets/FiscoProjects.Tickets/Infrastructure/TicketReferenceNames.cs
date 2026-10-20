using System.Globalization;
using FiscoProjects.SharedKernel;
using FiscoProjects.Tickets.Contracts;
using FiscoProjects.Tickets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Tickets.Infrastructure;

/// <summary>The tickets that audit records point to, by number and title, as "#104 Rejeição ao emitir" (ADR-0084).</summary>
internal sealed class TicketReferenceNames(TicketsDbContext db) : IAuditReferenceNames
{
    public string Kind => TicketsAudit.TicketReference;

    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        (await db.Tickets.AsNoTracking()
            .Where(ticket => ids.Contains(ticket.Id))
            .Select(ticket => new { ticket.Id, ticket.Number, ticket.Title })
            .ToListAsync(cancellationToken))
        .ToDictionary(ticket => ticket.Id, ticket => string.Create(CultureInfo.InvariantCulture, $"#{ticket.Number} {ticket.Title}"));
}

/// <summary>The names of the vendors that audit records point to (ADR-0084).</summary>
internal sealed class VendorReferenceNames(TicketsDbContext db) : IAuditReferenceNames
{
    public string Kind => TicketsAudit.VendorReference;

    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.Vendors.AsNoTracking()
            .Where(vendor => ids.Contains(vendor.Id))
            .ToDictionaryAsync(vendor => vendor.Id, vendor => vendor.Name, cancellationToken);
}
