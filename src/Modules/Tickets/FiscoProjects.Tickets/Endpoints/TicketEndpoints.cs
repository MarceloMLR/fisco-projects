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
    [property: Description("How many messages the ticket has.")] int Interactions);

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

/// <summary>An action of the flow the person may take now: where it leads and whether it needs a message (ADR-0024).</summary>
public sealed record TicketActionResponse(TicketAction Action, TicketStatus To, bool RequiresMessage);

/// <summary>What the person may do in the conversation, with the actions of the flow open now.</summary>
public sealed record TicketConversationPermissionsResponse(bool CanSend, bool CanAttachToDescription, IReadOnlyList<TicketActionResponse> Actions);

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
    bool Following);

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
        int pageSize = 25)
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
            new TicketQuery(view, projectId, team, situation, search, sort, descending, page, pageSize), viewer, cancellationToken);
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
                item.Id, item.Kind, item.ActorId, item.FromStatus, item.ToStatus, item.Action, item.AssigneeId, item.At))],
            [.. details.Attachments.Select(attachment => new TicketAttachmentResponse(
                attachment.Id, attachment.MessageId, attachment.Name, attachment.Size, attachment.ContentType, attachment.UploadedAt))],
            details.Followers,
            details.Mentionable,
            [.. details.Participants.Select(person => new TicketParticipantResponse(person.Id, person.Name, person.Role))],
            new TicketConversationPermissionsResponse(
                details.Permissions.CanSend,
                details.Permissions.CanAttachToDescription,
                [.. details.Permissions.Actions.Select(action => new TicketActionResponse(action.Action, action.To, action.RequiresMessage))]),
            details.Following));
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
            item.Interactions);

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
