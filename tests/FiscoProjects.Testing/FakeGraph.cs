using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace FiscoProjects.Testing;

/// <summary>A user of the simulated tenant: a member by default, active, with a mailbox when it has a mail.</summary>
public sealed record FakeGraphUser(
    string Id, string DisplayName, string? Mail, string UserPrincipalName, bool Guest = false, bool Enabled = true);

/// <summary>
/// The photo of a user of the simulated tenant: the bytes, the type Graph answers and whether Microsoft 365 has it in the
/// size of 96x96, as a photo kept only in Entra ID may not.
/// </summary>
public sealed record FakeGraphPhoto(byte[] Content, string ContentType = "image/jpeg", bool Sized = true);

/// <summary>An e-mail the simulation accepted from a mailbox: what the sending gave to Graph.</summary>
public sealed record FakeGraphMail(
    string Mailbox, string Subject, string ContentType, string Content, string ToAddress, string ToName, bool SaveToSentItems);

/// <summary>
/// Microsoft Graph simulated inside the process (ADR-0056, ADR-0119): the list and the search of the users of a
/// fictitious tenant and one user by id, as the directory of the people asks them, and the sending of e-mails from the
/// mailboxes the application may send from (ADR-0123), and the photos of the users (ADR-0124). Like the real one, the search finds the beginning of the words
/// of the name and the beginning of the e-mail and of the sign-in name, without telling case apart, and asks for the
/// header of the advanced queries; and a mailbox outside the scope of Exchange answers 403. Nothing reaches the network.
/// </summary>
public sealed partial class FakeGraph : HttpMessageHandler
{
    /// <summary>The token of the application the simulation accepts.</summary>
    public const string Token = "token-of-the-application-to-graph";

    private readonly ConcurrentQueue<Uri> _requests = new();
    private readonly ConcurrentQueue<FakeGraphMail> _mail = new();

    public List<FakeGraphUser> Users { get; } = [];

    /// <summary>The photos of the users, by the id of the user.</summary>
    public Dictionary<string, FakeGraphPhoto> Photos { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The mailboxes the application may send from, as the scope of "Application Mail.Send" in Exchange.</summary>
    public List<string> Mailboxes { get; } = [];

    /// <summary>The e-mails accepted, in order.</summary>
    public IReadOnlyList<FakeGraphMail> Mail => [.. _mail];

    /// <summary>When on, every request answers 503, as Graph out of service.</summary>
    public bool Down { get; set; }

    /// <summary>The addresses asked, in order.</summary>
    public IReadOnlyList<Uri> Requests => [.. _requests];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Yield();
        var address = request.RequestUri!;
        _requests.Enqueue(address);
        if (Down)
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }

        if (request.Headers.Authorization is not { Scheme: "Bearer", Parameter: Token })
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        var query = HttpUtility.ParseQueryString(address.Query);
        var path = address.AbsolutePath;
        var send = SendMail().Match(path);
        if (send.Success && request.Method == HttpMethod.Post)
        {
            return await SendMailAsync(Uri.UnescapeDataString(send.Groups[1].Value), request, cancellationToken);
        }

        var photo = UserPhoto().Match(path);
        if (photo.Success && request.Method == HttpMethod.Get)
        {
            return PhotoOf(photo.Groups[1].Value, sized: photo.Groups[2].Value != "photo");
        }

        if (path == "/v1.0/users")
        {
            return List(request, query);
        }

