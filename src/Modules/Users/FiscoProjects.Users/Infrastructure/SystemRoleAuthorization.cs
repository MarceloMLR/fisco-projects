using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Authorization;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>The person of the session must have one of these roles, as stored now (ADR-0006).</summary>
internal sealed class SystemRoleRequirement(params SystemRole[] roles) : IAuthorizationRequirement
{
    public IReadOnlyCollection<SystemRole> Roles { get; } = roles;
}

/// <summary>
/// Reads the role from the database on each request, so a role change applies at the next request, without a new
/// sign-in; the cookie keeps only the user id (ADR-0054).
/// </summary>
internal sealed class SystemRoleHandler(IUserAccess userAccess) : AuthorizationHandler<SystemRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, SystemRoleRequirement requirement)
    {
        var userId = UserSession.UserIdOf(context.User);
        if (userId is null)
        {
            return;
        }

        var access = await userAccess.FindAsync(userId.Value, CancellationToken.None);
        if (access is { IsActive: true } && requirement.Roles.Contains(access.Role))
        {
            context.Succeed(requirement);
        }
    }
}
