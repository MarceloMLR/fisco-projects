using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Notifications.Infrastructure.Persistence;

internal sealed class TeamsNoticeStore(NotificationsDbContext db) : ITeamsNoticeStore
{
    public void Add(TeamsNotice notice) => db.TeamsNotices.Add(notice);

    public async Task<IReadOnlyList<TeamsNotice>> DueAsync(DateTime now, int take, CancellationToken cancellationToken) =>
        await db.TeamsNotices
            .Where(notice => notice.Status == TeamsNoticeStatus.Pending && notice.NextAttemptAt <= now)
            .OrderBy(notice => notice.NextAttemptAt).ThenBy(notice => notice.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> RemoveOldAsync(DateTime before, CancellationToken cancellationToken) =>
        db.TeamsNotices
            .Where(notice => notice.Status != TeamsNoticeStatus.Pending && notice.CreatedAt < before)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<bool> TrySaveAsync(TeamsNotice notice, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else changed it: forget the local change, so the next saves do not try it again.
            db.Entry(notice).State = EntityState.Detached;
            return false;
        }
    }
}

internal sealed class TeamsAppStore(NotificationsDbContext db) : ITeamsAppStore
{
    private TeamsApp? _app;

    public async Task<TeamsApp> GetAsync(CancellationToken cancellationToken) =>
        _app ??= await db.TeamsApps.SingleAsync(app => app.Id == TeamsApp.SingleId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

internal sealed class NoticePreferenceStore(NotificationsDbContext db) : INoticePreferenceStore
{
    public async Task<IReadOnlyDictionary<Guid, ImmediateNoticeChannel>> ChannelsAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        userIds.Count == 0
            ? new Dictionary<Guid, ImmediateNoticeChannel>()
            : await db.NoticePreferences.AsNoTracking()
                .Where(preference => userIds.Contains(preference.UserId))
                .ToDictionaryAsync(preference => preference.UserId, preference => preference.Channel, cancellationToken);

    public Task<NoticePreference?> FindAsync(Guid userId, CancellationToken cancellationToken) =>
        db.NoticePreferences.SingleOrDefaultAsync(preference => preference.UserId == userId, cancellationToken);

    public void Add(NoticePreference preference) => db.NoticePreferences.Add(preference);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