        var one = OneUser().Match(path);
        if (!one.Success)
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        var user = Users.FirstOrDefault(candidate => string.Equals(candidate.Id, one.Groups[1].Value, StringComparison.OrdinalIgnoreCase));
        return user is null
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : Json(Shown(user, query["$select"], withState: true));
    }

    private HttpResponseMessage List(HttpRequestMessage request, System.Collections.Specialized.NameValueCollection query)
    {
        var search = query["$search"];
        var filter = query["$filter"];
        var advanced = (search is not null || filter is not null)
            && (!request.Headers.TryGetValues("ConsistencyLevel", out var level) || !level.Contains("eventual") || query["$count"] != "true");
        if (advanced)
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        IEnumerable<FakeGraphUser> found = Users;
        if (filter == "userType eq 'Member' and accountEnabled eq true")
        {
            found = found.Where(user => !user.Guest && user.Enabled);
        }
        else if (filter is not null)
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        if (search is not null)
        {
            var clauses = Clause().Matches(search).ToDictionary(
                match => match.Groups[1].Value, match => match.Groups[2].Value.Replace("\\\"", "\"").Replace("\\\\", "\\"));
            found = found.Where(user =>
                (clauses.TryGetValue("displayName", out var name) && Words(user.DisplayName).Any(word => Starts(word, name)))
                || (clauses.TryGetValue("mail", out var mail) && user.Mail is not null && Starts(user.Mail, mail))
                || (clauses.TryGetValue("userPrincipalName", out var upn) && Starts(user.UserPrincipalName, upn)));
        }

        if (query["$orderby"] == "displayName")
        {
            found = found.OrderBy(user => user.DisplayName, StringComparer.OrdinalIgnoreCase);
        }

        if (int.TryParse(query["$top"], out var top))
        {
            found = found.Take(top);
        }

        return Json(new { value = found.Select(user => Shown(user, query["$select"], withState: false)).ToList() });
    }

    private async Task<HttpResponseMessage> SendMailAsync(string mailbox, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!Mailboxes.Contains(mailbox, StringComparer.OrdinalIgnoreCase))
        {
            // As Exchange answers a mailbox outside the scope of the role of the application.
            return new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = JsonContent.Create(
                    new { error = new { code = "ErrorAccessDenied", message = $"Access is denied for {mailbox}." } },
                    options: JsonSerializerOptions.Web),
            };
        }

        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
        var message = body.RootElement.GetProperty("message");
        var to = message.GetProperty("toRecipients")[0].GetProperty("emailAddress");
        _mail.Enqueue(new FakeGraphMail(
            mailbox,
            message.GetProperty("subject").GetString()!,
            message.GetProperty("body").GetProperty("contentType").GetString()!,
            message.GetProperty("body").GetProperty("content").GetString()!,
            to.GetProperty("address").GetString()!,
            to.GetProperty("name").GetString()!,
            body.RootElement.GetProperty("saveToSentItems").GetBoolean()));
        return new HttpResponseMessage(HttpStatusCode.Accepted);
    }

    /// <summary>The photo of a user, as Graph answers it: 404 without a photo, or without one in the size asked.</summary>
    private HttpResponseMessage PhotoOf(string userId, bool sized)
    {
        if (!Photos.TryGetValue(userId, out var photo) || (sized && !photo.Sized))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = JsonContent.Create(
                    new { error = new { code = "ImageNotFound", message = "Exception of type 'Microsoft.Fast.Profile.Core.Exception.ImageNotFoundException' was thrown." } },
                    options: JsonSerializerOptions.Web),
            };
        }

        var content = new ByteArrayContent(photo.Content);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(photo.ContentType);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    /// <summary>The user with the fields asked, as Graph answers them.</summary>
    private static Dictionary<string, object?> Shown(FakeGraphUser user, string? select, bool withState)
    {
        var all = new Dictionary<string, object?>
        {
            ["id"] = user.Id,
            ["displayName"] = user.DisplayName,
            ["mail"] = user.Mail,
            ["userPrincipalName"] = user.UserPrincipalName,
        };
        if (withState)
        {
            all["userType"] = user.Guest ? "Guest" : "Member";
            all["accountEnabled"] = user.Enabled;
        }

        var fields = select?.Split(',') ?? [.. all.Keys];
        return all.Where(pair => fields.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static IEnumerable<string> Words(string text) => text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private static bool Starts(string value, string text) => value.StartsWith(text, StringComparison.OrdinalIgnoreCase);

    private static HttpResponseMessage Json(object body) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(body, options: JsonSerializerOptions.Web) };

    [GeneratedRegex("^/v1\\.0/users/([^/]+)$")]
    private static partial Regex OneUser();

    [GeneratedRegex("^/v1\\.0/users/([^/]+)/sendMail$")]
    private static partial Regex SendMail();

    [GeneratedRegex("^/v1\\.0/users/([^/]+)/(photos/96x96|photo)/\\$value$")]
    private static partial Regex UserPhoto();

    [GeneratedRegex("\"(\\w+):((?:[^\"\\\\]|\\\\.)*)\"")]
    private static partial Regex Clause();
}
