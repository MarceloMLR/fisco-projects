using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Users.Application;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>A directory of one identity provider, which the company directory joins.</summary>
internal interface IProviderDirectory : IPeopleDirectory
{
    string Provider { get; }
}

/// <summary>
/// The company directory in the tenant of the installation, read through Microsoft Graph with the token of the
/// application (ADR-0119): the active members, without guests or disabled accounts, as the sign-in admits them
/// (ADR-0007). Each person is "entra:{object id}", the link of the sign-in, so the first sign-in finds the user created
/// when someone included the person in a project. Off while the sign-in through Entra ID is off.
/// </summary>
internal sealed class GraphPeopleDirectory(
    IHttpClientFactory clients, IGraphTokens tokens, IOptions<EntraSignInOptions> options) : IProviderDirectory
{
    private const string Fields = "id,displayName,mail,userPrincipalName";

    // Only the members of the tenant who may still sign in (ADR-0007).
    private const string ActiveMembers = "userType eq 'Member' and accountEnabled eq true";

    public string Provider => SignInWithEntra.Provider;

    public async Task<IReadOnlyList<DirectoryPerson>> SearchAsync(
        string? search, int limit, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return [];
        }

        List<string> query =
        [
            $"$filter={Uri.EscapeDataString(ActiveMembers)}",
            $"$select={Fields}",
            "$orderby=displayName",
            $"$top={limit}",
            "$count=true",
        ];
        if (!string.IsNullOrWhiteSpace(search))
        {
            // The name by the beginning of its words, the e-mail and the sign-in name by their beginning (ADR-0119).
            var text = Quoted(search.Trim());
            query.Insert(0, $"$search={Uri.EscapeDataString(
                $"\"displayName:{text}\" OR \"mail:{text}\" OR \"userPrincipalName:{text}\"")}");
        }

        using var request = await RequestAsync($"users?{string.Join('&', query)}", cancellationToken);
        // $search and the filter on the type of the account are advanced queries of the directory.
        request.Headers.Add("ConsistencyLevel", "eventual");
        using var response = await clients.CreateClient(MicrosoftGraph.ClientName).SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var found = await response.Content.ReadFromJsonAsync<GraphUsers>(JsonSerializerOptions.Web, cancellationToken);
        return [.. (found?.Value ?? []).Select(PersonOf).OfType<DirectoryPerson>()];
    }

    public async Task<DirectoryPerson?> FindAsync(string provider, string externalId, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled || provider != Provider || !Guid.TryParse(externalId, out var objectId))
        {
            return null;
        }

        using var request = await RequestAsync(
            $"users/{objectId:D}?$select={Fields},userType,accountEnabled", cancellationToken);
        using var response = await clients.CreateClient(MicrosoftGraph.ClientName).SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<GraphUser>(JsonSerializerOptions.Web, cancellationToken);
        return user is { UserType: "Member", AccountEnabled: true } ? PersonOf(user) : null;
    }

    private async Task<HttpRequestMessage> RequestAsync(string path, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(MicrosoftGraph.Address, path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetAsync(cancellationToken));
        return request;
    }

    /// <summary>The person of a user of the directory: the e-mail or, without a mailbox, the sign-in name, as the sign-in.</summary>
    private DirectoryPerson? PersonOf(GraphUser user)
    {
        var email = (string.IsNullOrWhiteSpace(user.Mail) ? user.UserPrincipalName : user.Mail)?.Trim();
        if (string.IsNullOrWhiteSpace(user.Id) || string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var name = string.IsNullOrWhiteSpace(user.DisplayName) ? email : user.DisplayName.Trim();
        return new DirectoryPerson(Provider, user.Id.Trim().ToLowerInvariant(), name, email);
    }

    /// <summary>The text inside the double quotes of a clause of $search, with the double quotes and backslashes escaped.</summary>
    private static string Quoted(string text) => text.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal);

    private sealed record GraphUsers(IReadOnlyList<GraphUser>? Value);

    private sealed record GraphUser(
        string? Id, string? DisplayName, string? Mail, string? UserPrincipalName, string? UserType, bool? AccountEnabled);
}
