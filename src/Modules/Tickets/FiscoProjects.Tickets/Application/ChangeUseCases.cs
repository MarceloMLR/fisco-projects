using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Tickets.Application;

public enum TicketChangeOutcome
{
    Done = 1,
    NotFound = 2,
    Forbidden = 3,

    /// <summary>The person chosen does not serve some of the tickets, or the request is not valid.</summary>
    Invalid = 4,

    /// <summary>Some of the tickets are finished.</summary>
    NotAllowedNow = 5,

    /// <summary>Someone changed a ticket at the same time; nothing was saved.</summary>
    Changed = 6,
}

/// <summary>What a change of tickets did, and the numbers of the tickets that stopped it.</summary>
public sealed record TicketChangeResult(TicketChangeOutcome Outcome, IReadOnlyList<int> Numbers)
{
    public static readonly TicketChangeResult Done = new(TicketChangeOutcome.Done, []);

    public static TicketChangeResult Of(TicketChangeOutcome outcome, IEnumerable<int>? numbers = null) =>
        new(outcome, [.. (numbers ?? []).Order()]);
}

/// <summary>A ticket of a change, with its project as the person sees it.</summary>
public sealed record TicketInProject(Ticket Ticket, ProjectAccess Project);

/// <summary>
/// The tickets of a change of many at once (ADR-0027): all found and seen by the person, or the numbers missing; the
/// people of their projects, for the choices.
/// </summary>
public sealed class TicketBatch(IProjectAccess projects, ITicketStore store)
{
    /// <summary>The most tickets changed at once, as the largest page of the list.</summary>
    public const int MaxTickets = TicketQuery.MaxPageSize;

    public async Task<(IReadOnlyList<TicketInProject> Tickets, IReadOnlyList<int> Missing)> LoadAsync(
        IReadOnlyCollection<int> numbers, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var tickets = await store.FindManyAsync(numbers, cancellationToken);
        var visible = (await projects.VisibleAsync(viewer.UserId, viewer.IsAdministrator, cancellationToken))
            .ToDictionary(project => project.Id);
        var found = tickets
            .Where(ticket => visible.ContainsKey(ticket.ProjectId))
            .Select(ticket => new TicketInProject(ticket, visible[ticket.ProjectId]))
            .ToList();
        return (found, [.. numbers.Except(found.Select(item => item.Ticket.Number))]);
    }

    /// <summary>The role of each person in each project of the tickets.</summary>
    public async Task<IReadOnlyDictionary<(Guid ProjectId, Guid UserId), ProjectMemberRole>> RolesAsync(
        IEnumerable<TicketInProject> tickets, CancellationToken cancellationToken)
    {
        var members = await projects.MembersAsync([.. tickets.Select(item => item.Project.Id).Distinct()], cancellationToken);
        return members.ToDictionary(member => (member.ProjectId, member.UserId), member => member.Role);
    }

    public static bool IsValid(IReadOnlyCollection<int>? numbers) =>
        numbers is { Count: > 0 and <= MaxTickets } && numbers.Distinct().Count() == numbers.Count;
}

/// <summary>
/// Changes the assignee of one ticket or many (ADR-0027, ADR-0068): only the Administrator, the manager and the tech
/// lead, of open tickets, to a person of the project who serves the team of each ticket or is its manager. The new
/// assignee follows the ticket, and the conversation records the change. All or nothing.
/// </summary>
public sealed class ChangeTicketAssignees(TicketBatch batch, ITicketStore store, TimeProvider clock)
{
    public async Task<TicketChangeResult> ExecuteAsync(
        IReadOnlyCollection<int>? numbers, Guid? assigneeId, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (!TicketBatch.IsValid(numbers) || assigneeId is not { } assignee)
        {
            return TicketChangeResult.Of(TicketChangeOutcome.Invalid);
        }

        var (tickets, missing) = await batch.LoadAsync(numbers!, viewer, cancellationToken);
        if (missing.Count > 0)
        {
            return TicketChangeResult.Of(TicketChangeOutcome.NotFound, missing);
        }

        var forbidden = tickets.Where(item => !TicketAccess.MayAssign(viewer, item.Project.Role)).ToList();
        if (forbidden.Count > 0)
        {
            return TicketChangeResult.Of(TicketChangeOutcome.Forbidden, forbidden.Select(item => item.Ticket.Number));
        }

        var finished = tickets.Where(item => !item.Ticket.IsOpen).ToList();
        if (finished.Count > 0)
        {
            return TicketChangeResult.Of(TicketChangeOutcome.NotAllowedNow, finished.Select(item => item.Ticket.Number));
        }

        var roles = await batch.RolesAsync(tickets, cancellationToken);
        var refused = tickets
            .Where(item => !TicketAccess.CanBeAssignee(item.Ticket.Team, RoleOf(roles, item.Project.Id, assignee)))
            .ToList();
        if (refused.Count > 0)
        {
            return TicketChangeResult.Of(TicketChangeOutcome.Invalid, refused.Select(item => item.Ticket.Number));
        }

        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var ticket in tickets.Select(item => item.Ticket).Where(ticket => ticket.AssigneeId != assignee))
        {
            ticket.Assign(assignee, now);
            store.AddEvent(TicketEvent.AssigneeSet(ticket.Id, viewer.UserId, assignee, now));
            await store.FollowAsync(ticket.Id, assignee, now, cancellationToken);
        }

