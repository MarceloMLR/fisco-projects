using System.ComponentModel;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Users.Endpoints;

/// <summary>Where a person stands: with access, deactivated (ADR-0008) or anonymized, for good (ADR-0110).</summary>
public enum UserStatus
{
    Active,
    Inactive,
    Anonymized,
}

/// <summary>One person on the Users screen.</summary>
public sealed record UserResponse(
    Guid Id, string Name, string Email, SystemRole Role, UserStatus Status, DateTime? LastSignInAt);

public sealed record UserPageResponse(IReadOnlyList<UserResponse> Items, int Total);

public sealed record ChangeRoleRequest(SystemRole Role);

/// <summary>Problem types of the user administration, so the screen can tell the reason.</summary>
public static class UserProblems
{
    public const string LastAdministrator = "urn:fiscoprojects:problem:last-administrator";

    /// <summary>The person was anonymized: no reactivation, role or second anonymization (ADR-0110).</summary>
    public const string Anonymized = "urn:fiscoprojects:problem:user-anonymized";

    /// <summary>The person is active: the Administrator deactivates before anonymizing (ADR-0110).</summary>
    public const string Active = "urn:fiscoprojects:problem:user-active";
}

/// <summary>Users screen of the Administration: only the Administrator (ADR-0008, ADR-0046).</summary>
internal static class UserAdministrationEndpoints
{
    public static void MapUserAdministrationEndpoints(this RouteGroupBuilder api)
    {
        var users = api.MapGroup("/users")
            .RequireAuthorization(SystemPolicies.Administrator)
            .WithTags("Users")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        users.MapGet("/", ListAsync).WithName("ListUsers");
        users.MapPut("/{id:guid}/role", ChangeRoleAsync).WithName("ChangeUserRole")
            .ProducesProblem(StatusCodes.Status409Conflict);
        users.MapPost("/{id:guid}/deactivate", DeactivateAsync).WithName("DeactivateUser")
            .ProducesProblem(StatusCodes.Status409Conflict);
        users.MapPost("/{id:guid}/reactivate", ReactivateAsync).WithName("ReactivateUser")
            .ProducesProblem(StatusCodes.Status409Conflict);
        users.MapPost("/{id:guid}/anonymization", AnonymizeAsync).WithName("AnonymizeUser")
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Results<Ok<UserPageResponse>, ValidationProblem>> ListAsync(
        ListUsers listUsers,
        CancellationToken cancellationToken,
        [Description("Part of the name or of the e-mail, without telling case or accents apart.")] string? search = null,
        SystemRole? role = null,
        UserStatus? status = null,
        int page = 1,
        int pageSize = 25)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors[nameof(page)] = ["Must be 1 or more."];
        }

        if (pageSize is < 1 or > UserListQuery.MaxPageSize)
        {
            errors[nameof(pageSize)] = [$"Must be between 1 and {UserListQuery.MaxPageSize}."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var state = status switch
        {
            UserStatus.Active => UserState.Active,
            UserStatus.Inactive => UserState.Inactive,
            UserStatus.Anonymized => UserState.Anonymized,
            _ => (UserState?)null,
        };
        var result = await listUsers.ExecuteAsync(new UserListQuery(search, role, state, page, pageSize), cancellationToken);
        return TypedResults.Ok(new UserPageResponse(
            [.. result.Items.Select(user => new UserResponse(
                user.Id, user.Name, user.Email, user.Role, StatusOf(user.State), user.LastSignInAt))],
            result.Total));
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> ChangeRoleAsync(
        Guid id, ChangeRoleRequest request, ChangeSystemRole changeSystemRole, CancellationToken cancellationToken) =>
        ToResult(await changeSystemRole.ExecuteAsync(id, request.Role, cancellationToken));

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeactivateAsync(
        Guid id, DeactivateUser deactivateUser, CancellationToken cancellationToken) =>
        ToResult(await deactivateUser.ExecuteAsync(id, cancellationToken));

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> ReactivateAsync(
        Guid id, ReactivateUser reactivateUser, CancellationToken cancellationToken) =>
        ToResult(await reactivateUser.ExecuteAsync(id, cancellationToken));

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> AnonymizeAsync(
        Guid id, AnonymizeUser anonymizeUser, CancellationToken cancellationToken) =>
        await anonymizeUser.ExecuteAsync(id, cancellationToken) switch
        {
            AnonymizeResult.Done => TypedResults.NoContent(),
            AnonymizeResult.NotFound => TypedResults.NotFound(),
            AnonymizeResult.Active => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Active person",
                detail: "Deactivate the person before anonymizing.",
                type: UserProblems.Active),
            _ => AnonymizedProblem(),
        };

    private static UserStatus StatusOf(UserState state) => state switch
    {
        UserState.Active => UserStatus.Active,
        UserState.Inactive => UserStatus.Inactive,
        _ => UserStatus.Anonymized,
    };

    private static ProblemHttpResult AnonymizedProblem() => TypedResults.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Anonymized person",
        detail: "The person was anonymized, for good.",
        type: UserProblems.Anonymized);

    private static Results<NoContent, NotFound, ProblemHttpResult> ToResult(UserChangeResult result) => result switch
    {
        UserChangeResult.Done => TypedResults.NoContent(),
        UserChangeResult.NotFound => TypedResults.NotFound(),
        UserChangeResult.Anonymized => AnonymizedProblem(),
        _ => TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Last active administrator",
            detail: "The installation needs at least one active administrator.",
            type: UserProblems.LastAdministrator),
    };
}
