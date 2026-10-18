namespace FiscoProjects.Api;

/// <summary>
/// Common HTTP behavior of the API: health, Problem Details errors, OpenAPI contract and the /api route group.
/// Program.cs and the integration tests compose the application through these two methods.
/// </summary>
public static class ApiSetup
{
    public const string ApiPrefix = "/api";
    public const string HealthPath = "/health";

    public static WebApplicationBuilder AddFiscoProjectsApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
        builder.Services.AddHealthChecks();

        // Modules register their services here, e.g. builder.Services.AddChamadosModule(builder.Configuration).

        return builder;
    }

    public static WebApplication UseFiscoProjectsApi(this WebApplication app)
    {
        // Same error behavior in every environment: Problem Details, never exception details.
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.MapHealthChecks(HealthPath);

        // Modules map their routes on this group, e.g. api.MapChamadosEndpoints().
        _ = app.MapApiGroup();

        return app;
    }

    /// <summary>
    /// Route group that every module receives, so module routes live under /api.
    /// </summary>
    public static RouteGroupBuilder MapApiGroup(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup(ApiPrefix);
}
