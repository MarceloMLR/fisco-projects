using System.Text.Json;
using FiscoProjects.Notifications.Contracts;
using FiscoProjects.Notifications.Domain;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Notifications.Application;

/// <summary>A person who hears about a notice, with what the channels need.</summary>
public sealed record NoticeRecipient(Guid UserId, string Name, string Email);

/// <summary>
/// A way the notices reach people (ADR-0038): the e-mail now, the bell and the Teams later. The rules of who hears about
/// what never know the channels; each channel decides how the notice goes, as the e-mail between now and the summary.
/// </summary>
public interface INoticeChannel
{
    /// <param name="notice">What happened.</param>
    /// <param name="recipients">Who hears about it, already without who did it, who is inactive and who does not see the project.</param>
    /// <param name="actorName">The current name of who did it; null for the system.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    Task DeliverAsync(Notice notice, IReadOnlyList<NoticeRecipient> recipients, string? actorName, CancellationToken cancellationToken);
}

/// <summary>Where the notices waiting for the daily summary are kept; saved with the queue of the e-mails.</summary>
public interface IDigestStore
{
    /// <summary>The item of the person with the key not sent yet, or null.</summary>
    Task<DigestItem?> FindAsync(Guid userId, string key, CancellationToken cancellationToken);

    void Add(DigestItem item);

    /// <summary>The items noted before the moment, tracked.</summary>
    Task<IReadOnlyList<DigestItem>> BeforeAsync(DateTime before, CancellationToken cancellationToken);

    void Remove(IEnumerable<DigestItem> items);

    /// <summary>Takes the day for the summary; false when another process took it first.</summary>
    Task<bool> TryStartDayAsync(DailySummaryRun run, CancellationToken cancellationToken);
}

/// <summary>
/// The rules of who hears about a notice, in one place (ADR-0037, ADR-0038): never who did it, nor who is inactive, nor
/// who no longer sees the project of the item (neither in it nor the Administrator). The channels then deliver it, and
/// everything is saved together.
/// </summary>
public sealed class DeliverNotices(
    IUserDirectory users,
    IUserAccess access,
    IProjectAccess projects,
    IEnumerable<INoticeChannel> channels,
    IOutboxStore store)
{
    public async Task ExecuteAsync(IReadOnlyCollection<Notice> notices, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notices);
        var everyone = notices.SelectMany(notice => notice.Recipients)
            .Concat(notices.Where(notice => notice.ActorId is not null).Select(notice => notice.ActorId!.Value))
            .Distinct()
            .ToList();
        var people = (await users.PeopleAsync(everyone, cancellationToken)).ToDictionary(person => person.Id);
        var projectIds = notices.Where(notice => notice.ProjectId is not null).Select(notice => notice.ProjectId!.Value).Distinct().ToList();
        var members = (await projects.MembersAsync(projectIds, cancellationToken))
            .Select(member => (member.ProjectId, member.UserId))
            .ToHashSet();
        var administrators = new Dictionary<Guid, bool>();

        foreach (var notice in notices)
        {
            var recipients = new List<NoticeRecipient>();
            foreach (var userId in notice.Recipients.Distinct())
            {
                if (userId == notice.ActorId || !people.TryGetValue(userId, out var person) || !person.IsActive)
                {
                    continue;
                }

                if (notice.ProjectId is { } projectId && !members.Contains((projectId, userId))
                    && !await IsAdministratorAsync(administrators, userId, cancellationToken))
                {
                    continue;
                }

                recipients.Add(new NoticeRecipient(userId, person.Name, person.Email));
            }

            if (recipients.Count == 0)
            {
                continue;
            }

            var actorName = notice.ActorId is { } actorId && people.TryGetValue(actorId, out var actor) ? actor.Name : null;
            foreach (var channel in channels)
            {
                await channel.DeliverAsync(notice, recipients, actorName, cancellationToken);
            }
        }

        await store.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsAdministratorAsync(Dictionary<Guid, bool> known, Guid userId, CancellationToken cancellationToken)
    {
        if (!known.TryGetValue(userId, out var administrator))
        {
            administrator = await access.FindAsync(userId, cancellationToken) is { IsActive: true, Role: SystemRole.Administrator };
            known[userId] = administrator;
        }

        return administrator;
    }
}

/// <summary>
/// The e-mail channel (ADR-0037): what asks for an action of the person goes to the queue now; the rest waits for the
/// daily summary of the person, one item per item when the notice has a key.
/// </summary>
public sealed class EmailChannel(EmailQueue queue, IDigestStore digest, TimeProvider clock) : INoticeChannel
{
    /// <summary>The kinds that go now; the others go to the daily summary.</summary>
    public static readonly IReadOnlySet<NoticeKind> Immediate = new HashSet<NoticeKind>
    {
        NoticeKind.TicketOpened,
        NoticeKind.TicketMessage,
        NoticeKind.TicketMentioned,
        NoticeKind.TicketInformationRequested,
        NoticeKind.TicketResolved,
        NoticeKind.TicketAssigned,
        NoticeKind.TicketRequesterChanged,
        NoticeKind.TicketTransferred,
        NoticeKind.TicketDueSoon,
        NoticeKind.TicketOverdue,
        NoticeKind.AcceptanceRequested,
        NoticeKind.AcceptanceReminded,
    };

    public async Task DeliverAsync(
        Notice notice, IReadOnlyList<NoticeRecipient> recipients, string? actorName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notice);
        ArgumentNullException.ThrowIfNull(recipients);
        if (Immediate.Contains(notice.Kind))
        {
            foreach (var person in recipients)
            {
                await queue.EnqueueAsync(
                    notice.Kind.ToString(), person.Email, person.Name, NoticeEmails.ContentOf(notice, person.Name, actorName), cancellationToken);
            }

            return;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var values = JsonSerializer.Serialize(notice.Values);
        foreach (var person in recipients)
        {
            if (notice.Key is { } key && await digest.FindAsync(person.UserId, key, cancellationToken) is { } waiting)
            {
                waiting.Replace(notice.Kind, notice.ActorId, notice.Path, values, now);
                continue;
            }

            digest.Add(new DigestItem(person.UserId, notice.Kind, notice.ActorId, notice.Path, values, notice.Key, now));
        }
    }
}

/// <summary>The texts of the e-mail of each kind of notice, as keys of the texts of the e-mails (ADR-0013).</summary>
public static class NoticeEmails
{
    public static EmailContent ContentOf(Notice notice, string recipientName, string? actorName)
    {
        ArgumentNullException.ThrowIfNull(notice);
        var kind = KeyOf(notice.Kind);
        var values = new Dictionary<string, string>(notice.Values)
        {
            ["name"] = recipientName,
            ["actor"] = actorName ?? string.Empty,
        };
        return new EmailContent(
            $"notices.{kind}.subject",
            ["notices.greeting", $"notices.{kind}.body"],
            values,
            $"notices.{kind}.action",
            notice.Path);
    }

    /// <summary>The key of the kind in the texts: TicketOpened is ticketOpened.</summary>
    public static string KeyOf(NoticeKind kind)
    {
        var name = kind.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
