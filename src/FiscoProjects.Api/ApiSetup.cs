using FiscoProjects.Persistence;

namespace FiscoProjects.Api;

/// <summary>
/// Common HTTP behavior of the API: health (including the database), Problem Details errors, OpenAPI contract,
/// the /api route group, the shared persistence services and the frontend build.
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
        builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
        builder.Services.AddFiscoProjectsPersistence();

        if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue("Database:MigrateOnStartup", true))
        {
            builder.Services.AddHostedService<DevelopmentDatabaseMigration>();
        }

        // Modules register their services here, e.g. builder.Services.AddTicketsModule(builder.Configuration),
        // which calls services.AddModuleDbContext<TicketsDbContext>(configuration).

        return builder;
    }

    public static WebApplication UseFiscoProjectsApi(this WebApplication app)
    {
        // Same error behavior in every environment: Problem Details, never exception details.
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseStaticFiles(FrontendHosting.StaticFiles);

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.MapHealthChecks(HealthPath);

        // Modules map their routes on this group, e.g. api.MapTicketsEndpoints().
        _ = app.MapApiGroup();

        app.MapFrontendFallback();

        return app;
    }

    /// <summary>
    /// Route group that every module receives, so module routes live under /api.
    /// </summary>
    public static RouteGroupBuilder MapApiGroup(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup(ApiPrefix);
}
