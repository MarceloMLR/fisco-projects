namespace FiscoProjects.Users.Contracts;

/// <summary>How the modules reach Microsoft Graph with the identity of the application (ADR-0119, ADR-0120).</summary>
public static class MicrosoftGraph
{
    /// <summary>The name of the <see cref="HttpClient"/> of the people directory.</summary>
    public const string ClientName = "graph";

    /// <summary>The address of Microsoft Graph, version 1.0.</summary>
    public static readonly Uri Address = new("https://graph.microsoft.com/v1.0/");
}

/// <summary>
/// The access tokens of the application of the installation to Microsoft Graph (ADR-0119, ADR-0120), asked with the
/// proof of the managed identity, no secret. The Users module owns the application in Entra ID and gives them.
/// </summary>
public interface IGraphTokens
{
    Task<string> GetAsync(CancellationToken cancellationToken);
}
