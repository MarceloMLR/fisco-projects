namespace FiscoProjects.Api.Session;

/// <summary>
/// Protection against cross-site request forgery (ADR-0054): changes to the API (POST, PUT, PATCH, DELETE) must
/// carry the "X-CSRF: 1" header. A page on another site cannot send a custom header without CORS, which the API
/// does not allow, and the check needs no token readable by JavaScript.
/// </summary>
internal static class CsrfHeaderCheck
{
    public const string HeaderName = "X-CSRF";
    public const string HeaderValue = "1";

    public static IApplicationBuilder UseCsrfHeaderCheck(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (IsApiChange(context.Request) && context.Request.Headers[HeaderName] != HeaderValue)
            {
                // The body comes from UseStatusCodePages, in Problem Details.
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            await next(context);
        });

    private static bool IsApiChange(HttpRequest request) =>
        request.Path.StartsWithSegments(ApiSetup.ApiPrefix)
        && (HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method)
            || HttpMethods.IsPatch(request.Method) || HttpMethods.IsDelete(request.Method));
}
