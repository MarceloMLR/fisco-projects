using System.Globalization;
using FiscoProjects.Administration.Application;
using FiscoProjects.Administration.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Administration.Endpoints;

/// <summary>
/// The brand of the installation: the name shown, the address of the logo (null without one), the main color as
/// #RRGGBB (null for the color of the design system) and the address of the logo for the dark theme (null without one,
/// and the logo shows, ADR-0100). The address of a logo changes whenever the logo changes, so the browser may keep it.
/// </summary>
public sealed record InstallationResponse(string Name, string? LogoUrl, string? MainColor, string? DarkLogoUrl);

/// <summary>
/// The General settings form, saved as a whole: the name shown, the IANA time zone, the main color (#RRGGBB, or null
/// for the color of the design system), the time of the daily summary (HH:mm:ss, in the time zone), how many days the
/// notifications are kept (1 to 365), how many years the audit is kept (1 to 10; none keeps the years it had) and the
/// folder of the projects in the file storage, with the folders separated by "/" ("" for its root; none keeps the one
/// it had).
/// </summary>
public sealed record UpdateInstallationRequest(
    string Name,
    string TimeZone,
    string? MainColor,
    TimeOnly? DailySummaryAt,
    int? NotificationRetentionDays,
    int? AuditRetentionYears = null,
    string? ProjectsFolder = null);

/// <summary>
/// The rules of the installation for the screens: the IANA time zone of dates and times (ADR-0013) and the time of the
/// daily summary of e-mails, in that time zone (ADR-0037), how many days the notifications are kept (ADR-0038), how
/// many years the audit is kept (ADR-0110) and the folder of the projects in the file storage (ADR-0121).
/// </summary>
public sealed record GeneralRulesResponse(
    string TimeZone, TimeOnly DailySummaryAt, int NotificationRetentionDays, int AuditRetentionYears, string ProjectsFolder);

/// <summary>Problem types of the logo, so the screen can tell the reason.</summary>
public static class InstallationProblems
{
    public const string LogoFormat = "urn:fiscoprojects:problem:logo-format";
    public const string LogoTooLarge = "urn:fiscoprojects:problem:logo-too-large";
    public const string LogoUnsafe = "urn:fiscoprojects:problem:logo-unsafe";
}

/// <summary>
/// General settings of the installation (ADR-0003, ADR-0011). The brand is read without a session, since the sign-in
/// page shows it (ADR-0009); only the Administrator changes it (ADR-0046).
/// </summary>
internal static class InstallationEndpoints
{
    /// <summary>A body larger than this is refused before it is read; the logo itself has up to 1 MB.</summary>
    public const long UploadLimit = 2 * 1024 * 1024;

    // An SVG opened straight in the browser must not run anything nor load anything (ADR-0030).
    private const string LogoPolicy = "default-src 'none'; style-src 'unsafe-inline'; sandbox";

