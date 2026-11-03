using System.ComponentModel;
using FiscoProjects.Tickets.Application;
using FiscoProjects.Tickets.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Tickets.Endpoints;

/// <summary>A person of the ticket in Teams, with the part in it and without the e-mail (ADR-0152).</summary>
public sealed record TeamsTicketParticipantResponse(Guid Id, string Name, TicketParticipantRole Role);

/// <summary>A file of the conversation in Teams, only by the name: it opens only in FiscoProjects (ADR-0152).</summary>
public sealed record TeamsTicketAttachmentResponse(
    [property: Description("The message of the file, or null for the description.")] Guid? MessageId,
    string Name,
    long Size,
    string ContentType);

/// <summary>A ticket at a vendor, for the text of its events in the conversation (ADR-0028).</summary>
public sealed record TeamsTicketVendorResponse(Guid Id, string Vendor, string Code);

/// <summary>The description of the ticket, when it is among the interactions shown.</summary>
public sealed record TeamsTicketDescriptionResponse(
    string Text,
    [property: Description("The description with its formatting, as the JSON of the editor; null for one written as plain text.")] string? Body);

/// <summary>The last interactions of the conversation, the description counted as the first one (ADR-0152).</summary>
public sealed record TeamsTicketConversationResponse(
    [property: Description("How many interactions came before the ones shown, which stay in FiscoProjects.")] int Earlier,
    TeamsTicketDescriptionResponse? Description,
    IReadOnlyList<TicketMessageResponse> Messages,
    IReadOnlyList<TicketEventResponse> Events,
    IReadOnlyList<TeamsTicketAttachmentResponse> Attachments,
    IReadOnlyList<TeamsTicketParticipantResponse> Participants,
    IReadOnlyList<TeamsTicketVendorResponse> VendorTickets);

/// <summary>
/// A ticket as the page of FiscoProjects in Teams shows it, only to read (ADR-0152): the header, the people, the
/// deadlines, how many attachments it has and the last interactions; no e-mails, no permissions.
/// </summary>
public sealed record TeamsTicketResponse(
    int Number,
    string Title,
    TicketProjectResponse Project,
    TicketTeam Team,
    TicketPriority Priority,
    TicketStatus Status,
    TicketPersonResponse Requester,
    TicketPersonResponse? Assignee,
    IReadOnlyList<TicketPersonResponse> Managers,
    DateTime OpenedAt,
    int AttachmentCount,
    TicketDeadlinesResponse? Deadlines,
    [property: Description("The person of the token, for the page to say \"(você)\".")] Guid ViewerId,
    TeamsTicketConversationResponse Conversation);

/// <summary>
/// The readings of the tickets for the page of FiscoProjects in Teams (ADR-0152), on the group of Teams of the host: only
/// with the token of Teams, with the rules of who sees a ticket (ADR-0044).
/// </summary>
public static class TeamsTicketEndpoints
{
    public static RouteGroupBuilder MapTeamsTicketEndpoints(this RouteGroupBuilder teams)
    {
        teams.MapGet("/tickets/{number:int}", GetAsync)
            .WithTags("Teams")
            .WithName("GetTeamsTicket")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        return teams;
    }

    private static async Task<Results<Ok<TeamsTicketResponse>, NotFound>> GetAsync(
        int number, HttpContext http, IUserAccess access, GetTicket getTicket, CancellationToken cancellationToken)
    {
        // Personal and read with a token of the moment: never kept by the browser nor by a proxy.
        http.Response.Headers.CacheControl = "no-store";
        var viewer = await TicketEndpoints.ViewerOfAsync(http, access, cancellationToken);
        if (await getTicket.ExecuteAsync(number, viewer, cancellationToken) is not { } details)
        {
            return TypedResults.NotFound();
        }

        var view = TeamsTicketView.Of(details);
        var ticket = details.Ticket;
        return TypedResults.Ok(new TeamsTicketResponse(
            ticket.Number,
            ticket.Title,
            TicketEndpoints.ProjectOf(details.Project),
            ticket.Team,
            ticket.Priority,
            ticket.Status,
            TicketEndpoints.PersonOf(details.Requester),
            details.Assignee is { } assignee ? TicketEndpoints.PersonOf(assignee) : null,
            [.. details.Managers.Select(TicketEndpoints.PersonOf)],
            ticket.OpenedAt,
            details.Attachments.Count,
            details.Deadlines is { } figures ? TicketEndpoints.DeadlinesOf(figures) : null,
            viewer.UserId,
            new TeamsTicketConversationResponse(
                view.Earlier,
                view.ShowsDescription ? new TeamsTicketDescriptionResponse(ticket.Description, ticket.DescriptionBody) : null,
                [.. view.Messages.Select(TicketEndpoints.MessageOf)],
                [.. view.Events.Select(TicketEndpoints.EventOf)],
                [.. view.Attachments.Select(attachment => new TeamsTicketAttachmentResponse(
                    attachment.MessageId, attachment.Name, attachment.Size, attachment.ContentType))],
                [.. details.Participants.Select(person => new TeamsTicketParticipantResponse(person.Id, person.Name, person.Role))],
                [.. details.VendorTickets
                    .Where(item => view.Events.Any(shown => shown.VendorTicketId == item.Record.Id))
                    .Select(item => new TeamsTicketVendorResponse(item.Record.Id, item.VendorName, item.Record.Code))])));
    }
}
