using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Web;

namespace FiscoProjects.Testing;

/// <summary>
/// A site of SharePoint simulated inside the process (ADR-0056, ADR-0120), as Microsoft Graph shows it: the site of an
/// address, its document libraries and their folders and files, with ids that do not change, the versions of a file
/// (newest first), "fail" and "replace" on a name taken, the path of the parent of each item and the eTag a removal may
/// check. The content of a file comes in the answer itself, where Graph sends the address of the content. It can also
/// throttle the next requests, as Graph does. Nothing reaches the network.
/// </summary>
public sealed partial class FakeSharePoint : HttpMessageHandler
{
    /// <summary>The token of the application the simulation accepts.</summary>
    public const string Token = "token-of-the-application-to-graph";

    /// <summary>The address of the simulated site.</summary>
    public const string SiteUrl = "https://empresa.sharepoint.example/sites/Projetos";

    public const string SiteId = "empresa.sharepoint.example,6f1e2d3c-4b5a-4978-8695-a4b3c2d1e0f9,0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d";

    private const string Host = "empresa.sharepoint.example";
    private const string SitePath = "/sites/Projetos";

    private readonly object _lock = new();
    private readonly Dictionary<string, Item> _items = [];
    private readonly Dictionary<string, Drive> _drives = [];
    private readonly ConcurrentQueue<string> _requests = new();
    private int _next;

    public FakeSharePoint()
    {
        AddDrive("Documentos");
        AddDrive("Arquivo morto");
    }

    /// <summary>How many children a page has, to check that the pages are followed.</summary>
    public int PageSize { get; set; } = 200;

    /// <summary>The next requests answered 429 with "Retry-After: 0", as Graph throttling.</summary>
    public int ThrottleNext { get; set; }

    /// <summary>The requests, as "GET drives/.../items/...", in order.</summary>
    public IReadOnlyList<string> Requests => [.. _requests];

    /// <summary>The names of the folders and files directly inside a folder of a library, by path ("" for its root).</summary>
    public IReadOnlyList<string> NamesIn(string library, string path)
    {
        lock (_lock)
        {
            var folder = ByPath(DriveNamed(library), path);
            return folder is null ? [] : [.. Children(folder).Select(item => item.Name).Order(StringComparer.Ordinal)];
        }
    }

