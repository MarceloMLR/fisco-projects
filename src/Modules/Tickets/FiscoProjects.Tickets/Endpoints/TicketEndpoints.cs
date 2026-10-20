using System.ComponentModel;
using FiscoProjects.Tickets.Application;
using FiscoProjects.Tickets.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Tickets.Endpoints;

/// <summary>A person of a ticket.</summary>
public sealed record TicketPersonResponse(Guid Id, string Name);

/// <summary>The project of a ticket: the screens show the code beside the number (ADR-0029).</summary>
public sealed record TicketProjectResponse(Guid Id, string Code, string Name);

/// <summary>A ticket of a list; without an assignee until someone of the team answers it (ADR-0068).</summary>
public sealed record TicketListItemResponse(
    Guid Id,
    int Number,
    string Title,
    TicketProjectResponse Project,
    TicketTeam Team,
    TicketPriority Priority,
    TicketStatus Status,
    TicketPersonResponse Requester,
    TicketPersonResponse? Assignee,
    DateTime OpenedAt,
    DateTime UpdatedAt,
    [property: Description("How many messages and events the ticket has.")] int Interactions,
    [property: Description("The deadline of the resolution today (ADR-0025); null for a ticket opened before the deadlines.")] TicketDueResponse? Resolution,
    [property: Description("The person may change the assignee, also with others at once (ADR-0068).")] bool CanChangeAssignee,
    [property: Description("The person may change the requester with others at once (ADR-0027).")] bool CanChangeRequester);

public sealed record TicketPageResponse(IReadOnlyList<TicketListItemResponse> Items, int Total);

/// <summary>
/// How many tickets each list has: all of them, any status, and the open ones, the open ones without an assignee,
/// without an assignee in the teams of the person, in charge of the person and opened by the person.
/// </summary>
public sealed record TicketCountsResponse(
    int All, int Open, int Unassigned, int UnassignedInMyTeams, int AssignedToMe, int OpenedByMe, int Following);

/// <summary>
/// A project the person sees, for the filters and the opening: whether it is closed, whether the person may open
/// tickets in it and the teams with someone to serve them (ADR-0068).
/// </summary>
public sealed record TicketProjectChoiceResponse(
    Guid Id, string Code, string Name, bool IsClosed, bool CanOpen, IReadOnlyList<TicketTeam> Teams);

/// <summary>
/// A new ticket: the project, the team, the priority, the title and the description, without an assignee (ADR-0068). The
/// description comes with its formatting in <c>descriptionBody</c> (the JSON of the editor) or as plain text.
/// </summary>
public sealed record NewTicketRequest(
    Guid? ProjectId, TicketTeam? Team, TicketPriority? Priority, string? Title, string? Description, string? DescriptionBody = null);

public sealed record TicketCreatedResponse(Guid Id, int Number);

/// <summary>An attachment of a ticket: of a message, or of the description when the message is null.</summary>
public sealed record TicketAttachmentResponse(Guid Id, Guid? MessageId, string Name, long Size, string ContentType, DateTime UploadedAt);

/// <summary>A message of the conversation, with its text as the JSON of the editor.</summary>
public sealed record TicketMessageResponse(Guid Id, Guid AuthorId, string Body, DateTime SentAt);

/// <summary>A person of the conversation and the part the person plays in the ticket, for the color and the tag (ADR-0069).</summary>
public sealed record TicketParticipantResponse(Guid Id, string Name, TicketParticipantRole Role);

/// <summary>Where a deadline stands (ADR-0025): its state, the day it is due and, when overdue, by how many business days.</summary>
public sealed record TicketDueResponse(TicketDueState State, DateOnly? DueOn, int? DaysLate);

/// <summary>
/// The deadlines of a ticket today (ADR-0025): the first answer (null once the ticket finished without one) and when it
/// was given, the resolution, the business days used of the total and the business days it waited for the requester.
/// </summary>
public sealed record TicketDeadlinesResponse(
    TicketDueResponse? FirstResponse,
    DateTime? FirstRespondedAt,
    [property: Description("Null for a ticket closed without being resolved, as a cancelled one.")] TicketDueResponse? Resolution,
    int UsedDays,
    int TotalDays,
    int PausedDays);

/// <summary>The deadlines of a priority, in business days (ADR-0025).</summary>
public sealed record TicketDeadlineSettingResponse(TicketPriority Priority, int FirstResponseDays, int ResolutionDays);

