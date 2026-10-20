using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Domain;

namespace FiscoProjects.Tickets.Application;

/// <summary>
/// The tickets of a project in its overview (ADR-0073): every person of the project sees them (ADR-0044). The deadline
/// that counts is the first answer while not given, then the resolution while it runs (ADR-0025); a ticket needs
/// attention when it is late, due up to the next business day or without an assignee, and a ticket at a vendor when it
/// is stale (ADR-0028).
/// </summary>
public sealed class TicketProjectOverview(
    ITicketStore store, IVendorStore vendors, TicketDeadlines deadlines, VendorStaleSettings stale) : IProjectTickets
{
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
                deadline switch
                {
                    TicketDeadline.FirstResponse => ProjectTicketDeadline.FirstResponse,
                    TicketDeadline.Resolution => ProjectTicketDeadline.Resolution,
                    _ => null,
                },
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
}