    /// <summary>Puts a file somewhere in a library, as someone did outside the system.</summary>
    public string AddFile(string library, string path, string name, byte[] content)
    {
        lock (_lock)
        {
            var drive = DriveNamed(library);
            var folder = ByPath(drive, path) ?? throw new InvalidOperationException($"No folder {path}.");
            var file = NewItem(drive, folder, name, isFolder: false);
            file.Versions.Add(content);
            return file.Id;
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var address = request.RequestUri!;
        var path = address.AbsolutePath.StartsWith("/v1.0/", StringComparison.Ordinal) ? address.AbsolutePath[6..] : address.AbsolutePath;
        _requests.Enqueue($"{request.Method} {Uri.UnescapeDataString(path)}");
        if (ThrottleNext > 0)
        {
            ThrottleNext--;
            var throttled = Failure(HttpStatusCode.TooManyRequests, "activityLimitReached");
            throttled.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
            return throttled;
        }

        if (request.Headers.Authorization is not { Scheme: "Bearer", Parameter: Token })
        {
            return Failure(HttpStatusCode.Unauthorized, "InvalidAuthenticationToken");
        }

        var body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var query = HttpUtility.ParseQueryString(address.Query);
        lock (_lock)
        {
            return Answer(request, path, query, body);
        }
    }

    private HttpResponseMessage Answer(HttpRequestMessage request, string path, System.Collections.Specialized.NameValueCollection query, byte[] body)
    {
        var method = request.Method.Method;
        if (SiteByPath().Match(path) is { Success: true } byPath)
        {
            return byPath.Groups["host"].Value == Host && Uri.UnescapeDataString(byPath.Groups["path"].Value) == SitePath
                ? Json(new { id = SiteId, webUrl = SiteUrl })
                : Failure(HttpStatusCode.NotFound, "itemNotFound");
        }

        if (SiteDrives().Match(path) is { Success: true } drives)
        {
            if (Uri.UnescapeDataString(drives.Groups["site"].Value) != SiteId)
            {
                return Failure(HttpStatusCode.NotFound, "itemNotFound");
            }

            return drives.Groups["many"].Value == "drives"
                ? Json(new { value = _drives.Values.Select(drive => new { id = drive.Id, name = drive.Name }) })
                : Json(new { id = _drives.Values.First().Id, name = _drives.Values.First().Name });
        }

        var match = DriveItem().Match(path);
        if (!match.Success || !_drives.TryGetValue(Uri.UnescapeDataString(match.Groups["drive"].Value), out var drive))
        {
            return Failure(HttpStatusCode.NotFound, "itemNotFound");
        }

        var id = Uri.UnescapeDataString(match.Groups["id"].Value);
        var item = match.Groups["root"].Success ? _items[drive.RootId] : Find(drive, id);
        if (!match.Groups["root"].Success && !ValidId().IsMatch(id))
        {
            return Failure(HttpStatusCode.BadRequest, "invalidRequest");
        }

        if (item is null)
        {
            return Failure(HttpStatusCode.NotFound, "itemNotFound");
        }

        var name = match.Groups["name"].Success ? Uri.UnescapeDataString(match.Groups["name"].Value) : null;
        var rest = match.Groups["rest"].Value;
        return (method, name, rest) switch
        {
            ("GET", null, "") => Json(Shown(item)),
            ("GET", not null, "") => Children(item).FirstOrDefault(child => Same(child.Name, name)) is { } child
                ? Json(Shown(child))
                : Failure(HttpStatusCode.NotFound, "itemNotFound"),
            ("PUT", not null, ":/content") => Upload(drive, item, name, query["@microsoft.graph.conflictBehavior"] ?? "replace", body),
            ("POST", null, "/children") => CreateFolder(drive, item, body),
            ("GET", null, "/children") => ChildrenPage(drive, item, query["$skiptoken"]),
            ("GET", null, "/content") when !item.IsFolder => Content(item.Versions[^1]),
            ("GET", null, "/versions") when !item.IsFolder => Json(new
            {
                value = item.Versions.Select((content, index) => Version(index, content)).Reverse(),
            }),
            ("GET", null, _) when VersionPath().Match(rest) is { Success: true } version && !item.IsFolder =>
                VersionOf(item, Uri.UnescapeDataString(version.Groups["version"].Value), version.Groups["content"].Success),
            ("DELETE", null, "") => Delete(request, item),
            _ => Failure(HttpStatusCode.BadRequest, "invalidRequest"),
        };
    }

    private HttpResponseMessage Upload(Drive drive, Item folder, string name, string conflict, byte[] body)
    {
        if (!folder.IsFolder)
        {
            return Failure(HttpStatusCode.BadRequest, "invalidRequest");
        }

        if (Children(folder).FirstOrDefault(child => Same(child.Name, name)) is { } existing)
        {
            if (conflict == "fail" || existing.IsFolder)
            {
                return Failure(HttpStatusCode.Conflict, "nameAlreadyExists");
            }

            existing.Versions.Add(body);
            Touch(existing);
            return Json(Shown(existing));
        }

        var file = NewItem(drive, folder, name, isFolder: false);
        file.Versions.Add(body);
        return Json(Shown(file), HttpStatusCode.Created);
    }

    private HttpResponseMessage CreateFolder(Drive drive, Item parent, byte[] body)
    {
        var json = JsonNode.Parse(body)!;
        var name = json["name"]!.GetValue<string>();
        if (json["folder"] is not JsonObject || json["@microsoft.graph.conflictBehavior"]?.GetValue<string>() != "fail")
        {
            return Failure(HttpStatusCode.BadRequest, "invalidRequest");
        }

        if (Children(parent).Any(child => Same(child.Name, name)))
        {
            return Failure(HttpStatusCode.Conflict, "nameAlreadyExists");
        }

        return Json(Shown(NewItem(drive, parent, name, isFolder: true)), HttpStatusCode.Created);
    }

    private HttpResponseMessage ChildrenPage(Drive drive, Item folder, string? token)
    {
        var start = token is null ? 0 : int.Parse(token, CultureInfo.InvariantCulture);
        var children = Children(folder).OrderBy(child => child.Order).ToList();
        var page = children.Skip(start).Take(PageSize).Select(Shown).ToList();
        string? next = start + PageSize < children.Count
            ? $"https://graph.microsoft.com/v1.0/drives/{drive.Id}/items/{folder.Id}/children?$skiptoken={start + PageSize}"
            : null;
        var answer = new JsonObject { ["value"] = JsonSerializer.SerializeToNode(page, JsonSerializerOptions.Web) };
        if (next is not null)
        {
            answer["@odata.nextLink"] = next;
        }

        return Json(answer);
    }

    private static HttpResponseMessage VersionOf(Item file, string versionId, bool content)
    {
        var index = file.Versions.Count - 1;
        for (; index >= 0; index--)
        {
            if (VersionId(index) == versionId)
            {
                break;
            }
        }

        if (index < 0)
        {
            return Failure(HttpStatusCode.NotFound, "itemNotFound");
        }

        return content ? Content(file.Versions[index]) : Json(Version(index, file.Versions[index]));
    }

    private HttpResponseMessage Delete(HttpRequestMessage request, Item item)
    {
        if (item.ParentId is null)
        {
            return Failure(HttpStatusCode.BadRequest, "invalidRequest");
        }

        if (request.Headers.TryGetValues("If-Match", out var match) && match.Single() != ETagOf(item))
        {
            return Failure(HttpStatusCode.PreconditionFailed, "resourceModified");
        }

        foreach (var removed in Descendants(item).Append(item).ToList())
        {
            _items.Remove(removed.Id);
        }

        Touch(_items[item.ParentId]);
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    }

    private object Shown(Item item)
    {
        var parent = item.ParentId is null ? null : _items[item.ParentId];
        return new Dictionary<string, object?>
        {
            ["id"] = item.Id,
            ["name"] = item.Name,
            ["size"] = item.IsFolder ? Descendants(item).Where(child => !child.IsFolder).Sum(child => (long)child.Versions[^1].Length) : item.Versions[^1].Length,
            ["eTag"] = ETagOf(item),
            ["parentReference"] = parent is null ? new { driveId = item.DriveId } : new { id = parent.Id, driveId = item.DriveId, path = PathOf(parent) },
            [item.IsFolder ? "folder" : "file"] = item.IsFolder ? new { childCount = Children(item).Count() } : new { mimeType = "application/octet-stream" },
        };
    }

    private static object Version(int index, byte[] content) => new { id = VersionId(index), size = content.Length };

    private static string VersionId(int index) => $"{index + 1}.0";

    private string PathOf(Item folder)
    {
        var names = new List<string>();
        for (var current = folder; current.ParentId is not null; current = _items[current.ParentId])
        {
            names.Insert(0, current.Name);
        }

        return $"/drives/{folder.DriveId}/root:" + (names.Count == 0 ? string.Empty : "/" + string.Join('/', names));
    }

    private Item NewItem(Drive drive, Item parent, string name, bool isFolder)
    {
        var item = new Item(NewId(), drive.Id, parent.Id, name, isFolder, ++_next);
        _items.Add(item.Id, item);
        Touch(parent);
        return item;
    }

    private void AddDrive(string name)
    {
        var drive = new Drive($"b!{NewId()}", name, NewId());
        _drives.Add(drive.Id, drive);
        _items.Add(drive.RootId, new Item(drive.RootId, drive.Id, null, "root", IsFolder: true, Order: 0));
    }

    private Item? Find(Drive drive, string id) => _items.TryGetValue(id, out var item) && item.DriveId == drive.Id ? item : null;

    private Item? ByPath(Drive drive, string path)
    {
        var current = _items[drive.RootId];
        foreach (var name in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = Children(current).FirstOrDefault(child => Same(child.Name, name));
            if (current is null)
            {
                return null;
            }
        }

        return current;
    }

    private Drive DriveNamed(string name) => _drives.Values.Single(drive => drive.Name == name);

    private IEnumerable<Item> Children(Item folder) => _items.Values.Where(item => item.ParentId == folder.Id);

    private IEnumerable<Item> Descendants(Item folder) => Children(folder).SelectMany(child => Descendants(child).Append(child));

    private string NewId() => $"01{Interlocked.Increment(ref _next).ToString("D30", CultureInfo.InvariantCulture)}";

    private static void Touch(Item item) => item.Changes++;

    private static string ETagOf(Item item) => $"\"{{{item.Id}}},{item.Changes}\"";

    private static bool Same(string one, string other) => string.Equals(one, other, StringComparison.OrdinalIgnoreCase);

    private static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = JsonContent.Create(body, options: JsonSerializerOptions.Web) };

