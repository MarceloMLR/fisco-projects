using System.Globalization;
using FiscoProjects.Administration.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Administration.Endpoints;

/// <summary>
/// The brand of the installation: the name shown, the address of the logo (null without one) and the main color as
/// #RRGGBB (null for the color of the design system). The address of the logo changes whenever the logo changes, so the
/// browser may keep it.
/// </summary>
public sealed record InstallationResponse(string Name, string? LogoUrl, string? MainColor);

/// <summary>
/// The General settings form, saved as a whole: the name shown, the IANA time zone and the main color (#RRGGBB, or
/// null for the color of the design system).
/// </summary>
public sealed record UpdateInstallationRequest(string Name, string TimeZone, string? MainColor);

/// <summary>The rules of the installation for the screens: the IANA time zone of dates and times (ADR-0013).</summary>
public sealed record GeneralRulesResponse(string TimeZone);

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
    }

    private static async Task<Ok<InstallationResponse>> GetAsync(
        HttpContext http, LinkGenerator links, GetInstallation getInstallation, CancellationToken cancellationToken)
    {
        var installation = await getInstallation.ExecuteAsync(cancellationToken);
        var logoUrl = installation.LogoChangedAt is { } changedAt
            ? links.GetPathByName(http, "GetInstallationLogo", new { v = VersionOf(changedAt) })
            : null;
        return TypedResults.Ok(new InstallationResponse(installation.Name, logoUrl, installation.MainColor));
    }

    private static async Task<Results<FileStreamHttpResult, NotFound>> GetLogoAsync(
        HttpContext http, GetInstallationLogo getLogo, CancellationToken cancellationToken, string? v = null)
    {
        var logo = await getLogo.ExecuteAsync(cancellationToken);
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
        GetGeneralRules getRules, CancellationToken cancellationToken) =>
        TypedResults.Ok(new GeneralRulesResponse((await getRules.ExecuteAsync(cancellationToken)).TimeZone));

    private static async Task<Results<NoContent, ValidationProblem>> UpdateAsync(
        UpdateInstallationRequest request, UpdateInstallation updateInstallation, CancellationToken cancellationToken)
    {
        var invalid = await updateInstallation.ExecuteAsync(
            new InstallationChanges(request.Name, request.TimeZone, request.MainColor), cancellationToken);
        return invalid.Count == 0
            ? TypedResults.NoContent()
            : TypedResults.ValidationProblem(invalid.ToDictionary(KeyOf, field => new[] { ReasonOf(field) }));
    }

    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> ReplaceLogoAsync(
        IFormFile? file, ReplaceInstallationLogo replaceLogo, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["Send the logo in the field file."],
            });
        }

        await using var content = file.OpenReadStream();
        return await replaceLogo.ExecuteAsync(file.FileName, content, cancellationToken) switch
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
        await removeLogo.ExecuteAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static string KeyOf(InstallationField field) => field switch
    {
        InstallationField.Name => "name",
        InstallationField.TimeZone => "timeZone",
        _ => "mainColor",
    };

    private static string ReasonOf(InstallationField field) => field switch
    {
        InstallationField.Name => "The name is needed and has up to 60 characters.",
        InstallationField.TimeZone => "Choose an IANA time zone, such as America/Sao_Paulo.",
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
