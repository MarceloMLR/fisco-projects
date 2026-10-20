using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Domain;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FiscoProjects.Documents.Endpoints;

/// <summary>A person and the name.</summary>
public sealed record AcceptancePersonResponse(Guid Id, string Name);

/// <summary>A milestone of the schedule with its current name, its day and the day it was reached.</summary>
public sealed record AcceptanceMilestoneResponse(Guid Id, string Name, DateOnly? Day, DateOnly? ReachedOn);

/// <summary>
/// A term of a list: the title, the status, the milestone (null when it left the schedule), the person of the term, who
/// asked and when (who attached it, for a term without a request), who answered and when, and whether it was asked for
/// in the system.
/// </summary>
public sealed record AcceptanceResponse(
    Guid Id,
    string Title,
    AcceptanceStatus Status,
    Guid MilestoneId,
    AcceptanceMilestoneResponse? Milestone,
    AcceptancePersonResponse Recipient,
    DocumentActionResponse Requested,
    DocumentActionResponse? Answered,
    bool IsRequest);

/// <summary>How many terms of each status the person sees.</summary>
public sealed record AcceptanceCountsResponse(int All, int Pending, int Signed, int Refused);

/// <summary>
/// The terms the person sees, the counts, whether the person sees every term of the project (who manages them) and
/// whether the person asks for and attaches terms now, which a closed project does not let.
/// </summary>
public sealed record AcceptanceListResponse(
    IReadOnlyList<AcceptanceResponse> Items, AcceptanceCountsResponse Counts, bool CanManage, bool CanRequest);

/// <summary>A file of a term: the name and the size.</summary>
public sealed record AcceptanceFileResponse(string Name, long Size);

/// <summary>The document of the signed term in the folder of the terms, opened by the documents.</summary>
public sealed record AcceptanceSignedResponse(Guid DocumentId, string Name, long Size);

/// <summary>An event of the history: what happened, who did it and when.</summary>
public sealed record AcceptanceEventResponse(AcceptanceEventKind Kind, AcceptancePersonResponse Actor, DateTime At);

/// <summary>What the person may do with the term now.</summary>
public sealed record AcceptanceActionsResponse(bool CanAnswer, bool CanAttach, bool CanRemind, bool CanCancel);

/// <summary>A term with the message, the term to sign, the signed term, the reason of the refusal and the history.</summary>
public sealed record AcceptanceDetailsResponse(
    AcceptanceResponse Term,
    string? Message,
    AcceptanceFileResponse? ToSign,
    AcceptanceSignedResponse? Signed,
    string? RefusalReason,
    IReadOnlyList<AcceptanceEventResponse> Events,
    AcceptanceActionsResponse Actions);

public sealed record AcceptanceCreatedResponse(Guid Id);

/// <summary>The reason why the person refuses the term.</summary>
public sealed record AcceptanceRefusalRequest(string? Reason);

/// <summary>Problem types of the acceptance terms, so the screen can tell the reason.</summary>
public static class AcceptanceProblems
{
    /// <summary>The request was answered (or cancelled), also by someone at the same time (ADR-0095); status 409.</summary>
    public const string Answered = "urn:fiscoprojects:problem:acceptance-answered";

    /// <summary>
    /// The file is empty, larger than the documents take, or not a PDF (ADR-0035); status 400, with the extension reason
    /// (Empty, TooLarge or NotPdf).
    /// </summary>
    public const string FileRefused = "urn:fiscoprojects:problem:acceptance-file-refused";
}

/// <summary>
/// The acceptance terms of the projects (ADR-0035, ADR-0045, ADR-0095): the Administrator and the manager see and manage
/// every term; the person of a term sees it and answers it.
/// </summary>
public static class AcceptanceEndpoints
{
    // The form around the file: the fields and the boundaries.
    private const long FormOverhead = 1024 * 1024;

