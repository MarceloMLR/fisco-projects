using System.ComponentModel;
using FiscoProjects.Projects.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Projects.Endpoints;

/// <summary>
/// The folders of the file storage a project may use (ADR-0122), for who creates projects: the Administrator and the
/// Project manager.
/// </summary>
internal static class ProjectFolderEndpoints
{
    public static void MapProjectFolderEndpoints(this RouteGroupBuilder api)
    {
        api.MapGroup("/project-folders")
            .WithTags("Projects")
            .RequireAuthorization(SystemPolicies.ProjectCreators)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .MapGet("/", ListAsync).WithName("ListProjectFolders")
            .WithDescription("The folders inside a folder of the file storage, or of its root, and whether each may be the folder of a project (ADR-0122).");
    }

    /// <summary>Why a folder may not be the folder of a project, in English, for the problem.</summary>
    public static string ReasonOf(ProjectFolderRefusal? refusal) => refusal switch
    {
        ProjectFolderRefusal.InUse => "Another project uses the folder.",
        ProjectFolderRefusal.InsideProject => "The folder is inside the folder of another project.",
        ProjectFolderRefusal.ProjectsFolder => "The folder is the folder of the projects.",
        ProjectFolderRefusal.AboveProjects => "The folder holds the folder of the projects or of a project.",
        ProjectFolderRefusal.SystemFolder => "The folder is the folder of the system.",
        _ => "The folder does not exist in the file storage.",
    };

    private static async Task<Results<Ok<ProjectFolderChoicesResponse>, NotFound>> ListAsync(
        ProjectFolderChoices choices,
        CancellationToken cancellationToken,
        [Description("The folder whose folders are listed, by the id the list gave; none for the root.")] string? parentId = null,
        [Description("The project being edited, whose own folder may be chosen again.")] Guid? projectId = null)
    {
        var list = await choices.ListAsync(parentId, projectId, cancellationToken);
        return list is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new ProjectFolderChoicesResponse(
                list.Current is { } current ? ChoiceOf(current) : null, [.. list.Folders.Select(ChoiceOf)]));
    }

    private static ProjectFolderChoiceResponse ChoiceOf(ProjectFolderChoice choice) =>
        new(choice.Id, choice.Name, choice.Refusal, choice.ProjectCode);
}
