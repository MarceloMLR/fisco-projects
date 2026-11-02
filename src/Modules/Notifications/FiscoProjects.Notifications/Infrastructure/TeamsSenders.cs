using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Notifications.Application;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>
/// Sends the notices to the Activity of Teams through Microsoft Graph, with the token of the application (ADR-0149):
/// first it asks whether the application of Teams of the installation is installed for the person, with the
/// permission the application declares (TeamsAppInstallation.Read.User), which also gives the id of the application in
/// the catalog of the company, for the link to its page; then it sends the notice, with the permission
/// TeamsActivity.Send.User. Throttling and Graph out of service are tried again by the queue; any other refusal is for
/// good. The errors say the code of Graph, never the person.
/// </summary>
internal sealed class GraphTeamsSender(IHttpClientFactory clients, IGraphTokens tokens) : ITeamsSender
{
    /// <summary>The name of the <see cref="HttpClient"/> of the sending.</summary>
    public const string ClientName = "graph-teams";

    /// <summary>The id of the application in the catalog of the company, by the id of its manifest.</summary>
    private readonly Dictionary<Guid, string> _catalogIds = [];
    private readonly Lock _lock = new();

    public async Task<TeamsSendResult> SendAsync(TeamsDelivery delivery, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        if (string.IsNullOrWhiteSpace(delivery.EntraObjectId))
        {
            return new TeamsSendResult(TeamsSendOutcome.NotInstalled, "The person does not sign in by Entra ID.");
        }

        var client = clients.CreateClient(ClientName);
        var token = await tokens.GetAsync(cancellationToken);
        var user = Uri.EscapeDataString(delivery.EntraObjectId);
        var manifestId = delivery.ManifestId.ToString();

        using var installed = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(MicrosoftGraph.Address,
                $"users/{user}/teamwork/installedApps?$expand=teamsApp&$filter=teamsApp/externalId eq '{manifestId}'"));
        installed.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using (var answer = await client.SendAsync(installed, cancellationToken))
        {
            if (!answer.IsSuccessStatusCode)
            {
                // Without the application, its permissions do not exist for the person, and Graph refuses the reading.
                return ResultOf(answer.StatusCode, await ErrorCodeAsync(answer, cancellationToken), TeamsSendOutcome.NotInstalled);
            }

            var apps = await answer.Content.ReadFromJsonAsync<InstalledApps>(JsonSerializerOptions.Web, cancellationToken);
            if (apps?.Value is not [{ TeamsApp.Id: { Length: > 0 } catalogId }, ..])
            {
                return new TeamsSendResult(TeamsSendOutcome.NotInstalled, "The application of Teams is not installed for the person.");
            }

            lock (_lock)
            {
                _catalogIds[delivery.ManifestId] = catalogId;
            }
        }

        var message = delivery.Message;
        var body = new ActivityNotification(
            new Topic("text", message.Topic, LinkOf(CatalogIdOf(delivery.ManifestId), message.Path)),
            message.ActivityType,
            new PreviewText(message.Preview),
            [.. message.Parameters.Select(parameter => new TemplateParameter(parameter.Key, parameter.Value))]);
        using var send = new HttpRequestMessage(HttpMethod.Post, new Uri(MicrosoftGraph.Address, $"users/{user}/teamwork/sendActivityNotification"))
        {
            Content = JsonContent.Create(body, options: JsonSerializerOptions.Web),
        };
        send.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(send, cancellationToken);
        return response.IsSuccessStatusCode
            ? TeamsSendResult.Sent
            : ResultOf(response.StatusCode, await ErrorCodeAsync(response, cancellationToken), TeamsSendOutcome.Refused);
    }

    /// <summary>
    /// The link of the notice: the page of the application in Teams, with the address of the item in its context. Teams
    /// opens only its own addresses from a notice.
    /// </summary>
    public static string LinkOf(string appId, string path)
    {
        var context = JsonSerializer.Serialize(new { subEntityId = path });
        return string.Create(
            CultureInfo.InvariantCulture,
            $"https://teams.microsoft.com/l/entity/{Uri.EscapeDataString(appId)}/{TeamsAppManifest.PageId}?context={Uri.EscapeDataString(context)}");
    }

    private string CatalogIdOf(Guid manifestId)
    {
        lock (_lock)
        {
            return _catalogIds.GetValueOrDefault(manifestId) ?? manifestId.ToString();
        }
    }

    private static TeamsSendResult ResultOf(HttpStatusCode status, string? code, TeamsSendOutcome refusal)
    {
        var error = string.Create(CultureInfo.InvariantCulture, $"Microsoft Graph answered {(int)status}{(code is null ? string.Empty : $" {code}")}.");
        var temporary = status is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout || (int)status >= 500;
        return new TeamsSendResult(temporary ? TeamsSendOutcome.TryAgain : refusal, error);
    }

    /// <summary>The code of the error of Graph; the message may carry the person.</summary>
    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<GraphError>(JsonSerializerOptions.Web, cancellationToken);
            return error?.Error?.Code;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private sealed record InstalledApps(IReadOnlyList<InstalledApp>? Value);

    private sealed record InstalledApp(CatalogApp? TeamsApp);

    private sealed record CatalogApp(string? Id);

    private sealed record ActivityNotification(
        Topic Topic, string ActivityType, PreviewText PreviewText, IReadOnlyList<TemplateParameter> TemplateParameters);

    private sealed record Topic(string Source, string Value, string WebUrl);

    private sealed record PreviewText(string Content);

    private sealed record TemplateParameter(string Name, string Value);

    private sealed record GraphError(GraphErrorDetail? Error);

    private sealed record GraphErrorDetail(string? Code);
}

/// <summary>
/// Writes each notice of Teams as a .json file in a folder, without sending it (ADR-0004, ADR-0149): who receives it,
/// the kind, the texts and the link, for development and tests.
/// </summary>
internal sealed class LocalTeamsSender(string folder) : ITeamsSender
{
    private static readonly JsonSerializerOptions Indented = new(JsonSerializerOptions.Web) { WriteIndented = true };

    public string Folder { get; } = folder;

    public async Task<TeamsSendResult> SendAsync(TeamsDelivery delivery, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        Directory.CreateDirectory(Folder);
        var message = delivery.Message;
        var file = Path.Combine(Folder, $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(
            file,
            JsonSerializer.Serialize(
                new
                {
                    to = new { delivery.UserId, delivery.Name },
                    activityType = message.ActivityType,
                    parameters = message.Parameters.ToDictionary(parameter => parameter.Key, parameter => parameter.Value),
                    topic = message.Topic,
                    preview = message.Preview,
                    link = GraphTeamsSender.LinkOf(delivery.ManifestId.ToString(), message.Path),
                },
                Indented),
            cancellationToken);
        return TeamsSendResult.Sent;
    }
}

/// <summary>No sending to Teams configured: what is left in the queue goes by e-mail to who chose only Teams.</summary>
internal sealed class NoTeamsSender : ITeamsSender
{
    public Task<TeamsSendResult> SendAsync(TeamsDelivery delivery, CancellationToken cancellationToken) =>
        Task.FromResult(new TeamsSendResult(TeamsSendOutcome.Refused, "No sending to Teams is configured (Teams:Provider)."));
}
