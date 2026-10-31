using System.ComponentModel;
using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Contracts;
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

/// <summary>A phase in the strip of phases: its dates, its progress and whether a task or a milestone of it is late.</summary>
public sealed record SchedulePhaseSummaryResponse(string Name, DateOnly Start, DateOnly End, int Progress, bool Late);

/// <summary>
/// The schedule of a project in the list (ADR-0071): its dates, its progress, how many tasks and milestones are late
/// (ADR-0086), the current phase (the first late one or, without delays, the last that has started; none before the
/// start), the day the last task was completed when all are at 100%, the phases with items, the days of the
/// milestones and the business days of delay of the most late task or milestone (ADR-0096).
/// </summary>
public sealed record ProjectScheduleSummaryResponse(
    DateOnly Start,
    DateOnly End,
    int Progress,
    int LateTasks,
    int LateMilestones,
    string? CurrentPhase,
    DateOnly? CompletedOn,
    IReadOnlyList<SchedulePhaseSummaryResponse> Phases,
    IReadOnlyList<DateOnly> Milestones,
    int LateDays);

/// <summary>One project of the list; without a client, it is an internal project; without tasks and milestones, no schedule.</summary>
public sealed record ProjectListItemResponse(
    Guid Id,
    string Code,
    string Name,
    ProjectClientResponse? Client,
    ProjectStatus Status,
    IReadOnlyList<ProjectPersonResponse> Managers,
    ProjectScheduleSummaryResponse? Schedule);

/// <summary>The project of a milestone ahead.</summary>
public sealed record UpcomingMilestoneProjectResponse(Guid Id, string Code, string Name);

/// <summary>
/// A milestone ahead of the projects of the person: the day, the business days left until it (none on the day itself)
/// and whether a request of acceptance for it waits for the answer.
/// </summary>
public sealed record UpcomingMilestoneResponse(
    Guid Id, string Name, DateOnly Day, int BusinessDaysLeft, bool AcceptanceRequested, UpcomingMilestoneProjectResponse Project);

/// <summary>The milestones ahead, the soonest first, and today in the time zone of the installation.</summary>
public sealed record UpcomingMilestonesResponse(IReadOnlyList<UpcomingMilestoneResponse> Items, DateOnly Today);

/// <summary>A page of the list, with today in the time zone of the installation, the day the schedules are seen on.</summary>
public sealed record ProjectPageResponse(IReadOnlyList<ProjectListItemResponse> Items, int Total, DateOnly Today);

/// <summary>
/// What the person may do in the project (ADR-0043): change the data, only while the project is open; change the
/// status, including reopening it; and, while the project is open, include, change and remove people, do it also with
/// the managers (ADR-0042), and change the schedule.
/// </summary>
public sealed record ProjectPermissionsResponse(
    bool CanEdit,
    bool CanChangeStatus,
    bool CanManagePeople,
    bool CanManageManagers,
    bool CanEditSchedule,
    [property: Description("Deletes the project for good, with everything in it: only the Administrator (ADR-0041).")] bool CanDelete,
    [property: Description("Chooses the folder of the project in the file storage, while it has no files: the Administrator and the manager of an open project (ADR-0122).")] bool CanChooseFolder,
    [property: Description("Downloads the schedule in the XML of the MS Project, with the e-mails of the people in charge, also of a closed project: the Administrator and the manager (ADR-0136).")] bool CanExportSchedule);

/// <summary>
/// A project as its page shows it, with the people in it, what the person may do and the dates of the schedule (none
/// without tasks and milestones).
/// </summary>
public sealed record ProjectResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    ProjectClientResponse? Client,
    ProjectStatus Status,
    IReadOnlyList<ProjectPersonResponse> Members,
    ProjectPermissionsResponse Permissions,
    DateOnly? ScheduleStart,
    DateOnly? ScheduleEnd);

