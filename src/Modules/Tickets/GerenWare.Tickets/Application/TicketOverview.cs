using GerenWare.Projects.Contracts;
using GerenWare.Tickets.Domain;

namespace GerenWare.Tickets.Application;

/// <summary>
/// The tickets of a project in its overview (ADR-0073): every person of the project sees them (ADR-0044). The deadline
/// that counts is the first answer while not given, then the resolution while it runs (ADR-0025); a ticket needs
/// attention when it is late, due up to the next business day or without an assignee, and a ticket at a vendor when it
/// is stale (ADR-0028).
/// </summary>
public sealed class TicketProjectOverview(
    ITicketStore store, IVendorStore vendors, TicketDeadlines deadlines, VendorStaleSettings stale, ITicketImpactStore impacts)
    : IProjectTickets
{
    /// <summary>
    /// The open tickets of the project linked to items of the schedule (ADR-0164), with the deadline that counts now
    /// (<see cref="TicketDueNow"/>), for everyone who sees the schedule.
    /// </summary>
    public async Task<IReadOnlyList<ProjectTicketImpact>> ImpactsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var linked = await impacts.OpenOfProjectAsync(projectId, cancellationToken);
        if (linked.Count == 0)
        {
            return [];
        }

        var day = await deadlines.TodayAsync(cancellationToken);
        return [.. linked.Select(item =>
        {
            var (deadline, due) = TicketDueNow.Of(TicketDeadlines.FiguresOf(item.Ticket, day));
            return new ProjectTicketImpact(
                item.Ticket.Number, item.Ticket.Title, DeadlineOf(deadline), DueStateOf(due), due?.DueOn, due?.DaysLate, item.ItemIds);
        })];
    }

    public async Task<ProjectTicketOverview> OverviewAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var all = await store.OfProjectAsync(projectId, cancellationToken);
        if (all.Count == 0)
        {
            return ProjectTicketOverview.None;
        }

        var day = await deadlines.TodayAsync(cancellationToken);
        var open = all.Where(ticket => ticket.IsOpen).ToList();
        var interactions = await store.MessageCountsAsync([.. open.Select(ticket => ticket.Id)], cancellationToken);
        var attention = new List<(ProjectTicketAttention Item, int Order)>();
        var overdue = 0;
        foreach (var ticket in open)
        {
            var (deadline, due) = TicketDueNow.Of(TicketDeadlines.FiguresOf(ticket, day));
            if (due?.State == TicketDueState.Overdue)
            {
                overdue++;
            }

            var order = due?.State switch
            {
                TicketDueState.Overdue => 0,
                TicketDueState.Soon => 1,
                _ when ticket.AssigneeId is null => 2,
                _ => -1,
            };
            if (order < 0)
            {
                continue;
            }

            attention.Add((new ProjectTicketAttention(
                ticket.Number,
                ticket.Title,
                ticket.Team == TicketTeam.Functional ? ProjectTicketTeam.Functional : ProjectTicketTeam.Development,
                ticket.AssigneeId,
                interactions.TryGetValue(ticket.Id, out var count) ? count : 0,
                DeadlineOf(deadline),
                DueStateOf(due),
                due?.DueOn,
                due?.DaysLate), order));
        }

        var numbers = open.ToDictionary(ticket => ticket.Id, ticket => ticket.Number);
        var atVendors = (await vendors.TicketsOfAsync([.. numbers.Keys], cancellationToken))
            .Select(row => (Row: row, Stale: VendorTickets.StaleDaysOf(row.Record, day, stale)))
            .Where(entry => entry.Stale is not null)
            .OrderByDescending(entry => entry.Stale)
            .Select(entry => new ProjectVendorAttention(
                numbers[entry.Row.Record.TicketId], entry.Row.VendorName, entry.Row.Record.Code, entry.Stale!.Value))
            .ToList();
        return new ProjectTicketOverview(
            open.Count,
            all.Count,
            overdue,
            open.Count(ticket => ticket.Status == TicketStatus.WaitingForRequester),
            open.Count(ticket => ticket.Team == TicketTeam.Functional),
            open.Count(ticket => ticket.Team == TicketTeam.Development),
            [.. attention
                .OrderBy(entry => entry.Order)
                .ThenBy(entry => entry.Item.DueOn ?? DateOnly.MaxValue)
                .ThenBy(entry => entry.Item.Number)
                .Select(entry => entry.Item)],
            atVendors);
    }

    private static ProjectTicketDeadline? DeadlineOf(TicketDeadline? deadline) => deadline switch
    {
        TicketDeadline.FirstResponse => ProjectTicketDeadline.FirstResponse,
        TicketDeadline.Resolution => ProjectTicketDeadline.Resolution,
        _ => null,
    };

    private static ProjectTicketDueState? DueStateOf(TicketDue? due) => due?.State switch
    {
        TicketDueState.Overdue => ProjectTicketDueState.Overdue,
        TicketDueState.Soon => ProjectTicketDueState.Soon,
        TicketDueState.OnTrack => ProjectTicketDueState.OnTrack,
        _ => null,
    };
}

