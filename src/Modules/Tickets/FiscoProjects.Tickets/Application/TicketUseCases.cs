using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Tickets.Application;

/// <summary>The lists of tickets (ADR-0029, ADR-0068).</summary>
public enum TicketView
{
    /// <summary>Every ticket the person sees.</summary>
    All = 1,

    /// <summary>The tickets the person is in charge of.</summary>
    AssignedToMe = 2,

    /// <summary>The tickets without an assignee of the teams of the person (<see cref="TicketAccess.TeamsOf"/>).</summary>
    UnassignedInMyTeams = 3,

    /// <summary>The tickets without an assignee, as the tab of a project shows them.</summary>
    Unassigned = 4,

    /// <summary>The tickets the person opened.</summary>
    OpenedByMe = 5,

    /// <summary>The tickets the person follows (ADR-0026).</summary>
    Following = 6,

    /// <summary>The tickets with a ticket registered at a vendor (ADR-0028).</summary>
    WithVendor = 7,
}

/// <summary>The order of a list: by the priority, the highest first and the oldest first in each, or by the number.</summary>
public enum TicketSort
{
    Priority = 1,
    Number = 2,
}

/// <summary>A page of a list of tickets, filtered.</summary>
public sealed record TicketQuery(
    TicketView View,
    Guid? ProjectId,
    TicketTeam? Team,
    TicketSituation Situation,
    string? Search,
    TicketSort Sort,
    bool Descending,
    int Page,
    int PageSize,
    Guid? PersonId = null)
{
    public const int MaxPageSize = 100;
}

/// <summary>
/// The tickets a person sees and the teams of the person in each project (ADR-0044, ADR-0068): the tickets of all
/// projects for the Administrator, of the projects of the person for everyone else.
/// </summary>
public sealed record TicketScope(
    Guid UserId,
    IReadOnlyList<Guid> ProjectIds,
    IReadOnlyList<Guid> BothTeamsProjects,
    IReadOnlyList<Guid> FunctionalProjects,
    IReadOnlyList<Guid> DevelopmentProjects)
{
    /// <summary>The scope of the person, from the projects the person sees.</summary>
    public static TicketScope Of(TicketViewer viewer, IReadOnlyList<ProjectAccess> projects)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(projects);
        List<Guid> TeamProjects(Func<IReadOnlyList<TicketTeam>, bool> teams) =>
            [.. projects.Where(project => teams(TicketAccess.TeamsOf(project.Role))).Select(project => project.Id)];
        return new TicketScope(
            viewer.UserId,
            [.. projects.Select(project => project.Id)],
            TeamProjects(teams => teams.Count == 2),
            TeamProjects(teams => teams is [TicketTeam.Functional]),
            TeamProjects(teams => teams is [TicketTeam.Development]));
    }
}

/// <summary>A page of tickets and how many there are in the list.</summary>
public sealed record TicketRows(IReadOnlyList<Ticket> Items, int Total);

/// <summary>
/// How many tickets each list has: all of them, any status, and the open ones (<see cref="Ticket.OpenStatuses"/>), the
/// open ones without an assignee, without an assignee in the teams of the person, in charge of the person, opened by
/// the person, followed by the person and with a ticket at a vendor.
/// </summary>
public sealed record TicketCounts(
    int All, int Open, int Unassigned, int UnassignedInMyTeams, int AssignedToMe, int OpenedByMe, int Following, int WithVendor);

/// <summary>Where the tickets are kept.</summary>
public interface ITicketStore
{
    /// <summary>The next number of the installation, never given twice (ADR-0029).</summary>
    Task<int> NextNumberAsync(CancellationToken cancellationToken);

    void Add(Ticket ticket);

    /// <summary>Saves; throws <see cref="TicketChangedException"/> when someone changed the ticket since it was read.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<TicketRows> ListAsync(TicketQuery query, TicketScope scope, CancellationToken cancellationToken);

    /// <summary>
    /// The open tickets of the person for Meu trabalho (ADR-0096): in charge of, opened by or followed by the person, or
    /// without an assignee in the teams of the person, with the ones the person follows.
    /// </summary>
    Task<TicketWork> WorkOfAsync(TicketScope scope, CancellationToken cancellationToken);

