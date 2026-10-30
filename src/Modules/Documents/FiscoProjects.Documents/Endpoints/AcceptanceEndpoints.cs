using System.Globalization;
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

/// <summary>A signer of a request in a list: the name and where the signer stands.</summary>
public sealed record AcceptanceSignerSummaryResponse(string Name, AcceptanceSignerStatus Status);

/// <summary>
/// A term of a list: the title, the status, the milestone (null when it left the schedule), the signers at the
/// provider, the person of a request asked before (ADR-0129), who asked and when (who attached it, for a term without a
/// request), who answered in the system and when, and whether it was asked for in the system.
/// </summary>
public sealed record AcceptanceResponse(
    Guid Id,
    string Title,
    AcceptanceStatus Status,
    Guid MilestoneId,
    AcceptanceMilestoneResponse? Milestone,
    IReadOnlyList<AcceptanceSignerSummaryResponse> Signers,
    AcceptancePersonResponse? Recipient,
    DocumentActionResponse Requested,
    DocumentActionResponse? Answered,
    DateTime? ClosedAt,
    bool IsRequest);

/// <summary>How many terms of each status the project has.</summary>
public sealed record AcceptanceCountsResponse(int All, int Pending, int Signed, int Refused);

/// <summary>
/// The terms of the project, for who manages them (ADR-0129): the counts, whether a signature can be asked for now (the
/// installation has a provider and the project is open) and whether a signed term can be attached (the project open).
/// </summary>
public sealed record AcceptanceListResponse(
    IReadOnlyList<AcceptanceResponse> Items, AcceptanceCountsResponse Counts, bool CanRequest, bool CanAttach);

/// <summary>A file of a term: the name and the size.</summary>
public sealed record AcceptanceFileResponse(string Name, long Size);

/// <summary>The document of the signed term in the folder of the terms, opened by the documents.</summary>
public sealed record AcceptanceSignedResponse(Guid DocumentId, string Name, long Size);

/// <summary>
/// A signer of a request: the name, the e-mail only while the request is open (ADR-0129), where the signer stands, when
/// the signer signed or refused, the reason of a refusal and, from the local provider, the link to sign.
/// </summary>
public sealed record AcceptanceSignerResponse(
    Guid Id,
    string Name,
    string? Email,
    AcceptanceSignerStatus Status,
    DateTime? RespondedAt,
    string? RefusalReason,
    string? SigningLink);

/// <summary>An event of the history: what happened, who did it (the person, or the signer) and when.</summary>
public sealed record AcceptanceEventResponse(AcceptanceEventKind Kind, AcceptancePersonResponse? Actor, string? Signer, DateTime At);

/// <summary>What the person may do with the term now.</summary>
public sealed record AcceptanceActionsResponse(bool CanAttach, bool CanRemind, bool CanCancel);

/// <summary>A term with the message, the term to sign, the signers, the signed term, the reason of the refusal and the history.</summary>
public sealed record AcceptanceDetailsResponse(
    AcceptanceResponse Term,
    string? Message,
    AcceptanceFileResponse? ToSign,
    IReadOnlyList<AcceptanceSignerResponse> Signers,
    AcceptanceSignedResponse? Signed,
    string? RefusalReason,
    IReadOnlyList<AcceptanceEventResponse> Events,
    AcceptanceActionsResponse Actions);

public sealed record AcceptanceCreatedResponse(Guid Id);

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

    /// <summary>
    /// The installation has no provider of signatures, or the request is not at it, as one asked of a person before
    /// (ADR-0129); status 409.
    /// </summary>
    public const string NoProvider = "urn:fiscoprojects:problem:signature-provider-missing";

    /// <summary>The provider of signatures failed or refused, and nothing was saved; status 502.</summary>
    public const string ProviderFailed = "urn:fiscoprojects:problem:signature-provider-failed";
}

