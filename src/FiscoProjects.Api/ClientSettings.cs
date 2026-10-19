namespace FiscoProjects.Api;

/// <summary>What the browser needs from the configuration of the installation: the license key of the timeline.</summary>
/// <param name="TimelineLicense">The Syncfusion license key (ADR-0051, ADR-0087); none when not configured.</param>
public sealed record ClientSettingsResponse(string? TimelineLicense);

/// <summary>
/// The settings the frontend reads from the configuration of the installation, with a session (ADR-0087). Keys stay in
/// the user secrets in development and in the Key Vault in production, never in the repository (ADR-0052).
/// </summary>
internal static class ClientSettings
{
    public const string TimelineLicenseKey = "Syncfusion:LicenseKey";

    public static RouteGroupBuilder MapClientSettingsEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/client-settings", (IConfiguration configuration) =>
                TypedResults.Ok(new ClientSettingsResponse(
                    string.IsNullOrWhiteSpace(configuration[TimelineLicenseKey]) ? null : configuration[TimelineLicenseKey])))
            .WithName("GetClientSettings");
        return api;
    }
}
