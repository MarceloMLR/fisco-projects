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
/// milestone), the progress (zero for a milestone), the day a task reached 100% or a milestone was reached, whether it
/// is late and by how many business days (from the day after the end up to today; for a task with subtasks, the most
/// of its subtasks; zero when it is not late, and while the delay began on non-business days only), whether the person
/// may update its progress now (only a task without subtasks, while the project is open), the owner, the ids of the
/// items it depends on, the task it is in and whether it has subtasks, whose dates and progress it then shows
/// (ADR-0088), whether it is on the critical path (never a task with subtasks), and its start and end in the baseline
/// with the business days its end is after (positive) or before (negative) them, all null when it was not in the
/// baseline (ADR-0021).
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
    DateOnly? ReachedOn,
    bool Late,
    int LateDays,
    bool CanUpdateProgress,
    ScheduleOwnerResponse? Owner,
    IReadOnlyList<Guid> PredecessorIds,
    Guid? ParentId,
    bool HasSubtasks,
    bool Critical,
    DateOnly? BaselineStart,
    DateOnly? BaselineEnd,
    int? BaselineDeviation);

/// <summary>
/// The baseline of the project (ADR-0021): the day it was saved, the start and the end of the project then, and the
/// business days the end of the project now is after (positive) or before (negative) it, null without items now.
/// </summary>
public sealed record ScheduleBaselineResponse(DateOnly SavedOn, DateOnly Start, DateOnly End, int? Deviation);

/// <summary>
/// A phase, with its dates (none without items), its progress weighted by duration and its items in order, each task
/// followed by its subtasks.
/// </summary>
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
/// how many tasks and milestones are late, the phases in order, the non-business days of the calendar from the month
/// before the start to the month after the end, for the timeline to shade (none without items), the baseline, none
/// until the manager saves one, and, for who edits the schedule, what undoing and redoing would do, none when nothing.
/// </summary>
public sealed record ScheduleResponse(
    DateOnly Today,
    DateOnly? Start,
    DateOnly? End,
    int Progress,
    int LateTasks,
    int LateMilestones,
    IReadOnlyList<SchedulePhaseResponse> Phases,
    IReadOnlyList<DateOnly> NonWorkingDays,
    ScheduleBaselineResponse? Baseline,
    ScheduleStepResponse? Undo,
    ScheduleStepResponse? Redo);

/// <summary>
/// A change of the schedule the person made (ADR-0091): its kind and the name of the phase or item, as what undoing or
/// redoing does, or did.
/// </summary>
public sealed record ScheduleStepResponse(ScheduleAction Action, string Name);

public sealed record SchedulePhaseRequest(string? Name);

/// <summary>
/// A new task or milestone: the kind, the phase, the name, the start, the duration in business days (zero or none for a
/// milestone), the owner (a person of the project who is not a viewer; none for a milestone), the progress from 0 to
/// 100 (zero for a milestone), the items it depends on, and the task it goes into, right in the same phase (none for
/// right in the phase). The first subtask of a task takes the dependencies of the task (ADR-0088).
/// </summary>
public sealed record NewScheduleItemRequest(
    ScheduleItemKind Kind,
    Guid PhaseId,
    string? Name,
    DateOnly Start,
    int Duration,
    Guid? OwnerId,
    int Progress,
    IReadOnlyList<Guid>? PredecessorIds,
    Guid? ParentId = null);

/// <summary>
/// A change of a task or a milestone, whose kind stays; without the progress, it stays as it is. Without the task it is
/// in, it goes right in the phase. A task with subtasks changes only its phase, name and owner: its start and duration
/// are not looked at, and it has no dependencies (ADR-0088).
/// </summary>
public sealed record ScheduleItemRequest(
    Guid PhaseId,
    string? Name,
    DateOnly Start,
    int Duration,
    Guid? OwnerId,
    int? Progress,
    IReadOnlyList<Guid>? PredecessorIds,
    Guid? ParentId = null);

/// <summary>The progress of a task, from 0 to 100.</summary>
public sealed record ScheduleProgressRequest(int Progress);

/// <summary>Whether a milestone was reached.</summary>
public sealed record MilestoneReachedRequest(bool Reached);

/// <summary>
/// New dates from the timeline: the start alone keeps the duration; with the end, a task lasts the business days
/// between them. A milestone has no end.
/// </summary>
public sealed record ScheduleItemDatesRequest(DateOnly Start, DateOnly? End);

