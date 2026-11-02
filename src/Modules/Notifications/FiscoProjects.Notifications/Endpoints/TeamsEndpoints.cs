using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Notifications.Endpoints;

/// <summary>
/// The channel of the notices sent now of the person (ADR-0149): E-mail, Teams or Both, and whether Teams is there for
/// the person (on in the installation, and the person signs in by Entra ID).
/// </summary>
public sealed record ImmediateNoticesResponse(ImmediateNoticeChannel Channel, bool TeamsAvailable);

public sealed record ChooseImmediateNoticesRequest(ImmediateNoticeChannel Channel);

/// <summary>
/// The application of Teams of the installation (ADR-0149): whether the sending to Teams is configured in the
/// application, whether the notices go to Teams and the version of the last package (null before the first).
/// </summary>
public sealed record TeamsAppResponse(bool Available, bool NoticesOn, string? Version);

public sealed record ChangeTeamsAppRequest(bool NoticesOn);

/// <summary>What the icons of the package show, as the address of the logo with its version or the main color.</summary>
public sealed record TeamsPackageRequest(string IconSource);

/// <summary>The manifest of the package of Teams, as the file manifest.json, and its version.</summary>
public sealed record TeamsPackageResponse(string Manifest, string Version);

/// <summary>The types of the problems of the application of Teams (RFC 9457).</summary>
public static class TeamsProblems
{
    public const string Unavailable = "urn:fiscoprojects:problem:teams-unavailable";
}

/// <summary>
/// The channel of the notices of the person in the session, in My profile, and the application of Teams of the
/// installation, only for the Administrator (ADR-0046, ADR-0149).
/// </summary>
public static class TeamsEndpoints
{
    public static RouteGroupBuilder MapTeamsEndpoints(this RouteGroupBuilder api)
    {
        var profile = api.MapGroup("/profile").WithTags("Profile").ProducesProblem(StatusCodes.Status401Unauthorized);
        profile.MapGet("/immediate-notices", GetChannelAsync).WithName("GetImmediateNotices");
        profile.MapPut("/immediate-notices", ChooseChannelAsync).WithName("ChooseImmediateNotices").ProducesValidationProblem();

        var teams = api.MapGroup("/admin/teams-app")
            .WithTags("Notifications")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        teams.MapGet("/", GetAppAsync).WithName("GetTeamsApp");
        teams.MapPut("/", ChangeAppAsync).WithName("ChangeTeamsApp").ProducesValidationProblem();
        teams.MapPost("/package", PackageAsync).WithName("PackageTeamsApp")
            .WithDescription("The manifest of the package of Teams of the installation, with a new version when its content changed; the screen adds the icons.")
            .ProducesProblem(StatusCodes.Status409Conflict);
        return api;
    }

    private static async Task<Ok<ImmediateNoticesResponse>> GetChannelAsync(
        HttpContext http, GetImmediateNotices get, CancellationToken cancellationToken)
    {
        var notices = await get.ExecuteAsync(UserOf(http), cancellationToken);
        return TypedResults.Ok(new ImmediateNoticesResponse(notices.Channel, notices.TeamsAvailable));
    }

    private static async Task<Results<NoContent, ValidationProblem>> ChooseChannelAsync(
        ChooseImmediateNoticesRequest request, HttpContext http, ChooseImmediateNotices choose, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Channel))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["channel"] = ["Choose Email, Teams or Both."],
            });
        }

        await choose.ExecuteAsync(UserOf(http), request.Channel, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<TeamsAppResponse>> GetAppAsync(GetTeamsApp get, CancellationToken cancellationToken)
    {
        var app = await get.ExecuteAsync(cancellationToken);
        return TypedResults.Ok(new TeamsAppResponse(app.Available, app.NoticesOn, app.Version));
    }

    private static async Task<Results<NoContent, ValidationProblem>> ChangeAppAsync(
        ChangeTeamsAppRequest request, ChangeTeamsNotices change, CancellationToken cancellationToken) =>
        await change.ExecuteAsync(request.NoticesOn, cancellationToken)
            ? TypedResults.NoContent()
            : TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["noticesOn"] = ["The sending to Teams is not configured in this installation (Teams:Provider)."],
            });

    private static async Task<Results<Ok<TeamsPackageResponse>, ProblemHttpResult>> PackageAsync(
        TeamsPackageRequest request, PackageTeamsApp package, CancellationToken cancellationToken) =>
        await package.ExecuteAsync(request.IconSource ?? string.Empty, cancellationToken) is { } built
            ? TypedResults.Ok(new TeamsPackageResponse(built.Manifest, built.Version))
            : TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Teams is not available",
                detail: "The sending to Teams is not configured in this installation (Teams:Provider).",
                type: TeamsProblems.Unavailable);

    private static Guid UserOf(HttpContext http) =>
        UserSession.UserIdOf(http.User) ?? throw new InvalidOperationException("The channel is read in a session.");
}
