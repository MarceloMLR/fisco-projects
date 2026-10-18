using System.Security.Claims;

namespace FiscoProjects.Users.Contracts;

/// <summary>
/// What the session cookie carries about the person: only the internal user id (ADR-0054). The Users module
/// opens sessions with it, and the host reads it back on every request.
/// </summary>
public static class UserSession
{
    public const string AuthenticationType = "session";

    public static ClaimsPrincipal For(Guid userId) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], AuthenticationType));

    public static Guid? UserIdOf(ClaimsPrincipal? principal) =>
        Guid.TryParse(principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
