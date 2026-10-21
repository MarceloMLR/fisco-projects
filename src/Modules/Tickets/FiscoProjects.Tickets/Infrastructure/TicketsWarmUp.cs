using FiscoProjects.MyWork.Contracts;
using FiscoProjects.SharedKernel;
using FiscoProjects.Tickets.Application;
using FiscoProjects.Tickets.Domain;
using FiscoProjects.Tickets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Tickets.Infrastructure;

/// <summary>
/// The reads of the screens of tickets (ADR-0106), as the requester of the last ticket sees them: the lists of the page
/// Chamados and of the tab of the project, the counts, the projects to choose, the ticket and Meu trabalho.
/// </summary>
internal sealed class TicketsWarmUp(
    TicketsDbContext db,
    ListTickets listTickets,
    CountTickets countTickets,
    ListTicketChoices choices,
    GetTicket getTicket,
    TicketDeadlines deadlines,
    IEnumerable<IMyWorkSource> work) : IWarmUp
{
    public string Module => "Tickets";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var sample = await db.Tickets.AsNoTracking()
            .OrderByDescending(ticket => ticket.Number)
            .Select(ticket => new { ticket.Number, ticket.ProjectId, ticket.RequesterId })
            .FirstOrDefaultAsync(cancellationToken);
        if (sample is null)
        {
            return;
        }

        var viewer = new TicketViewer(sample.RequesterId, IsAdministrator: false);
        await listTickets.ExecuteAsync(Page(projectId: null), viewer, cancellationToken);
        await listTickets.ExecuteAsync(Page(sample.ProjectId), viewer, cancellationToken);
        await countTickets.ExecuteAsync(null, viewer, cancellationToken);
        await countTickets.ExecuteAsync(sample.ProjectId, viewer, cancellationToken);
        await choices.ExecuteAsync(viewer, cancellationToken);
        await getTicket.ExecuteAsync(sample.Number, viewer, cancellationToken);
        var today = (await deadlines.TodayAsync(cancellationToken)).Today;
        foreach (var source in work.OfType<TicketWorkSource>())
        {
            await source.ItemsAsync(new MyWorkQuery(sample.RequesterId, false, today, today.AddDays(7)), cancellationToken);
        }
    }

    /// <summary>The first page of the open tickets, as the lists open.</summary>
    private static TicketQuery Page(Guid? projectId) =>
        new(TicketView.All, projectId, null, TicketSituation.Open, null, TicketSort.Priority, Descending: true, 1, 25);
}