/// <summary>
/// The open tickets of a project for the status report (ADR-0165): the status and the priority, whether the deadline
/// that counts now is late (<see cref="TicketDueNow"/>), the ticket at a vendor a list shows
/// (<see cref="VendorTickets.ShownIn"/>) and the items of the schedule they are linked to (ADR-0164).
/// </summary>
public sealed class TicketReportSource(ITicketStore store, IVendorStore vendors, TicketDeadlines deadlines, ITicketImpactStore impacts)
    : IProjectReportTickets
{
    public async Task<IReadOnlyList<ReportTicket>> OpenAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var open = (await store.OfProjectAsync(projectId, cancellationToken)).Where(ticket => ticket.IsOpen).ToList();
        if (open.Count == 0)
        {
            return [];
        }

        var day = await deadlines.TodayAsync(cancellationToken);
        var atVendors = VendorTickets.ShownIn(await vendors.TicketsOfAsync([.. open.Select(ticket => ticket.Id)], cancellationToken));
        var linked = (await impacts.OpenOfProjectAsync(projectId, cancellationToken))
            .ToDictionary(item => item.Ticket.Id, item => item.ItemIds);
        return [.. open.OrderBy(ticket => ticket.Number).Select(ticket =>
        {
            var (_, due) = TicketDueNow.Of(TicketDeadlines.FiguresOf(ticket, day));
            return new ReportTicket(
                ticket.Number,
                ticket.Title,
                StatusOf(ticket.Status),
                PriorityOf(ticket.Priority),
                due?.State == TicketDueState.Overdue,
                atVendors.TryGetValue(ticket.Id, out var row)
                    ? new ReportVendorTicket(row.VendorName, row.Record.Code, VendorStatusOf(row.Record.Status))
                    : null,
                linked.TryGetValue(ticket.Id, out var items) ? items : []);
        })];
    }

    private static ReportTicketStatus StatusOf(TicketStatus status) => status switch
    {
        TicketStatus.Triage => ReportTicketStatus.Triage,
        TicketStatus.InProgress => ReportTicketStatus.InProgress,
        TicketStatus.WaitingForRequester => ReportTicketStatus.WaitingForRequester,
        _ => ReportTicketStatus.Open,
    };

    private static ReportTicketPriority PriorityOf(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => ReportTicketPriority.Low,
        TicketPriority.High => ReportTicketPriority.High,
        TicketPriority.Critical => ReportTicketPriority.Critical,
        _ => ReportTicketPriority.Medium,
    };

    private static ReportVendorStatus VendorStatusOf(VendorTicketStatus status) => status switch
    {
        VendorTicketStatus.InAnalysis => ReportVendorStatus.InAnalysis,
        VendorTicketStatus.WaitingForUs => ReportVendorStatus.WaitingForUs,
        VendorTicketStatus.Resolved => ReportVendorStatus.Resolved,
        VendorTicketStatus.Closed => ReportVendorStatus.Closed,
        _ => ReportVendorStatus.Open,
    };
}
