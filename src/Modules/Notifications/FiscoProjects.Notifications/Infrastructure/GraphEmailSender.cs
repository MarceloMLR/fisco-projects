using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Notifications.Application;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>
/// Sends the e-mails through Microsoft Graph, from the shared mailbox of the installation, with the token of the
/// application (ADR-0036, ADR-0123): Exchange lets the application send only from that mailbox. Any answer but the
/// acceptance is a failure of the try, which the queue tries again in its time; the failure says the code of Graph,
/// never the address nor the content.
/// </summary>
internal sealed class GraphEmailSender(IHttpClientFactory clients, IGraphTokens tokens, string mailbox) : IEmailSender
{
    /// <summary>The name of the <see cref="HttpClient"/> of the sending.</summary>
    public const string ClientName = "graph-mail";

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var mail = new SendMail(
            new GraphMessage(
                message.Subject,
                new GraphBody("HTML", message.HtmlBody),
                [new GraphRecipient(new GraphAddress(message.ToAddress, message.ToName))]),
            SaveToSentItems: true);
        using var request = new HttpRequestMessage(
            HttpMethod.Post, new Uri(MicrosoftGraph.Address, $"users/{Uri.EscapeDataString(mailbox)}/sendMail"))
        {
            Content = JsonContent.Create(mail, options: JsonSerializerOptions.Web),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetAsync(cancellationToken));
        using var response = await clients.CreateClient(ClientName).SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Accepted)
        {
            return;
        }

        throw new EmailSendException((int)response.StatusCode, await ErrorCodeAsync(response, cancellationToken));
    }

    /// <summary>The code of the error of Graph, as ErrorAccessDenied; the message may carry the addresses.</summary>
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

    private sealed record SendMail(GraphMessage Message, bool SaveToSentItems);

    private sealed record GraphMessage(string Subject, GraphBody Body, IReadOnlyList<GraphRecipient> ToRecipients);

    private sealed record GraphBody(string ContentType, string Content);

    private sealed record GraphRecipient(GraphAddress EmailAddress);

    private sealed record GraphAddress(string Address, string Name);

    private sealed record GraphError(GraphErrorDetail? Error);

    private sealed record GraphErrorDetail(string? Code);
}

/// <summary>Microsoft Graph did not accept an e-mail: the status of the answer and the code of the error.</summary>
public sealed class EmailSendException(int status, string? code)
    : Exception($"Microsoft Graph did not accept the e-mail: {status}{(code is null ? string.Empty : $" {code}")}.")
{
    public int Status { get; } = status;

    public string? Code { get; } = code;
}