/// <summary>The deadlines of a priority to save, from 1 to 365 business days.</summary>
public sealed record TicketDeadlineSettingRequest(TicketPriority? Priority, int FirstResponseDays, int ResolutionDays);

/// <summary>The deadlines of the four priorities, all at once.</summary>
public sealed record TicketDeadlinesRequest(IReadOnlyList<TicketDeadlineSettingRequest>? Deadlines);

/// <summary>A new priority for the ticket.</summary>
public sealed record TicketPriorityRequest(TicketPriority? Priority);

/// <summary>An action of the flow the person may take now: where it leads and whether it needs a message (ADR-0024).</summary>
public sealed record TicketActionResponse(TicketAction Action, TicketStatus To, bool RequiresMessage);

/// <summary>What the person may do in the conversation, with the actions of the flow open now and the priority.</summary>
public sealed record TicketConversationPermissionsResponse(
    bool CanSend,
    bool CanAttachToDescription,
    IReadOnlyList<TicketActionResponse> Actions,
    bool CanChangePriority,
    bool CanChangeAssignee,
    bool CanChangeRequester,
    bool CanTransfer);

/// <summary>A new assignee for the tickets, one or many (ADR-0027, ADR-0068).</summary>
public sealed record TicketAssigneeRequest(IReadOnlyList<int>? Numbers, Guid? AssigneeId);

/// <summary>A new requester for the tickets, one or many (ADR-0027).</summary>
public sealed record TicketRequesterRequest(IReadOnlyList<int>? Numbers, Guid? RequesterId);

/// <summary>The other team for the ticket (ADR-0068).</summary>
public sealed record TicketTeamRequest(TicketTeam? Team);

/// <summary>
/// An event of the conversation (ADR-0069): a change of status, with the action, or the assignee set; the actor is
/// null when the system did it.
/// </summary>
public sealed record TicketEventResponse(
    Guid Id,
    TicketEventKind Kind,
    Guid? ActorId,
    TicketStatus? From,
    TicketStatus? To,
    TicketAction? Action,
    Guid? AssigneeId,
    TicketPriority? FromPriority,
    TicketPriority? ToPriority,
    TicketTeam? FromTeam,
    TicketTeam? ToTeam,
    [property: Description("The requester before, or the assignee who left.")] Guid? FromPersonId,
    [property: Description("The new requester.")] Guid? ToPersonId,
    DateTime At);

/// <summary>An action of the flow taken in a ticket, with the message when the action needs one.</summary>
public sealed record TicketTransitionRequest(
    TicketAction? Action,
    [property: Description("The message, as the JSON of the editor, without mentions; needed by the actions that ask for one.")] string? Body = null);

/// <summary>A transition of the flow (ADR-0024): from a status to another by an action, who may take it, and how.</summary>
public sealed record TicketTransitionResponse(
    TicketStatus From,
    TicketStatus To,
    TicketAction Action,
    [property: Description("Attendant, Requester, Manager, Administrator or System.")] IReadOnlyList<TicketActors> Actors,
    bool RequiresMessage,
    [property: Description("Taken by the system on its own, never offered as a button.")] bool Automatic);

/// <summary>
/// The flow of the tickets, read from its table, with the statuses in their order and after how many business days a
/// resolved ticket without an answer closes.
/// </summary>
public sealed record TicketFlowResponse(
    IReadOnlyList<TicketStatus> Statuses, IReadOnlyList<TicketTransitionResponse> Transitions, int AutoCloseBusinessDays);

/// <summary>
/// A ticket as its page shows it, with the people: the requester, the assignee, the managers and the tech leads of the
/// project; and its conversation (ADR-0026, ADR-0069): the messages, the attachments, who follows it, who may be
/// mentioned, the parts of the people who appear in it and what the person may do.
/// </summary>
public sealed record TicketResponse(
    Guid Id,
    int Number,
    string Title,
    string Description,
    [property: Description("The description with its formatting, as the JSON of the editor; null for one written as plain text.")] string? DescriptionBody,
    TicketProjectResponse Project,
    TicketTeam Team,
    TicketPriority Priority,
    TicketStatus Status,
    TicketPersonResponse Requester,
    TicketPersonResponse? Assignee,
    IReadOnlyList<TicketPersonResponse> Managers,
    IReadOnlyList<TicketPersonResponse> TechLeads,
    DateTime OpenedAt,
    DateTime UpdatedAt,
    IReadOnlyList<TicketMessageResponse> Messages,
    IReadOnlyList<TicketEventResponse> Events,
    IReadOnlyList<TicketAttachmentResponse> Attachments,
    IReadOnlyList<Guid> Followers,
    IReadOnlyList<Guid> Mentionable,
    IReadOnlyList<TicketParticipantResponse> Participants,
    TicketConversationPermissionsResponse Permissions,
    bool Following,
    TicketDeadlinesResponse? Deadlines);