    /// <summary>The counts of the lists of the person, in one project or in all the person sees.</summary>
    Task<TicketCounts> CountAsync(TicketScope scope, Guid? projectId, CancellationToken cancellationToken);

    Task<Ticket?> FindAsync(int number, CancellationToken cancellationToken);

    /// <summary>The tickets of the numbers, tracked, in any order; the missing ones are left out.</summary>
    Task<IReadOnlyList<Ticket>> FindManyAsync(IReadOnlyCollection<int> numbers, CancellationToken cancellationToken);

    /// <summary>The tickets of a project that are not closed, tracked.</summary>
    Task<IReadOnlyList<Ticket>> NotClosedAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>How many tickets of a project are not closed.</summary>
    Task<int> CountNotClosedAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The open tickets of a project with the person as the assignee, tracked.</summary>
    Task<IReadOnlyList<Ticket>> AssignedOpenAsync(Guid projectId, Guid userId, CancellationToken cancellationToken);

    /// <summary>How many messages and events each ticket has: its interactions in the lists.</summary>
    Task<IReadOnlyDictionary<Guid, int>> MessageCountsAsync(IReadOnlyCollection<Guid> ticketIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<TicketMessage>> MessagesAsync(Guid ticketId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TicketAttachment>> AttachmentsAsync(Guid ticketId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> FollowersAsync(Guid ticketId, CancellationToken cancellationToken);

    Task<bool> HasMessagesAsync(Guid ticketId, CancellationToken cancellationToken);

    void AddMessage(TicketMessage message);

    /// <summary>The flow of the tickets, read from its table and checked (ADR-0024).</summary>
    Task<TicketFlow> FlowAsync(CancellationToken cancellationToken);

    void AddEvent(TicketEvent ticketEvent);

    /// <summary>The deadlines of each priority (ADR-0025), tracked so the Administrator changes them.</summary>
    Task<IReadOnlyList<TicketDeadlineSetting>> DeadlinesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<TicketEvent>> EventsAsync(Guid ticketId, CancellationToken cancellationToken);

    /// <summary>The resolved tickets, to be changed: the automatic closing looks at them.</summary>
    Task<IReadOnlyList<Ticket>> ResolvedAsync(CancellationToken cancellationToken);

    /// <summary>The open tickets with deadlines: the alerts of the deadlines look at them.</summary>
    Task<IReadOnlyList<Ticket>> OpenWithDeadlinesAsync(CancellationToken cancellationToken);

    /// <summary>The alerts told of the deadlines of the tickets, tracked.</summary>
    Task<IReadOnlyList<TicketDeadlineAlert>> AlertsAsync(IReadOnlyCollection<Guid> ticketIds, CancellationToken cancellationToken);

    void AddAlert(TicketDeadlineAlert alert);

    void AddAttachment(TicketAttachment attachment);

    /// <summary>The person starts to follow the ticket, unless already following it.</summary>
    Task FollowAsync(Guid ticketId, Guid userId, DateTime since, CancellationToken cancellationToken);

    /// <summary>The person stops following the ticket; nothing when not following it.</summary>
    Task UnfollowAsync(Guid ticketId, Guid userId, CancellationToken cancellationToken);
}

/// <summary>A person as a ticket shows it.</summary>
public sealed record TicketPerson(Guid Id, string Name);

/// <summary>The project of a ticket.</summary>
public sealed record TicketProject(Guid Id, string Code, string Name);

/// <summary>A ticket of a list, with its project, the names of the requester and the assignee and its messages.</summary>
public sealed record TicketListItem(
    Ticket Ticket, TicketProject Project, TicketPerson Requester, TicketPerson? Assignee, int Interactions)
{
    /// <summary>The deadlines of the ticket today (ADR-0025).</summary>
    public TicketDeadlineFigures? Deadlines { get; init; }

    /// <summary>The person may change the assignee, also with others at once (ADR-0068).</summary>
    public bool CanChangeAssignee { get; init; }

    /// <summary>The person may change the requester with others at once (ADR-0027).</summary>
    public bool CanChangeRequester { get; init; }

    /// <summary>The ticket at a vendor the list shows: the open one registered last, or the last one (ADR-0028).</summary>
    public TicketVendorTicket? Vendor { get; init; }
}

public sealed record TicketPage(IReadOnlyList<TicketListItem> Items, int Total);

/// <summary>
/// Someone else changed the ticket between reading and saving it, as two first answers at once or a reopening while
/// the automatic closing runs: nothing was saved, and the person tries again on the ticket as it is now.
/// </summary>
public sealed class TicketChangedException(Exception inner) : InvalidOperationException("The ticket changed since it was read.", inner);

/// <summary>Lists the tickets the person sees (ADR-0044), filtered (ADR-0029).</summary>
public sealed class ListTickets(
    IProjectAccess projects,
    ITicketStore store,
    IUserDirectory users,
    TicketDeadlines deadlines,
    IVendorStore vendors,
    VendorStaleSettings stale)
{
    public async Task<TicketPage> ExecuteAsync(TicketQuery query, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(viewer);
        var visible = await projects.VisibleAsync(viewer.UserId, viewer.IsAdministrator, cancellationToken);
        var rows = await store.ListAsync(query, TicketScope.Of(viewer, visible), cancellationToken);
        var interactions = await store.MessageCountsAsync([.. rows.Items.Select(ticket => ticket.Id)], cancellationToken);
        var items = await TicketNames.ItemsOfAsync(rows.Items, visible, interactions, users, cancellationToken);
        var day = await deadlines.TodayAsync(cancellationToken);
        var roles = visible.ToDictionary(project => project.Id, project => project.Role);
        var atVendors = VendorTickets.ShownIn(
            await vendors.TicketsOfAsync([.. rows.Items.Select(ticket => ticket.Id)], cancellationToken));
        return new TicketPage(
            [.. items.Select(item =>
            {
                var manages = TicketAccess.MayAssign(viewer, roles[item.Ticket.ProjectId]);
                return item with
                {
                    Deadlines = TicketDeadlines.FiguresOf(item.Ticket, day),
                    CanChangeAssignee = manages && item.Ticket.IsOpen,
                    CanChangeRequester = manages && item.Ticket.Status != TicketStatus.Closed,
                    Vendor = atVendors.TryGetValue(item.Ticket.Id, out var vendor) ? VendorTickets.Of(vendor, day, stale) : null,
                };
            })],
            rows.Total);
    }
}

/// <summary>How many tickets each list of the person has, in all the projects the person sees or in one of them.</summary>
public sealed class CountTickets(IProjectAccess projects, ITicketStore store)
{
    /// <summary>The counts; null for a project the person does not see.</summary>
    public async Task<TicketCounts?> ExecuteAsync(Guid? projectId, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var visible = await projects.VisibleAsync(viewer.UserId, viewer.IsAdministrator, cancellationToken);
        if (projectId is { } id && visible.All(project => project.Id != id))
        {
            return null;
        }

        return await store.CountAsync(TicketScope.Of(viewer, visible), projectId, cancellationToken);
    }
}

/// <summary>
/// A project of the filters and of the opening: whether it is closed, whether the person may open tickets in it and the
/// teams with someone to serve them (ADR-0068).
/// </summary>
public sealed record TicketProjectChoice(
    Guid Id, string Code, string Name, bool IsClosed, bool CanOpen, IReadOnlyList<TicketTeam> Teams);

/// <summary>The projects the person sees, for the filter of the lists and for opening tickets.</summary>
public sealed class ListTicketChoices(IProjectAccess projects)
{
    public async Task<IReadOnlyList<TicketProjectChoice>> ExecuteAsync(TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var visible = await projects.VisibleAsync(viewer.UserId, viewer.IsAdministrator, cancellationToken);
        var members = (await projects.MembersAsync([.. visible.Select(project => project.Id)], cancellationToken))
            .ToLookup(member => member.ProjectId, member => member.Role);
        return [.. visible.Select(project =>
        {
            var canOpen = !project.IsClosed && TicketAccess.MayOpen(viewer, project);
            return new TicketProjectChoice(
                project.Id,
                project.Code,
                project.Name,
                project.IsClosed,
                canOpen,
                canOpen ? TicketAccess.TeamsServedBy(members[project.Id]) : []);
        })];
    }
}

/// <summary>
/// A new ticket as the person fills it in: the description with its formatting (<c>DescriptionBody</c>, the JSON of the
/// editor) or as plain text.
/// </summary>
public sealed record NewTicket(
    Guid? ProjectId, TicketTeam? Team, TicketPriority? Priority, string? Title, string? Description, string? DescriptionBody = null);

public enum OpenTicketOutcome
{
    Opened = 1,
    Invalid = 2,
    NotFound = 3,
    Forbidden = 4,
    Closed = 5,
}

/// <summary>What happened to a new ticket: its id and number, or why it was not opened.</summary>
public sealed record OpenTicketResult(OpenTicketOutcome Outcome, Guid? Id, int? Number, IReadOnlyList<TicketField> Invalid)
{
    public static OpenTicketResult Of(OpenTicketOutcome outcome) => new(outcome, null, null, []);

    public static OpenTicketResult InvalidFields(IReadOnlyList<TicketField> invalid) => new(OpenTicketOutcome.Invalid, null, null, invalid);
}

/// <summary>
/// Opens a ticket in a project (ADR-0022, ADR-0068): Open, without an assignee, with the person as the requester, for a
/// team with someone to serve it. Who is not in the project gets 404, the viewer 403 (ADR-0044), and a completed or
/// cancelled project 409, until it is reopened.
/// </summary>
public sealed class OpenTicket(
    IProjectAccess projects, ITicketStore store, TicketDeadlines deadlines, TimeProvider clock, TicketNotices notices)
{
    public async Task<OpenTicketResult> ExecuteAsync(NewTicket ticket, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(viewer);
        var form = TicketForm.Of(ticket.Team, ticket.Priority, ticket.Title, ticket.Description, ticket.DescriptionBody);
        if (ticket.ProjectId is not { } projectId)
        {
            return OpenTicketResult.InvalidFields([TicketField.Project, .. form.Invalid]);
        }

        var project = await projects.FindAsync(projectId, viewer.UserId, viewer.IsAdministrator, cancellationToken);
        if (project is null)
        {
            return OpenTicketResult.Of(OpenTicketOutcome.NotFound);
        }

        if (!TicketAccess.MayOpen(viewer, project))
        {
            return OpenTicketResult.Of(OpenTicketOutcome.Forbidden);
        }

        if (project.IsClosed)
        {
            return OpenTicketResult.Of(OpenTicketOutcome.Closed);
        }

        var invalid = form.Invalid.ToList();
        if (form.Team is { } team)
        {
            var roles = (await projects.MembersAsync([projectId], cancellationToken)).Select(member => member.Role);
            if (!TicketAccess.TeamsServedBy(roles).Contains(team))
            {
                // A team without anyone to serve it takes no tickets (ADR-0068).
                invalid.Insert(0, TicketField.Team);
            }
        }

        if (invalid.Count > 0)
        {
            return OpenTicketResult.InvalidFields(invalid);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var opened = Ticket.Open(await store.NextNumberAsync(cancellationToken), projectId, form, viewer.UserId, now);
        await deadlines.StartAsync(opened, cancellationToken);
        store.Add(opened);
        // The requester follows the ticket from the start (ADR-0026).
        await store.FollowAsync(opened.Id, viewer.UserId, now, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        // Who serves the team hears about it now (ADR-0037).
        await notices.OpenedAsync(opened, project, viewer.UserId, cancellationToken);
        return new OpenTicketResult(OpenTicketOutcome.Opened, opened.Id, opened.Number, []);
    }
}

/// <summary>A person of the conversation and the part the person plays in the ticket (ADR-0069).</summary>
public sealed record TicketParticipant(Guid Id, string Name, TicketParticipantRole Role);

/// <summary>
/// What the person may do in the conversation: send messages, attach files to the description, and the actions of the
/// flow open now (ADR-0024).
/// </summary>
public sealed record TicketConversationPermissions(
    bool CanSend, bool CanAttachToDescription, IReadOnlyList<TicketTransition> Actions, bool CanChangePriority)
{
    /// <summary>The assignee, by the Administrator and the managers (ADR-0068).</summary>
    public bool CanChangeAssignee { get; init; }

    /// <summary>The requester, by the Administrator, the managers and the requester (ADR-0027).</summary>
    public bool CanChangeRequester { get; init; }

    /// <summary>To the other team, by who serves it (ADR-0044).</summary>
    public bool CanTransfer { get; init; }

    /// <summary>Registers and updates tickets at vendors, by who serves it, until it is closed (ADR-0028).</summary>
    public bool CanRegisterVendorTicket { get; init; }
}

/// <summary>
/// A ticket with its project, the requester, the assignee and the managers and tech leads of the project, and its
/// conversation (ADR-0026, ADR-0069): the messages, the attachments, who follows it, who may be mentioned, the people
/// who appear in it with their parts, and what the person may do.
/// </summary>
public sealed record TicketDetails(
    Ticket Ticket,
    TicketProject Project,
    TicketPerson Requester,
    TicketPerson? Assignee,
    IReadOnlyList<TicketPerson> Managers,
    IReadOnlyList<TicketPerson> TechLeads,
    IReadOnlyList<TicketMessage> Messages,
    IReadOnlyList<TicketEvent> Events,
    IReadOnlyList<TicketAttachment> Attachments,
    IReadOnlyList<Guid> Followers,
    IReadOnlyList<Guid> Mentionable,
    IReadOnlyList<TicketParticipant> Participants,
    TicketConversationPermissions Permissions,
    bool Following)
{
    /// <summary>The deadlines of the ticket today (ADR-0025).</summary>
    public TicketDeadlineFigures? Deadlines { get; init; }

    /// <summary>The tickets registered at vendors, in the order they were registered (ADR-0028).</summary>
    public IReadOnlyList<TicketVendorTicket> VendorTickets { get; init; } = [];
}

/// <summary>A ticket by its number, for everyone in its project and the Administrator (ADR-0044); null for anyone else.</summary>
public sealed class GetTicket(
    IProjectAccess projects,
    ITicketStore store,
    IUserDirectory users,
    TicketDeadlines deadlines,
    IVendorStore vendors,
    VendorStaleSettings stale)
{
    public async Task<TicketDetails?> ExecuteAsync(int number, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var ticket = await store.FindAsync(number, cancellationToken);
        var project = ticket is null
            ? null
            : await projects.FindAsync(ticket.ProjectId, viewer.UserId, viewer.IsAdministrator, cancellationToken);
        if (ticket is null || project is null)
        {
            return null;
        }

        var members = await projects.MembersAsync([project.Id], cancellationToken);
        var roles = members.ToDictionary(member => member.UserId, member => member.Role);
        var managers = members.Where(member => member.Role == ProjectMemberRole.Manager).Select(member => member.UserId).ToList();
        var techLeads = members.Where(member => member.Role == ProjectMemberRole.TechLead).Select(member => member.UserId).ToList();
        var messages = await store.MessagesAsync(ticket.Id, cancellationToken);
        var events = await store.EventsAsync(ticket.Id, cancellationToken);
        var attachments = await store.AttachmentsAsync(ticket.Id, cancellationToken);
        var followers = await store.FollowersAsync(ticket.Id, cancellationToken);
        var atVendors = await vendors.TicketsOfAsync([ticket.Id], cancellationToken);
        var mentionable = TicketAccess.Mentionable(ticket, members);
        List<Guid> people =
        [
            ticket.RequesterId,
            .. ticket.AssigneeId is { } assignee ? [assignee] : Array.Empty<Guid>(),
            .. managers,
            .. techLeads,
            .. messages.Select(message => message.AuthorId),
            .. events.Where(item => item.ActorId is not null).Select(item => item.ActorId!.Value),
            .. followers,
            .. mentionable,
            .. atVendors.Select(row => row.Record.RegisteredById),
            viewer.UserId,
        ];
        var names = await users.NamesAsync([.. people.Distinct()], cancellationToken);
        var everyone = people.Distinct().Where(names.ContainsKey).ToList();
        // The Administrator is told apart only for whom the session says it is; the others are known by the project.
        var participants = everyone.Select(id => new TicketParticipant(
                id,
                names[id],
                TicketAccess.RoleIn(
                    ticket,
                    id,
                    roles.TryGetValue(id, out var role) ? role : null,
                    id == viewer.UserId && viewer.IsAdministrator)))
            .ToList();
        var canSend = TicketAccess.MaySend(viewer, project.Role, ticket) && ticket.Status != TicketStatus.Closed;
        var flow = await store.FlowAsync(cancellationToken);
        var actors = TicketAccess.ActorsOf(viewer, project.Role, ticket);
        var day = await deadlines.TodayAsync(cancellationToken);
        return new TicketDetails(
            ticket,
            new TicketProject(project.Id, project.Code, project.Name),
            TicketNames.PersonOf(ticket.RequesterId, names),
            ticket.AssigneeId is { } assigneeId ? TicketNames.PersonOf(assigneeId, names) : null,
            [.. managers.Select(id => TicketNames.PersonOf(id, names)).OrderBy(person => person.Name, StringComparer.CurrentCulture)],
            [.. techLeads.Select(id => TicketNames.PersonOf(id, names)).OrderBy(person => person.Name, StringComparer.CurrentCulture)],
            messages,
            events,
            attachments,
            followers,
            mentionable,
            participants,
            new TicketConversationPermissions(
                canSend,
                ticket.RequesterId == viewer.UserId && messages.Count == 0 && ticket.Status != TicketStatus.Closed,
                flow.AvailableTo(ticket.Status, actors),
                ticket.IsOpen && (actors & TicketActors.Attendant) != 0)
            {
                CanChangeAssignee = ticket.IsOpen && TicketAccess.MayAssign(viewer, project.Role),
                CanChangeRequester = ticket.Status != TicketStatus.Closed
                    && TicketAccess.MayChangeRequester(viewer, project.Role, ticket, inBulk: false),
                CanTransfer = ticket.IsOpen && TicketAccess.MayTransfer(viewer, project.Role, ticket),
                CanRegisterVendorTicket = ticket.Status != TicketStatus.Closed
                    && TicketAccess.MayRegisterAtVendor(viewer, project.Role, ticket),
            },
            followers.Contains(viewer.UserId))
        {
            Deadlines = TicketDeadlines.FiguresOf(ticket, day),
            VendorTickets = [.. atVendors.Select(row => VendorTickets.Of(row, day, stale))],
        };
    }
}

/// <summary>The names of the people and of the projects of tickets.</summary>
internal static class TicketNames
{
    public static TicketPerson PersonOf(Guid id, IReadOnlyDictionary<Guid, string> names) =>
        new(id, names.TryGetValue(id, out var name) ? name : string.Empty);

    public static async Task<IReadOnlyList<TicketListItem>> ItemsOfAsync(
        IReadOnlyList<Ticket> tickets,
        IReadOnlyList<ProjectAccess> projects,
        IReadOnlyDictionary<Guid, int> interactions,
        IUserDirectory users,
        CancellationToken cancellationToken)
    {
        var byId = projects.ToDictionary(project => project.Id);
        var names = await users.NamesAsync(
            [.. tickets.SelectMany(ticket => ticket.AssigneeId is { } assignee ? [ticket.RequesterId, assignee] : new[] { ticket.RequesterId }).Distinct()],
            cancellationToken);
        return [.. tickets.Select(ticket =>
        {
            var project = byId[ticket.ProjectId];
            return new TicketListItem(
                ticket,
                new TicketProject(project.Id, project.Code, project.Name),
                PersonOf(ticket.RequesterId, names),
                ticket.AssigneeId is { } assignee ? PersonOf(assignee, names) : null,
                interactions.TryGetValue(ticket.Id, out var count) ? count : 0);
        })];
    }
}
