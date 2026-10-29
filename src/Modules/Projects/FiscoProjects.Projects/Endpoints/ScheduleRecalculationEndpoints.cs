using FiscoProjects.Projects.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Projects.Endpoints;

/// <summary>
/// The tasks and milestones of open projects that change with the calendar as it is now: how many in all, and each
/// project, by the code, with how many of its own.
/// </summary>
public sealed record ScheduleRecalculationResponse(int Items, IReadOnlyList<RecalculatedScheduleResponse> Projects);

public sealed record RecalculatedScheduleResponse(Guid Id, string Code, string Name, int Items);

/// <summary>
/// The recalculation of the dates of the open schedules by the calendar (ADR-0126), only for the Administrator, who
/// changes the calendar (ADR-0046): what it would change, and the recalculation itself.
/// </summary>
internal static class ScheduleRecalculationEndpoints
{
    public static void MapScheduleRecalculationEndpoints(this RouteGroupBuilder api)
    {
        var recalculation = api.MapGroup("/admin/schedule-recalculation")
            .WithTags("Schedule")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        recalculation.MapGet("/", GetAsync).WithName("GetScheduleRecalculation");
        recalculation.MapPost("/", RecalculateAsync).WithName("RecalculateSchedules");
    }

    private static async Task<Ok<ScheduleRecalculationResponse>> GetAsync(
        PreviewScheduleRecalculation preview, CancellationToken cancellationToken) =>
        TypedResults.Ok(Of(await preview.ExecuteAsync(cancellationToken)));

    private static async Task<Ok<ScheduleRecalculationResponse>> RecalculateAsync(
        HttpContext http, RecalculateSchedules recalculate, CancellationToken cancellationToken)
    {
        var actorId = UserSession.UserIdOf(http.User)
            ?? throw new InvalidOperationException("The schedules are recalculated in a session.");
        return TypedResults.Ok(Of(await recalculate.ExecuteAsync(actorId, cancellationToken)));
    }

    private static ScheduleRecalculationResponse Of(IReadOnlyList<RecalculatedSchedule> schedules) => new(
        schedules.Sum(schedule => schedule.Items),
        [.. schedules.Select(schedule => new RecalculatedScheduleResponse(schedule.ProjectId, schedule.Code, schedule.Name, schedule.Items))]);
}