/// <summary>
/// The new project: the code (as ERP-FIN, in any case), the name, the description and the client (none for an internal
/// project), the manager (the person who creates it, when none) and, optionally, the tech lead: a user or a person of
/// the company directory who never signed in, by the <c>directoryId</c> of the people search (ADR-0005). With a model, the
/// schedule is made from it, starting on <c>scheduleStart</c> (ADR-0017); instead of a model, <c>importedSchedule</c> is the
/// schedule the preview of the MS Project converted, with who each resource is (ADR-0135). With <c>folderId</c>, a folder
/// of the file storage that already exists, from <c>GET /api/project-folders</c>, holds the files instead of the one the
/// system creates (ADR-0122).
/// </summary>
public sealed record NewProjectRequest(
    string? Code,
    string? Name,
    string? Description,
    Guid? ClientId,
    Guid? ManagerId,
    Guid? TechLeadId,
    string? TechLeadDirectoryId = null,
    Guid? TemplateId = null,
    DateOnly? ScheduleStart = null,
    string? FolderId = null,
    ImportedScheduleRequest? ImportedSchedule = null);

/// <summary>The data of a project that may change after its creation.</summary>
public sealed record ProjectRequest(string? Code, string? Name, string? Description, Guid? ClientId);

public sealed record ProjectStatusRequest(ProjectStatus Status);

/// <summary>
/// The folder of the project in the file storage (ADR-0122): its name, whether someone chose it among the ones that
/// already existed, whether a module stored files of the project (then it stays) and whether the person may change it.
/// </summary>
public sealed record ProjectFolderResponse(string Name, bool Chosen, bool HasFiles, bool CanChange);

/// <summary>A folder that already exists, from <c>GET /api/project-folders</c>; none goes back to the folder the system creates.</summary>
public sealed record ProjectFolderRequest(string? FolderId);

/// <summary>
/// A folder of the file storage (ADR-0122): when it may not be the folder of a project, why, and the code of the
/// project that uses it or holds it.
/// </summary>
public sealed record ProjectFolderChoiceResponse(string Id, string Name, ProjectFolderRefusal? Refusal, string? ProjectCode);

/// <summary>The folder listed (none for the root, which is never chosen) and the folders inside it, in alphabetical order.</summary>
public sealed record ProjectFolderChoicesResponse(ProjectFolderChoiceResponse? Current, IReadOnlyList<ProjectFolderChoiceResponse> Folders);

/// <summary>
/// What completing or cancelling the project would leave behind (ADR-0043): the tickets not closed, which keep it from
/// being completed and are closed by the cancellation, and the tasks below 100% and the requests of acceptance waiting
/// for the answer, warnings.
/// </summary>
public sealed record ProjectClosingResponse(int OpenTickets, int IncompleteTasks, int PendingAcceptances);

public sealed record ProjectCreatedResponse(Guid Id);

/// <summary>What deleting the project would remove (ADR-0041): how many items of each kind it has in every module.</summary>
public sealed record ProjectDeletionResponse(
    Guid Id,
    string Code,
    string Name,
    int Tickets,
    int Documents,
    int Links,
    int Credentials,
    int AcceptanceTerms,
    int ScheduleItems,
    int Members,
    int TimeEntries);

/// <summary>The code of the project, typed by the Administrator to confirm the deletion (ADR-0041).</summary>
public sealed record DeleteProjectRequest(string? Code);

/// <summary>Problem types of the projects, so the screen can tell the reason.</summary>
public static class ProjectProblems
{
    /// <summary>Another project has the code; the extension projectName tells which.</summary>
    public const string CodeInUse = "urn:fiscoprojects:problem:project-code-in-use";

    /// <summary>The project is completed or cancelled: reopen it to change it (ADR-0016).</summary>
    public const string Closed = "urn:fiscoprojects:problem:project-closed";

    /// <summary>
    /// The project has tickets not closed and is not completed (ADR-0043); status 409, with how many in the extension
    /// openTickets.
    /// </summary>
    public const string HasOpenTickets = "urn:fiscoprojects:problem:project-has-open-tickets";

    /// <summary>Someone changed a ticket while the cancellation closed them, and nothing changed (ADR-0043); status 409.</summary>
    public const string TicketsChanged = "urn:fiscoprojects:problem:project-tickets-changed";

    /// <summary>The person is already in the project: change the role instead (ADR-0042).</summary>
    public const string MemberExists = "urn:fiscoprojects:problem:project-member-exists";

    /// <summary>The change would leave the project without a manager (ADR-0042).</summary>
    public const string LastManager = "urn:fiscoprojects:problem:project-last-manager";