/// <summary>
/// The acceptance terms of the projects (ADR-0129): only the Administrator and the manager see them, ask for signatures
/// at the provider, attach signed terms, remind and cancel.
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
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        terms.MapGet("/", ListAsync).WithName("ListAcceptances");
        terms.MapGet("/milestones", MilestonesAsync).WithName("ListAcceptanceMilestones");
        terms.MapGet("/{termId:guid}", GetAsync).WithName("GetAcceptance");
        terms.MapGet("/{termId:guid}/file", FileAsync).WithName("OpenAcceptanceFile")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf");
        // The protection against other sites is the X-CSRF header of the application (ADR-0054), not the antiforgery
        // token of ASP.NET Core forms.
        terms.MapPost("/", RequestAsync).WithName("RequestAcceptance")
            .DisableAntiforgery()
            .WithMetadata(size)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status502BadGateway);
        terms.MapPost("/attached", AttachAsync).WithName("AttachAcceptance")
            .DisableAntiforgery()
            .WithMetadata(size)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        terms.MapPost("/{termId:guid}/attached", AttachToRequestAsync).WithName("AttachAcceptanceToRequest")
            .DisableAntiforgery()
            .WithMetadata(size)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        terms.MapPost("/{termId:guid}/reminder", RemindAsync).WithName("RemindAcceptance")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status502BadGateway);
        terms.MapDelete("/{termId:guid}", CancelAsync).WithName("CancelAcceptance")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status502BadGateway);
        api.MapLocalSigningEndpoints();
        return api;
    }

    private static async Task<Results<Ok<AcceptanceListResponse>, NotFound, ForbidHttpResult>> ListAsync(
        Guid projectId, AcceptanceStatus? status, HttpContext http, IUserAccess access, ListAcceptances list, CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var (outcome, terms) = await list.ExecuteAsync(projectId, status, viewer, cancellationToken);
        if (terms is null)
        {
            return outcome == AcceptanceOutcome.Forbidden ? TypedResults.Forbid() : TypedResults.NotFound();
        }

        return TypedResults.Ok(new AcceptanceListResponse(
            [.. terms.Items.Select(TermOf)],
            new AcceptanceCountsResponse(terms.Counts.All, terms.Counts.Pending, terms.Counts.Signed, terms.Counts.Refused),
            terms.CanRequest,
            terms.CanAttach));
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

    private static async Task<Results<Ok<AcceptanceDetailsResponse>, NotFound, ForbidHttpResult>> GetAsync(
        Guid projectId, Guid termId, HttpContext http, IUserAccess access, GetAcceptance get, CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var (outcome, details) = await get.ExecuteAsync(projectId, termId, viewer, cancellationToken);
        if (details is null)
        {
            return outcome == AcceptanceOutcome.Forbidden ? TypedResults.Forbid() : TypedResults.NotFound();
        }

        var term = details.View.Term;
        return TypedResults.Ok(new AcceptanceDetailsResponse(
            TermOf(details.View),
            term.Message,
            term.FileName is { } fileName ? new AcceptanceFileResponse(fileName, term.FileSize ?? 0) : null,
            [.. details.Signers.Select(item => new AcceptanceSignerResponse(
                item.Signer.Id,
                item.Signer.Name,
                item.Signer.Email,
                item.Signer.Status,
                item.Signer.RespondedAt,
                item.Signer.RefusalReason,
                item.SigningLink))],
            details.Signed is { } signed ? new AcceptanceSignedResponse(signed.Id, signed.Name, signed.Size) : null,
            term.RefusalReason,
            [.. details.Events.Select(item => new AcceptanceEventResponse(
                item.Event.Kind,
                item.Event.ActorId is { } actor ? new AcceptancePersonResponse(actor, item.Actor) : null,
                item.Event.ActorId is null ? item.Actor : null,
                item.Event.At))],
            new AcceptanceActionsResponse(details.Actions.CanAttach, details.Actions.CanRemind, details.Actions.CanCancel)));
    }

    private static async Task<Results<FileStreamHttpResult, NotFound, ForbidHttpResult>> FileAsync(
        Guid projectId, Guid termId, HttpContext http, IUserAccess access, OpenAcceptanceFile open, CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var (outcome, opened) = await open.ExecuteAsync(projectId, termId, viewer, cancellationToken);
        if (opened is null)
        {
            return outcome == AcceptanceOutcome.Forbidden ? TypedResults.Forbid() : TypedResults.NotFound();
        }

        return PdfOf(http, opened.File.Content, opened.ContentType);
    }

    private static async Task<Results<Created<AcceptanceCreatedResponse>, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> RequestAsync(
        Guid projectId,
        [FromForm] Guid? milestoneId,
        [FromForm] string? title,
        [FromForm] string? message,
        IFormFile? file,
        HttpContext http,
        IUserAccess access,
        RequestAcceptance request,
        CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var form = await http.Request.ReadFormAsync(cancellationToken);
        await using var content = file?.OpenReadStream();
        var result = await request.ExecuteAsync(
            projectId, new AcceptanceRequest(milestoneId, title, message, SignersOf(form)), FileOf(file, content), viewer, cancellationToken);
        return Created(result);
    }

    private static async Task<Results<Created<AcceptanceCreatedResponse>, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> AttachAsync(
        Guid projectId,
        [FromForm] Guid? milestoneId,
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
            projectId, new AcceptanceRequest(milestoneId, title, null), FileOf(file, content), viewer, cancellationToken);
        return Created(result);
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound, ForbidHttpResult, ProblemHttpResult>> AttachToRequestAsync(
        Guid projectId,
        Guid termId,
        IFormFile? file,
        HttpContext http,
        IUserAccess access,
        AttachSignedTerm attach,
        CancellationToken cancellationToken)
    {
        var viewer = await DocumentEndpoints.ViewerOfAsync(http, access, cancellationToken);
        await using var content = file?.OpenReadStream();
        var result = await attach.ExecuteAsync(projectId, termId, FileOf(file, content), viewer, cancellationToken);
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

    /// <summary>A PDF that opens in a tab as it is: the browser neither guesses another type nor runs anything of it (ADR-0031).</summary>
    internal static FileStreamHttpResult PdfOf(HttpContext http, Stream content, string contentType)
    {
        var headers = http.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.ContentSecurityPolicy = "sandbox";
        headers.CacheControl = "private, no-store";
        return TypedResults.Stream(content, contentType, enableRangeProcessing: false);
    }

    /// <summary>The signers of the form, in order: <c>signers[0].name</c>, <c>signers[0].email</c> and so on, until one is missing.</summary>
    private static List<(string? Name, string? Email)> SignersOf(IFormCollection form)
    {
        var signers = new List<(string? Name, string? Email)>();
        // One more than the limit, so a request with too many signers is refused, not cut.
        for (var index = 0; index <= AcceptanceSignerForm.MaxSigners; index++)
        {
            var name = form[string.Create(CultureInfo.InvariantCulture, $"signers[{index}].name")];
            var email = form[string.Create(CultureInfo.InvariantCulture, $"signers[{index}].email")];
            if (name.Count == 0 && email.Count == 0)
            {
                break;
            }

            signers.Add((name.ToString(), email.ToString()));
        }

        return signers;
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
            AcceptanceOutcome.NoProvider => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "No provider of signatures",
                detail: "The installation has no provider of signatures for this request; attach the signed term instead.",
                type: AcceptanceProblems.NoProvider),
            AcceptanceOutcome.ProviderFailed => TypedResults.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Provider of signatures failed",
                detail: "The provider of signatures did not take the change, and nothing was saved; try again.",
                type: AcceptanceProblems.ProviderFailed),
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
        AcceptanceField.Title => "title",
        AcceptanceField.Message => "message",
        AcceptanceField.Signers => "signers",
        _ => "file",
    };

    private static string MessageOf(AcceptanceField field) => field switch
    {
        AcceptanceField.Milestone => "The milestone is one of the schedule of the project.",
        AcceptanceField.Title => $"The title is needed and has up to {AcceptanceTerm.TitleMaxLength} characters.",
        AcceptanceField.Message => $"The message has up to {AcceptanceTerm.MessageMaxLength} characters.",
        AcceptanceField.Signers =>
            $"From 1 to {AcceptanceSignerForm.MaxSigners} signers, each with a name of up to {AcceptanceSigner.NameMaxLength} characters and a valid e-mail, without the same e-mail twice.",
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
            [.. term.Signers.OrderBy(signer => signer.Position).Select(signer => new AcceptanceSignerSummaryResponse(signer.Name, signer.Status))],
            term.RecipientId is { } recipient ? new AcceptancePersonResponse(recipient, view.Recipient ?? string.Empty) : null,
            new DocumentActionResponse(term.RequestedById, view.RequestedBy, term.RequestedAt),
            term.AnsweredById is { } answeredById && term.AnsweredAt is { } answeredAt
                ? new DocumentActionResponse(answeredById, view.AnsweredBy ?? string.Empty, answeredAt)
                : null,
            term.Status == AcceptanceStatus.Pending ? null : term.AnsweredAt,
            term.IsRequest);
    }
}
