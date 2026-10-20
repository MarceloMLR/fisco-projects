using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Domain;

namespace FiscoProjects.Tickets.Application;

/// <summary>After how many business days a resolved ticket without an answer is closed (<c>Tickets:AutoCloseBusinessDays</c>).</summary>
public sealed record TicketAutoCloseSettings(int BusinessDays)
{
    public const int Default = 5;
}

public enum StatusChangeOutcome
{
    Done = 1,
    NotFound = 2,
    Forbidden = 3,

    /// <summary>The action does not leave the status the ticket is in.</summary>
    NotAllowedNow = 4,

    /// <summary>The action needs a message, or the message is not valid.</summary>
    MessageNeeded = 5,
}

/// <summary>The flow of the tickets as its page shows it (ADR-0024).</summary>
public sealed class GetTicketFlow(ITicketStore store)
{
    public Task<TicketFlow> ExecuteAsync(CancellationToken cancellationToken) => store.FlowAsync(cancellationToken);
}

/// <summary>
/// Takes an action of the flow in a ticket (ADR-0024, ADR-0044): only who may take it, from the status the ticket is
/// in; the action that needs a message gets it as a message of the conversation.
/// </summary>
public sealed class ChangeTicketStatus(
    IProjectAccess projects, ITicketStore store, TicketDeadlines deadlines, TicketAnswers answers, TimeProvider clock)
{
    public async Task<StatusChangeOutcome> ExecuteAsync(
        int number, TicketAction action, string? body, TicketViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var ticket = await store.FindAsync(number, cancellationToken);
        var project = ticket is null
            ? null
            : await projects.FindAsync(ticket.ProjectId, viewer.UserId, viewer.IsAdministrator, cancellationToken);
        if (ticket is null || project is null)
        {
            return StatusChangeOutcome.NotFound;
        }

        var flow = await store.FlowAsync(cancellationToken);
        var actors = TicketAccess.ActorsOf(viewer, project.Role, ticket);
        var anywhere = flow.Transitions.Where(transition => transition.Action == action && !transition.Automatic).ToList();
        if (anywhere.Count == 0 || anywhere.All(transition => (transition.Actors & actors) == 0))
        {
            return StatusChangeOutcome.Forbidden;
        }

        if (flow.Of(ticket.Status, action) is not { Automatic: false } transition)
        {
            return StatusChangeOutcome.NotAllowedNow;
        }

        if ((transition.Actors & actors) == 0)
        {
            return StatusChangeOutcome.Forbidden;
        }

        var text = body is null ? null : RichText.Parse(body);
        if ((transition.RequiresMessage && text is null) || (body is not null && text is null) || text?.Mentions.Count > 0)
        {
            return StatusChangeOutcome.MessageNeeded;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        if (text is not null)
        {
            // The message of the action is an answer like any other (ADR-0068).
            store.AddMessage(new TicketMessage(ticket.Id, viewer.UserId, text, now));
            await answers.ClaimAsync(ticket, project, viewer, now, cancellationToken);
        }

        await deadlines.TakeAsync(ticket, transition, viewer.UserId, now, cancellationToken);
        try
        {
            await store.SaveChangesAsync(cancellationToken);
        }
        catch (TicketChangedException)
        {
            // Someone changed the ticket meanwhile: the person sees it as it is now and decides again.
            return StatusChangeOutcome.NotAllowedNow;
        }

        return StatusChangeOutcome.Done;
    }
}

/// <summary>
/// Closes the tickets resolved without an answer for the business days of the installation (ADR-0024), by the
/// calendar of the installation and on its day.
/// </summary>
public sealed class CloseResolvedTickets(
    ITicketStore store, TicketDeadlines deadlines, TicketAutoCloseSettings settings, TimeProvider clock)
{
    /// <summary>How many tickets were closed.</summary>
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var resolved = await store.ResolvedAsync(cancellationToken);
        if (resolved.Count == 0)
        {
            return 0;
        }

        var flow = await store.FlowAsync(cancellationToken);
        if (flow.Of(TicketStatus.Resolved, TicketAction.AutoClose) is not { } close)
        {
            return 0;
        }

        var day = await deadlines.TodayAsync(cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;
        var closed = 0;
        foreach (var ticket in resolved.Where(ticket => ticket.ResolvedAt is not null))
        {
            var due = day.Days.AddBusinessDays(day.DayOf(ticket.ResolvedAt!.Value), settings.BusinessDays);
            if (day.Today >= due)
            {
                await deadlines.TakeAsync(ticket, close, null, now, cancellationToken);
                closed++;
            }
        }

        if (closed > 0)
        {
            await store.SaveChangesAsync(cancellationToken);
        }

        return closed;
    }

}
