using System.Globalization;
using FiscoProjects.Notifications.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Domain;

namespace FiscoProjects.Tickets.Application;

/// <summary>
/// What the tickets tell the notices (ADR-0037, ADR-0038), after the change is saved. One action gives each person one
/// notice at most, the most specific first: the mention, the request for information or the resolution, the message,
/// and only then the change of status, for the daily summary of who follows the ticket.
/// </summary>
public sealed class TicketNotices(IEnumerable<INoticeBoard> boards, ITicketStore store, IProjectAccess projects)
{
    /// <summary>A new ticket, for who serves its team (ADR-0068).</summary>
    public async Task OpenedAsync(Ticket ticket, ProjectAccess project, Guid actorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(project);
        var servers = await ServersAsync(ticket.ProjectId, ticket.Team, cancellationToken);
        await boards.PostAsync([NoticeOf(NoticeKind.TicketOpened, ticket, project.Code, servers, actorId)], cancellationToken);
    }

    /// <summary>
    /// A message, also of an action of the flow: the mentioned people, the requester asked for information or told of
    /// the resolution, and who follows the ticket.
    /// </summary>
    public async Task MessageAsync(
        Ticket ticket,
        ProjectAccess project,
        Guid actorId,
        IReadOnlyCollection<Guid> mentioned,
        TicketTransition? transition,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(mentioned);
        var told = new HashSet<Guid> { actorId };
        var notices = new List<Notice>();
        Add(notices, told, NoticeOf(NoticeKind.TicketMentioned, ticket, project.Code, mentioned, actorId));
        if (RequesterKindOf(transition) is { } kind)
        {
            Add(notices, told, NoticeOf(kind, ticket, project.Code, [ticket.RequesterId], actorId));
        }

        Add(notices, told, NoticeOf(NoticeKind.TicketMessage, ticket, project.Code, await InvolvedAsync(ticket, cancellationToken), actorId));
        await boards.PostAsync(notices, cancellationToken);
    }

    /// <summary>
    /// A change of status without a message: the requester of a resolved ticket now; who follows it, in the daily
    /// summary. The actor is null for the system, as the automatic closing.
    /// </summary>
    public async Task StatusChangedAsync(
        IReadOnlyCollection<(Ticket Ticket, TicketStatus From)> changes, Guid? actorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(changes);
        if (changes.Count == 0)
        {
            return;
        }

        var codes = await CodesAsync(changes.Select(change => change.Ticket.ProjectId), cancellationToken);
        var notices = new List<Notice>();
        foreach (var (ticket, from) in changes)
        {
            var told = new HashSet<Guid>();
            if (actorId is { } actor)
            {
                told.Add(actor);
            }

            var code = codes.GetValueOrDefault(ticket.ProjectId, string.Empty);
            if (ticket.Status == TicketStatus.Resolved)
            {
                Add(notices, told, NoticeOf(NoticeKind.TicketResolved, ticket, code, [ticket.RequesterId], actorId));
            }

            var values = ValuesOf(ticket, code);
            values[NoticeValues.From] = from.ToString();
            values[NoticeValues.To] = ticket.Status.ToString();
            Add(notices, told, new Notice(
                NoticeKind.TicketStatusChanged, await InvolvedAsync(ticket, cancellationToken), actorId, ticket.ProjectId, PathOf(ticket), values));
        }

        await boards.PostAsync(notices, cancellationToken);
    }

    /// <summary>The tickets closed because their project was cancelled (ADR-0043), for the daily summary of who follows them.</summary>
    public async Task ClosedWithProjectAsync(
        IReadOnlyCollection<(Ticket Ticket, TicketStatus From)> closed, Guid actorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(closed);
        var codes = await CodesAsync(closed.Select(item => item.Ticket.ProjectId), cancellationToken);
        var notices = new List<Notice>();
        foreach (var (ticket, from) in closed)
        {
            var values = ValuesOf(ticket, codes.GetValueOrDefault(ticket.ProjectId, string.Empty));
            values[NoticeValues.From] = from.ToString();
            values[NoticeValues.To] = ticket.Status.ToString();
            values[NoticeValues.ClosedWithProject] = "true";
            notices.Add(new Notice(
                NoticeKind.TicketStatusChanged, await InvolvedAsync(ticket, cancellationToken), actorId, ticket.ProjectId, PathOf(ticket), values));
        }

        await boards.PostAsync(notices, cancellationToken);
    }

    /// <summary>The new assignee of each ticket.</summary>
    public Task AssignedAsync(IReadOnlyCollection<TicketInProject> tickets, Guid actorId, CancellationToken cancellationToken) =>
        boards.PostAsync(
            [.. tickets.Where(item => item.Ticket.AssigneeId is not null)
                .Select(item => NoticeOf(NoticeKind.TicketAssigned, item.Ticket, item.Project.Code, [item.Ticket.AssigneeId!.Value], actorId))],
            cancellationToken);

    /// <summary>The new requester of each ticket.</summary>
    public Task RequesterChangedAsync(IReadOnlyCollection<TicketInProject> tickets, Guid actorId, CancellationToken cancellationToken) =>
        boards.PostAsync(
            [.. tickets.Select(item => NoticeOf(NoticeKind.TicketRequesterChanged, item.Ticket, item.Project.Code, [item.Ticket.RequesterId], actorId))],
            cancellationToken);

