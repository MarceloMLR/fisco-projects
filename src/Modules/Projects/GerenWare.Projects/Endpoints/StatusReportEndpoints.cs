using System.ComponentModel;
using GerenWare.Projects.Application;
using GerenWare.Projects.Contracts;
using GerenWare.Projects.Domain;
using GerenWare.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace GerenWare.Projects.Endpoints;

/// <summary>When and by whom the last report of the project was generated.</summary>
public sealed record StatusReportLastResponse(DateTime GeneratedAt, OverviewPersonResponse GeneratedBy);

/// <summary>An open ticket of the project that may go in as a risk, and whether it threatens milestones (ADR-0164).</summary>
public sealed record StatusReportTicketResponse(int Number, string Title, ReportTicketStatus Status, bool Threatens);

/// <summary>A risk of the report: the ticket, its level and the point of attention.</summary>
public sealed record StatusReportRiskResponse(int Number, RiskLevel Level, string Attention);

/// <summary>
/// The draft of a new status report (ADR-0165): the last report, its four texts, the open tickets of the project and
/// the risks suggested, the ones of the last report still open and the tickets that threaten milestones.
/// </summary>
public sealed record StatusReportDraftResponse(
    StatusReportLastResponse? Last,
    string Summary,
    string Focus,
    string NextSteps,
    string ClientNeeds,
    IReadOnlyList<StatusReportTicketResponse> Tickets,
    IReadOnlyList<StatusReportRiskResponse> Risks);

/// <summary>A risk chosen: the number of an open ticket of the project, its level and a point of attention up to 500 characters.</summary>
public sealed record StatusReportRiskRequest(int Number, RiskLevel? Level, string? Attention);

/// <summary>What the person wrote and chose to generate a status report (ADR-0165, ADR-0166).</summary>
public sealed record StatusReportRequest(
    [property: Description("The summary of the period, up to 1,000 characters.")] string? Summary,
    [property: Description("The current focus, up to 1,000 characters.")] string? Focus,
    [property: Description("The next steps, up to 1,000 characters.")] string? NextSteps,
    [property: Description("What the client must do, up to 1,000 characters.")] string? ClientNeeds,
    [property: Description("From 0 to 10 open tickets of the project, without repeating, each with its level.")] IReadOnlyList<StatusReportRiskRequest>? Risks,
    [property: Description("Whether the hours of the project go in, only the totals (ADR-0166).")] bool ShowHours = false);

/// <summary>The report kept in the documents of the project: the document, its folder, its name and its version.</summary>
public sealed record StatusReportResponse(Guid DocumentId, Guid FolderId, string FolderName, string Name, int Version);

/// <summary>The status report of a project (ADR-0165, ADR-0166), for the Administrator and the manager.</summary>
internal static class StatusReportEndpoints
{
    public static void MapStatusReportEndpoints(this RouteGroupBuilder projects)
    {
        projects.MapGet("/{id:guid}/status-report", DraftAsync).WithName("GetStatusReportDraft")
            .WithDescription("The draft of a new status report of the project: the last texts, the open tickets and the risks suggested (ADR-0165).")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        projects.MapPost("/{id:guid}/status-report", GenerateAsync).WithName("GenerateStatusReport")
            .WithDescription(
                "Generates the status report of the project in PowerPoint and keeps it in the folder of the status reports, in any status of the project (ADR-0165, ADR-0166).")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<StatusReportDraftResponse>, NotFound, ForbidHttpResult>> DraftAsync(
        Guid id, HttpContext http, IUserAccess access, GetStatusReportDraft draft, CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var (result, outcome) = await draft.ExecuteAsync(id, viewer, cancellationToken);
        return outcome switch
        {
            StatusReportOutcome.Forbidden => TypedResults.Forbid(),
            StatusReportOutcome.Done when result is not null => TypedResults.Ok(new StatusReportDraftResponse(
                result.Last is { } last
                    ? new StatusReportLastResponse(last.GeneratedAt, new OverviewPersonResponse(last.GeneratedById, last.GeneratedBy))
                    : null,
                result.Texts.Summary,
                result.Texts.Focus,
                result.Texts.NextSteps,
                result.Texts.ClientNeeds,
                [.. result.Tickets.Select(ticket => new StatusReportTicketResponse(ticket.Number, ticket.Title, ticket.Status, ticket.Threatens))],
                [.. result.Risks.Select(risk => new StatusReportRiskResponse(risk.TicketNumber, risk.Level, risk.Attention))])),
            _ => TypedResults.NotFound(),
        };
    }

    private static async Task<Results<Ok<StatusReportResponse>, NotFound, ForbidHttpResult, ValidationProblem>> GenerateAsync(
        Guid id,
        StatusReportRequest request,
        HttpContext http,
        IUserAccess access,
        GenerateStatusReport generate,
        CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await generate.ExecuteAsync(
            id,
            viewer,
            new StatusReportTexts(request.Summary ?? string.Empty, request.Focus ?? string.Empty, request.NextSteps ?? string.Empty, request.ClientNeeds ?? string.Empty),
            request.Risks?.Select(risk => new StatusReportRiskInput(risk.Number, risk.Level, risk.Attention)).ToList(),
            request.ShowHours,
            cancellationToken);
        return result.Outcome switch
        {
            StatusReportOutcome.Done when result.Saved is { } saved => TypedResults.Ok(
                new StatusReportResponse(saved.DocumentId, saved.FolderId, saved.FolderName, saved.Name, saved.Version)),
            StatusReportOutcome.Forbidden => TypedResults.Forbid(),
            StatusReportOutcome.Invalid => TypedResults.ValidationProblem(
                (result.Invalid ?? []).ToDictionary(FieldOf, MessageOf)),
            _ => TypedResults.NotFound(),
        };
    }

    private static string FieldOf(StatusReportField field) => field switch
    {
        StatusReportField.Summary => "summary",
        StatusReportField.Focus => "focus",
        StatusReportField.NextSteps => "nextSteps",
        StatusReportField.ClientNeeds => "clientNeeds",
        _ => "risks",
    };

    private static string[] MessageOf(StatusReportField field) => field switch
    {
        StatusReportField.Risks =>
            ["From 0 to 10 open tickets of the project, without repeating, each with its level and a point of attention up to 500 characters."],
        _ => ["Up to 1,000 characters, without control characters."],
    };
}
