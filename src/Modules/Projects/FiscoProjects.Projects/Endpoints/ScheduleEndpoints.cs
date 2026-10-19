using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Projects.Endpoints;

/// <summary>The person in charge of a task.</summary>
public sealed record ScheduleOwnerResponse(Guid Id, string Name);

/// <summary>
/// A task or a milestone: the dates in business days of the calendar of the installation, the duration (zero for a
/// milestone), the progress (zero for a milestone), the day it reached 100%, whether it is late, the owner and the ids of
/// the items it depends on.
/// </summary>
public sealed record ScheduleItemResponse(
    Guid Id,
    ScheduleItemKind Kind,
    Guid PhaseId,
    string Name,
    DateOnly Start,
    DateOnly End,
    int Duration,
    int Progress,
    DateOnly? CompletedOn,
    bool Late,
    ScheduleOwnerResponse? Owner,
    IReadOnlyList<Guid> PredecessorIds);

/// <summary>A phase, with its dates (none without items), its progress weighted by duration and its items in order.</summary>
public sealed record SchedulePhaseResponse(
    Guid Id,
    string Name,
    DateOnly? Start,
    DateOnly? End,
    int Progress,
    bool Late,
    IReadOnlyList<ScheduleItemResponse> Items);

/// <summary>
/// The schedule of a project on the day <c>today</c> of the installation: its dates (none without items), its progress,
/// how many tasks are late, and the phases in order.
/// </summary>
public sealed record ScheduleResponse(
    DateOnly Today,
    DateOnly? Start,
    DateOnly? End,
    int Progress,
    int LateTasks,
    IReadOnlyList<SchedulePhaseResponse> Phases);

public sealed record SchedulePhaseRequest(string? Name);

/// <summary>
/// A new task or milestone: the kind, the phase, the name, the start, the duration in business days (zero or none for a
/// milestone), the owner (a person of the project who is not a viewer; none for a milestone), the progress from 0 to
/// 100 (zero for a milestone) and the items it depends on.
/// </summary>
public sealed record NewScheduleItemRequest(
    ScheduleItemKind Kind,
    Guid PhaseId,
    string? Name,
    DateOnly Start,
    int Duration,
    Guid? OwnerId,
    int Progress,
    IReadOnlyList<Guid>? PredecessorIds);

/// <summary>A change of a task or a milestone, whose kind stays.</summary>
public sealed record ScheduleItemRequest(
    Guid PhaseId,
    string? Name,
    DateOnly Start,
    int Duration,
    Guid? OwnerId,
    int Progress,
    IReadOnlyList<Guid>? PredecessorIds);

public sealed record ScheduleCreatedResponse(Guid Id);

/// <summary>How many items after the changed one were pushed later (ADR-0019).</summary>
public sealed record ScheduleItemChangedResponse(int Pushed);

