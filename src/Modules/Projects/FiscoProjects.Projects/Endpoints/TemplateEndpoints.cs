using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Projects.Endpoints;

/// <summary>A model of project: its name, description and situation, and its phases with their items (ADR-0017).</summary>
public sealed record TemplateResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    IReadOnlyList<TemplatePhaseResponse> Phases);

/// <summary>A phase of a model with its items in order.</summary>
public sealed record TemplatePhaseResponse(string Name, IReadOnlyList<TemplateItemResponse> Items);

/// <summary>
/// A task or milestone of a model: its kind, name, duration in business days, how many business days after the start
/// it starts, and whether it is a subtask.
/// </summary>
public sealed record TemplateItemResponse(ScheduleItemKind Kind, string Name, int StartOffset, int Duration, bool IsSubtask);

/// <summary>A new model from a schedule: its name and, optionally, its description.</summary>
public sealed record NewTemplateRequest(string? Name, string? Description);

/// <summary>The name, the description and the situation of a model.</summary>
public sealed record TemplateRequest(string? Name, string? Description, bool IsActive);

public sealed record TemplateCreatedResponse(Guid Id);

/// <summary>
/// The models of project (ADR-0017): saved from a schedule by the Administrator and the manager (ADR-0043), offered to
/// whoever creates projects while active, and kept by the Administrator (ADR-0046).
/// </summary>
internal static class TemplateEndpoints
{
    public const string NameInUse = "urn:fiscoprojects:problem:template-name-in-use";

    public static void MapTemplateEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/projects/{id:guid}/schedule/template", SaveAsync).WithName("SaveScheduleAsTemplate")
            .WithTags("Templates")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        api.MapGet("/project-templates", ListActiveAsync).WithName("ListTemplates")
            .WithTags("Templates")
            .RequireAuthorization(SystemPolicies.ProjectCreators)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        var admin = api.MapGroup("/admin/project-templates")
            .WithTags("Templates")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        admin.MapGet("/", ListAllAsync).WithName("ListAllTemplates");
        admin.MapPut("/{id:guid}", ChangeAsync).WithName("ChangeTemplate").ProducesProblem(StatusCodes.Status409Conflict);
        admin.MapDelete("/{id:guid}", RemoveAsync).WithName("RemoveTemplate");
    }

    private static async Task<Results<Created<TemplateCreatedResponse>, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> SaveAsync(
        Guid id, HttpContext http, NewTemplateRequest request, IUserAccess access, SaveScheduleAsTemplate save, CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await save.ExecuteAsync(id, request.Name, request.Description, viewer, cancellationToken);
        return result.Outcome switch
        {
            TemplateOutcome.Done => TypedResults.Created((string?)null, new TemplateCreatedResponse(result.Id!.Value)),
            TemplateOutcome.NotFound => TypedResults.NotFound(),
            TemplateOutcome.Forbidden => TypedResults.Forbid(),
            TemplateOutcome.Invalid => Invalid(result.Invalid),
            _ => InUse(),
        };
    }

    private static async Task<Ok<IReadOnlyList<TemplateResponse>>> ListActiveAsync(ListTemplates list, CancellationToken cancellationToken) =>
        TypedResults.Ok(Of(await list.ExecuteAsync(activeOnly: true, cancellationToken)));

    private static async Task<Ok<IReadOnlyList<TemplateResponse>>> ListAllAsync(ListTemplates list, CancellationToken cancellationToken) =>
        TypedResults.Ok(Of(await list.ExecuteAsync(activeOnly: false, cancellationToken)));

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> ChangeAsync(
        Guid id, TemplateRequest request, ChangeTemplate change, CancellationToken cancellationToken)
    {
        var result = await change.ExecuteAsync(id, request.Name, request.Description, request.IsActive, cancellationToken);
        return result.Outcome switch
        {
            TemplateOutcome.Done => TypedResults.NoContent(),
            TemplateOutcome.NotFound => TypedResults.NotFound(),
            TemplateOutcome.Invalid => Invalid(result.Invalid),
            _ => InUse(),
        };
    }

    private static async Task<Results<NoContent, NotFound>> RemoveAsync(Guid id, RemoveTemplate remove, CancellationToken cancellationToken) =>
        (await remove.ExecuteAsync(id, cancellationToken)).Outcome == TemplateOutcome.Done ? TypedResults.NoContent() : TypedResults.NotFound();

    private static IReadOnlyList<TemplateResponse> Of(IReadOnlyList<ProjectTemplate> templates) =>
        [.. templates.Select(template => new TemplateResponse(
            template.Id,
            template.Name,
            template.Description,
            template.IsActive,
            [.. template.Phases.OrderBy(phase => phase.Position).Select(phase => new TemplatePhaseResponse(
                phase.Name,
                [.. template.ItemsOf(phase.Id).Select(item => new TemplateItemResponse(
                    item.Kind, item.Name, item.StartOffset, item.Duration, item.ParentId is not null))]))]))];

    private static ProblemHttpResult InUse() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Name in use",
            detail: "Another model of project has the name.",
            type: NameInUse);

    private static ValidationProblem Invalid(IReadOnlyList<TemplateField> fields) =>
        TypedResults.ValidationProblem(fields.ToDictionary(
            field => field switch
            {
                TemplateField.Name => "name",
                TemplateField.Description => "description",
                _ => "schedule",
            },
            field => new[]
            {
                field switch
                {
                    TemplateField.Name => "The name is needed and has up to 150 characters.",
                    TemplateField.Description => "The description has up to 500 characters.",
                    _ => "A model needs tasks or milestones in the schedule.",
                },
            }));
}
