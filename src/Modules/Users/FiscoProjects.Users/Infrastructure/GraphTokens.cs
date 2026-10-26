using Azure.Core;
using Azure.Identity;
using FiscoProjects.Users.Application;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>How the application reaches Microsoft Graph (ADR-0119).</summary>
public static class MicrosoftGraph
{
    /// <summary>The name of the <see cref="HttpClient"/> of the calls to Graph.</summary>
    public const string ClientName = "graph";

    /// <summary>The address of Microsoft Graph, version 1.0.</summary>
    public static readonly Uri Address = new("https://graph.microsoft.com/v1.0/");
}

/// <summary>The access tokens of the application to Microsoft Graph (ADR-0119).</summary>
public interface IGraphTokens
{
    Task<string> GetAsync(CancellationToken cancellationToken);
}

/// <summary>
/// The token of the application to Microsoft Graph, asked to Entra ID with the proof of the managed identity, as the
/// sign-in does (ADR-0117, ADR-0119): the same tenant and application, no secret. The Azure SDK keeps the token until it
/// is about to expire.
/// </summary>
internal sealed class ClientAssertionGraphTokens(IOptions<EntraSignInOptions> options, IEntraClientAssertion assertion)
    : IGraphTokens
{
    private static readonly TokenRequestContext Graph = new(["https://graph.microsoft.com/.default"]);

    private readonly Lazy<ClientAssertionCredential> _credential = new(() => new ClientAssertionCredential(
        options.Value.TenantId,
        options.Value.ClientId,
        assertion.GetAsync,
        new ClientAssertionCredentialOptions { AuthorityHost = new Uri(options.Value.Instance) }));

    public async Task<string> GetAsync(CancellationToken cancellationToken) =>
        (await _credential.Value.GetTokenAsync(Graph, cancellationToken)).Token;
}
