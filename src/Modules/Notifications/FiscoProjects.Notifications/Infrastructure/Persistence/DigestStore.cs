using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Notifications.Infrastructure.Persistence;

internal sealed class DigestStore(NotificationsDbContext db) : IDigestStore
{
    public async Task<DigestItem?> FindAsync(Guid userId, string key, CancellationToken cancellationToken) =>
        db.DigestItems.Local.FirstOrDefault(item => item.UserId == userId && item.Key == key)
        ?? await db.DigestItems.FirstOrDefaultAsync(item => item.UserId == userId && item.Key == key, cancellationToken);

    public void Add(DigestItem item) => db.DigestItems.Add(item);

    public async Task<IReadOnlyList<DigestItem>> BeforeAsync(DateTime before, CancellationToken cancellationToken) =>
        await db.DigestItems.Where(item => item.CreatedAt <= before).ToListAsync(cancellationToken);

    public void Remove(IEnumerable<DigestItem> items) => db.DigestItems.RemoveRange(items);

    public async Task<bool> TryStartDayAsync(DailySummaryRun run, CancellationToken cancellationToken)
    {
        if (await db.DailySummaries.AnyAsync(day => day.Day == run.Day, cancellationToken))
        {
            return false;
        }

        db.DailySummaries.Add(run);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2627 or 2601 })
        {
            // Another process took the day first.
            db.Entry(run).State = EntityState.Detached;
            return false;
        }
    }
}
