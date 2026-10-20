using FiscoProjects.Tickets.Application;
using FiscoProjects.Tickets.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Tickets.Infrastructure.Persistence;

internal sealed class VendorStore(TicketsDbContext db) : IVendorStore
{
    public async Task<IReadOnlyList<Vendor>> ListAsync(bool activeOnly, CancellationToken cancellationToken) =>
        await db.Vendors.AsNoTracking()
            .Where(vendor => !activeOnly || vendor.IsActive)
            .OrderBy(vendor => vendor.Name)
            .ToListAsync(cancellationToken);

    public Task<Vendor?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Vendors.SingleOrDefaultAsync(vendor => vendor.Id == id, cancellationToken);

    public Task<bool> NameInUseAsync(string name, Guid? except, CancellationToken cancellationToken) =>
        db.Vendors.AnyAsync(vendor => vendor.Name == name && vendor.Id != except, cancellationToken);

    public void Add(Vendor vendor) => db.Vendors.Add(vendor);

    public async Task<IReadOnlyList<VendorTicketRow>> TicketsOfAsync(
        IReadOnlyCollection<Guid> ticketIds, CancellationToken cancellationToken) =>
        await db.VendorTickets.AsNoTracking()
            .Where(vendorTicket => ticketIds.Contains(vendorTicket.TicketId))
            .Join(db.Vendors, vendorTicket => vendorTicket.VendorId, vendor => vendor.Id, (vendorTicket, vendor) => new { vendorTicket, vendor.Name })
            .OrderBy(row => row.vendorTicket.RegisteredAt).ThenBy(row => row.vendorTicket.Id)
            .Select(row => new VendorTicketRow(row.vendorTicket, row.Name))
            .ToListAsync(cancellationToken);

    public Task<VendorTicket?> FindTicketAsync(Guid ticketId, Guid id, CancellationToken cancellationToken) =>
        db.VendorTickets.SingleOrDefaultAsync(vendorTicket => vendorTicket.Id == id && vendorTicket.TicketId == ticketId, cancellationToken);

    public void AddTicket(VendorTicket vendorTicket) => db.VendorTickets.Add(vendorTicket);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 } sql
            && sql.Message.Contains(VendorConfiguration.NameIndex, StringComparison.Ordinal))
        {
            throw new VendorNameInUseException(exception);
        }
    }
}
