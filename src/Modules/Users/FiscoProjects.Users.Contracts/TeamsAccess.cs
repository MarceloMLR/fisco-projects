namespace FiscoProjects.Users.Contracts;

/// <summary>
/// The readings of the page of FiscoProjects in Teams (ADR-0152): under /api/teams, only with the token Teams gives the
/// page for the application of the installation, never with the session cookie, which does not reach the frame of
/// Teams. The token is valid nowhere else.
/// </summary>
public static class TeamsAccess
{
    /// <summary>The authentication scheme of the token of Teams.</summary>
    public const string Scheme = "teams";

    /// <summary>The policy of the routes of Teams: only the scheme of Teams, with a person recognized.</summary>
    public const string Policy = "TeamsReadings";

    /// <summary>The prefix of the routes of Teams, inside /api.</summary>
    public const string Prefix = "/teams";
}