    public static void MapInstallationEndpoints(this RouteGroupBuilder api)
    {
        var installation = api.MapGroup("/installation").WithTags("Installation");

        installation.MapGet("/", GetAsync).WithName("GetInstallation").AllowAnonymous();
        // The time zone is no part of the brand: only with a session, as every route of the group.
        installation.MapGet("/rules", GetRulesAsync).WithName("GetGeneralRules")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        installation.MapGet("/logo", GetLogoAsync).WithName("GetInstallationLogo").AllowAnonymous()
            .WithMetadata(KeepSession.Instance)
            .Produces(StatusCodes.Status200OK, contentType: "image/png", additionalContentTypes: "image/svg+xml");

        installation.MapPut("/", UpdateAsync).WithName("UpdateInstallation")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        // The protection against other sites is the X-CSRF header of the application (ADR-0054), not the antiforgery
        // token of ASP.NET Core forms.
        installation.MapPut("/logo", ReplaceLogoAsync).WithName("ReplaceInstallationLogo")
            .RequireAuthorization(SystemPolicies.Administrator)
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(UploadLimit))
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        installation.MapDelete("/logo", RemoveLogoAsync).WithName("RemoveInstallationLogo")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // The logo for the dark theme, optional, with the same rules (ADR-0100).
        installation.MapGet("/logo/dark", GetDarkLogoAsync).WithName("GetInstallationDarkLogo").AllowAnonymous()
            .WithMetadata(KeepSession.Instance)
            .Produces(StatusCodes.Status200OK, contentType: "image/png", additionalContentTypes: "image/svg+xml");
        installation.MapPut("/logo/dark", ReplaceDarkLogoAsync).WithName("ReplaceInstallationDarkLogo")
            .RequireAuthorization(SystemPolicies.Administrator)
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(UploadLimit))
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        installation.MapDelete("/logo/dark", RemoveDarkLogoAsync).WithName("RemoveInstallationDarkLogo")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<Ok<InstallationResponse>> GetAsync(
        HttpContext http, LinkGenerator links, GetInstallation getInstallation, CancellationToken cancellationToken)
    {
        var installation = await getInstallation.ExecuteAsync(cancellationToken);
        var logoUrl = installation.LogoChangedAt is { } changedAt
            ? links.GetPathByName(http, "GetInstallationLogo", new { v = VersionOf(changedAt) })
            : null;
        var darkLogoUrl = installation.DarkLogoChangedAt is { } darkChangedAt
            ? links.GetPathByName(http, "GetInstallationDarkLogo", new { v = VersionOf(darkChangedAt) })
            : null;
        return TypedResults.Ok(new InstallationResponse(installation.Name, logoUrl, installation.MainColor, darkLogoUrl));
    }

    private static Task<Results<FileStreamHttpResult, NotFound>> GetLogoAsync(
        HttpContext http, GetInstallationLogo getLogo, CancellationToken cancellationToken, string? v = null) =>
        LogoAsync(http, getLogo, LogoTheme.Light, v, cancellationToken);

    private static Task<Results<FileStreamHttpResult, NotFound>> GetDarkLogoAsync(
        HttpContext http, GetInstallationLogo getLogo, CancellationToken cancellationToken, string? v = null) =>
        LogoAsync(http, getLogo, LogoTheme.Dark, v, cancellationToken);

    private static async Task<Results<FileStreamHttpResult, NotFound>> LogoAsync(
        HttpContext http, GetInstallationLogo getLogo, LogoTheme theme, string? v, CancellationToken cancellationToken)
    {
        var logo = await getLogo.ExecuteAsync(theme, cancellationToken);
        if (logo is null)
        {
            return TypedResults.NotFound();
        }

        var headers = http.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.ContentSecurityPolicy = LogoPolicy;
        // The address of the current logo never shows another file; any other address is checked again.
        headers.CacheControl = v == VersionOf(logo.ChangedAt) ? "public, max-age=31536000, immutable" : "no-cache";
        return TypedResults.Stream(logo.File.Content, logo.ContentType);
    }

    private static async Task<Ok<GeneralRulesResponse>> GetRulesAsync(
        GetGeneralRules getRules, CancellationToken cancellationToken)
    {
        var rules = await getRules.ExecuteAsync(cancellationToken);
        return TypedResults.Ok(new GeneralRulesResponse(
            rules.TimeZone, rules.DailySummaryAt, rules.NotificationRetentionDays, rules.AuditRetentionYears, rules.ProjectsFolder));
    }

    private static async Task<Results<NoContent, ValidationProblem>> UpdateAsync(
        UpdateInstallationRequest request, UpdateInstallation updateInstallation, CancellationToken cancellationToken)
    {
        var invalid = await updateInstallation.ExecuteAsync(
            new InstallationChanges(
                request.Name,
                request.TimeZone,
                request.MainColor,
                request.DailySummaryAt,
                request.NotificationRetentionDays,
                request.AuditRetentionYears,
                request.ProjectsFolder),
            cancellationToken);
        return invalid.Count == 0
            ? TypedResults.NoContent()
            : TypedResults.ValidationProblem(invalid.ToDictionary(KeyOf, field => new[] { ReasonOf(field) }));
    }

    private static Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> ReplaceLogoAsync(
        IFormFile? file, ReplaceInstallationLogo replaceLogo, CancellationToken cancellationToken) =>
        ReplaceAsync(file, replaceLogo, LogoTheme.Light, cancellationToken);

    private static Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> ReplaceDarkLogoAsync(
        IFormFile? file, ReplaceInstallationLogo replaceLogo, CancellationToken cancellationToken) =>
        ReplaceAsync(file, replaceLogo, LogoTheme.Dark, cancellationToken);

    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> ReplaceAsync(
        IFormFile? file, ReplaceInstallationLogo replaceLogo, LogoTheme theme, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["Send the logo in the field file."],
            });
        }

        await using var content = file.OpenReadStream();
        return await replaceLogo.ExecuteAsync(file.FileName, content, theme, cancellationToken) switch
        {
            null => TypedResults.NoContent(),
            LogoRefusal.TooLarge => Refused(InstallationProblems.LogoTooLarge, "Logo too large", "The logo has up to 1 MB."),
            LogoRefusal.Unsafe => Refused(
                InstallationProblems.LogoUnsafe, "Logo not accepted", "The SVG has a script, a DTD or a link that runs code."),
            _ => Refused(InstallationProblems.LogoFormat, "Logo not accepted", "The logo is a PNG or an SVG."),
        };
    }

    private static async Task<NoContent> RemoveLogoAsync(
        RemoveInstallationLogo removeLogo, CancellationToken cancellationToken)
    {
        await removeLogo.ExecuteAsync(LogoTheme.Light, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> RemoveDarkLogoAsync(
        RemoveInstallationLogo removeLogo, CancellationToken cancellationToken)
    {
        await removeLogo.ExecuteAsync(LogoTheme.Dark, cancellationToken);
        return TypedResults.NoContent();
    }

    private static string KeyOf(InstallationField field) => field switch
    {
        InstallationField.Name => "name",
        InstallationField.TimeZone => "timeZone",
        InstallationField.DailySummaryAt => "dailySummaryAt",
        InstallationField.NotificationRetentionDays => "notificationRetentionDays",
        InstallationField.AuditRetentionYears => "auditRetentionYears",
        InstallationField.ProjectsFolder => "projectsFolder",
        _ => "mainColor",
    };

    private static string ReasonOf(InstallationField field) => field switch
    {
        InstallationField.Name => "The name is needed and has up to 60 characters.",
        InstallationField.TimeZone => "Choose an IANA time zone, such as America/Sao_Paulo.",
        InstallationField.DailySummaryAt => "Choose the time of the daily summary, such as 07:30:00.",
        InstallationField.NotificationRetentionDays => "Keep the notifications from 1 to 365 days.",
        InstallationField.AuditRetentionYears => "Keep the audit from 1 to 10 years.",
        InstallationField.ProjectsFolder =>
            "Separate the folders with /, with no empty folder, no . or .., none of \" * : < > ? \\ | and up to 400 characters.",
        _ => "Choose a #RRGGBB color with a contrast of 4.5:1 with white, or none for the default color.",
    };

    private static ProblemHttpResult Refused(string type, string title, string detail) =>
        TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: title, detail: detail, type: type);

    private static string VersionOf(DateTime changedAt) => changedAt.Ticks.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Routes of the Administration module (src/Modules/README.md).</summary>
public static class AdministrationEndpoints
{
    public static RouteGroupBuilder MapAdministrationEndpoints(this RouteGroupBuilder api)
    {
        api.MapInstallationEndpoints();
        return api;
    }
}