    private static HttpResponseMessage Content(byte[] content) => new(HttpStatusCode.OK) { Content = new ByteArrayContent(content) };

    private static HttpResponseMessage Failure(HttpStatusCode status, string code) =>
        Json(new { error = new { code, message = code } }, status);

    [GeneratedRegex("^sites/(?<host>[^/:]+):(?<path>/.+)$")]
    private static partial Regex SiteByPath();

    [GeneratedRegex("^sites/(?<site>[^/]+)/(?<many>drives?)$")]
    private static partial Regex SiteDrives();

    [GeneratedRegex("^drives/(?<drive>[^/]+)/(?:(?<root>root)|items/(?<id>[^/:]+))(?::/(?<name>[^/:]+))?(?<rest>(?::/content|/children|/content|/versions.*)?)$")]
    private static partial Regex DriveItem();

    [GeneratedRegex("^/versions/(?<version>[^/]+)(?<content>/content)?$")]
    private static partial Regex VersionPath();

    [GeneratedRegex("^[0-9A-Z]+$")]
    private static partial Regex ValidId();

    private sealed record Drive(string Id, string Name, string RootId);

    private sealed record Item(string Id, string DriveId, string? ParentId, string Name, bool IsFolder, int Order)
    {
        public List<byte[]> Versions { get; } = [];

        public int Changes { get; set; }
    }
}
