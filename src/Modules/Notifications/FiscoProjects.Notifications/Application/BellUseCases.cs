using System.Text.Json;
using FiscoProjects.Administration.Contracts;
using FiscoProjects.Notifications.Contracts;
using FiscoProjects.Notifications.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Notifications.Application;

/// <summary>Where the notifications of the bell are kept; saved with the queue of the e-mails.</summary>
public interface INotificationStore
{
    void Add(Notification notification);

    /// <summary>A page of the notifications of the person, the newest first.</summary>
    Task<IReadOnlyList<Notification>> PageAsync(Guid userId, bool unreadOnly, int skip, int take, CancellationToken cancellationToken);

    Task<int> UnreadCountAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>The notification, tracked, only when it is of the person.</summary>
    Task<Notification?> FindAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>Marks every notification of the person as read; how many were not.</summary>
    Task<int> ReadAllAsync(Guid userId, DateTime now, CancellationToken cancellationToken);

    /// <summary>Removes the notifications that arrived before the moment; how many.</summary>
    Task<int> RemoveBeforeAsync(DateTime before, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// The channel of the bell (ADR-0038): every notice becomes, now, a notification for each person who hears about it,
/// also the ones whose e-mail waits for the daily summary.
/// </summary>
public sealed class BellChannel(INotificationStore store, TimeProvider clock) : INoticeChannel
{
    public Task DeliverAsync(
        Notice notice, IReadOnlyList<NoticeRecipient> recipients, string? actorName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notice);
        ArgumentNullException.ThrowIfNull(recipients);
        var now = clock.GetUtcNow().UtcDateTime;
        var values = JsonSerializer.Serialize(notice.Values);
        foreach (var person in recipients)
        {
            store.Add(new Notification(person.UserId, notice.Kind, notice.ActorId, notice.ProjectId, notice.Path, values, now));
        }

        return Task.CompletedTask;
    }
}

/// <summary>A notification as the bell shows it, with the current name of who did it (null for the system).</summary>
public sealed record BellItem(
    Guid Id,
    NoticeKind Kind,
    string? Actor,
    string Path,
    IReadOnlyDictionary<string, string> Values,
    DateTime CreatedAt,
    bool Read);

/// <summary>A page of the bell: the notifications, how many are not read and whether there are older ones.</summary>
public sealed record BellPage(IReadOnlyList<BellItem> Items, int Unread, bool HasMore);

/// <summary>The notifications of the person in the session, the newest first (ADR-0038).</summary>
public sealed class ListNotifications(INotificationStore store, IUserDirectory users)
{
    public const int PageSize = 30;

    public async Task<BellPage> ExecuteAsync(Guid userId, bool unreadOnly, int skip, CancellationToken cancellationToken)
    {
        var rows = await store.PageAsync(userId, unreadOnly, Math.Max(0, skip), PageSize + 1, cancellationToken);
        var page = rows.Take(PageSize).ToList();
        var actorIds = page.Where(row => row.ActorId is not null).Select(row => row.ActorId!.Value).Distinct().ToList();
        var names = await users.NamesAsync(actorIds, cancellationToken);
        var items = page.Select(row => new BellItem(
                row.Id,
                row.Kind,
                row.ActorId is { } actorId && names.TryGetValue(actorId, out var name) ? name : null,
                row.Path,
                JsonSerializer.Deserialize<Dictionary<string, string>>(row.Values) ?? [],
                row.CreatedAt,
                row.IsRead))
            .ToList();
        return new BellPage(items, await store.UnreadCountAsync(userId, cancellationToken), rows.Count > PageSize);
    }
}

public sealed class CountUnreadNotifications(INotificationStore store)
{
    public Task<int> ExecuteAsync(Guid userId, CancellationToken cancellationToken) => store.UnreadCountAsync(userId, cancellationToken);
}

/// <summary>Marks a notification of the person as read; false when there is no such notification of the person.</summary>
public sealed class ReadNotification(INotificationStore store, TimeProvider clock)
{
    public async Task<bool> ExecuteAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        if (await store.FindAsync(id, userId, cancellationToken) is not { } notification)
        {
            return false;
        }

        notification.Read(clock.GetUtcNow().UtcDateTime);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class ReadAllNotifications(INotificationStore store, TimeProvider clock)
{
    public Task<int> ExecuteAsync(Guid userId, CancellationToken cancellationToken) =>
        store.ReadAllAsync(userId, clock.GetUtcNow().UtcDateTime, cancellationToken);
}

/// <summary>
/// Removes the notifications older than the days the installation keeps them, read or not (ADR-0038); how many.
/// </summary>
public sealed class ForgetOldNotifications(INotificationStore store, IInstallationSettings installation, TimeProvider clock)
{
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var days = (await installation.GetAsync(cancellationToken)).NotificationRetentionDays;
        return await store.RemoveBeforeAsync(clock.GetUtcNow().UtcDateTime.AddDays(-days), cancellationToken);
    }
}
