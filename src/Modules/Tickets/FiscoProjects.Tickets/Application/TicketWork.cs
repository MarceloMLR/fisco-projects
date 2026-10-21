using FiscoProjects.MyWork.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Domain;

namespace FiscoProjects.Tickets.Application;

/// <summary>
/// The open tickets of a person for Meu trabalho (ADR-0096): in charge of, opened by or followed by the person, or
/// without an assignee in the teams of the person, with the ones the person follows.
/// </summary>
public sealed record TicketWork(IReadOnlyList<Ticket> Tickets, IReadOnlySet<Guid> Followed);

/// <summary>
/// The tickets of the person in Meu trabalho (ADR-0070, ADR-0096), with the rules of the tickets: the projects the person
/// sees (ADR-0044), the teams of the person and who changes the assignee (ADR-0068), and the deadline of the first answer
/// while not given, then of the resolution, paused while the ticket waits for the requester (ADR-0025).
/// </summary>
public sealed class TicketWorkSource(IProjectAccess projects, ITicketStore store, TicketDeadlines deadlines) : IMyWorkSource
{
    public async Task<IReadOnlyList<MyWorkItem>> ItemsAsync(MyWorkQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var viewer = new TicketViewer(query.UserId, query.IsAdministrator);
        var visible = (await projects.VisibleAsync(viewer.UserId, viewer.IsAdministrator, cancellationToken))
            .Where(project => !project.IsClosed)
            .ToList();
        if (visible.Count == 0)
        {
            return [];
        }

        var work = await store.WorkOfAsync(TicketScope.Of(viewer, visible), cancellationToken);
        var interactions = await store.MessageCountsAsync([.. work.Tickets.Select(ticket => ticket.Id)], cancellationToken);
        var day = await deadlines.TodayAsync(cancellationToken) with { Today = query.Today };
        var byId = visible.ToDictionary(project => project.Id);
        var items = new List<MyWorkItem>();
        foreach (var ticket in work.Tickets)
        {
            var project = byId[ticket.ProjectId];
            var views = ViewsOf(ticket, viewer.UserId, project.Role, work.Followed.Contains(ticket.Id));
            if (views == MyWorkViews.None)
            {
                continue;
            }

            var waits = ticket.RequesterId == viewer.UserId && ticket.Status == TicketStatus.WaitingForRequester;
            var (deadline, due) = TicketDueNow.Of(TicketDeadlines.FiguresOf(ticket, day));
            items.Add(new MyWorkItem(MyWorkKind.Ticket, ticket.Id, project.Id, project.Code, ticket.Title, views)
            {
                DueOn = due?.DueOn,
                DaysLate = due?.DaysLate,
                WaitsForYou = waits,
                WaitingSince = waits ? ticket.PausedSince ?? ticket.UpdatedAt : null,
                Ticket = new MyWorkTicket(
                    ticket.Number,
                    StatusOf(ticket.Status),
                    ticket.Team == TicketTeam.Functional ? MyWorkTeam.Functional : MyWorkTeam.Development,
                    ticket.RequesterId,
                    ticket.AssigneeId,
                    TicketAccess.MayAssign(viewer, project.Role),
                    interactions.TryGetValue(ticket.Id, out var count) ? count : 0,
                    deadline switch
                    {
                        TicketDeadline.FirstResponse => MyWorkDeadline.FirstResponse,
                        TicketDeadline.Resolution => MyWorkDeadline.Resolution,
                        _ => null,
                    }),
            });
        }

        return items;
    }

    /// <summary>
    /// The views of a ticket for the person: Para mim, in charge of it while it does not wait for the requester, opened
    /// by the person and waiting for the answer, or without an assignee in the teams of the person; Abertos por mim,
    /// opened by the person; Que acompanho, followed by the person.
    /// </summary>
    public static MyWorkViews ViewsOf(Ticket ticket, Guid userId, ProjectMemberRole? role, bool followed)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        var views = MyWorkViews.None;
        var waiting = ticket.Status == TicketStatus.WaitingForRequester;
        if ((ticket.AssigneeId == userId && !waiting)
            || (ticket.RequesterId == userId && waiting)
            || (ticket.AssigneeId is null && TicketAccess.TeamsOf(role).Contains(ticket.Team)))
        {
            views |= MyWorkViews.ForMe;
        }

        if (ticket.RequesterId == userId)
        {
            views |= MyWorkViews.OpenedByMe;
        }

        if (followed)
        {
            views |= MyWorkViews.Following;
        }

        return views;
    }

    private static MyWorkTicketStatus StatusOf(TicketStatus status) => status switch
    {
        TicketStatus.Open => MyWorkTicketStatus.Open,
        TicketStatus.Triage => MyWorkTicketStatus.Triage,
        TicketStatus.InProgress => MyWorkTicketStatus.InProgress,
        TicketStatus.WaitingForRequester => MyWorkTicketStatus.WaitingForRequester,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Only open tickets are in Meu trabalho."),
    };
}
