using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Projects.Endpoints;

/// <summary>
/// A person of a project as the Members tab shows it: the e-mail, the role, when the person joined, the last sign-in
/// (null for who never signed in) and whether the person may be a manager (ADR-0085) or a tech lead (ADR-0097) of a project.
/// </summary>
public sealed record ProjectMemberResponse(
    Guid Id, string Name, string Email, ProjectRole Role, DateTime AddedAt, DateTime? LastSignInAt, bool CanBeManager,
    bool CanBeTechLead);

/// <summary>
/// The person to include, by <c>userId</c> or, for a person of the company directory who never signed in, by the
/// <c>directoryId</c> of the people search (ADR-0005), and the role in the project.
/// </summary>
public sealed record NewProjectMemberRequest(Guid? UserId, string? DirectoryId, ProjectRole Role);

public sealed record ProjectMemberRoleRequest(ProjectRole Role);

/// <summary>The user now in the project; a person of the directory got this user when included.</summary>
public sealed record ProjectMemberCreatedResponse(Guid Id);

/// <summary>
/// The people of a project (ADR-0040, ADR-0042, ADR-0043): whoever sees the project sees them; the Administrator and the
/// manager include, change and remove anyone, the tech lead anyone but managers; the project always has a manager.
/// </summary>
internal static class ProjectMemberEndpoints
{
    public static void MapProjectMemberEndpoints(this RouteGroupBuilder projects)
    {
        projects.MapGet("/{id:guid}/members", ListAsync).WithName("ListProjectMembers");
        projects.MapPost("/{id:guid}/members", AddAsync).WithName("AddProjectMember")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapPut("/{id:guid}/members/{userId:guid}/role", ChangeRoleAsync).WithName("ChangeProjectMemberRole")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
        projects.MapDelete("/{id:guid}/members/{userId:guid}", RemoveAsync).WithName("RemoveProjectMember")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Results<Ok<IReadOnlyList<ProjectMemberResponse>>, NotFound>> ListAsync(
        Guid id, HttpContext http, IUserAccess access, ListProjectMembers listMembers, CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var members = await listMembers.ExecuteAsync(id, viewer, cancellationToken);
        return members is null
            ? TypedResults.NotFound()
            : TypedResults.Ok<IReadOnlyList<ProjectMemberResponse>>([.. members.Select(member => new ProjectMemberResponse(
                member.Id, member.Name, member.Email, member.Role, member.AddedAt, member.LastSignInAt, member.CanBeManager,
                member.CanBeTechLead))]);
    }

    private static async Task<Results<Created<ProjectMemberCreatedResponse>, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> AddAsync(
        Guid id,
        HttpContext http,
        NewProjectMemberRequest request,
        IUserAccess access,
        AddProjectMember addMember,
        CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        var result = await addMember.ExecuteAsync(
            id, new NewProjectMember(request.UserId, request.DirectoryId, request.Role), viewer, cancellationToken);
        return result.Outcome switch
        {
            MemberOutcome.Done => TypedResults.Created((string?)null, new ProjectMemberCreatedResponse(result.UserId!.Value)),
            MemberOutcome.NotFound => TypedResults.NotFound(),
            MemberOutcome.Forbidden => TypedResults.Forbid(),
            MemberOutcome.InvalidPerson or MemberOutcome.InvalidRole => Invalid(result.Outcome),
            _ => Conflict(result.Outcome),
        };
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> ChangeRoleAsync(
        Guid id,
        Guid userId,
        HttpContext http,
        ProjectMemberRoleRequest request,
        IUserAccess access,
        ChangeProjectMemberRole changeRole,
        CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        return Answer(await changeRole.ExecuteAsync(id, userId, request.Role, viewer, cancellationToken));
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult>> RemoveAsync(
        Guid id, Guid userId, HttpContext http, IUserAccess access, RemoveProjectMember removeMember, CancellationToken cancellationToken)
    {
        var viewer = await ProjectEndpoints.ViewerOfAsync(http, access, cancellationToken);
        return Answer(await removeMember.ExecuteAsync(id, userId, viewer, cancellationToken));
    }

    /// <summary>The answer to a change of the role or a removal.</summary>
    private static Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem, ProblemHttpResult> Answer(MemberOutcome outcome) =>
        outcome switch
        {
            MemberOutcome.Done => TypedResults.NoContent(),
            MemberOutcome.NotFound => TypedResults.NotFound(),
            MemberOutcome.Forbidden => TypedResults.Forbid(),
            MemberOutcome.InvalidPerson or MemberOutcome.InvalidRole => Invalid(outcome),
            _ => Conflict(outcome),
        };

    private static ValidationProblem Invalid(MemberOutcome outcome) =>
        TypedResults.ValidationProblem(outcome == MemberOutcome.InvalidPerson
            ? new Dictionary<string, string[]> { ["person"] = ["The person is an active user, or a person of the company directory."] }
            : new Dictionary<string, string[]>
            {
                ["role"] = ["The role is a project role; a manager is an active Project manager or Administrator."],
            });

    private static ProblemHttpResult Conflict(MemberOutcome outcome) =>
        outcome switch
        {
            MemberOutcome.Closed => ProjectEndpoints.Closed(),
            MemberOutcome.MemberExists => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Already in the project",
                detail: "The person is already in the project; change the role instead.",
                type: ProjectProblems.MemberExists),
            _ => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Last manager",
                detail: "The project always has a manager: include another manager first.",
                type: ProjectProblems.LastManager),
        };
}
