using FiscoProjects.Api.Session;
using FiscoProjects.Persistence;
using FiscoProjects.Users.Endpoints;
using FiscoProjects.Users.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FiscoProjects.Api;

/// <summary>
/// Common HTTP behavior of the API: health (including the database), Problem Details errors, OpenAPI contract,
/// the session, the /api route group, the shared persistence services and the frontend build.
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
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSessionAuthentication(builder.Configuration);

        if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue("Database:MigrateOnStartup", true))
        {
            builder.Services.AddHostedService<DevelopmentDatabaseMigration>();
        }

        // Each module registers its services from its Infrastructure layer (src/Modules/README.md).
        builder.Services.AddUsersModule(builder.Configuration);

        return builder;
    }

    public static WebApplication UseFiscoProjectsApi(this WebApplication app)
    {
        // Same error behavior in every environment: Problem Details, never exception details.
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseStaticFiles(FrontendHosting.StaticFiles);
        app.UseCsrfHeaderCheck();
        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.MapHealthChecks(HealthPath);

        // Each module maps its routes on this group, from its Endpoints layer.
        _ = app.MapApiGroup()
            .MapUsersEndpoints();

        app.MapFrontendFallback();

        return app;
    }

    /// <summary>
    /// Route group that every module receives, so module routes live under /api. Its routes require a session;
    /// a route that must stay open (signing in, for one) says so with AllowAnonymous.
    /// </summary>
    public static RouteGroupBuilder MapApiGroup(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup(ApiPrefix).RequireAuthorization();
}
