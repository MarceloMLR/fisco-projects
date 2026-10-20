using System.Globalization;
using System.Linq.Expressions;
using FiscoProjects.Tickets.Application;
using FiscoProjects.Tickets.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Tickets.Infrastructure.Persistence;

internal sealed class TicketStore(TicketsDbContext db) : ITicketStore
{
    private const string AccentInsensitive = "Latin1_General_100_CI_AI";

    // The priority as a number, the highest the greatest: the column keeps the name, which does not sort by urgency.
    private static readonly Expression<Func<Ticket, int>> Urgency = ticket =>
        ticket.Priority == TicketPriority.Critical ? 4
        : ticket.Priority == TicketPriority.High ? 3
        : ticket.Priority == TicketPriority.Medium ? 2
        : 1;

    // Asked before saving, so the audit of the opening already has the number.
    private const string NextNumberSql = "SELECT NEXT VALUE FOR [tickets].[ticket_numbers] AS [Value]";

    // Read as it is, without composing: NEXT VALUE FOR is not allowed in a subquery.
    public async Task<int> NextNumberAsync(CancellationToken cancellationToken) =>
        (await db.Database.SqlQueryRaw<int>(NextNumberSql).ToListAsync(cancellationToken)).Single();

    public void Add(Ticket ticket) => db.Tickets.Add(ticket);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public async Task<TicketRows> ListAsync(TicketQuery query, TicketScope scope, CancellationToken cancellationToken)
    {
        var tickets = Seen(scope);
        if (query.ProjectId is { } projectId)
        {
            tickets = tickets.Where(ticket => ticket.ProjectId == projectId);
        }

        tickets = query.View switch
        {
            TicketView.AssignedToMe => tickets.Where(ticket => ticket.AssigneeId == scope.UserId),
            TicketView.UnassignedInMyTeams => UnassignedInTeamsOf(tickets, scope),
            TicketView.Unassigned => tickets.Where(ticket => ticket.AssigneeId == null),
            TicketView.OpenedByMe => tickets.Where(ticket => ticket.RequesterId == scope.UserId),
            _ => tickets,
        };

        if (query.Team is { } team)
        {
            tickets = tickets.Where(ticket => ticket.Team == team);
        }

        tickets = query.Situation switch
        {
            TicketSituation.Open => Open(tickets),
            TicketSituation.Finished => tickets.Where(ticket => ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed),
            _ => tickets,
        };

        if (query.Search?.Trim() is { Length: > 0 } search)
        {
            // By the number, with or without #, and by the title and the description, without telling uppercase,
            // lowercase and accents apart.
            var number = int.TryParse(search.TrimStart('#'), NumberStyles.None, CultureInfo.InvariantCulture, out var value)
                ? value
                : (int?)null;
            tickets = tickets.Where(ticket =>
                ticket.Number == number
                || EF.Functions.Collate(ticket.Title, AccentInsensitive).Contains(search)
                || EF.Functions.Collate(ticket.Description, AccentInsensitive).Contains(search));
        }

        var total = await tickets.CountAsync(cancellationToken);
        var ordered = query.Sort switch
        {
            TicketSort.Number when query.Descending => tickets.OrderByDescending(ticket => ticket.Number),
            TicketSort.Number => tickets.OrderBy(ticket => ticket.Number),
            _ when query.Descending => tickets.OrderByDescending(Urgency).ThenBy(ticket => ticket.Number),
            _ => tickets.OrderBy(Urgency).ThenBy(ticket => ticket.Number),
        };
        var items = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        return new TicketRows(items, total);
    }

    public async Task<TicketCounts> CountAsync(TicketScope scope, Guid? projectId, CancellationToken cancellationToken)
    {
        var tickets = Seen(scope);
        if (projectId is { } id)
        {
            tickets = tickets.Where(ticket => ticket.ProjectId == id);
        }

        var open = Open(tickets);
        return new TicketCounts(
            await tickets.CountAsync(cancellationToken),
            await open.CountAsync(cancellationToken),
            await open.CountAsync(ticket => ticket.AssigneeId == null, cancellationToken),
            await UnassignedInTeamsOf(open, scope).CountAsync(cancellationToken),
            await open.CountAsync(ticket => ticket.AssigneeId == scope.UserId, cancellationToken),
            await open.CountAsync(ticket => ticket.RequesterId == scope.UserId, cancellationToken));
    }

    public Task<Ticket?> FindAsync(int number, CancellationToken cancellationToken) =>
        db.Tickets.SingleOrDefaultAsync(ticket => ticket.Number == number, cancellationToken);

    private IQueryable<Ticket> Seen(TicketScope scope)
    {
        var projectIds = scope.ProjectIds;
        return db.Tickets.AsNoTracking().Where(ticket => projectIds.Contains(ticket.ProjectId));
    }

    private static IQueryable<Ticket> Open(IQueryable<Ticket> tickets) =>
        tickets.Where(ticket => ticket.Status != TicketStatus.Resolved && ticket.Status != TicketStatus.Closed);

    private static IQueryable<Ticket> UnassignedInTeamsOf(IQueryable<Ticket> tickets, TicketScope scope)
    {
        var both = scope.BothTeamsProjects;
        var functional = scope.FunctionalProjects;
        var development = scope.DevelopmentProjects;
        return tickets.Where(ticket => ticket.AssigneeId == null
            && (both.Contains(ticket.ProjectId)
                || (ticket.Team == TicketTeam.Functional && functional.Contains(ticket.ProjectId))
                || (ticket.Team == TicketTeam.Development && development.Contains(ticket.ProjectId))));
    }
}
