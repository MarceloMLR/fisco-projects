using System.Text.Json.Serialization;
using FiscoProjects.Administration.Endpoints;
using FiscoProjects.Administration.Infrastructure;
using FiscoProjects.Api.Session;
using FiscoProjects.Audit.Endpoints;
using FiscoProjects.Audit.Infrastructure;
using FiscoProjects.Calendar.Endpoints;
using FiscoProjects.Calendar.Infrastructure;
using FiscoProjects.Files.Infrastructure;
using FiscoProjects.Notifications.Endpoints;
using FiscoProjects.Notifications.Infrastructure;
using FiscoProjects.Persistence;
using FiscoProjects.Projects.Endpoints;
using FiscoProjects.Projects.Infrastructure;
using FiscoProjects.Tickets.Endpoints;
using FiscoProjects.Tickets.Infrastructure;
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
        // A request the API cannot read (a malformed body, an unknown enum value) is the caller's error: 400, not 500.
        // In Development the framework throws it instead of answering, so the handler keeps its status.
        builder.Services.AddExceptionHandler(options => options.StatusCodeSelector = exception =>
            exception is BadHttpRequestException badRequest ? badRequest.StatusCode : StatusCodes.Status500InternalServerError);
        // Enums travel as their names, so the contract and the frontend types list the values; numbers are only
        // numbers, so the contract does not type them as "number or text".
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
            options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
        });
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
        builder.Services.AddAuditModule(builder.Configuration);
        builder.Services.AddFilesModule(builder.Configuration);
        builder.Services.AddAdministrationModule(builder.Configuration);
        builder.Services.AddCalendarModule(builder.Configuration);
        builder.Services.AddProjectsModule(builder.Configuration);
        builder.Services.AddTicketsModule(builder.Configuration);
        builder.Services.AddNotificationsModule(builder.Configuration);

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
            .MapUsersEndpoints()
            .MapAuditEndpoints()
            .MapAdministrationEndpoints()
            .MapCalendarEndpoints()
            .MapProjectsEndpoints()
            .MapTicketsEndpoints()
            .MapNotificationsEndpoints()
            .MapClientSettingsEndpoints();

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