public sealed record ScheduleCreatedResponse(Guid Id);

/// <summary>How many items after the changed one were pushed later (ADR-0019).</summary>
public sealed record ScheduleItemChangedResponse(int Pushed);

/// <summary>
/// The schedule of a project (ADR-0018 to ADR-0020, ADR-0043): whoever sees the project sees it; only the Administrator
/// and the manager change it, and the people in charge of the tasks update their progress, while the project is open.
/// The server computes every date.
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
        projects.MapPut("/{id:guid}/schedule/items/{itemId:guid}/progress", UpdateProgressAsync).WithName("UpdateScheduleItemProgress")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapPut("/{id:guid}/schedule/items/{itemId:guid}/reached", SetReachedAsync).WithName("SetMilestoneReached")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapPut("/{id:guid}/schedule/items/{itemId:guid}/dates", RescheduleAsync).WithName("RescheduleScheduleItem")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapPut("/{id:guid}/schedule/baseline", SaveBaselineAsync).WithName("SaveScheduleBaseline")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapPost("/{id:guid}/schedule/undo", UndoAsync).WithName("UndoScheduleChange")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapPost("/{id:guid}/schedule/redo", RedoAsync).WithName("RedoScheduleChange")
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
            figures.LateMilestones,
            [.. figures.Phases.Select(phase => new SchedulePhaseResponse(
                phase.Phase.Id,
                phase.Phase.Name,
                phase.Start,
                phase.End,
                phase.Progress,
                phase.IsLate,
                [.. phase.Items.Select(item => ItemOf(item, view))]))],
            view.NonWorkingDays,
            view.Baseline is { } baseline
                ? new ScheduleBaselineResponse(baseline.SavedOn, baseline.Start, baseline.End, baseline.Deviation)
                : null,
            StepOf(view.Undo),
            StepOf(view.Redo)));
    }

    private static ScheduleStepResponse? StepOf(ScheduleChangeLabel? label) =>
        label is null ? null : new ScheduleStepResponse(label.Action, label.Name);

    /// <summary>Undoes the last change of the schedule the person made (ADR-0091), saying what it was.</summary>
    private static async Task<Results<Ok<ScheduleStepResponse>, NotFound, ForbidHttpResult, ProblemHttpResult>> UndoAsync(
        Guid id, HttpContext http, IUserAccess access, UndoScheduleChange undo, CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        return Step(await undo.ExecuteAsync(id, viewer, cancellationToken));
    }

    /// <summary>Redoes the last change of the schedule the person undid (ADR-0091), saying what it was.</summary>
    private static async Task<Results<Ok<ScheduleStepResponse>, NotFound, ForbidHttpResult, ProblemHttpResult>> RedoAsync(
        Guid id, HttpContext http, IUserAccess access, RedoScheduleChange redo, CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        return Step(await redo.ExecuteAsync(id, viewer, cancellationToken));
    }

    private static Results<Ok<ScheduleStepResponse>, NotFound, ForbidHttpResult, ProblemHttpResult> Step(ScheduleResult result) =>
        result.Outcome switch
        {
            ScheduleOutcome.Done => TypedResults.Ok(StepOf(result.Label)!),
            ScheduleOutcome.NotFound => TypedResults.NotFound(),
            ScheduleOutcome.Forbidden => TypedResults.Forbid(),
            ScheduleOutcome.NothingToUndo => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Nothing to undo",
                detail: "The person has no change of the schedule to undo or redo.",
                type: ProjectProblems.ScheduleNothingToUndo),
            ScheduleOutcome.UndoBlocked => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Changed since",
                detail: "Someone changed these items after the change; it left the history of the person.",
                type: ProjectProblems.ScheduleUndoBlocked),
            _ => Problem(result.Outcome),
        };

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
                request.PhaseId,
                request.Name,
                request.Start,
                request.Duration,
                request.OwnerId,
                request.Progress,
                request.PredecessorIds,
                request.ParentId),
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
                request.PhaseId,
                request.Name,
                request.Start,
                request.Duration,
                request.OwnerId,
                request.Progress,
                request.PredecessorIds,
                request.ParentId),
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

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> UpdateProgressAsync(
        Guid id,
        Guid itemId,
        HttpContext http,
        ScheduleProgressRequest request,
        IUserAccess access,
        UpdateScheduleItemProgress updateProgress,
        CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        return Answer(await updateProgress.ExecuteAsync(id, itemId, request.Progress, viewer, cancellationToken));
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> SetReachedAsync(
        Guid id,
        Guid itemId,
        HttpContext http,
        MilestoneReachedRequest request,
        IUserAccess access,
        SetMilestoneReached setReached,
        CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        return Answer(await setReached.ExecuteAsync(id, itemId, request.Reached, viewer, cancellationToken));
    }

    private static async Task<Results<Ok<ScheduleItemChangedResponse>, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> RescheduleAsync(
        Guid id,
        Guid itemId,
        HttpContext http,
        ScheduleItemDatesRequest request,
        IUserAccess access,
        RescheduleScheduleItem reschedule,
        CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await reschedule.ExecuteAsync(id, itemId, new ScheduleItemDates(request.Start, request.End), viewer, cancellationToken);
        return result.Outcome switch
        {
            ScheduleOutcome.Done => TypedResults.Ok(new ScheduleItemChangedResponse(result.Pushed)),
            ScheduleOutcome.NotFound => TypedResults.NotFound(),
            ScheduleOutcome.Forbidden => TypedResults.Forbid(),
            ScheduleOutcome.Invalid => Invalid(result.Invalid),
            _ => Problem(result.Outcome),
        };
    }

    /// <summary>
    /// Saves the baseline of the project, or updates it, with the dates of the schedule today (ADR-0021). Without a
    /// body: the server copies the dates it has.
    /// </summary>
    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> SaveBaselineAsync(
        Guid id, HttpContext http, IUserAccess access, SaveScheduleBaseline saveBaseline, CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        return Answer(await saveBaseline.ExecuteAsync(id, viewer, cancellationToken));
    }

    private static ScheduleItemResponse ItemOf(ScheduleItem item, ScheduleView view)
    {
        var baseline = view.Baseline is not null && view.Baseline.Items.TryGetValue(item.Id, out var saved) ? saved : null;
        return new(
            item.Id,
            item.Kind,
            item.PhaseId,
            item.Name,
            item.Start,
            item.End,
            item.Duration,
            item.Progress,
            item.CompletedOn,
            item.ReachedOn,
            view.Figures.LateItems.Contains(item.Id),
            view.LateDays.TryGetValue(item.Id, out var lateDays) ? lateDays : 0,
            view.ProgressUpdatable.Contains(item.Id),
            item.OwnerId is { } ownerId
                ? new ScheduleOwnerResponse(ownerId, view.OwnerNames.TryGetValue(ownerId, out var name) ? name : string.Empty)
                : null,
            view.Predecessors.TryGetValue(item.Id, out var predecessors) ? predecessors : [],
            item.ParentId,
            view.Figures.Summaries.Contains(item.Id),
            view.Critical.Contains(item.Id),
            baseline?.Start,
            baseline?.End,
            baseline?.Deviation);
    }

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
                ScheduleField.Reached => "reached",
                ScheduleField.End => "end",
                ScheduleField.Parent => "parentId",
                ScheduleField.Baseline => "baseline",
                _ => "predecessorIds",
            },
            field => new[]
            {
                field switch
                {
                    ScheduleField.Name => "The name is needed and has up to 150 characters for a phase and 200 for a task.",
                    ScheduleField.Phase => "The phase is a phase of the schedule.",
                    ScheduleField.Start => "The start is a day from 2000 to 2099; a task with subtasks takes its dates from them.",
                    ScheduleField.Duration => "A task lasts from 1 to 999 business days; a milestone has no duration.",
                    ScheduleField.Progress => "A task goes from 0 to 100%; a milestone has no progress, and a task with subtasks takes it from them.",
                    ScheduleField.Owner => "The owner is a person of the project who is not a viewer; a milestone has none.",
                    ScheduleField.Reached => "Only a milestone is reached; a task has a progress.",
                    ScheduleField.End => "The end of a task comes on or after its start, up to 999 business days; a milestone has no end.",
                    ScheduleField.Parent => "The item goes into a task right in the same phase, which is not inside another; a task with subtasks goes into none.",
                    ScheduleField.Baseline => "A baseline needs tasks or milestones in the schedule.",
                    _ => "The items are tasks and milestones of the schedule with dates of their own, other than the item itself and the task it goes into.",
                },
            }));
}
