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
    int PageSize)
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
/// open ones without an assignee, without an assignee in the teams of the person, in charge of the person and opened by
/// the person.
/// </summary>
public sealed record TicketCounts(int All, int Open, int Unassigned, int UnassignedInMyTeams, int AssignedToMe, int OpenedByMe);

/// <summary>Where the tickets are kept.</summary>
public interface ITicketStore
{
    /// <summary>The next number of the installation, never given twice (ADR-0029).</summary>
    Task<int> NextNumberAsync(CancellationToken cancellationToken);

    void Add(Ticket ticket);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<TicketRows> ListAsync(TicketQuery query, TicketScope scope, CancellationToken cancellationToken);

    /// <summary>The counts of the lists of the person, in one project or in all the person sees.</summary>
    Task<TicketCounts> CountAsync(TicketScope scope, Guid? projectId, CancellationToken cancellationToken);

    Task<Ticket?> FindAsync(int number, CancellationToken cancellationToken);
}

/// <summary>A person as a ticket shows it.</summary>
public sealed record TicketPerson(Guid Id, string Name);

/// <summary>The project of a ticket.</summary>
public sealed record TicketProject(Guid Id, string Code, string Name);

/// <summary>A ticket of a list, with its project and the names of the requester and the assignee.</summary>
public sealed record TicketListItem(Ticket Ticket, TicketProject Project, TicketPerson Requester, TicketPerson? Assignee);

public sealed record TicketPage(IReadOnlyList<TicketListItem> Items, int Total);

/// <summary>Lists the tickets the person sees (ADR-0044), filtered (ADR-0029).</summary>
public sealed class ListTickets(IProjectAccess projects, ITicketStore store, IUserDirectory users)
{
    public async Task<TicketPage> ExecuteAsync(TicketQuery query, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(viewer);
        var visible = await projects.VisibleAsync(viewer.UserId, viewer.IsAdministrator, cancellationToken);
        var rows = await store.ListAsync(query, TicketScope.Of(viewer, visible), cancellationToken);
        return new TicketPage(await TicketNames.ItemsOfAsync(rows.Items, visible, users, cancellationToken), rows.Total);
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

/// <summary>A new ticket as the person fills it in.</summary>
public sealed record NewTicket(Guid? ProjectId, TicketTeam? Team, TicketPriority? Priority, string? Title, string? Description);

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
public sealed class OpenTicket(IProjectAccess projects, ITicketStore store, TimeProvider clock)
{
    public async Task<OpenTicketResult> ExecuteAsync(NewTicket ticket, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(viewer);
        var form = TicketForm.Of(ticket.Team, ticket.Priority, ticket.Title, ticket.Description);
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

        var opened = Ticket.Open(
            await store.NextNumberAsync(cancellationToken), projectId, form, viewer.UserId, clock.GetUtcNow().UtcDateTime);
        store.Add(opened);
        await store.SaveChangesAsync(cancellationToken);
        return new OpenTicketResult(OpenTicketOutcome.Opened, opened.Id, opened.Number, []);
    }
}

/// <summary>A ticket with its project, the requester, the assignee and the managers and tech leads of the project.</summary>
public sealed record TicketDetails(
    Ticket Ticket,
    TicketProject Project,
    TicketPerson Requester,
    TicketPerson? Assignee,
    IReadOnlyList<TicketPerson> Managers,
    IReadOnlyList<TicketPerson> TechLeads);

/// <summary>A ticket by its number, for everyone in its project and the Administrator (ADR-0044); null for anyone else.</summary>
public sealed class GetTicket(IProjectAccess projects, ITicketStore store, IUserDirectory users)
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
        var managers = members.Where(member => member.Role == ProjectMemberRole.Manager).Select(member => member.UserId).ToList();
        var techLeads = members.Where(member => member.Role == ProjectMemberRole.TechLead).Select(member => member.UserId).ToList();
        var names = await users.NamesAsync(
            [ticket.RequesterId, .. ticket.AssigneeId is { } assignee ? [assignee] : Array.Empty<Guid>(), .. managers, .. techLeads],
            cancellationToken);
        return new TicketDetails(
            ticket,
            new TicketProject(project.Id, project.Code, project.Name),
            TicketNames.PersonOf(ticket.RequesterId, names),
            ticket.AssigneeId is { } assigneeId ? TicketNames.PersonOf(assigneeId, names) : null,
            [.. managers.Select(id => TicketNames.PersonOf(id, names)).OrderBy(person => person.Name, StringComparer.CurrentCulture)],
            [.. techLeads.Select(id => TicketNames.PersonOf(id, names)).OrderBy(person => person.Name, StringComparer.CurrentCulture)]);
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
                ticket.AssigneeId is { } assignee ? PersonOf(assignee, names) : null);
        })];
    }
}
