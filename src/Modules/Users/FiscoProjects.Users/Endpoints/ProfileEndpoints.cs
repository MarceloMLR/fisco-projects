using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Users.Endpoints;

/// <summary>
/// My profile: who the person is, how they sign in and the theme they chose. Sign-in providers come as their codes
/// ("local", and "entra" later); the screen says them in words.
/// </summary>
public sealed record ProfileResponse(
    Guid Id,
    string Name,
    string Email,
    SystemRole Role,
    ThemePreference Theme,
    DateTime LastSignInAt,
    IReadOnlyList<string> SignInMethods);

public sealed record ChooseThemeRequest(ThemePreference Theme);

/// <summary>Routes of My profile, for the person in the session only (ADR-0014).</summary>
internal static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this RouteGroupBuilder api)
    {
        var profile = api.MapGroup("/profile").WithTags("Profile").ProducesProblem(StatusCodes.Status401Unauthorized);

        profile.MapGet("/", GetAsync).WithName("GetProfile");
        profile.MapPut("/theme", ChooseThemeAsync).WithName("ChooseTheme").ProducesValidationProblem();
    }

    private static async Task<Results<Ok<ProfileResponse>, UnauthorizedHttpResult>> GetAsync(
        HttpContext http, GetProfile getProfile, CancellationToken cancellationToken)
    {
        var profile = UserSession.UserIdOf(http.User) is { } userId
            ? await getProfile.ExecuteAsync(userId, cancellationToken)
            : null;
        return profile is null
            ? TypedResults.Unauthorized()
            : TypedResults.Ok(new ProfileResponse(
                profile.Id, profile.Name, profile.Email, profile.Role, profile.Theme, profile.LastSignInAt,
                profile.SignInProviders));
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult, ValidationProblem>> ChooseThemeAsync(
        ChooseThemeRequest request, HttpContext http, ChooseTheme chooseTheme, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Theme))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["theme"] = ["Choose Automatic, Light or Dark."],
            });
        }

        return UserSession.UserIdOf(http.User) is { } userId
               && await chooseTheme.ExecuteAsync(userId, request.Theme, cancellationToken)
            ? TypedResults.NoContent()
            : TypedResults.Unauthorized();
    }
}
