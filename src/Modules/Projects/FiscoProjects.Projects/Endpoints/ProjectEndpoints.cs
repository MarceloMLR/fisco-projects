using System.ComponentModel;
using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Projects.Endpoints;

/// <summary>The client of a project, with the address of its current logo (null without one).</summary>
public sealed record ProjectClientResponse(Guid Id, string Name, string? LogoUrl);

/// <summary>A person in a project, with the role in it.</summary>
public sealed record ProjectPersonResponse(Guid Id, string Name, ProjectRole Role);

/// <summary>One project of the list; without a client, it is an internal project.</summary>
public sealed record ProjectListItemResponse(
    Guid Id,
    string Code,
    string Name,
    ProjectClientResponse? Client,
    ProjectStatus Status,
    IReadOnlyList<ProjectPersonResponse> Managers);

public sealed record ProjectPageResponse(IReadOnlyList<ProjectListItemResponse> Items, int Total);

/// <summary>
/// What the person may do in the project (ADR-0043): change the data, only while the project is open, and change the
/// status, including reopening it.
/// </summary>
public sealed record ProjectPermissionsResponse(bool CanEdit, bool CanChangeStatus);

/// <summary>A project as its page shows it, with the people in it and what the person may do.</summary>
public sealed record ProjectResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    ProjectClientResponse? Client,
    ProjectStatus Status,
    IReadOnlyList<ProjectPersonResponse> Members,
    ProjectPermissionsResponse Permissions);

/// <summary>
/// The new project: the code (as ERP-FIN, in any case), the name, the description and the client (none for an internal
/// project), the manager (the person who creates it, when none) and, optionally, the tech lead.
/// </summary>
public sealed record NewProjectRequest(
    string? Code, string? Name, string? Description, Guid? ClientId, Guid? ManagerId, Guid? TechLeadId);

/// <summary>The data of a project that may change after its creation.</summary>
public sealed record ProjectRequest(string? Code, string? Name, string? Description, Guid? ClientId);

public sealed record ProjectStatusRequest(ProjectStatus Status);

public sealed record ProjectCreatedResponse(Guid Id);

/// <summary>Problem types of the projects, so the screen can tell the reason.</summary>
public static class ProjectProblems
{
    /// <summary>Another project has the code; the extension projectName tells which.</summary>
    public const string CodeInUse = "urn:fiscoprojects:problem:project-code-in-use";

    /// <summary>The project is completed or cancelled: reopen it to change it (ADR-0016).</summary>
    public const string Closed = "urn:fiscoprojects:problem:project-closed";
}

