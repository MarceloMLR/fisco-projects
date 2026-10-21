using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;

namespace FiscoProjects.Api;

/// <summary>
/// The monitoring of the application (ADR-0057, ADR-0112): OpenTelemetry exported to Application Insights by the Azure
/// Monitor distro, only when the connection of Application Insights is configured; without it, nothing is collected
/// nor sent, as in development, the tests and the demonstration.
/// </summary>
public static class Monitoring
{
    /// <summary>The setting of the App Service with the connection, from the Key Vault.</summary>
    public const string ConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>The same connection, in the configuration of the application.</summary>
    public const string ConnectionStringSetting = "AzureMonitor:ConnectionString";

    /// <summary>The traces kept per second, the default of the distro; a peak beyond it is sampled.</summary>
    public const double DefaultTracesPerSecond = 5;

    /// <summary>The meters of the modules all start with it, as <c>FiscoProjects.Notifications</c>.</summary>
    public const string Meters = "FiscoProjects.*";

    public const string ServiceName = "FiscoProjects";

    public static WebApplicationBuilder AddFiscoProjectsMonitoring(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var connectionString = builder.Configuration[ConnectionStringKey] ?? builder.Configuration[ConnectionStringSetting];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return builder;
        }

        // Nothing about the SDK itself goes to Microsoft, as the telemetry of the other tools is off (ADR-0112).
        Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_STATSBEAT_DISABLED", "true");
        Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_SDKSTATS_DISABLED", "true");
        var tracesPerSecond = builder.Configuration.GetValue("Monitoring:TracesPerSecond", DefaultTracesPerSecond);
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(ServiceName)
                .AddAttributes([new("deployment.environment.name", builder.Environment.EnvironmentName)]))
            .UseAzureMonitor(options =>
            {
                options.ConnectionString = connectionString;
                options.TracesPerSecond = tracesPerSecond;
            })
            .WithMetrics(metrics => metrics.AddMeter(Meters));

        // The test of availability calls the health all the time: it is no request of a person.
        builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
            options.Filter = http => !http.Request.Path.StartsWithSegments(ApiSetup.HealthPath, StringComparison.OrdinalIgnoreCase));
        return builder;
    }
}
