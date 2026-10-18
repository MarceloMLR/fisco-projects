using FiscoProjects.Audit.Application;
using FiscoProjects.Audit.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Audit.Infrastructure.Persistence;

internal sealed class AuditStore(AuditDbContext db) : IAuditStore
{
    public async Task<AuditPage> ListAsync(AuditQuery query, CancellationToken cancellationToken)
    {
        var entries = db.Entries.AsNoTracking();
        if (query.ActorId is { } actorId)
        {
            entries = entries.Where(entry => entry.ActorId == actorId);
        }

        if (!string.IsNullOrWhiteSpace(query.ItemType))
        {
            entries = entries.Where(entry => entry.ItemType == query.ItemType);
        }

        if (query.From is { } from)
        {
            var start = from.UtcDateTime;
            entries = entries.Where(entry => entry.OccurredAt >= start);
        }

        if (query.To is { } to)
        {
            var end = to.UtcDateTime;
            entries = entries.Where(entry => entry.OccurredAt < end);
        }

        var total = await entries.CountAsync(cancellationToken);
        var items = await entries
            .OrderByDescending(entry => entry.OccurredAt)
            .ThenByDescending(entry => entry.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        return new AuditPage(items, total);
    }

    public async Task AddAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        db.Entries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
    }
}
