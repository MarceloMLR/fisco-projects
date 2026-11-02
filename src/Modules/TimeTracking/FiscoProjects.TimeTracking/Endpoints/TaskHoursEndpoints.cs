using FiscoProjects.TimeTracking.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.TimeTracking.Endpoints;

/// <summary>
/// The hours of the tasks of a project and the entries of each task (ADR-0138, ADR-0144), for the Administrator and the
/// manager of the project, of any status: 403 for the other people of the project and 404 for whoever is not in it. The
/// planned hours change only in the routes of the Administrator.
/// </summary>
public static class TaskHoursEndpoints
{
    public static RouteGroupBuilder MapTaskHoursEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);
        var hours = api.MapGroup("/projects/{projectId:guid}/task-hours")
            .WithTags("TimeTracking")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        hours.MapGet("/", TasksAsync).WithName("GetTaskHours");
        hours.MapGet("/{itemId:guid}/entries", EntriesAsync).WithName("ListTaskTimeEntries");
        return api;
    }

    private static async Task<Results<Ok<ProjectTaskHoursResponse>, ForbidHttpResult, NotFound>> TasksAsync(
        Guid projectId, HttpContext http, IUserAccess access, GetTaskHours get, CancellationToken cancellationToken)
    {
        var viewer = await TimeEntryEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var (view, outcome) = await get.ExecuteAsync(projectId, viewer, cancellationToken);
        return outcome switch
        {
            TaskHoursOutcome.Forbidden => TypedResults.Forbid(),
            TaskHoursOutcome.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Ok(new ProjectTaskHoursResponse(
                view!.Hours.Planned,
                view.Hours.Logged,
                view.Hours.Outside,
                [.. view.Hours.Items.Select(task => new TaskHoursResponse(
                    task.ItemId, task.Name, task.Planned, task.Logged, ProjectHoursEndpoints.BalanceOf(task.Balance)))],
                view.CanChangePlanned)),
        };
    }

    private static async Task<Results<Ok<TimeEntryListResponse>, ForbidHttpResult, NotFound>> EntriesAsync(
        Guid projectId,
        Guid itemId,
        HttpContext http,
        IUserAccess access,
        ListTaskTimeEntries list,
        CancellationToken cancellationToken)
    {
        var viewer = await TimeEntryEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var (entries, outcome) = await list.ExecuteAsync(projectId, itemId, viewer, cancellationToken);
        return outcome switch
        {
            TaskHoursOutcome.Forbidden => TypedResults.Forbid(),
            TaskHoursOutcome.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Ok(new TimeEntryListResponse([.. entries!.Items.Select(TimeEntryEndpoints.ResponseOf)], entries.Total)),
        };
    }
}
