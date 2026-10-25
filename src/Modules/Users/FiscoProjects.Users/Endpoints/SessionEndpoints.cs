using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Endpoints;

/// <summary>The person in the session, with the role in the system and the theme of their screens.</summary>
public sealed record SessionUserResponse(Guid Id, string Name, string Email, SystemRole Role, ThemePreference Theme);

/// <summary>How the sign-in page may sign people in: the local sign-in of the demo, Entra ID, or both.</summary>
public sealed record SignInOptionsResponse(bool LocalSignIn, bool EntraSignIn);

public sealed record LocalSignInRequest(string Email);

/// <summary>
/// The start of the sign-in through Entra ID: the e-mail for the hint, the screen to go back to and, after an expired
/// session, the user who had it.
/// </summary>
public sealed record EntraSignInRequest(string? Email, string? ReturnUrl, Guid? PreviousUserId);

/// <summary>The address of Microsoft where the browser goes to sign in.</summary>
public sealed record EntraSignInStartResponse(string Location);

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
        session.MapPost("/entra", StartEntraSignInAsync).WithName("StartEntraSignIn");
        session.MapPost("/sign-out", (Delegate)SignOutAsync).WithName("SignOut");
    }

    private static async Task<Results<Ok<SessionUserResponse>, UnauthorizedHttpResult>> GetSessionAsync(
        HttpContext http, GetSignedInUser getSignedInUser, CancellationToken cancellationToken)
    {
        var userId = UserSession.UserIdOf(http.User);
        var user = userId is null ? null : await getSignedInUser.ExecuteAsync(userId.Value, cancellationToken);
        return user is null
            ? TypedResults.Unauthorized()
            : TypedResults.Ok(new SessionUserResponse(user.Id, user.Name, user.Email, user.Role, user.Theme));
    }

    private static Ok<SignInOptionsResponse> GetSignInOptions(SignInLocally signInLocally, IOptions<EntraSignInOptions> entra) =>
        TypedResults.Ok(new SignInOptionsResponse(signInLocally.IsEnabled, entra.Value.Enabled));

    /// <summary>
    /// Starts the sign-in through Entra ID (ADR-0117) and answers with the address of Microsoft instead of a redirect,
    /// so the e-mail goes in the body here and only in the address of Microsoft. The cookies that the handler writes
    /// to check the way back stay in the answer.
    /// </summary>
    private static async Task<Results<Ok<EntraSignInStartResponse>, NotFound>> StartEntraSignInAsync(
        EntraSignInRequest request, HttpContext http, IOptions<EntraSignInOptions> entra)
    {
        if (!entra.Value.Enabled)
        {
            return TypedResults.NotFound();
        }

        // The properties keep the screen to go back to and the user of the expired session; the e-mail leaves them
        // when it becomes the hint to Microsoft.
        var properties = new AuthenticationProperties { RedirectUri = LocalPath(request.ReturnUrl) };
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            properties.Items[EntraSignInOptions.LoginHintItem] = request.Email.Trim();
        }

        if (request.PreviousUserId is { } previous)
        {
            properties.Items[EntraSignInOptions.PreviousUserItem] = previous.ToString();
        }

        await http.ChallengeAsync(EntraSignInOptions.Scheme, properties);
        var location = http.Response.Headers.Location.ToString();
        http.Response.Headers.Remove("Location");
        return TypedResults.Ok(new EntraSignInStartResponse(location));
    }

    /// <summary>A path of this site, or the home screen: the way back never leads to another site.</summary>
    internal static string LocalPath(string? url) =>
        url is ['/'] or ['/', not ('/' or '\\'), ..] ? url : "/";

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
