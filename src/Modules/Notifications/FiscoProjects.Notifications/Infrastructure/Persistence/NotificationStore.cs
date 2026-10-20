using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Notifications.Infrastructure.Persistence;

internal sealed class NotificationStore(NotificationsDbContext db) : INotificationStore
{
    public void Add(Notification notification) => db.Notifications.Add(notification);

    public async Task<IReadOnlyList<Notification>> PageAsync(
        Guid userId, bool unreadOnly, int skip, int take, CancellationToken cancellationToken)
    {
        var mine = db.Notifications.AsNoTracking().Where(notification => notification.UserId == userId);
        if (unreadOnly)
        {
            mine = mine.Where(notification => notification.ReadAt == null);
        }

        return await mine
            .OrderByDescending(notification => notification.CreatedAt).ThenByDescending(notification => notification.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> UnreadCountAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Notifications.CountAsync(notification => notification.UserId == userId && notification.ReadAt == null, cancellationToken);

    public Task<Notification?> FindAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        db.Notifications.FirstOrDefaultAsync(notification => notification.Id == id && notification.UserId == userId, cancellationToken);

    public Task<int> ReadAllAsync(Guid userId, DateTime now, CancellationToken cancellationToken) =>
        db.Notifications
            .Where(notification => notification.UserId == userId && notification.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(notification => notification.ReadAt, now), cancellationToken);

    public Task<int> RemoveBeforeAsync(DateTime before, CancellationToken cancellationToken) =>
        db.Notifications.Where(notification => notification.CreatedAt < before).ExecuteDeleteAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