/// <summary>
/// The schedule of a project (ADR-0018 to ADR-0020, ADR-0043): whoever sees the project sees it; only the Administrator
/// and the manager change it, while the project is open. The server computes every date.
/// </summary>
internal static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this RouteGroupBuilder projects)
    {
        projects.MapGet("/{id:guid}/schedule", GetAsync).WithName("GetSchedule");
        projects.MapPost("/{id:guid}/schedule/phases", AddPhaseAsync).WithName("AddSchedulePhase")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapPut("/{id:guid}/schedule/phases/{phaseId:guid}", RenamePhaseAsync).WithName("RenameSchedulePhase")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapDelete("/{id:guid}/schedule/phases/{phaseId:guid}", RemovePhaseAsync).WithName("RemoveSchedulePhase")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapPost("/{id:guid}/schedule/items", AddItemAsync).WithName("AddScheduleItem")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapPut("/{id:guid}/schedule/items/{itemId:guid}", ChangeItemAsync).WithName("ChangeScheduleItem")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapDelete("/{id:guid}/schedule/items/{itemId:guid}", RemoveItemAsync).WithName("RemoveScheduleItem")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Results<Ok<ScheduleResponse>, NotFound>> GetAsync(
        Guid id, HttpContext http, IUserAccess access, GetSchedule getSchedule, CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var view = await getSchedule.ExecuteAsync(id, viewer, cancellationToken);
        if (view is null)
        {
            return TypedResults.NotFound();
        }

        var figures = view.Figures;
        return TypedResults.Ok(new ScheduleResponse(
            figures.Today,
            figures.Start,
            figures.End,
            figures.Progress,
            figures.LateTasks,
            [.. figures.Phases.Select(phase => new SchedulePhaseResponse(
                phase.Phase.Id,
                phase.Phase.Name,
                phase.Start,
                phase.End,
                phase.Progress,
                phase.IsLate,
                [.. phase.Items.Select(item => ItemOf(item, view, figures.Today))]))]));
    }

    private static async Task<Results<Created<ScheduleCreatedResponse>, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> AddPhaseAsync(
        Guid id,
        HttpContext http,
        SchedulePhaseRequest request,
        IUserAccess access,
        AddSchedulePhase addPhase,
        CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await addPhase.ExecuteAsync(id, request.Name, viewer, cancellationToken);
        return result.Outcome switch
        {
            ScheduleOutcome.Done => TypedResults.Created((string?)null, new ScheduleCreatedResponse(result.Id!.Value)),
            ScheduleOutcome.NotFound => TypedResults.NotFound(),
            ScheduleOutcome.Forbidden => TypedResults.Forbid(),
            ScheduleOutcome.Invalid => Invalid(result.Invalid),
            _ => Problem(result.Outcome),
        };
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> RenamePhaseAsync(
        Guid id,
        Guid phaseId,
        HttpContext http,
        SchedulePhaseRequest request,
        IUserAccess access,
        RenameSchedulePhase renamePhase,
        CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        return Answer(await renamePhase.ExecuteAsync(id, phaseId, request.Name, viewer, cancellationToken));
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> RemovePhaseAsync(
        Guid id, Guid phaseId, HttpContext http, IUserAccess access, RemoveSchedulePhase removePhase, CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        return Answer(await removePhase.ExecuteAsync(id, phaseId, viewer, cancellationToken));
    }

    private static async Task<Results<Created<ScheduleCreatedResponse>, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> AddItemAsync(
        Guid id,
        HttpContext http,
        NewScheduleItemRequest request,
        IUserAccess access,
        AddScheduleItem addItem,
        CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await addItem.ExecuteAsync(
            id,
            request.Kind,
            new ScheduleItemForm(
                request.PhaseId, request.Name, request.Start, request.Duration, request.OwnerId, request.Progress, request.PredecessorIds),
            viewer,
            cancellationToken);
        return result.Outcome switch
        {
            ScheduleOutcome.Done => TypedResults.Created((string?)null, new ScheduleCreatedResponse(result.Id!.Value)),
            ScheduleOutcome.NotFound => TypedResults.NotFound(),
            ScheduleOutcome.Forbidden => TypedResults.Forbid(),
            ScheduleOutcome.Invalid => Invalid(result.Invalid),
            _ => Problem(result.Outcome),
        };
    }

    private static async Task<Results<Ok<ScheduleItemChangedResponse>, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> ChangeItemAsync(
        Guid id,
        Guid itemId,
        HttpContext http,
        ScheduleItemRequest request,
        IUserAccess access,
        ChangeScheduleItem changeItem,
        CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await changeItem.ExecuteAsync(
            id,
            itemId,
            new ScheduleItemForm(
                request.PhaseId, request.Name, request.Start, request.Duration, request.OwnerId, request.Progress, request.PredecessorIds),
            viewer,
            cancellationToken);
        return result.Outcome switch
        {
            ScheduleOutcome.Done => TypedResults.Ok(new ScheduleItemChangedResponse(result.Pushed)),
            ScheduleOutcome.NotFound => TypedResults.NotFound(),
            ScheduleOutcome.Forbidden => TypedResults.Forbid(),
            ScheduleOutcome.Invalid => Invalid(result.Invalid),
            _ => Problem(result.Outcome),
        };
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> RemoveItemAsync(
        Guid id, Guid itemId, HttpContext http, IUserAccess access, RemoveScheduleItem removeItem, CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        return Answer(await removeItem.ExecuteAsync(id, itemId, viewer, cancellationToken));
    }

    private static ScheduleItemResponse ItemOf(ScheduleItem item, ScheduleView view, DateOnly today) =>
        new(
            item.Id,
            item.Kind,
            item.PhaseId,
            item.Name,
            item.Start,
            item.End,
            item.Duration,
            item.Progress,
            item.CompletedOn,
            item.IsLate(today),
            item.OwnerId is { } ownerId
                ? new ScheduleOwnerResponse(ownerId, view.OwnerNames.TryGetValue(ownerId, out var name) ? name : string.Empty)
                : null,
            view.Predecessors.TryGetValue(item.Id, out var predecessors) ? predecessors : []);

    /// <summary>The answer to a change without a body of its own.</summary>
    private static Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult> Answer(ScheduleResult result) =>
        result.Outcome switch
        {
            ScheduleOutcome.Done => TypedResults.NoContent(),
            ScheduleOutcome.NotFound => TypedResults.NotFound(),
            ScheduleOutcome.Forbidden => TypedResults.Forbid(),
            ScheduleOutcome.Invalid => Invalid(result.Invalid),
            _ => Problem(result.Outcome),
        };

    /// <summary>A closed project (409) or a dependency that would make a cycle (400, with its own problem type).</summary>
    private static ProblemHttpResult Problem(ScheduleOutcome outcome) =>
        outcome == ScheduleOutcome.Cycle
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Dependency cycle",
                detail: "An item it would depend on already depends on it.",
                type: ProjectProblems.ScheduleCycle)
            : ProjectEndpoints.Closed();

    private static ValidationProblem Invalid(IReadOnlyList<ScheduleField> fields) =>
        TypedResults.ValidationProblem(fields.ToDictionary(
            field => field switch
            {
                ScheduleField.Name => "name",
                ScheduleField.Phase => "phaseId",
                ScheduleField.Start => "start",
                ScheduleField.Duration => "duration",
                ScheduleField.Progress => "progress",
                ScheduleField.Owner => "ownerId",
                _ => "predecessorIds",
            },
            field => new[]
            {
                field switch
                {
                    ScheduleField.Name => "The name is needed and has up to 150 characters for a phase and 200 for a task.",
                    ScheduleField.Phase => "The phase is a phase of the schedule.",
                    ScheduleField.Start => "The start is a day from 2000 to 2099.",
                    ScheduleField.Duration => "A task lasts from 1 to 999 business days; a milestone has no duration.",
                    ScheduleField.Progress => "A task goes from 0 to 100%; a milestone has no progress.",
                    ScheduleField.Owner => "The owner is a person of the project who is not a viewer; a milestone has none.",
                    _ => "The items are tasks and milestones of the schedule, other than the item itself.",
                },
            }));
}
