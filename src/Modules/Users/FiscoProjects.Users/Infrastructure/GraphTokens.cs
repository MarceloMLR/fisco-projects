using Azure.Core;
using Azure.Identity;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Infrastructure;

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

    public async Task<string> GetAsync(CancellationToken cancellationToken)
    {
        if (options.Value is not { Enabled: true, TenantId.Length: > 0, ClientId.Length: > 0 })
        {
            throw new InvalidOperationException(
                "Microsoft Graph needs the application of the installation in Entra ID: turn on the sign-in through Entra ID (EntraSignIn:*).");
        }

        return (await _credential.Value.GetTokenAsync(Graph, cancellationToken)).Token;
    }
}
