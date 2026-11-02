using FiscoProjects.Notifications.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>How the notices reach the Activity of Teams (ADR-0149).</summary>
public sealed class TeamsOptions
{
    public const string Section = "Teams";

    /// <summary>The folder of the disk, for development and tests: nothing leaves (ADR-0004).</summary>
    public const string LocalProvider = "Local";

    /// <summary>Microsoft Graph, with the token of the application and the permissions of the application of Teams.</summary>
    public const string GraphProvider = "Graph";

    /// <summary>Which implementation sends the notices; empty while none is configured, and nothing goes to Teams.</summary>
    public string? Provider { get; set; }

    public LocalTeamsOptions Local { get; set; } = new();

    public bool IsLocal => string.Equals(Provider, LocalProvider, StringComparison.OrdinalIgnoreCase);

    public bool IsGraph => string.Equals(Provider, GraphProvider, StringComparison.OrdinalIgnoreCase);

    public TeamsProvider Kind => IsGraph ? TeamsProvider.Graph : IsLocal ? TeamsProvider.Local : TeamsProvider.None;
}

public sealed class LocalTeamsOptions
{
    /// <summary>Folder of the notices, absolute or relative to the folder of the application.</summary>
    public string Folder { get; set; } = "App_Data/teams";
}

/// <summary>
/// Checked when the host starts, so a wrong setup stops the application instead of running: Graph only with the sign-in
/// by Entra ID, whose application the package of Teams names; the local folder never in production; and the address of
/// the screens, which the package of Teams opens.
/// </summary>
internal sealed class TeamsOptionsValidation(IHostEnvironment environment, IConfiguration configuration) : IValidateOptions<TeamsOptions>
{
    public ValidateOptionsResult Validate(string? name, TeamsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            return ValidateOptionsResult.Success;
        }

        if (!options.IsGraph && !options.IsLocal)
        {
            return ValidateOptionsResult.Fail(
                $"The sending to Teams '{options.Provider}' (Teams:Provider) is unknown. Use {TeamsOptions.GraphProvider}, or {TeamsOptions.LocalProvider} outside production.");
        }

        if (options.IsGraph && !configuration.GetValue<bool>("EntraSignIn:Enabled"))
        {
            return ValidateOptionsResult.Fail(
                "The sending to Teams by the Microsoft Graph (Teams:Provider = Graph) needs the sign-in by Entra ID on (EntraSignIn:Enabled): Teams reaches only who signs in by Entra (ADR-0149).");
        }

        if (options.IsLocal && environment.IsProduction())
        {
            return ValidateOptionsResult.Fail(
                "The local sending to Teams (Teams:Provider = Local) is not allowed in production (ADR-0149): nothing would leave. Use Graph, or leave it empty.");
        }

        if (options.IsLocal && string.IsNullOrWhiteSpace(options.Local.Folder))
        {
            return ValidateOptionsResult.Fail("The local sending to Teams needs a folder (Teams:Local:Folder).");
        }

        return Uri.TryCreate(configuration["Email:SiteUrl"], UriKind.Absolute, out _)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("The sending to Teams needs the address of the screens (Email:SiteUrl), which the application of Teams opens.");
    }
}