    /// <summary>A module stored a file of the project, so its folder does not change anymore (ADR-0122); status 409.</summary>
    public const string HasFiles = "urn:fiscoprojects:problem:project-has-files";

    /// <summary>An item of the schedule would depend on an item that already depends on it (ADR-0019); status 400.</summary>
    public const string ScheduleCycle = "urn:fiscoprojects:problem:schedule-cycle";

    /// <summary>The person has no change of the schedule to undo, or to redo (ADR-0091); status 409.</summary>
    public const string ScheduleNothingToUndo = "urn:fiscoprojects:problem:schedule-nothing-to-undo";

    /// <summary>Someone changed the items of the change after it, so it is not undone nor redone (ADR-0091); status 409.</summary>
    public const string ScheduleUndoBlocked = "urn:fiscoprojects:problem:schedule-undo-blocked";
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
        projects.MapGet("/upcoming-milestones", UpcomingMilestonesAsync).WithName("ListUpcomingMilestones")
            .WithDescription(
                "The first 10 milestones not reached, from today on, of the open projects the person is in (ADR-0096).");
        projects.MapGet("/{id:guid}", GetAsync).WithName("GetProject");
        projects.MapPut("/{id:guid}", ChangeAsync).WithName("ChangeProject")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapPut("/{id:guid}/status", ChangeStatusAsync).WithName("ChangeProjectStatus")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapGet("/{id:guid}/closing", ClosingAsync).WithName("GetProjectClosing")
            .WithDescription("What completing or cancelling the project would leave behind (ADR-0043), for who changes its status.")
            .ProducesProblem(StatusCodes.Status403Forbidden);
        projects.MapGet("/{id:guid}/deletion", DeletionAsync).WithName("GetProjectDeletion")
            .WithDescription("What deleting the project would remove (ADR-0041), for the Administrator.")
            .ProducesProblem(StatusCodes.Status403Forbidden);
        projects.MapPost("/{id:guid}/deletion", DeleteAsync).WithName("DeleteProject")
            .WithDescription(
                "Deletes the project for good, with everything in it (ADR-0041, ADR-0111): only the Administrator, with the code of the project.")
            .ProducesProblem(StatusCodes.Status403Forbidden);
        projects.MapGet("/{id:guid}/folder", FolderAsync).WithName("GetProjectFolder")
            .WithDescription("The folder of the project in the file storage (ADR-0122).");
        projects.MapPut("/{id:guid}/folder", ChangeFolderAsync).WithName("ChangeProjectFolder")
            .WithDescription(
                "Changes the folder of the project to one that already exists, or back to the one the system creates, while the project has no files (ADR-0122).")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapMsProjectImportEndpoints();
        projects.MapProjectMemberEndpoints();
        projects.MapScheduleEndpoints();
        projects.MapOverviewEndpoints();
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
                [.. item.Managers.Select(PersonOf)],
                item.Schedule is { } schedule ? SummaryOf(schedule, item.LateDays) : null))],
            result.Total,
            result.Today));
    }

    private static async Task<Ok<UpcomingMilestonesResponse>> UpcomingMilestonesAsync(
        HttpContext http, ListUpcomingMilestones list, CancellationToken cancellationToken)
    {
        var userId = UserSession.UserIdOf(http.User)
            ?? throw new InvalidOperationException("The milestones are read in a session.");
        var result = await list.ExecuteAsync(userId, cancellationToken);
        return TypedResults.Ok(new UpcomingMilestonesResponse(
            [.. result.Items.Select(item => new UpcomingMilestoneResponse(
                item.Milestone.Id,
                item.Milestone.Name,
                item.Milestone.Day,
                item.BusinessDaysLeft,
                item.AcceptanceRequested,
                new UpcomingMilestoneProjectResponse(item.Milestone.ProjectId, item.Milestone.ProjectCode, item.Milestone.ProjectName)))],
            result.Today));
    }

    private static async Task<Results<Created<ProjectCreatedResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        HttpContext http, NewProjectRequest request, CreateProject createProject, CancellationToken cancellationToken)
    {
        var creatorId = UserSession.UserIdOf(http.User)
            ?? throw new InvalidOperationException("Projects are created in a session.");
        var imported = request.ImportedSchedule is { } schedule ? MsProjectImportEndpoints.Of(schedule) : null;
        if (request.ImportedSchedule is not null && imported is null)
        {
            return Invalid([ProjectField.ImportedSchedule]);
        }

        var result = await createProject.ExecuteAsync(
            new NewProject(
                request.Code, request.Name, request.Description, request.ClientId, request.ManagerId, request.TechLeadId,
                request.TechLeadDirectoryId, request.TemplateId, request.ScheduleStart, request.FolderId, imported),
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
                CanChangeStatus: details.Permissions.CanChangeStatus,
                CanManagePeople: details.Permissions.CanManagePeople && !project.IsClosed,
                CanManageManagers: details.Permissions.CanManageManagers && !project.IsClosed,
                CanEditSchedule: details.Permissions.CanEditSchedule && !project.IsClosed,
                CanDelete: details.Permissions.CanDelete,
                CanChooseFolder: details.Permissions.CanChooseFolder && !project.IsClosed,
                CanExportSchedule: details.Permissions.CanExportSchedule),
            details.Schedule?.Start,
            details.Schedule?.End));
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
            ProjectSaveOutcome.Closed => Closed(),
            ProjectSaveOutcome.CodeInUse => CodeInUse(result.CodeInUseBy),
            _ => Invalid(result.Invalid),
        };
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, ProblemHttpResult>> ChangeStatusAsync(
        Guid id,
        HttpContext http,
        ProjectStatusRequest request,
        IUserAccess access,
        ChangeProjectStatus changeStatus,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await changeStatus.ExecuteAsync(id, request.Status, viewer, cancellationToken);
        return result.Outcome switch
        {
            ProjectStatusOutcome.Saved => TypedResults.NoContent(),
            ProjectStatusOutcome.NotFound => TypedResults.NotFound(),
            ProjectStatusOutcome.HasOpenTickets => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Open tickets",
                detail: "The project is completed only with all its tickets closed.",
                type: ProjectProblems.HasOpenTickets,
                extensions: new Dictionary<string, object?> { ["openTickets"] = result.OpenTickets }),
            ProjectStatusOutcome.TicketsChanged => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tickets changed",
                detail: "Someone changed a ticket of the project at the same time; nothing changed.",
                type: ProjectProblems.TicketsChanged),
            _ => TypedResults.Forbid(),
        };
    }

    private static async Task<Results<Ok<ProjectFolderResponse>, NotFound>> FolderAsync(
        Guid id, HttpContext http, IUserAccess access, GetProjectFolder getFolder, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        return await getFolder.ExecuteAsync(id, viewer, cancellationToken) is { } folder
            ? TypedResults.Ok(new ProjectFolderResponse(folder.Name, folder.Chosen, folder.HasFiles, folder.CanChange))
            : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> ChangeFolderAsync(
        Guid id,
        HttpContext http,
        ProjectFolderRequest request,
        IUserAccess access,
        ChangeProjectFolder changeFolder,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var result = await changeFolder.ExecuteAsync(id, request.FolderId, viewer, cancellationToken);
        return result.Outcome switch
        {
            ProjectFolderOutcome.Saved => TypedResults.NoContent(),
            ProjectFolderOutcome.NotFound => TypedResults.NotFound(),
            ProjectFolderOutcome.Forbidden => TypedResults.Forbid(),
            ProjectFolderOutcome.Closed => Closed(),
            ProjectFolderOutcome.HasFiles => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Project with files",
                detail: "The folder of a project does not change after the project stores files.",
                type: ProjectProblems.HasFiles),
            _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["folderId"] = [ProjectFolderEndpoints.ReasonOf(result.Refusal)],
            }),
        };
    }

    private static async Task<Results<Ok<ProjectDeletionResponse>, NotFound, ForbidHttpResult>> DeletionAsync(
        Guid id, HttpContext http, IUserAccess access, GetProjectDeletion getDeletion, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var (outcome, summary) = await getDeletion.ExecuteAsync(id, viewer, cancellationToken);
        return outcome switch
        {
            ProjectDeletionOutcome.Deleted => TypedResults.Ok(new ProjectDeletionResponse(
                summary!.Id,
                summary.Code,
                summary.Name,
                summary.CountOf(ProjectItemKind.Tickets),
                summary.CountOf(ProjectItemKind.Documents),
                summary.CountOf(ProjectItemKind.Links),
                summary.CountOf(ProjectItemKind.Credentials),
                summary.CountOf(ProjectItemKind.AcceptanceTerms),
                summary.CountOf(ProjectItemKind.ScheduleItems),
                summary.CountOf(ProjectItemKind.Members),
                summary.CountOf(ProjectItemKind.TimeEntries))),
            ProjectDeletionOutcome.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Forbid(),
        };
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem>> DeleteAsync(
        Guid id,
        HttpContext http,
        DeleteProjectRequest request,
        IUserAccess access,
        DeleteProject deleteProject,
        CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        return await deleteProject.ExecuteAsync(id, request.Code, viewer, cancellationToken) switch
        {
            ProjectDeletionOutcome.Deleted => TypedResults.NoContent(),
            ProjectDeletionOutcome.NotFound => TypedResults.NotFound(),
            ProjectDeletionOutcome.Forbidden => TypedResults.Forbid(),
            _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["code"] = ["Type the code of the project to delete it."],
            }),
        };
    }

    private static async Task<Results<Ok<ProjectClosingResponse>, NotFound, ForbidHttpResult>> ClosingAsync(
        Guid id, HttpContext http, IUserAccess access, GetProjectClosing getClosing, CancellationToken cancellationToken)
    {
        var viewer = await ViewerOfAsync(http, access, cancellationToken);
        var (outcome, closing) = await getClosing.ExecuteAsync(id, viewer, cancellationToken);
        return outcome switch
        {
            ProjectStatusOutcome.Saved => TypedResults.Ok(new ProjectClosingResponse(closing!.OpenTickets, closing.IncompleteTasks, closing.PendingAcceptances)),
            ProjectStatusOutcome.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Forbid(),
        };
    }

    /// <summary>The person of the session, and whether the person is the Administrator, read from the database now.</summary>
    internal static async Task<ProjectViewer> ViewerOfAsync(HttpContext http, IUserAccess access, CancellationToken cancellationToken)
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

    private static ProjectScheduleSummaryResponse SummaryOf(ScheduleFigures figures, int lateDays) =>
        new(
            figures.Start!.Value,
            figures.End!.Value,
            figures.Progress,
            figures.LateTasks,
            figures.LateMilestones,
            figures.CurrentPhase?.Name,
            figures.CompletedOn,
            [.. figures.Phases
                .Where(phase => phase.Start is not null)
                .Select(phase => new SchedulePhaseSummaryResponse(
                    phase.Phase.Name, phase.Start!.Value, phase.End!.Value, phase.Progress, phase.IsLate))],
            figures.Milestones,
            lateDays);

    private static ValidationProblem Invalid(IReadOnlyList<ProjectField> fields) =>
        TypedResults.ValidationProblem(fields.ToDictionary(
            field => field switch
            {
                ProjectField.Code => "code",
                ProjectField.Name => "name",
                ProjectField.Description => "description",
                ProjectField.Client => "clientId",
                ProjectField.Manager => "managerId",
                ProjectField.Template => "templateId",
                ProjectField.ScheduleStart => "scheduleStart",
                ProjectField.Folder => "folderId",
                ProjectField.ImportedSchedule => "importedSchedule",
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
                    ProjectField.Template => "The model is an active model of project.",
                    ProjectField.ScheduleStart => "A project made from a model or the MS Project needs the day its schedule starts, from 2000 to 2099.",
                    ProjectField.Folder => "The folder exists and is no folder of another project, of the projects or of the system, nor inside or above one.",
                    ProjectField.ImportedSchedule => "The schedule converted from the MS Project, without a model, holds together and names people who may be in the project.",
                    _ => "The tech lead is an active Tech lead or Administrator other than the manager.",
                },
            }));

    internal static ProblemHttpResult Closed() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Project closed",
            detail: "A completed or cancelled project is read-only until it is reopened.",
            type: ProjectProblems.Closed);

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
        api.MapProjectFolderEndpoints();
        api.MapTemplateEndpoints();
        api.MapScheduleRecalculationEndpoints();
        return api;
    }
}
