namespace FiscoProjects.Api;

/// <summary>
/// Serves the React build (web/, built into wwwroot) from this host, at the same address as the API (ADR-0047, ADR-0054).
/// </summary>
internal static class FrontendHosting
{
    private const string IndexFile = "index.html";
    private const string BuildAssetsPath = "/assets";

    // Files under /assets carry a content hash in their names, so they never change; the main page must
    // always be checked again, so a new version reaches people who already have the application open.
    private const string ImmutableCache = "public, max-age=31536000, immutable";
    private const string NoCache = "no-cache";

    public static readonly StaticFileOptions StaticFiles = new()
    {
        OnPrepareResponse = context => context.Context.Response.Headers.CacheControl =
            context.Context.Request.Path.StartsWithSegments(BuildAssetsPath) ? ImmutableCache : NoCache,
    };

    /// <summary>
    /// Paths that are not files and not API routes get the main page, which picks the screen. Unknown API routes
    /// keep the empty 404 that becomes Problem Details.
    /// </summary>
    public static void MapFrontendFallback(this WebApplication app)
    {
        app.MapFallback($"{ApiSetup.ApiPrefix}/{{**path}}", () => Results.NotFound());
        app.MapFallbackToFile(IndexFile, StaticFiles);
    }
}