/// <summary>Problem types of the tickets, so the screen can tell the reason.</summary>
public static class TicketProblems
{
    /// <summary>The project is completed or cancelled and takes no tickets until it is reopened (ADR-0022); status 409.</summary>
    public const string ProjectClosed = "urn:fiscoprojects:problem:project-closed";

    /// <summary>The message mentions someone who may not be mentioned in the ticket (ADR-0026); status 400.</summary>
    public const string MentionRefused = "urn:fiscoprojects:problem:ticket-mention-refused";

    /// <summary>
    /// A file is too large, empty or a program or script (ADR-0026); status 400, with the extensions fileName and reason
    /// (TooLarge, Empty or Dangerous).
    /// </summary>
    public const string AttachmentRefused = "urn:fiscoprojects:problem:ticket-attachment-refused";

    /// <summary>The ticket is closed and takes no more messages (ADR-0024); status 409.</summary>
    public const string TicketClosed = "urn:fiscoprojects:problem:ticket-closed";

    /// <summary>
    /// The person chosen does not serve some of the tickets (ADR-0068), or the team has nobody to serve it; status 400,
    /// with the numbers of those tickets in the extension numbers.
    /// </summary>
    public const string PersonRefused = "urn:fiscoprojects:problem:ticket-person-refused";

    /// <summary>Some of the tickets are resolved or closed; status 409, with their numbers in the extension numbers.</summary>
    public const string TicketFinished = "urn:fiscoprojects:problem:ticket-finished";

    /// <summary>Someone changed the ticket at the same time, and nothing was saved; status 409.</summary>
    public const string TicketChanged = "urn:fiscoprojects:problem:ticket-changed";

    /// <summary>The action does not leave the status the ticket is in now (ADR-0024); status 409.</summary>
    public const string ActionNotAllowedNow = "urn:fiscoprojects:problem:ticket-action-not-allowed-now";
}

/// <summary>
/// The tickets (ADR-0022, ADR-0029, ADR-0044, ADR-0068): everyone in a project sees its tickets and the Administrator
/// sees all; everyone in the project but the viewer opens them.
/// </summary>
public static class TicketEndpoints
{
    // Five files of 15 MB and the text; a larger setting of the installation takes fewer files at a time.
    private const long UploadLimit = 80L * 1024 * 1024;

