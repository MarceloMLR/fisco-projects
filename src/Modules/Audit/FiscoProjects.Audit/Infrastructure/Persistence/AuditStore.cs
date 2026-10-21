using FiscoProjects.Audit.Application;
using FiscoProjects.Audit.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Audit.Infrastructure.Persistence;

internal sealed class AuditStore(AuditDbContext db) : IAuditStore
{
    /// <summary>How many records go in one delete: small enough to keep the locks short.</summary>
    private const int RemovalBatch = 5_000;

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

    public Task<bool> HasCleanupAsync(DateOnly month, CancellationToken cancellationToken) =>
        db.Cleanups.AnyAsync(cleanup => cleanup.Month == month, cancellationToken);

    public async Task<int> RemoveBeforeAsync(DateTime before, CancellationToken cancellationToken)
    {
        var removed = 0;
        while (true)
        {
            var batch = await db.Entries
                .Where(entry => entry.OccurredAt < before)
                .OrderBy(entry => entry.OccurredAt)
                .Take(RemovalBatch)
                .ExecuteDeleteAsync(cancellationToken);
            removed += batch;
            if (batch < RemovalBatch)
            {
                return removed;
            }
        }
    }

    public async Task<bool> RecordCleanupAsync(AuditCleanupRun cleanup, AuditEntry? entry, CancellationToken cancellationToken)
    {
        db.Cleanups.Add(cleanup);
        if (entry is not null)
        {
            db.Entries.Add(entry);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }
}
