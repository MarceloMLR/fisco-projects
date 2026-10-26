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
/// Microsoft Graph simulated inside the process (ADR-0056, ADR-0119): the list and the search of the users of a
/// fictitious tenant and one user by id, as the directory of the people asks them. Like the real one, the search finds
/// the beginning of the words of the name and the beginning of the e-mail and of the sign-in name, without telling case
/// apart, and asks for the header of the advanced queries. Nothing reaches the network.
/// </summary>
public sealed partial class FakeGraph : HttpMessageHandler
{
    /// <summary>The token of the application the simulation accepts.</summary>
    public const string Token = "token-of-the-application-to-graph";

    private readonly ConcurrentQueue<Uri> _requests = new();

    public List<FakeGraphUser> Users { get; } = [];

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

    [GeneratedRegex("\"(\\w+):((?:[^\"\\\\]|\\\\.)*)\"")]
    private static partial Regex Clause();
}