/// <summary>
/// The projects (ADR-0016, ADR-0043, ADR-0072): the Administrator and the Project manager create them; the Administrator
/// sees all, and everyone else only the projects they are in; changing a project depends on the role in it.
/// </summary>
internal static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this RouteGroupBuilder api)
    {
        var projects = api.MapGroup("/projects")
            .WithTags("Projects")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        projects.MapGet("/", ListAsync).WithName("ListProjects");
        projects.MapPost("/", CreateAsync).WithName("CreateProject")
            .RequireAuthorization(SystemPolicies.ProjectCreators)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapGet("/{id:guid}", GetAsync).WithName("GetProject");
        projects.MapPut("/{id:guid}", ChangeAsync).WithName("ChangeProject")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapPut("/{id:guid}/status", ChangeStatusAsync).WithName("ChangeProjectStatus")
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<Results<Ok<ProjectPageResponse>, ValidationProblem>> ListAsync(
        HttpContext http,
        LinkGenerator links,
        IUserAccess access,
        ListProjects listProjects,
        CancellationToken cancellationToken,
        [Description("Part of the code or of the name, without telling case or accents apart.")] string? search = null,
        [Description("Only the projects of this client.")] Guid? clientId = null,
        [Description("Active (the default), Closed (completed and cancelled) or All.")] ProjectSituation situation = ProjectSituation.Active,
        [Description("Only the projects the person is in, even for the Administrator.")] bool mine = false,
        int page = 1,
        int pageSize = 25)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors[nameof(page)] = ["Must be 1 or more."];
        }

        if (pageSize is < 1 or > ProjectListQuery.MaxPageSize)
        {
            errors[nameof(pageSize)] = [$"Must be between 1 and {ProjectListQuery.MaxPageSize}."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await listProjects.ExecuteAsync(
            new ProjectListQuery(search, clientId, situation, mine, page, pageSize), viewer, cancellationToken);
        return TypedResults.Ok(new ProjectPageResponse(
            [.. result.Items.Select(item => new ProjectListItemResponse(
                item.Id,
                item.Code,
                item.Name,
                ClientOf(http, links, item.Client),
                item.Status,
                [.. item.Managers.Select(PersonOf)]))],
            result.Total));
    }

    private static async Task<Results<Created<ProjectCreatedResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        HttpContext http, NewProjectRequest request, CreateProject createProject, CancellationToken cancellationToken)
    {
        var creatorId = UserSession.UserIdOf(http.User)
            ?? throw new InvalidOperationException("Projects are created in a session.");
        var result = await createProject.ExecuteAsync(
            new NewProject(request.Code, request.Name, request.Description, request.ClientId, request.ManagerId, request.TechLeadId),
            creatorId,
            cancellationToken);
        return result.Outcome switch
        {
            ProjectSaveOutcome.Saved => TypedResults.Created((string?)null, new ProjectCreatedResponse(result.Id!.Value)),
            ProjectSaveOutcome.CodeInUse => CodeInUse(result.CodeInUseBy),
            _ => Invalid(result.Invalid),
        };
    }

    private static async Task<Results<Ok<ProjectResponse>, NotFound>> GetAsync(
        Guid id,
        HttpContext http,
        LinkGenerator links,
        IUserAccess access,
        GetProject getProject,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var details = await getProject.ExecuteAsync(id, viewer, cancellationToken);
        if (details is null)
        {
            return TypedResults.NotFound();
        }

        var project = details.Project;
        return TypedResults.Ok(new ProjectResponse(
            project.Id,
            project.Code,
            project.Name,
            project.Description,
            ClientOf(http, links, details.Client),
            project.Status,
            [.. details.Members.Select(PersonOf)],
            new ProjectPermissionsResponse(
                CanEdit: details.Permissions.CanEditData && !project.IsClosed,
                CanChangeStatus: details.Permissions.CanChangeStatus)));
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> ChangeAsync(
        Guid id,
        HttpContext http,
        ProjectRequest request,
        IUserAccess access,
        ChangeProject changeProject,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await changeProject.ExecuteAsync(
            id, new ProjectChanges(request.Code, request.Name, request.Description, request.ClientId), viewer, cancellationToken);
        return result.Outcome switch
        {
            ProjectSaveOutcome.Saved => TypedResults.NoContent(),
            ProjectSaveOutcome.NotFound => TypedResults.NotFound(),
            ProjectSaveOutcome.Forbidden => TypedResults.Forbid(),
            ProjectSaveOutcome.Closed => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Project closed",
                detail: "A completed or cancelled project is read-only until it is reopened.",
                type: ProjectProblems.Closed),
            ProjectSaveOutcome.CodeInUse => CodeInUse(result.CodeInUseBy),
            _ => Invalid(result.Invalid),
        };
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> ChangeStatusAsync(
        Guid id,
        HttpContext http,
        ProjectStatusRequest request,
        IUserAccess access,
        ChangeProjectStatus changeStatus,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        return await changeStatus.ExecuteAsync(id, request.Status, viewer, cancellationToken) switch
        {
            ProjectStatusOutcome.Saved => TypedResults.NoContent(),
            ProjectStatusOutcome.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Forbid(),
        };
    }

    /// <summary>The person of the session, and whether the person is the Administrator, read from the database now.</summary>
    private static async Task<ProjectViewer> ViewerOfAsync(HttpContext http, IUserAccess access, CancellationToken cancellationToken)
    {
        var userId = UserSession.UserIdOf(http.User)
            ?? throw new InvalidOperationException("The projects are read in a session.");
        var person = await access.FindAsync(userId, cancellationToken);
        return new ProjectViewer(userId, person is { IsActive: true, Role: SystemRole.Administrator });
    }

    private static ProjectClientResponse? ClientOf(HttpContext http, LinkGenerator links, ProjectClient? client) =>
        client is null
            ? null
            : new ProjectClientResponse(client.Id, client.Name, ClientEndpoints.LogoUrlOf(http, links, client.Id, client.LogoChangedAt));

    private static ProjectPersonResponse PersonOf(ProjectPerson person) => new(person.Id, person.Name, person.Role);

    private static ValidationProblem Invalid(IReadOnlyList<ProjectField> fields) =>
        TypedResults.ValidationProblem(fields.ToDictionary(
            field => field switch
            {
                ProjectField.Code => "code",
                ProjectField.Name => "name",
                ProjectField.Description => "description",
                ProjectField.Client => "clientId",
                ProjectField.Manager => "managerId",
                _ => "techLeadId",
            },
            field => new[]
            {
                field switch
                {
                    ProjectField.Code => "The code has from 2 to 15 letters, digits and hyphens, and starts with a letter.",
                    ProjectField.Name => "The name is needed and has up to 150 characters.",
                    ProjectField.Description => "The description has up to 4,000 characters.",
                    ProjectField.Client => "The client is an active client.",
                    ProjectField.Manager => "The manager is an active Project manager or Administrator.",
                    _ => "The tech lead is an active person other than the manager.",
                },
            }));

    private static ProblemHttpResult CodeInUse(string? projectName) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Code in use",
            detail: "Another project has this code.",
            type: ProjectProblems.CodeInUse,
            extensions: new Dictionary<string, object?> { ["projectName"] = projectName });
}

/// <summary>Routes of the Projects module: the clients and the projects.</summary>
public static class ProjectsEndpoints
{
    public static RouteGroupBuilder MapProjectsEndpoints(this RouteGroupBuilder api)
    {
        api.MapClientEndpoints();
        api.MapProjectEndpoints();
        return api;
    }
}
