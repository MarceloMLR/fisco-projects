using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Infrastructure.Persistence;
using FiscoProjects.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Notifications.Infrastructure;

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