    public static RouteGroupBuilder MapTicketsEndpoints(this RouteGroupBuilder api)
    {
        var tickets = api.MapGroup("/tickets")
            .WithTags("Tickets")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        tickets.MapGet("/", ListAsync).WithName("ListTickets");
        tickets.MapGet("/counts", CountAsync).WithName("CountTickets");
        tickets.MapGet("/choices", ChoicesAsync).WithName("ListTicketChoices");
        tickets.MapPost("/", OpenAsync).WithName("OpenTicket")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        tickets.MapGet("/flow", FlowAsync).WithName("GetTicketFlow");
        tickets.MapPut("/assignee", AssignAsync).WithName("ChangeTicketAssignees")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        tickets.MapPut("/requester", ChangeRequesterAsync).WithName("ChangeTicketRequesters")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        tickets.MapGet("/assignee-choices", (
                    [FromQuery(Name = "number")] int[] numbers,
                    HttpContext http,
                    IUserAccess access,
                    ListTicketPeopleChoices choices,
                    CancellationToken cancellationToken) =>
                ChoicesOfAsync(numbers, http, access, (viewer, token) => choices.AssigneesAsync(numbers, viewer, token), cancellationToken))
            .WithName("ListTicketAssigneeChoices")
            .WithDescription("The people who may be the assignee of all the tickets of the numbers (ADR-0068).");
        tickets.MapGet("/requester-choices", (
                    [FromQuery(Name = "number")] int[] numbers,
                    HttpContext http,
                    IUserAccess access,
                    ListTicketPeopleChoices choices,
                    CancellationToken cancellationToken) =>
                ChoicesOfAsync(numbers, http, access, (viewer, token) => choices.RequestersAsync(numbers, viewer, token), cancellationToken))
            .WithName("ListTicketRequesterChoices")
            .WithDescription("The people who may be the requester of all the tickets of the numbers (ADR-0027).");
        tickets.MapPut("/{number:int}/team", TransferAsync).WithName("TransferTicket")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        tickets.MapPut("/{number:int}/priority", ChangePriorityAsync).WithName("ChangeTicketPriority")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        tickets.MapGet("/{number:int}", GetAsync).WithName("GetTicket");
        tickets.MapPost("/{number:int}/transitions", ChangeStatusAsync).WithName("ChangeTicketStatus")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        // The protection against other sites is the X-CSRF header of the application (ADR-0054), not the antiforgery
        // token of ASP.NET Core forms.
        tickets.MapPost("/{number:int}/messages", SendAsync).WithName("SendTicketMessage")
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(UploadLimit))
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        tickets.MapPost("/{number:int}/attachments", AttachAsync).WithName("AttachToTicketDescription")
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(UploadLimit))
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        tickets.MapGet("/{number:int}/attachments/{id:guid}", OpenAttachmentAsync).WithName("OpenTicketAttachment")
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream");
        tickets.MapPut("/{number:int}/following", (int number, HttpContext http, IUserAccess access, FollowTicket follow, CancellationToken cancellationToken) =>
                FollowAsync(number, true, http, access, follow, cancellationToken))
            .WithName("FollowTicket");
        tickets.MapDelete("/{number:int}/following", (int number, HttpContext http, IUserAccess access, FollowTicket follow, CancellationToken cancellationToken) =>
                FollowAsync(number, false, http, access, follow, cancellationToken))
            .WithName("UnfollowTicket");

        // The deadlines of the priorities, only for the Administrator (ADR-0025, ADR-0046).
        var deadlines = api.MapGroup("/admin/ticket-deadlines")
            .WithTags("Tickets")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        deadlines.MapGet("/", DeadlinesAsync).WithName("GetTicketDeadlines");
        deadlines.MapPut("/", ChangeDeadlinesAsync).WithName("ChangeTicketDeadlines")
            .ProducesProblem(StatusCodes.Status400BadRequest);
        return api;
    }

    private static async Task<Results<Ok<TicketPageResponse>, ValidationProblem>> ListAsync(
        HttpContext http,
        IUserAccess access,
        ListTickets listTickets,
        CancellationToken cancellationToken,
        [Description("All (the default), AssignedToMe, UnassignedInMyTeams, Unassigned, OpenedByMe or Following.")] TicketView view = TicketView.All,
        [Description("Only the tickets of this project.")] Guid? projectId = null,
        [Description("Only the tickets of this team.")] TicketTeam? team = null,
        [Description("Open (the default: not resolved nor closed), Finished (resolved and closed) or All.")] TicketSituation situation = TicketSituation.Open,
        [Description("The number, with or without #, or part of the title or of the description, without telling case or accents apart.")] string? search = null,
        [Description("Priority (the default, the oldest first in each) or Number.")] TicketSort sort = TicketSort.Priority,
        [Description("From the highest priority or number to the lowest (the default).")] bool descending = true,
        int page = 1,
        int pageSize = 25,
        [Description("Only the tickets of which this person is the assignee or the requester.")] Guid? personId = null)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors[nameof(page)] = ["Must be 1 or more."];
        }

        if (pageSize is < 1 or > TicketQuery.MaxPageSize)
        {
            errors[nameof(pageSize)] = [$"Must be between 1 and {TicketQuery.MaxPageSize}."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await listTickets.ExecuteAsync(
            new TicketQuery(view, projectId, team, situation, search, sort, descending, page, pageSize, personId), viewer, cancellationToken);
        return TypedResults.Ok(new TicketPageResponse([.. result.Items.Select(ItemOf)], result.Total));
    }

    private static async Task<Results<Ok<TicketCountsResponse>, NotFound>> CountAsync(
        HttpContext http,
        IUserAccess access,
        CountTickets countTickets,
        CancellationToken cancellationToken,
        [Description("Only the tickets of this project.")] Guid? projectId = null)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        return await countTickets.ExecuteAsync(projectId, viewer, cancellationToken) is { } counts
            ? TypedResults.Ok(new TicketCountsResponse(
                counts.All, counts.Open, counts.Unassigned, counts.UnassignedInMyTeams, counts.AssignedToMe, counts.OpenedByMe, counts.Following))
            : TypedResults.NotFound();
    }

    private static async Task<Ok<IReadOnlyList<TicketProjectChoiceResponse>>> ChoicesAsync(
        HttpContext http, IUserAccess access, ListTicketChoices listChoices, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var choices = await listChoices.ExecuteAsync(viewer, cancellationToken);
        return TypedResults.Ok<IReadOnlyList<TicketProjectChoiceResponse>>([.. choices.Select(choice =>
            new TicketProjectChoiceResponse(choice.Id, choice.Code, choice.Name, choice.IsClosed, choice.CanOpen, choice.Teams))]);
    }

    private static async Task<Results<Created<TicketCreatedResponse>, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> OpenAsync(
        HttpContext http, NewTicketRequest request, IUserAccess access, OpenTicket openTicket, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await openTicket.ExecuteAsync(
            new NewTicket(request.ProjectId, request.Team, request.Priority, request.Title, request.Description, request.DescriptionBody),
            viewer,
            cancellationToken);
        return result.Outcome switch
        {
            OpenTicketOutcome.Opened => TypedResults.Created(
                (string?)null, new TicketCreatedResponse(result.Id!.Value, result.Number!.Value)),
            OpenTicketOutcome.NotFound => TypedResults.NotFound(),
            OpenTicketOutcome.Forbidden => TypedResults.Forbid(),
            OpenTicketOutcome.Closed => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Project closed",
                detail: "A completed or cancelled project takes no tickets until it is reopened.",
                type: TicketProblems.ProjectClosed),
            _ => Invalid(result.Invalid),
        };
    }

    private static async Task<Results<Ok<TicketResponse>, NotFound>> GetAsync(
        int number, HttpContext http, IUserAccess access, GetTicket getTicket, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        if (await getTicket.ExecuteAsync(number, viewer, cancellationToken) is not { } details)
        {
            return TypedResults.NotFound();
        }

        var ticket = details.Ticket;
        return TypedResults.Ok(new TicketResponse(
            ticket.Id,
            ticket.Number,
            ticket.Title,
            ticket.Description,
            ticket.DescriptionBody,
            ProjectOf(details.Project),
            ticket.Team,
            ticket.Priority,
            ticket.Status,
            PersonOf(details.Requester),
            details.Assignee is { } assignee ? PersonOf(assignee) : null,
            [.. details.Managers.Select(PersonOf)],
            [.. details.TechLeads.Select(PersonOf)],
            ticket.OpenedAt,
            ticket.UpdatedAt,
            [.. details.Messages.Select(message => new TicketMessageResponse(message.Id, message.AuthorId, message.Body, message.SentAt))],
            [.. details.Events.Select(item => new TicketEventResponse(
                item.Id,
                item.Kind,
                item.ActorId,
                item.FromStatus,
                item.ToStatus,
                item.Action,
                item.AssigneeId,
                item.FromPriority,
                item.ToPriority,
                item.FromTeam,
                item.ToTeam,
                item.FromPersonId,
                item.ToPersonId,
                item.At))],
            [.. details.Attachments.Select(attachment => new TicketAttachmentResponse(
                attachment.Id, attachment.MessageId, attachment.Name, attachment.Size, attachment.ContentType, attachment.UploadedAt))],
            details.Followers,
            details.Mentionable,
            [.. details.Participants.Select(person => new TicketParticipantResponse(person.Id, person.Name, person.Role))],
            new TicketConversationPermissionsResponse(
                details.Permissions.CanSend,
                details.Permissions.CanAttachToDescription,
                [.. details.Permissions.Actions.Select(action => new TicketActionResponse(action.Action, action.To, action.RequiresMessage))],
                details.Permissions.CanChangePriority,
                details.Permissions.CanChangeAssignee,
                details.Permissions.CanChangeRequester,
                details.Permissions.CanTransfer),
            details.Following,
            details.Deadlines is { } figures ? DeadlinesOf(figures) : null));
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> AssignAsync(
        TicketAssigneeRequest request, HttpContext http, IUserAccess access, ChangeTicketAssignees change, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        return ChangeResultOf(await change.ExecuteAsync(request.Numbers, request.AssigneeId, viewer, cancellationToken));
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> ChangeRequesterAsync(
        TicketRequesterRequest request, HttpContext http, IUserAccess access, ChangeTicketRequesters change, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        return ChangeResultOf(await change.ExecuteAsync(request.Numbers, request.RequesterId, viewer, cancellationToken));
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> TransferAsync(
        int number, TicketTeamRequest request, HttpContext http, IUserAccess access, TransferTicket transfer, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        return ChangeResultOf(await transfer.ExecuteAsync(number, request.Team, viewer, cancellationToken));
    }

    private static async Task<Results<Ok<IReadOnlyList<TicketPersonResponse>>, NotFound>> ChoicesOfAsync(
        int[] numbers,
        HttpContext http,
        IUserAccess access,
        Func<TicketViewer, CancellationToken, Task<IReadOnlyList<TicketPerson>?>> choose,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        return numbers.Length > 0 && await choose(viewer, cancellationToken) is { } people
            ? TypedResults.Ok<IReadOnlyList<TicketPersonResponse>>([.. people.Select(PersonOf)])
            : TypedResults.NotFound();
    }

    private static Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult> ChangeResultOf(TicketChangeResult result) =>
        result.Outcome switch
        {
            TicketChangeOutcome.Done => TypedResults.NoContent(),
            TicketChangeOutcome.NotFound => TypedResults.NotFound(),
            TicketChangeOutcome.Forbidden => TypedResults.Forbid(),
            TicketChangeOutcome.Invalid when result.Numbers.Count == 0 => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["numbers"] = [$"From 1 to {TicketBatch.MaxTickets} different numbers, and the person or the team chosen."],
            }),
            TicketChangeOutcome.Invalid => TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Person or team refused",
                detail: "The person chosen does not serve these tickets, or the team has nobody in the project to serve it.",
                type: TicketProblems.PersonRefused,
                extensions: new Dictionary<string, object?> { ["numbers"] = result.Numbers }),
            TicketChangeOutcome.NotAllowedNow => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Ticket finished",
                detail: "Resolved or closed tickets keep their people and team.",
                type: TicketProblems.TicketFinished,
                extensions: new Dictionary<string, object?> { ["numbers"] = result.Numbers }),
            _ => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Ticket changed",
                detail: "Someone changed a ticket at the same time; nothing was saved.",
                type: TicketProblems.TicketChanged),
        };

    private static async Task<Ok<IReadOnlyList<TicketDeadlineSettingResponse>>> DeadlinesAsync(
        GetTicketDeadlines getDeadlines, CancellationToken cancellationToken)
    {
        var settings = await getDeadlines.ExecuteAsync(cancellationToken);
        return TypedResults.Ok<IReadOnlyList<TicketDeadlineSettingResponse>>([.. settings.Select(setting =>
            new TicketDeadlineSettingResponse(setting.Priority, setting.FirstResponseDays, setting.ResolutionDays))]);
    }

    private static async Task<Results<NoContent, ValidationProblem>> ChangeDeadlinesAsync(
        TicketDeadlinesRequest request, ChangeTicketDeadlines changeDeadlines, CancellationToken cancellationToken)
    {
        var invalid = await changeDeadlines.ExecuteAsync(
            [.. (request.Deadlines ?? []).Select(item => new TicketDeadlineChange(item.Priority, item.FirstResponseDays, item.ResolutionDays))],
            cancellationToken);
        return invalid.Count == 0
            ? TypedResults.NoContent()
            : TypedResults.ValidationProblem(invalid.ToDictionary(
                key => key,
                key => new[]
                {
                    key == "deadlines"
                        ? "The deadlines come once for each priority: Low, Medium, High and Critical."
                        : $"The deadlines go from {TicketDeadlineSetting.MinDays} to {TicketDeadlineSetting.MaxDays} business days.",
                }));
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> ChangePriorityAsync(
        int number,
        TicketPriorityRequest request,
        HttpContext http,
        IUserAccess access,
        ChangeTicketPriority changePriority,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        return await changePriority.ExecuteAsync(number, request.Priority, viewer, cancellationToken) switch
        {
            PriorityChangeOutcome.Done => TypedResults.NoContent(),
            PriorityChangeOutcome.NotFound => TypedResults.NotFound(),
            PriorityChangeOutcome.Forbidden => TypedResults.Forbid(),
            PriorityChangeOutcome.Invalid => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["priority"] = ["The priority is Low, Medium, High or Critical."],
            }),
            _ => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Ticket finished",
                detail: "A resolved or closed ticket keeps its priority.",
                type: TicketProblems.ActionNotAllowedNow),
        };
    }

    private static async Task<Ok<TicketFlowResponse>> FlowAsync(
        GetTicketFlow getFlow, TicketAutoCloseSettings autoClose, CancellationToken cancellationToken)
    {
        var flow = await getFlow.ExecuteAsync(cancellationToken);
        return TypedResults.Ok(new TicketFlowResponse(
            Enum.GetValues<TicketStatus>(),
            [.. flow.Transitions.Select(transition => new TicketTransitionResponse(
                transition.From,
                transition.To,
                transition.Action,
                [.. Enum.GetValues<TicketActors>().Where(actor => actor != TicketActors.None && transition.Actors.HasFlag(actor))],
                transition.RequiresMessage,
                transition.Automatic))],
            autoClose.BusinessDays));
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> ChangeStatusAsync(
        int number,
        TicketTransitionRequest request,
        HttpContext http,
        IUserAccess access,
        ChangeTicketStatus changeStatus,
        CancellationToken cancellationToken)
    {
        if (request.Action is not { } action || !Enum.IsDefined(action))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["action"] = ["The action is one of the flow of the tickets."],
            });
        }

        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        return await changeStatus.ExecuteAsync(number, action, request.Body, viewer, cancellationToken) switch
        {
            StatusChangeOutcome.Done => TypedResults.NoContent(),
            StatusChangeOutcome.NotFound => TypedResults.NotFound(),
            StatusChangeOutcome.Forbidden => TypedResults.Forbid(),
            StatusChangeOutcome.MessageNeeded => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = ["This action needs a message: the JSON of the editor, not empty, with only paragraphs, lists, bold and links."],
            }),
            _ => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Action not allowed now",
                detail: "The action does not leave the status the ticket is in.",
                type: TicketProblems.ActionNotAllowedNow),
        };
    }

    private static async Task<Results<Created, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> SendAsync(
        int number,
        HttpContext http,
        [FromForm] string? body,
        IFormFileCollection files,
        IUserAccess access,
        SendTicketMessage send,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var attachments = AttachmentsOf(files);
        try
        {
            return ResultOf(await send.ExecuteAsync(number, body, attachments, viewer, cancellationToken));
        }
        finally
        {
            foreach (var attachment in attachments)
            {
                await attachment.Content.DisposeAsync();
            }
        }
    }

    private static async Task<Results<Created, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> AttachAsync(
        int number,
        HttpContext http,
        IFormFileCollection files,
        IUserAccess access,
        AttachToDescription attach,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var attachments = AttachmentsOf(files);
        try
        {
            return ResultOf(await attach.ExecuteAsync(number, attachments, viewer, cancellationToken));
        }
        finally
        {
            foreach (var attachment in attachments)
            {
                await attachment.Content.DisposeAsync();
            }
        }
    }

    private static async Task<Results<FileStreamHttpResult, NotFound>> OpenAttachmentAsync(
        int number, Guid id, HttpContext http, IUserAccess access, OpenTicketAttachment open, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        if (await open.ExecuteAsync(number, id, viewer, cancellationToken) is not { } opened)
        {
            return TypedResults.NotFound();
        }

        var contentType = opened.Attachment.ContentType;
        var inline = TicketAttachments.OpensInBrowser(contentType);
        var headers = http.Response.Headers;
        // The browser neither guesses another type nor runs anything of the file (ADR-0026).
        headers.XContentTypeOptions = "nosniff";
        headers.ContentSecurityPolicy = "sandbox";
        headers.CacheControl = "private, no-store";
        return TypedResults.Stream(
            opened.File.Content,
            contentType,
            fileDownloadName: inline ? null : opened.Attachment.Name,
            enableRangeProcessing: false);
    }

    private static async Task<Results<NoContent, NotFound>> FollowAsync(
        int number, bool follow, HttpContext http, IUserAccess access, FollowTicket followTicket, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        return await followTicket.ExecuteAsync(number, follow, viewer, cancellationToken)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static List<NewAttachment> AttachmentsOf(IFormFileCollection files) =>
        [.. files.Select(file => new NewAttachment(Path.GetFileName(file.FileName), file.Length, file.OpenReadStream()))];

    private static Results<Created, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult> ResultOf(ConversationResult result) =>
        result.Outcome switch
        {
            ConversationOutcome.Done => TypedResults.Created(),
            ConversationOutcome.NotFound => TypedResults.NotFound(),
            ConversationOutcome.Forbidden => TypedResults.Forbid(),
            ConversationOutcome.InvalidBody => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = ["The text is the JSON of the editor, not empty, with only paragraphs, lists, bold, links and mentions."],
            }),
            ConversationOutcome.MentionRefused => TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Mention refused",
                detail: "Only the managers of the project and the people involved in the ticket may be mentioned.",
                type: TicketProblems.MentionRefused),
            ConversationOutcome.Closed => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Ticket closed",
                detail: "A closed ticket takes no more messages; a new ticket is opened instead.",
                type: TicketProblems.TicketClosed),
            ConversationOutcome.Changed => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Ticket changed",
                detail: "Someone changed the ticket at the same time; nothing was saved.",
                type: TicketProblems.TicketChanged),
            _ => TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Attachment refused",
                detail: "A file is empty, larger than the installation takes, or a program or script.",
                type: TicketProblems.AttachmentRefused,
                extensions: new Dictionary<string, object?>
                {
                    ["fileName"] = result.FileName,
                    ["reason"] = result.Refusal.ToString(),
                }),
        };

    /// <summary>The person of the session, and whether the person is the Administrator, read from the database now.</summary>
    private static async Task<TicketViewer> ViewerOfAsync(HttpContext http, IUserAccess access, CancellationToken cancellationToken)
    {
        var userId = UserSession.UserIdOf(http.User)
            ?? throw new InvalidOperationException("The tickets are read in a session.");
        var person = await access.FindAsync(userId, cancellationToken);
        return new TicketViewer(userId, person is { IsActive: true, Role: SystemRole.Administrator });
    }

    private static TicketListItemResponse ItemOf(TicketListItem item) =>
        new(
            item.Ticket.Id,
            item.Ticket.Number,
            item.Ticket.Title,
            ProjectOf(item.Project),
            item.Ticket.Team,
            item.Ticket.Priority,
            item.Ticket.Status,
            PersonOf(item.Requester),
            item.Assignee is { } assignee ? PersonOf(assignee) : null,
            item.Ticket.OpenedAt,
            item.Ticket.UpdatedAt,
            item.Interactions,
            item.Deadlines?.Resolution is { } resolution ? DueOf(resolution) : null,
            item.CanChangeAssignee,
            item.CanChangeRequester);

    private static TicketDueResponse DueOf(TicketDue due) => new(due.State, due.DueOn, due.DaysLate);

    private static TicketDeadlinesResponse DeadlinesOf(TicketDeadlineFigures figures) =>
        new(
            figures.FirstResponse is { } first ? DueOf(first) : null,
            figures.FirstRespondedAt,
            figures.Resolution is { } resolution ? DueOf(resolution) : null,
            figures.UsedDays,
            figures.TotalDays,
            figures.PausedDays);

    private static TicketProjectResponse ProjectOf(TicketProject project) => new(project.Id, project.Code, project.Name);

    private static TicketPersonResponse PersonOf(TicketPerson person) => new(person.Id, person.Name);

    private static ValidationProblem Invalid(IReadOnlyList<TicketField> fields) =>
        TypedResults.ValidationProblem(fields.Distinct().ToDictionary(
            field => field switch
            {
                TicketField.Project => "projectId",
                TicketField.Team => "team",
                TicketField.Priority => "priority",
                TicketField.Title => "title",
                _ => "description",
            },
            field => new[]
            {
                field switch
                {
                    TicketField.Project => "The ticket needs a project.",
                    TicketField.Team => "The team is Functional or Development, with someone in the project to serve it.",
                    TicketField.Priority => "The priority is Low, Medium, High or Critical.",
                    TicketField.Title => "The title is needed and has up to 200 characters.",
                    _ => "The description is needed and has up to 10,000 characters.",
                },
            }));
}