    public static RouteGroupBuilder MapAcceptanceEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);
        var settings = ((IEndpointRouteBuilder)api).ServiceProvider.GetRequiredService<DocumentSettings>();
        var size = new RequestSizeLimitAttribute(settings.MaxBytes + FormOverhead);
        var terms = api.MapGroup("/projects/{projectId:guid}/acceptances")
            .WithTags("Acceptances")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        terms.MapGet("/", ListAsync).WithName("ListAcceptances");
        terms.MapGet("/milestones", MilestonesAsync).WithName("ListAcceptanceMilestones")
            .ProducesProblem(StatusCodes.Status403Forbidden);
        terms.MapGet("/{termId:guid}", GetAsync).WithName("GetAcceptance");
        terms.MapGet("/{termId:guid}/file", FileAsync).WithName("OpenAcceptanceFile")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf");
        // The protection against other sites is the X-CSRF header of the application (ADR-0054), not the antiforgery
        // token of ASP.NET Core forms.
        terms.MapPost("/", RequestAsync).WithName("RequestAcceptance")
            .DisableAntiforgery()
            .WithMetadata(size)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        terms.MapPost("/attached", AttachAsync).WithName("AttachAcceptance")
            .DisableAntiforgery()
            .WithMetadata(size)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        terms.MapPost("/{termId:guid}/signed", SignAsync).WithName("SignAcceptance")
            .DisableAntiforgery()
            .WithMetadata(size)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        terms.MapPost("/{termId:guid}/attached", AttachToRequestAsync).WithName("AttachAcceptanceToRequest")
            .DisableAntiforgery()
            .WithMetadata(size)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        terms.MapPost("/{termId:guid}/refusal", RefuseAsync).WithName("RefuseAcceptance")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        terms.MapPost("/{termId:guid}/reminder", RemindAsync).WithName("RemindAcceptance")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        terms.MapDelete("/{termId:guid}", CancelAsync).WithName("CancelAcceptance")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        return api;
    }

    private static async Task<Results<Ok<AcceptanceListResponse>, NotFound>> ListAsync(
        Guid projectId, AcceptanceStatus? status, HttpContext http, IUserAccess access, ListAcceptances list, CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        if (await list.ExecuteAsync(projectId, status, viewer, cancellationToken) is not { } terms)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new AcceptanceListResponse(
            [.. terms.Items.Select(TermOf)],
            new AcceptanceCountsResponse(terms.Counts.All, terms.Counts.Pending, terms.Counts.Signed, terms.Counts.Refused),
            terms.CanManage,
            terms.CanManage && !terms.IsClosed));
    }

    private static async Task<Results<Ok<IReadOnlyList<AcceptanceMilestoneResponse>>, NotFound, ForbidHttpResult>> MilestonesAsync(
        Guid projectId, HttpContext http, IUserAccess access, ListAcceptanceMilestones list, CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var (outcome, milestones) = await list.ExecuteAsync(projectId, viewer, cancellationToken);
        return outcome switch
        {
            AcceptanceOutcome.Done => TypedResults.Ok<IReadOnlyList<AcceptanceMilestoneResponse>>([.. milestones.Select(MilestoneOf)]),
            AcceptanceOutcome.Forbidden => TypedResults.Forbid(),
            _ => TypedResults.NotFound(),
        };
    }

    private static async Task<Results<Ok<AcceptanceDetailsResponse>, NotFound>> GetAsync(
        Guid projectId, Guid termId, HttpContext http, IUserAccess access, GetAcceptance get, CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        if (await get.ExecuteAsync(projectId, termId, viewer, cancellationToken) is not { } details)
        {
            return TypedResults.NotFound();
        }

        var term = details.View.Term;
        return TypedResults.Ok(new AcceptanceDetailsResponse(
            TermOf(details.View),
            term.Message,
            term.FileName is { } fileName ? new AcceptanceFileResponse(fileName, term.FileSize ?? 0) : null,
            details.Signed is { } signed ? new AcceptanceSignedResponse(signed.Id, signed.Name, signed.Size) : null,
            term.RefusalReason,
            [.. details.Events.Select(item => new AcceptanceEventResponse(
                item.Event.Kind, new AcceptancePersonResponse(item.Event.ActorId, item.Actor), item.Event.At))],
            new AcceptanceActionsResponse(details.Actions.CanAnswer, details.Actions.CanAttach, details.Actions.CanRemind, details.Actions.CanCancel)));
    }

    private static async Task<Results<FileStreamHttpResult, NotFound>> FileAsync(
        Guid projectId, Guid termId, HttpContext http, IUserAccess access, OpenAcceptanceFile open, CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        if (await open.ExecuteAsync(projectId, termId, viewer, cancellationToken) is not { } opened)
        {
            return TypedResults.NotFound();
        }

        // A PDF opens in a tab as it is; the browser neither guesses another type nor runs anything of it (ADR-0031).
        var headers = http.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.ContentSecurityPolicy = "sandbox";
        headers.CacheControl = "private, no-store";
        return TypedResults.Stream(opened.File.Content, opened.ContentType, enableRangeProcessing: false);
    }

    private static async Task<Results<Created<AcceptanceCreatedResponse>, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> RequestAsync(
        Guid projectId,
        [FromForm] Guid? milestoneId,
        [FromForm] Guid? recipientId,
        [FromForm] string? title,
        [FromForm] string? message,
        IFormFile? file,
        HttpContext http,
        IUserAccess access,
        RequestAcceptance request,
        CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        await using var content = file?.OpenReadStream();
        var result = await request.ExecuteAsync(
            projectId, new AcceptanceRequest(milestoneId, recipientId, title, message), FileOf(file, content), viewer, cancellationToken);
        return Created(result);
    }

    private static async Task<Results<Created<AcceptanceCreatedResponse>, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> AttachAsync(
        Guid projectId,
        [FromForm] Guid? milestoneId,
        [FromForm] Guid? recipientId,
        [FromForm] string? title,
        IFormFile? file,
        HttpContext http,
        IUserAccess access,
        AttachAcceptance attach,
        CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        await using var content = file?.OpenReadStream();
        var result = await attach.ExecuteAsync(
            projectId, new AcceptanceRequest(milestoneId, recipientId, title, null), FileOf(file, content), viewer, cancellationToken);
        return Created(result);
    }

    private static Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> SignAsync(
        Guid projectId, Guid termId, IFormFile? file, HttpContext http, IUserAccess access, SignAcceptance sign, CancellationToken cancellationToken) =>
        SignedAsync(projectId, termId, file, onBehalf: false, http, access, sign, cancellationToken);

    private static Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> AttachToRequestAsync(
        Guid projectId, Guid termId, IFormFile? file, HttpContext http, IUserAccess access, SignAcceptance sign, CancellationToken cancellationToken) =>
        SignedAsync(projectId, termId, file, onBehalf: true, http, access, sign, cancellationToken);

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> SignedAsync(
        Guid projectId,
        Guid termId,
        IFormFile? file,
        bool onBehalf,
        HttpContext http,
        IUserAccess access,
        SignAcceptance sign,
        CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        await using var content = file?.OpenReadStream();
        var result = await sign.ExecuteAsync(projectId, termId, FileOf(file, content), onBehalf, viewer, cancellationToken);
        return result.Outcome == AcceptanceOutcome.Done ? TypedResults.NoContent() : Refusal<NoContent>(result);
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> RefuseAsync(
        Guid projectId,
        Guid termId,
        AcceptanceRefusalRequest request,
        HttpContext http,
        IUserAccess access,
        RefuseAcceptance refuse,
        CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await refuse.ExecuteAsync(projectId, termId, request.Reason, viewer, cancellationToken);
        return result.Outcome == AcceptanceOutcome.Done ? TypedResults.NoContent() : Refusal<NoContent>(result);
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> RemindAsync(
        Guid projectId, Guid termId, HttpContext http, IUserAccess access, RemindAcceptance remind, CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await remind.ExecuteAsync(projectId, termId, viewer, cancellationToken);
        return result.Outcome == AcceptanceOutcome.Done ? TypedResults.NoContent() : Refusal<NoContent>(result);
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> CancelAsync(
        Guid projectId, Guid termId, HttpContext http, IUserAccess access, CancelAcceptance cancel, CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await cancel.ExecuteAsync(projectId, termId, viewer, cancellationToken);
        return result.Outcome == AcceptanceOutcome.Done ? TypedResults.NoContent() : Refusal<NoContent>(result);
    }

    private static DocumentFile? FileOf(IFormFile? file, Stream? content) =>
        file is null || content is null ? null : new DocumentFile(file.FileName, file.Length, content);

    private static Results<Created<AcceptanceCreatedResponse>, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult> Created(
        AcceptanceResult result) =>
        result.Outcome == AcceptanceOutcome.Done
            ? TypedResults.Created((string?)null, new AcceptanceCreatedResponse(result.Id!.Value))
            : Refusal<Created<AcceptanceCreatedResponse>>(result);

    private static Results<T, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult> Refusal<T>(AcceptanceResult result)
        where T : IResult =>
        result.Outcome switch
        {
            AcceptanceOutcome.NotFound => TypedResults.NotFound(),
            AcceptanceOutcome.Forbidden => TypedResults.Forbid(),
            AcceptanceOutcome.ProjectClosed => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Project closed",
                detail: "A completed or cancelled project is read-only until it is reopened.",
                type: DocumentProblems.ProjectClosed),
            AcceptanceOutcome.Answered => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Request answered",
                detail: "The request was answered or cancelled, and takes nothing else.",
                type: AcceptanceProblems.Answered),
            AcceptanceOutcome.Refused => TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "File refused",
                detail: "The file is empty, larger than the documents take, or not a PDF.",
                type: AcceptanceProblems.FileRefused,
                extensions: new Dictionary<string, object?> { ["reason"] = result.Refusal.ToString() }),
            _ => TypedResults.ValidationProblem((result.Invalid ?? []).Distinct().ToDictionary(FieldName, field => new[] { MessageOf(field) })),
        };

    private static string FieldName(AcceptanceField field) => field switch
    {
        AcceptanceField.Milestone => "milestoneId",
        AcceptanceField.Recipient => "recipientId",
        AcceptanceField.Title => "title",
        AcceptanceField.Message => "message",
        AcceptanceField.Reason => "reason",
        _ => "file",
    };

    private static string MessageOf(AcceptanceField field) => field switch
    {
        AcceptanceField.Milestone => "The milestone is one of the schedule of the project.",
        AcceptanceField.Recipient => "The person is one of the project.",
        AcceptanceField.Title => $"The title is needed and has up to {AcceptanceTerm.TitleMaxLength} characters.",
        AcceptanceField.Message => $"The message has up to {AcceptanceTerm.MessageMaxLength} characters.",
        AcceptanceField.Reason => $"The reason is needed and has up to {AcceptanceTerm.ReasonMaxLength} characters.",
        _ => $"The file is needed, with a name of up to {Document.NameMaxLength} characters.",
    };

    private static AcceptanceMilestoneResponse MilestoneOf(ScheduleItemInfo milestone) =>
        new(milestone.Id, milestone.Name, milestone.Day, milestone.ReachedOn);

    private static AcceptanceResponse TermOf(AcceptanceView view)
    {
        var term = view.Term;
        return new(
            term.Id,
            term.Title,
            term.Status,
            term.MilestoneId,
            view.Milestone is { } milestone ? MilestoneOf(milestone) : null,
            new AcceptancePersonResponse(term.RecipientId, view.Recipient),
            new DocumentActionResponse(term.RequestedById, view.RequestedBy, term.RequestedAt),
            term.AnsweredById is { } answeredById && term.AnsweredAt is { } answeredAt
                ? new DocumentActionResponse(answeredById, view.AnsweredBy ?? string.Empty, answeredAt)
                : null,
            term.IsRequest);
    }
}