        return await TicketChanges.SaveAsync(store, cancellationToken);
    }

    internal static ProjectMemberRole? RoleOf(
        IReadOnlyDictionary<(Guid ProjectId, Guid UserId), ProjectMemberRole> roles, Guid projectId, Guid userId) =>
        roles.TryGetValue((projectId, userId), out var role) ? role : null;
}

/// <summary>
/// Changes the requester of one ticket or many (ADR-0027, ADR-0044): the Administrator and the managers, of tickets not
/// closed; the requester, of the own ticket, one at a time. The new requester is a person of the project who opens
/// tickets; the former one keeps following, and the conversation records the change. All or nothing.
/// </summary>
public sealed class ChangeTicketRequesters(TicketBatch batch, ITicketStore store, TimeProvider clock)
{
    public async Task<TicketChangeResult> ExecuteAsync(
        IReadOnlyCollection<int>? numbers, Guid? requesterId, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (!TicketBatch.IsValid(numbers) || requesterId is not { } requester)
        {
            return TicketChangeResult.Of(TicketChangeOutcome.Invalid);
        }

        var (tickets, missing) = await batch.LoadAsync(numbers!, viewer, cancellationToken);
        if (missing.Count > 0)
        {
            return TicketChangeResult.Of(TicketChangeOutcome.NotFound, missing);
        }

        var inBulk = tickets.Count > 1;
        var forbidden = tickets
            .Where(item => !TicketAccess.MayChangeRequester(viewer, item.Project.Role, item.Ticket, inBulk))
            .ToList();
        if (forbidden.Count > 0)
        {
            return TicketChangeResult.Of(TicketChangeOutcome.Forbidden, forbidden.Select(item => item.Ticket.Number));
        }

        var closed = tickets.Where(item => item.Ticket.Status == TicketStatus.Closed).ToList();
        if (closed.Count > 0)
        {
            return TicketChangeResult.Of(TicketChangeOutcome.NotAllowedNow, closed.Select(item => item.Ticket.Number));
        }

        var roles = await batch.RolesAsync(tickets, cancellationToken);
        var refused = tickets
            .Where(item => !TicketAccess.CanBeRequester(ChangeTicketAssignees.RoleOf(roles, item.Project.Id, requester)))
            .ToList();
        if (refused.Count > 0)
        {
            return TicketChangeResult.Of(TicketChangeOutcome.Invalid, refused.Select(item => item.Ticket.Number));
        }

        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var ticket in tickets.Select(item => item.Ticket).Where(ticket => ticket.RequesterId != requester))
        {
            var before = ticket.RequesterId;
            ticket.ChangeRequester(requester, now);
            store.AddEvent(TicketEvent.RequesterChanged(ticket.Id, viewer.UserId, before, requester, now));
            await store.FollowAsync(ticket.Id, before, now, cancellationToken);
            await store.FollowAsync(ticket.Id, requester, now, cancellationToken);
        }

        return await TicketChanges.SaveAsync(store, cancellationToken);
    }
}

/// <summary>
/// Who may be chosen as the assignee or as the requester of all the tickets (ADR-0027, ADR-0068), by name; the rule is
/// the one of the change, so the screen only shows it. Null when a ticket is not seen.
/// </summary>
public sealed class ListTicketPeopleChoices(TicketBatch batch, IUserDirectory users)
{
    public async Task<IReadOnlyList<TicketPerson>?> AssigneesAsync(
        IReadOnlyCollection<int>? numbers, TicketViewer viewer, CancellationToken cancellationToken) =>
        await ChoicesAsync(numbers, viewer, (ticket, role) => TicketAccess.CanBeAssignee(ticket.Team, role), cancellationToken);

    public async Task<IReadOnlyList<TicketPerson>?> RequestersAsync(
        IReadOnlyCollection<int>? numbers, TicketViewer viewer, CancellationToken cancellationToken) =>
        await ChoicesAsync(numbers, viewer, (_, role) => TicketAccess.CanBeRequester(role), cancellationToken);

