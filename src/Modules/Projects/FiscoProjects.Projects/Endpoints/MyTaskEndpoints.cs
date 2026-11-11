using System.ComponentModel;
using FiscoProjects.Projects.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Projects.Endpoints;

/// <summary>A task of the person in Minhas tarefas: the item as the Schedule tab shows it, its phase and its parent task.</summary>
public sealed record MyTaskResponse(
    ScheduleItemResponse Item,
    string Phase,
    [property: Description("The task the subtask is in; null for a task right in the phase (ADR-0088).")] string? Parent);

/// <summary>A project with the tasks of the person, the open ones by the end and then the completed ones.</summary>
public sealed record MyTasksProjectResponse(Guid Id, string Code, string Name, IReadOnlyList<MyTaskResponse> Tasks);

/// <summary>The tasks of the person by project, by the code, and today in the time zone of the installation.</summary>
public sealed record MyTasksResponse(DateOnly Today, IReadOnlyList<MyTasksProjectResponse> Projects);

/// <summary>
/// Minhas tarefas (ADR-0158): the tasks the person in the session is in charge of, in the open projects the person is
/// in; only the person's own, with nothing new about who sees what.
/// </summary>
internal static class MyTaskEndpoints
{
    public static void MapMyTaskEndpoints(this RouteGroupBuilder api) =>
        api.MapGet("/my-tasks", ListAsync).WithName("ListMyTasks");

    private static async Task<Ok<MyTasksResponse>> ListAsync(
        HttpContext http,
        IUserAccess access,
        ListMyTasks list,
        CancellationToken cancellationToken,
        [Description("Also the tasks completed in the last 30 days.")] bool done = false)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var tasks = await list.ExecuteAsync(viewer, done, cancellationToken);
        return TypedResults.Ok(new MyTasksResponse(
            tasks.Today,
            [.. tasks.Projects.Select(project => new MyTasksProjectResponse(
                project.Project.Id,
                project.Project.Code,
                project.Project.Name,
                [.. project.Tasks.Select(task => new MyTaskResponse(
                    ScheduleEndpoints.ItemOf(task.Item, project.View), task.Phase, task.Parent))]))]));
    }
}
