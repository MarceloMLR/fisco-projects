using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Users.Endpoints;

/// <summary>The person in the session.</summary>
public sealed record SessionUserResponse(Guid Id, string Name, string Email);

/// <summary>How the sign-in page may sign people in.</summary>
public sealed record SignInOptionsResponse(bool LocalSignIn);

public sealed record LocalSignInRequest(string Email);

/// <summary>Problem types of a refused sign-in, so the sign-in page can tell the person why.</summary>
public static class SignInProblems
{
    public const string NotInDirectory = "urn:fiscoprojects:problem:not-in-demo-directory";
    public const string Inactive = "urn:fiscoprojects:problem:user-inactive";
}

/// <summary>
/// Session routes (ADR-0009, ADR-0054, ADR-0078). They stay open, since they are how a session starts and ends;
/// the session itself (cookie, timeout, active user) is set up by the host.
/// </summary>
internal static class SessionEndpoints
{
    public static void MapSessionEndpoints(this RouteGroupBuilder api)
    {
        var session = api.MapGroup("/session").AllowAnonymous().WithTags("Session");

        session.MapGet("/", GetSessionAsync).WithName("GetSession").ProducesProblem(StatusCodes.Status401Unauthorized);
        session.MapGet("/sign-in-options", GetSignInOptions).WithName("GetSignInOptions");
        session.MapPost("/local", SignInLocallyAsync).WithName("SignInLocally").ProducesProblem(StatusCodes.Status403Forbidden);
        session.MapPost("/sign-out", (Delegate)SignOutAsync).WithName("SignOut");
    }

    private static async Task<Results<Ok<SessionUserResponse>, UnauthorizedHttpResult>> GetSessionAsync(
        HttpContext http, GetSignedInUser getSignedInUser, CancellationToken cancellationToken)
    {
        var userId = UserSession.UserIdOf(http.User);
        var user = userId is null ? null : await getSignedInUser.ExecuteAsync(userId.Value, cancellationToken);
        return user is null
            ? TypedResults.Unauthorized()
            : TypedResults.Ok(new SessionUserResponse(user.Id, user.Name, user.Email));
    }

    private static Ok<SignInOptionsResponse> GetSignInOptions(SignInLocally signInLocally) =>
        TypedResults.Ok(new SignInOptionsResponse(signInLocally.IsEnabled));

    private static async Task<Results<NoContent, ProblemHttpResult, NotFound>> SignInLocallyAsync(
        LocalSignInRequest request, HttpContext http, SignInLocally signInLocally, CancellationToken cancellationToken)
    {
        switch (await signInLocally.ExecuteAsync(request.Email, cancellationToken))
        {
            case LocalSignInResult.SignedIn signedIn:
                await http.SignInAsync(UserSession.For(signedIn.UserId));
                return TypedResults.NoContent();
            case LocalSignInResult.NotInDirectory:
                return Refused(SignInProblems.NotInDirectory, "No person of the demo directory uses this e-mail.");
            case LocalSignInResult.Inactive:
                return Refused(SignInProblems.Inactive, "This user may no longer use the system.");
            default:
                return TypedResults.NotFound();
        }
    }

    private static ProblemHttpResult Refused(string type, string detail) =>
        TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Access not granted", detail: detail, type: type);

    private static async Task<NoContent> SignOutAsync(HttpContext http)
    {
        await http.SignOutAsync();
        return TypedResults.NoContent();
    }
}
