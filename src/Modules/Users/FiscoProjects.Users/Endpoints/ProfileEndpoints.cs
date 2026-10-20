using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Users.Endpoints;

/// <summary>
/// My profile: who the person is, how they sign in, the theme they chose and whether they receive the daily summary of
/// e-mails. Sign-in providers come as their codes ("local", and "entra" later); the screen says them in words.
/// </summary>
public sealed record ProfileResponse(
    Guid Id,
    string Name,
    string Email,
    SystemRole Role,
    ThemePreference Theme,
    DateTime LastSignInAt,
    IReadOnlyList<string> SignInMethods,
    bool ReceivesDailySummary);

public sealed record ChooseThemeRequest(ThemePreference Theme);

/// <summary>Whether the person receives the daily summary of e-mails (ADR-0037).</summary>
public sealed record ChooseDailySummaryRequest(bool Receives);

/// <summary>Routes of My profile, for the person in the session only (ADR-0014).</summary>
internal static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this RouteGroupBuilder api)
    {
        var profile = api.MapGroup("/profile").WithTags("Profile").ProducesProblem(StatusCodes.Status401Unauthorized);

        profile.MapGet("/", GetAsync).WithName("GetProfile");
        profile.MapPut("/theme", ChooseThemeAsync).WithName("ChooseTheme").ProducesValidationProblem();
        profile.MapPut("/daily-summary", ChooseDailySummaryAsync).WithName("ChooseDailySummary");
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
                profile.SignInProviders, profile.ReceivesDailySummary));
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

    private static async Task<Results<NoContent, UnauthorizedHttpResult>> ChooseDailySummaryAsync(
        ChooseDailySummaryRequest request, HttpContext http, ChooseDailySummary choose, CancellationToken cancellationToken) =>
        UserSession.UserIdOf(http.User) is { } userId && await choose.ExecuteAsync(userId, request.Receives, cancellationToken)
            ? TypedResults.NoContent()
            : TypedResults.Unauthorized();
}