    private async Task<IReadOnlyList<TicketPerson>?> ChoicesAsync(
        IReadOnlyCollection<int>? numbers,
        TicketViewer viewer,
        Func<Ticket, ProjectMemberRole?, bool> fits,
        CancellationToken cancellationToken)
    {
        if (!TicketBatch.IsValid(numbers))
        {
            return null;
        }

        var (tickets, missing) = await batch.LoadAsync(numbers!, viewer, cancellationToken);
        if (missing.Count > 0)
        {
            return null;
        }

        var roles = await batch.RolesAsync(tickets, cancellationToken);
        var people = roles.Keys.Select(key => key.UserId).Distinct()
            .Where(person => tickets.All(item => fits(item.Ticket, ChangeTicketAssignees.RoleOf(roles, item.Project.Id, person))))
            .ToList();
        var names = await users.NamesAsync(people, cancellationToken);
        return [.. people
            .Where(names.ContainsKey)
            .Select(person => new TicketPerson(person, names[person]))
            .OrderBy(person => person.Name, StringComparer.CurrentCulture)];
    }
}

/// <summary>
/// Transfers an open ticket to the other team (ADR-0044, ADR-0068): who serves it, when the other team has someone in
/// the project to serve it. The ticket is left without an assignee until someone of the new team answers.
/// </summary>
public sealed class TransferTicket(IProjectAccess projects, ITicketStore store, TimeProvider clock)
{
    public async Task<TicketChangeResult> ExecuteAsync(
        int number, TicketTeam? team, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (team is not { } target || !Enum.IsDefined(target))
        {
            return TicketChangeResult.Of(TicketChangeOutcome.Invalid);
        }

        var ticket = await store.FindAsync(number, cancellationToken);
        var project = ticket is null
            ? null
            : await projects.FindAsync(ticket.ProjectId, viewer.UserId, viewer.IsAdministrator, cancellationToken);
        if (ticket is null || project is null)
        {
            return TicketChangeResult.Of(TicketChangeOutcome.NotFound, [number]);
        }

        if (!TicketAccess.MayTransfer(viewer, project.Role, ticket))
        {
            return TicketChangeResult.Of(TicketChangeOutcome.Forbidden, [number]);
        }

        if (!ticket.IsOpen)
        {
            return TicketChangeResult.Of(TicketChangeOutcome.NotAllowedNow, [number]);
        }

        if (ticket.Team == target)
        {
            return TicketChangeResult.Done;
        }

        var roles = (await projects.MembersAsync([project.Id], cancellationToken)).Select(member => member.Role);
        if (!TicketAccess.TeamsServedBy(roles).Contains(target))
        {
            // A team without anyone to serve it takes no tickets (ADR-0068).
            return TicketChangeResult.Of(TicketChangeOutcome.Invalid, [number]);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var before = ticket.Team;
        ticket.Transfer(target, now);
        store.AddEvent(TicketEvent.TeamChanged(ticket.Id, viewer.UserId, before, target, now));
        return await TicketChanges.SaveAsync(store, cancellationToken);
    }
}

/// <summary>
/// The tickets of a person who left the project or got another role (ADR-0042, ADR-0068): the open ones of which the
/// person is in charge and which the role no longer serves are left without an assignee, with the reason in the
/// conversation. The ones the person opened stay with the person. Idempotent.
/// </summary>
public sealed class TicketMemberChanges(ITicketStore store, TimeProvider clock) : IProjectMemberChanges
{
    public async Task MemberChangedAsync(
        Guid projectId, Guid userId, ProjectMemberRole? role, Guid actorId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var left = (await store.AssignedOpenAsync(projectId, userId, cancellationToken))
            .Where(ticket => !TicketAccess.CanBeAssignee(ticket.Team, role))
            .ToList();
        foreach (var ticket in left)
        {
            ticket.Unassign(now);
            store.AddEvent(TicketEvent.AssigneeCleared(ticket.Id, actorId, userId, now));
        }

        if (left.Count > 0)
        {
            await store.SaveChangesAsync(cancellationToken);
        }
    }
}

internal static class TicketChanges
{
    public static async Task<TicketChangeResult> SaveAsync(ITicketStore store, CancellationToken cancellationToken)
    {
        try
        {
            await store.SaveChangesAsync(cancellationToken);
            return TicketChangeResult.Done;
        }
        catch (TicketChangedException)
        {
            return TicketChangeResult.Of(TicketChangeOutcome.Changed);
        }
    }
}
