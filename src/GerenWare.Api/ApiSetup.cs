using System.Text.Json.Serialization;
using GerenWare.Administration.Endpoints;
using GerenWare.Administration.Infrastructure;
using GerenWare.Api.Session;
using GerenWare.Audit.Endpoints;
using GerenWare.Audit.Infrastructure;
using GerenWare.Calendar.Endpoints;
using GerenWare.Calendar.Infrastructure;
using GerenWare.Credentials.Endpoints;
using GerenWare.Credentials.Infrastructure;
using GerenWare.Documents.Endpoints;
using GerenWare.Documents.Infrastructure;
using GerenWare.Files.Infrastructure;
using GerenWare.MyWork.Endpoints;
using GerenWare.MyWork.Infrastructure;
using GerenWare.Notifications.Endpoints;
using GerenWare.Notifications.Infrastructure;
using GerenWare.Persistence;
using GerenWare.Projects.Endpoints;
using GerenWare.Projects.Infrastructure;
using GerenWare.Tickets.Endpoints;
using GerenWare.Tickets.Infrastructure;
using GerenWare.TimeTracking.Endpoints;
using GerenWare.TimeTracking.Infrastructure;
using GerenWare.Users.Contracts;
using GerenWare.Users.Endpoints;
using GerenWare.Users.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GerenWare.Api;

/// <summary>
/// Common HTTP behavior of the API: health (including the database), Problem Details errors, OpenAPI contract,
/// the monitoring, the session, the /api route group, the shared persistence services and the frontend build.
/// Program.cs and the integration tests compose the application through these two methods.
/// </summary>
public static class ApiSetup
{
    public const string ApiPrefix = "/api";
    public const string HealthPath = "/health";

    public static WebApplicationBuilder AddGerenWareApi(this WebApplicationBuilder builder)
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
        // Application Insights, only with its connection (ADR-0057, ADR-0112).
        builder.AddGerenWareMonitoring();
        builder.Services.AddGerenWarePersistence();
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
        builder.Services.AddDocumentsModule(builder.Configuration);
        builder.Services.AddCredentialsModule(builder.Configuration);
        builder.Services.AddNotificationsModule(builder.Configuration);
        builder.Services.AddMyWorkModule(builder.Configuration);
        builder.Services.AddTimeTrackingModule(builder.Configuration);

        // The main reads of every module, once after the API starts, in the background (ADR-0106).
        builder.Services.AddHostedService<StartupWarmUp>();

        return builder;
    }

    public static WebApplication UseGerenWareApi(this WebApplication app)
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
            .MapDocumentsEndpoints()
            .MapCredentialsEndpoints()
            .MapNotificationsEndpoints()
            .MapTeamsEndpoints()
            .MapMyWorkEndpoints()
            .MapTimeTrackingEndpoints()
            .MapProjectHoursEndpoints()
            .MapTaskHoursEndpoints()
            .MapTeamLoadEndpoints();

        // The readings of the page in Teams, only with the token of Teams (ADR-0152).
        _ = app.MapTeamsApiGroup()
            .MapTeamsTicketEndpoints();

        // The notices of the provider of signatures, outside /api: no session nor X-CSRF (ADR-0130).
        app.MapSignatureNoticeEndpoints();

        app.MapFrontendFallback();

        return app;
    }

    /// <summary>
    /// Route group that every module receives, so module routes live under /api. Its routes require a session;
    /// a route that must stay open (signing in, for one) says so with AllowAnonymous.
    /// </summary>
    public static RouteGroupBuilder MapApiGroup(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup(ApiPrefix).RequireAuthorization();

    /// <summary>
    /// Route group of the readings of the page in Teams (ADR-0152), under /api/teams: only the token of Teams, never the
    /// session cookie, and only GET. Outside <see cref="MapApiGroup"/>, whose default policy is the session.
    /// </summary>
    public static RouteGroupBuilder MapTeamsApiGroup(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup(ApiPrefix + TeamsAccess.Prefix).RequireAuthorization(TeamsAccess.Policy);
}