    /// <summary>A ticket that went to the other team, for who serves it.</summary>
    public async Task TransferredAsync(Ticket ticket, ProjectAccess project, Guid actorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(project);
        var servers = await ServersAsync(ticket.ProjectId, ticket.Team, cancellationToken);
        await boards.PostAsync([NoticeOf(NoticeKind.TicketTransferred, ticket, project.Code, servers, actorId)], cancellationToken);
    }

    /// <summary>A ticket at a vendor registered or updated, for the daily summary of who follows the ticket (ADR-0028).</summary>
    public async Task VendorUpdatedAsync(
        Ticket ticket, string vendor, VendorTicketStatus status, Guid actorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        var codes = await CodesAsync([ticket.ProjectId], cancellationToken);
        var values = ValuesOf(ticket, codes.GetValueOrDefault(ticket.ProjectId, string.Empty));
        values[NoticeValues.Vendor] = vendor;
        values[NoticeValues.VendorStatus] = status.ToString();
        await boards.PostAsync(
            [new Notice(NoticeKind.VendorTicketUpdated, await InvolvedAsync(ticket, cancellationToken), actorId, ticket.ProjectId, PathOf(ticket), values)],
            cancellationToken);
    }

    /// <summary>
    /// A deadline due soon or overdue (ADR-0025), for the assignee or, without one, for who serves the team of the
    /// ticket.
    /// </summary>
    public async Task DueAsync(
        IReadOnlyCollection<TicketDueAlert> alerts, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alerts);
        if (alerts.Count == 0)
        {
            return;
        }

        var codes = await CodesAsync(alerts.Select(alert => alert.Ticket.ProjectId), cancellationToken);
        var notices = new List<Notice>();
        foreach (var alert in alerts)
        {
            var ticket = alert.Ticket;
            IReadOnlyCollection<Guid> recipients = ticket.AssigneeId is { } assignee
                ? [assignee]
                : await ServersAsync(ticket.ProjectId, ticket.Team, cancellationToken);
            var values = ValuesOf(ticket, codes.GetValueOrDefault(ticket.ProjectId, string.Empty));
            values[NoticeValues.Deadline] = alert.Deadline.ToString();
            values[NoticeValues.DueOn] = alert.DueOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            notices.Add(new Notice(
                alert.State == TicketDueState.Overdue ? NoticeKind.TicketOverdue : NoticeKind.TicketDueSoon,
                recipients,
                null,
                ticket.ProjectId,
                PathOf(ticket),
                values));
        }

        await boards.PostAsync(notices, cancellationToken);
    }

    private static NoticeKind? RequesterKindOf(TicketTransition? transition) => transition?.Action switch
    {
        TicketAction.AskForInformation => NoticeKind.TicketInformationRequested,
        TicketAction.Resolve => NoticeKind.TicketResolved,
        _ => null,
    };

    /// <summary>Adds the notice for who was not told of the action yet.</summary>
    private static void Add(List<Notice> notices, HashSet<Guid> told, Notice notice)
    {
        var recipients = notice.Recipients.Where(person => !told.Contains(person)).Distinct().ToList();
        if (recipients.Count == 0)
        {
            return;
        }

        told.UnionWith(recipients);
        notices.Add(notice with { Recipients = recipients });
    }

    private static Notice NoticeOf(NoticeKind kind, Ticket ticket, string projectCode, IReadOnlyCollection<Guid> recipients, Guid? actorId) =>
        new(kind, recipients, actorId, ticket.ProjectId, PathOf(ticket), ValuesOf(ticket, projectCode));

    private static string PathOf(Ticket ticket) => $"/tickets/{ticket.Number}";

    private static Dictionary<string, string> ValuesOf(Ticket ticket, string projectCode) => new()
    {
        [NoticeValues.Number] = ticket.Number.ToString(CultureInfo.InvariantCulture),
        [NoticeValues.Title] = ticket.Title,
        [NoticeValues.Project] = projectCode,
        [NoticeValues.Team] = ticket.Team.ToString(),
    };

    /// <summary>Who follows the ticket (ADR-0026), with the requester and the assignee.</summary>
    private async Task<IReadOnlyCollection<Guid>> InvolvedAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        var people = new HashSet<Guid>(await store.FollowersAsync(ticket.Id, cancellationToken)) { ticket.RequesterId };
        if (ticket.AssigneeId is { } assignee)
        {
            people.Add(assignee);
        }

        return people;
    }

    /// <summary>Who serves the team in the project (ADR-0068).</summary>
    private async Task<IReadOnlyCollection<Guid>> ServersAsync(Guid projectId, TicketTeam team, CancellationToken cancellationToken) =>
        [.. (await projects.MembersAsync([projectId], cancellationToken))
            .Where(member => TicketAccess.Serves(team, member.Role))
            .Select(member => member.UserId)];

    /// <summary>The codes of the projects, as the system sees them.</summary>
    private async Task<IReadOnlyDictionary<Guid, string>> CodesAsync(IEnumerable<Guid> projectIds, CancellationToken cancellationToken)
    {
        var wanted = projectIds.ToHashSet();
        return (await projects.VisibleAsync(Guid.Empty, isAdministrator: true, cancellationToken))
            .Where(project => wanted.Contains(project.Id))
            .ToDictionary(project => project.Id, project => project.Code);
    }
}

/// <summary>A deadline of a ticket that reached a state worth a notice: due soon or overdue.</summary>
public sealed record TicketDueAlert(Ticket Ticket, TicketDeadline Deadline, TicketDueState State, DateOnly DueOn);
