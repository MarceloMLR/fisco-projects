using System.Globalization;
using System.Text.Json;
using FiscoProjects.Administration.Contracts;
using FiscoProjects.Calendar.Contracts;
using FiscoProjects.Notifications.Contracts;
using FiscoProjects.Notifications.Domain;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Notifications.Application;

/// <summary>The kinds of e-mails of the queue, kept after the content is forgotten.</summary>
public static class SummaryKinds
{
    /// <summary>The daily summary of a person (ADR-0037).</summary>
    public const string DailySummary = "DailySummary";
}

/// <summary>
/// The daily summary (ADR-0037): once the time of the installation comes, in its time zone, and only once a day, each
/// active person who did not turn it off gets one e-mail with the news since the last summary and the own tasks that end
/// from today to the second business day after; the requests of acceptance are signed at the provider, and none waits
/// for the person (ADR-0129). A summary without news is not sent. The items are removed afterwards, also of who turned the summary off, so they
/// never pile up.
/// </summary>
public sealed class SendDailySummaries(
    IDigestStore digest,
    IEnumerable<IUpcomingTasks> upcoming,
    IUserDirectory users,
    IInstallationSettings installation,
    IWorkCalendar calendar,
    EmailQueue queue,
    IOutboxStore outbox,
    TimeProvider clock)
{
    /// <summary>How many business days after today the tasks of the summary end, at most.</summary>
    public const int TaskBusinessDays = 2;

    private static readonly IReadOnlyDictionary<NoticeKind, string> Sections = new Dictionary<NoticeKind, string>
    {
        [NoticeKind.TicketStatusChanged] = "tickets",
        [NoticeKind.VendorTicketUpdated] = "tickets",
        [NoticeKind.ScheduleDatesChanged] = "schedule",
        [NoticeKind.ProjectMemberAdded] = "projects",
        [NoticeKind.ProjectRoleChanged] = "projects",
        [NoticeKind.ProjectStatusChanged] = "projects",
        [NoticeKind.AcceptanceSigned] = "acceptances",
        [NoticeKind.AcceptanceRefused] = "acceptances",
    };

    /// <summary>How many summaries were put in the queue; null when it was not the time, or the day had its summary.</summary>
    public async Task<int?> ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var info = await installation.GetAsync(cancellationToken);
        var zone = TimeZoneInfo.TryFindSystemTimeZoneById(info.TimeZone, out var found) ? found : TimeZoneInfo.Utc;
        var local = TimeZoneInfo.ConvertTimeFromUtc(now, zone);
        var today = DateOnly.FromDateTime(local);
        if (TimeOnly.FromDateTime(local) < info.DailySummaryAt)
        {
            return null;
        }

        var run = new DailySummaryRun(today, now);
        if (!await digest.TryStartDayAsync(run, cancellationToken))
        {
            return null;
        }

        var items = await digest.BeforeAsync(now, cancellationToken);
        var days = await calendar.GetAsync(cancellationToken);
        var tasks = new List<UpcomingTask>();
        foreach (var source in upcoming)
        {
            tasks.AddRange(await source.EndingAsync(today, days.AddBusinessDays(today, TaskBusinessDays), cancellationToken));
        }

        var userIds = items.Select(item => item.UserId)
            .Concat(tasks.Select(task => task.OwnerId))
            .Distinct()
            .ToList();
        var actorIds = items.Where(item => item.ActorId is not null).Select(item => item.ActorId!.Value).Distinct().ToList();
        var people = await users.PeopleAsync(userIds, cancellationToken);
        var actors = await users.NamesAsync(actorIds, cancellationToken);
        var sent = 0;
        foreach (var person in people.Where(person => person.IsActive && person.ReceivesDailySummary))
        {
            var content = ContentOf(
                person,
                [.. tasks.Where(task => task.OwnerId == person.Id).OrderBy(task => task.End).ThenBy(task => task.ProjectCode)],
                [.. items.Where(item => item.UserId == person.Id).OrderBy(item => item.CreatedAt)],
                actors);
            if (content is null)
            {
                continue;
            }

            await queue.EnqueueAsync(SummaryKinds.DailySummary, person.Email, person.Name, content, cancellationToken);
            sent++;
        }

        digest.Remove(items);
        run.Finish(sent, clock.GetUtcNow().UtcDateTime);
        await outbox.SaveChangesAsync(cancellationToken);
        return sent;
    }

    /// <summary>The summary of one person; null without news.</summary>
    private static EmailContent? ContentOf(
        PersonInfo person,
        IReadOnlyList<UpcomingTask> tasks,
        IReadOnlyList<DigestItem> items,
        IReadOnlyDictionary<Guid, string> actors)
    {
        if (tasks.Count == 0 && items.Count == 0)
        {
            return null;
        }

        var sections = new List<EmailSection>();
        if (tasks.Count > 0)
        {
            sections.Add(new EmailSection("summary.sections.tasks", [.. tasks.Select(task => new EmailLine(
                "summary.lines.task",
                new Dictionary<string, string>
                {
                    ["item"] = task.Name,
                    ["project"] = task.ProjectCode,
                    ["end"] = task.End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                },
                $"/projects/{task.ProjectId}/schedule"))]));
        }

        foreach (var section in new[] { "tickets", "schedule", "acceptances", "projects" })
        {
            var lines = items.Where(item => Sections.GetValueOrDefault(item.Kind) == section)
                .Select(item => LineOf(item, actors))
                .ToList();
            if (lines.Count > 0)
            {
                sections.Add(new EmailSection($"summary.sections.{section}", lines));
            }
        }

        return new EmailContent(
            "summary.subject",
            ["summary.greeting", "summary.intro"],
            new Dictionary<string, string> { ["name"] = person.Name },
            "summary.action",
            "/")
        {
            Sections = sections,
            NoteKey = "summary.note",
        };
    }

    private static EmailLine LineOf(DigestItem item, IReadOnlyDictionary<Guid, string> actors)
    {
        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(item.Values) ?? [];
        values["actor"] = item.ActorId is { } actorId && actors.TryGetValue(actorId, out var name) ? name : string.Empty;
        var key = item.Kind switch
        {
            NoticeKind.TicketStatusChanged when values.ContainsKey(NoticeValues.ClosedWithProject) => "ticketClosedWithProject",
            NoticeKind.TicketStatusChanged when item.ActorId is null => "ticketStatusChangedBySystem",
            NoticeKind.AcceptanceSigned when values.ContainsKey(NoticeValues.Attached) => "acceptanceAttached",
            NoticeKind.AcceptanceSigned when item.ActorId is null => "acceptanceSignedBySigners",
            NoticeKind.AcceptanceRefused when item.ActorId is null => "acceptanceRefusedBySigner",
            _ => NoticeEmails.KeyOf(item.Kind),
        };
        return new EmailLine($"summary.lines.{key}", values, item.Path);
    }
}
