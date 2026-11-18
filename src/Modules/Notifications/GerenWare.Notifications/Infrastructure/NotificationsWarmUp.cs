using GerenWare.Notifications.Application;
using GerenWare.Notifications.Infrastructure.Persistence;
using GerenWare.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace GerenWare.Notifications.Infrastructure;

/// <summary>The reads of the bell (ADR-0106), asked on every screen, as the person of the last notification sees them.</summary>
internal sealed class NotificationsWarmUp(
    NotificationsDbContext db, CountUnreadNotifications unread, ListNotifications list) : IWarmUp
{
    public string Module => "Notifications";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var userId = await db.Notifications.AsNoTracking()
            .OrderByDescending(notification => notification.Id)
            .Select(notification => (Guid?)notification.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (userId is not { } person)
        {
            return;
        }

        await unread.ExecuteAsync(person, cancellationToken);
        await list.ExecuteAsync(person, unreadOnly: false, skip: 0, cancellationToken);
    }
}
