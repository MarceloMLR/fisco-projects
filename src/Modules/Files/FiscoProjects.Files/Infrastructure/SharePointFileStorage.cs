using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FiscoProjects.Files.Contracts;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Files.Infrastructure;

/// <summary>
/// Files in a site of the SharePoint of the company, through Microsoft Graph with the token of the application
/// (ADR-0030, ADR-0120). The root is a folder of a document library of the site, created when missing; folders and
/// files are known by the ids of SharePoint, which do not change when someone renames them, and the versions are the
/// ones SharePoint keeps. The application writes only in that site (Sites.Selected) and never touches permissions. An
/// id outside the root counts as not found, so nothing outside is ever opened or removed.
/// </summary>
internal sealed class SharePointFileStorage(IHttpClientFactory clients, IGraphTokens tokens, SharePointStorageOptions options)
    : IFileStorage
{
    /// <summary>The name of the <see cref="HttpClient"/> of the storage.</summary>
    public const string ClientName = "sharepoint";

    private const int Attempts = 4;
    private const string ItemFields = "id,name,size,folder,file,parentReference,eTag";

    private static readonly TimeSpan LongestWait = TimeSpan.FromSeconds(30);
    private static readonly StringComparer Alphabetical = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true);

    private readonly SemaphoreSlim _finding = new(1, 1);
    private Root? _root;

    public async Task<StoredFolder> EnsureFolderAsync(string? parentFolderId, string path, CancellationToken cancellationToken)
    {
        var segments = FileNames.Segments(path);
        var root = await RootAsync(cancellationToken);
        var parent = await FolderIdAsync(root, parentFolderId, cancellationToken);
        GraphItem? folder = null;
        foreach (var segment in segments)
        {
            folder = await ChildFolderAsync(root.DriveId, parent, segment, cancellationToken);
            parent = folder.Id;
        }

        return new StoredFolder(folder!.Id, folder.Name);
    }

    public async Task<StoredFolder?> RenameFolderAsync(string folderId, string name, CancellationToken cancellationToken)
    {
        var newName = FileNames.Normalize(name);
        var root = await RootAsync(cancellationToken);
        if (string.IsNullOrEmpty(folderId) || folderId == root.FolderId)
        {
            throw new ArgumentException("The root of the storage is never renamed.", nameof(folderId));
        }

        if (await ItemInsideAsync(root, folderId, cancellationToken) is not { Folder: not null } folder)
        {
            throw new StorageFolderNotFoundException(folderId);
        }

        if (folder.Name == newName)
        {
            return new StoredFolder(folder.Id, folder.Name);
        }

        // The item keeps its id; "fail" says another item beside it already has the name.
        using var response = await SendAsync(
            () => new HttpRequestMessage(HttpMethod.Patch, $"drives/{root.DriveId}/items/{folder.Id}")
            {
                Content = JsonContent.Create(new NewName(newName, "fail"), options: JsonSerializerOptions.Web),
            },
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageFolderNotFoundException(folderId);
        }

        var renamed = await ReadAsync<GraphItem>(response, "rename a folder", cancellationToken);
        return new StoredFolder(renamed.Id, renamed.Name);
    }

    public async Task<IReadOnlyList<StoredFolder>> ListFoldersAsync(string? parentFolderId, CancellationToken cancellationToken)
    {
        var root = await RootAsync(cancellationToken);
        var parent = await FolderIdAsync(root, parentFolderId, cancellationToken);
        var folders = new List<StoredFolder>();
        string? page = $"drives/{root.DriveId}/items/{parent}/children?$select=id,name,folder&$top=999";
        while (page is not null)
        {
            var children = await GetAsync<GraphList<GraphItem>>(page, cancellationToken)
                ?? throw new StorageFolderNotFoundException(parentFolderId ?? string.Empty);
            folders.AddRange(children.Value
                .Where(item => item.Folder is not null)
                .Where(item => parent != root.FolderId || !string.Equals(item.Name, FileNames.InternalFolder, StringComparison.OrdinalIgnoreCase))
                .Select(item => new StoredFolder(item.Id, item.Name)));
            page = children.NextLink;
        }

        return [.. folders.OrderBy(folder => folder.Name, Alphabetical)];
    }

    public async Task<StoredFile> UploadAsync(
        string folderId, string fileName, Stream content, SameNameAction sameName, CancellationToken cancellationToken)
    {
        var name = FileNames.Normalize(fileName);
        var root = await RootAsync(cancellationToken);
        var folder = await FolderIdAsync(root, folderId, cancellationToken);

        // In memory, so a throttled request can be sent again; the modules limit the size of what they store.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.GetBuffer().AsMemory(0, (int)buffer.Length);

        for (var number = 0; ; number++)
        {
            var candidate = number == 0 || sameName == SameNameAction.NewVersion ? name : FileNames.Numbered(name, number);
            // "replace" makes a new version of the same item; "fail" says the name is taken, to try the next one.
            var conflict = sameName == SameNameAction.NewVersion ? "replace" : "fail";
            using var response = await SendAsync(
                () => new HttpRequestMessage(
                    HttpMethod.Put,
                    $"drives/{root.DriveId}/items/{folder}:/{Escaped(candidate)}:/content?@microsoft.graph.conflictBehavior={conflict}")
                {
                    Content = new ReadOnlyMemoryContent(bytes) { Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") } },
                },
                cancellationToken);
            if (response.StatusCode == HttpStatusCode.Conflict && sameName == SameNameAction.KeepBoth)
            {
                continue;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new StorageFolderNotFoundException(folderId);
            }

            var item = await ReadAsync<GraphItem>(response, "upload a file", cancellationToken);
            var version = await CurrentVersionAsync(root.DriveId, item.Id, cancellationToken);
            return new StoredFile(item.Id, item.Name, item.Size, version?.Id ?? "1.0");
        }
    }

    public async Task<FileDownload?> OpenAsync(string fileId, string? versionId, CancellationToken cancellationToken)
    {
        var root = await RootAsync(cancellationToken);
        if (await ItemInsideAsync(root, fileId, cancellationToken) is not { File: not null } item)
        {
            return null;
        }

        var current = await CurrentVersionAsync(root.DriveId, item.Id, cancellationToken);
        var currentId = current?.Id ?? "1.0";
        string path;
        long size;
        if (versionId is null || versionId == currentId)
        {
            (path, size, versionId) = ($"drives/{root.DriveId}/items/{item.Id}/content", item.Size, currentId);
        }
        else if (await GetAsync<GraphVersion>($"drives/{root.DriveId}/items/{item.Id}/versions/{Escaped(versionId)}", cancellationToken)
            is { } version)
        {
            (path, size) = ($"drives/{root.DriveId}/items/{item.Id}/versions/{Escaped(version.Id)}/content", version.Size ?? 0);
        }
        else
        {
            return null;
        }

        // Graph answers with the address of the content, which the client follows without the token.
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, path), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            response.Dispose();
            return null;
        }

        await EnsureSuccessAsync(response, "open a file", cancellationToken);
        var stream = new ResponseStream(response, await response.Content.ReadAsStreamAsync(cancellationToken));
        return new FileDownload(stream, item.Name, ContentTypes.Of(item.Name), size, versionId);
    }

    public async Task<bool> DeleteFileAsync(string fileId, CancellationToken cancellationToken)
    {
        var root = await RootAsync(cancellationToken);
        if (await ItemInsideAsync(root, fileId, cancellationToken) is not { File: not null } item)
        {
            return false;
        }

        // SharePoint keeps the file in the recycle bin of the site, with its versions, as for any removal.
        return await DeleteAsync(root.DriveId, item, cancellationToken);
    }

    public async Task<bool> DeleteFolderIfEmptyAsync(string folderId, CancellationToken cancellationToken)
    {
        var root = await RootAsync(cancellationToken);
        // The root is never removed, and a folder with anything in it stays (ADR-0041).
        if (folderId == root.FolderId
            || await ItemInsideAsync(root, folderId, cancellationToken) is not { Folder.ChildCount: 0 } folder)
        {
            return false;
        }

        return await DeleteAsync(root.DriveId, folder, cancellationToken);
    }

    public async Task<string?> FolderOfAsync(string itemId, CancellationToken cancellationToken)
    {
        var root = await RootAsync(cancellationToken);
        if (itemId == root.FolderId || await ItemInsideAsync(root, itemId, cancellationToken) is not { } item)
        {
            return null;
        }

        var parent = item.ParentReference?.Id;
        return parent is null || parent == root.FolderId ? null : parent;
    }

    /// <summary>The folder of the files of the system, found once: the site, its library and the root folder in it.</summary>
    private async Task<Root> RootAsync(CancellationToken cancellationToken)
    {
        if (_root is { } known)
        {
            return known;
        }

        await _finding.WaitAsync(cancellationToken);
        try
        {
            return _root ??= await FindRootAsync(cancellationToken);
        }
        finally
        {
            _finding.Release();
        }
    }

    private async Task<Root> FindRootAsync(CancellationToken cancellationToken)
    {
        var site = await GetAsync<GraphSite>(SitePath(options.SiteUrl!), cancellationToken)
            ?? throw new InvalidOperationException(
                "The SharePoint site of FileStorage:SharePoint:SiteUrl was not found, or the application was not given access to it (Sites.Selected, docs/instalacao.md).");
        GraphDrive drive;
        if (string.IsNullOrWhiteSpace(options.Library))
        {
            drive = await GetAsync<GraphDrive>($"sites/{site.Id}/drive?$select=id,name", cancellationToken)
                ?? throw new InvalidOperationException("The SharePoint site has no document library.");
        }
        else
        {
            var drives = await GetAsync<GraphList<GraphDrive>>($"sites/{site.Id}/drives?$select=id,name", cancellationToken);
            drive = drives?.Value.FirstOrDefault(candidate => string.Equals(candidate.Name, options.Library.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException(
                    $"The SharePoint site has no document library named '{options.Library.Trim()}' (FileStorage:SharePoint:Library).");
        }

        var folder = await GetAsync<GraphItem>($"drives/{drive.Id}/root?$select={ItemFields}", cancellationToken)
            ?? throw new InvalidOperationException("The document library of the SharePoint storage could not be opened.");
        var names = new List<string>();
        if (!string.IsNullOrWhiteSpace(options.RootFolder))
        {
            foreach (var segment in FileNames.Segments(options.RootFolder))
            {
                folder = await ChildFolderAsync(drive.Id, folder.Id, segment, cancellationToken);
                names.Add(folder.Name);
            }
        }

        return new Root(drive.Id, folder.Id, names.Count == 0 ? string.Empty : "/" + string.Join('/', names));
    }

    /// <summary>The folder with the name inside the parent, created when missing.</summary>
    private async Task<GraphItem> ChildFolderAsync(string driveId, string parentId, string name, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            if (await GetAsync<GraphItem>($"drives/{driveId}/items/{parentId}:/{Escaped(name)}?$select={ItemFields}", cancellationToken)
                is { } existing)
            {
                return existing.Folder is not null
                    ? existing
                    : throw new InvalidOperationException("A file has the name of a folder of the storage.");
            }

            using var created = await SendAsync(
                () => new HttpRequestMessage(HttpMethod.Post, $"drives/{driveId}/items/{parentId}/children")
                {
                    Content = JsonContent.Create(
                        new NewFolder(name, new EmptyFacet(), "fail"), options: JsonSerializerOptions.Web),
                },
                cancellationToken);
            if (created.StatusCode == HttpStatusCode.NotFound)
            {
                throw new StorageFolderNotFoundException(parentId);
            }

            // Someone else created the same folder meanwhile: it is the one.
            if (created.StatusCode != HttpStatusCode.Conflict)
            {
                return await ReadAsync<GraphItem>(created, "create a folder", cancellationToken);
            }
        }

        throw new InvalidOperationException("A folder of the storage could not be created.");
    }

    /// <summary>The id of a folder given by a module, or of the root when none is given.</summary>
    private async Task<string> FolderIdAsync(Root root, string? folderId, CancellationToken cancellationToken) =>
        folderId is null || folderId == root.FolderId
            ? root.FolderId
            : await ItemInsideAsync(root, folderId, cancellationToken) is { Folder: not null } folder
                ? folder.Id
                : throw new StorageFolderNotFoundException(folderId);

    /// <summary>The item of an id, when it exists and is inside the root; anything else is not found.</summary>
    private async Task<GraphItem?> ItemInsideAsync(Root root, string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var item = await GetAsync<GraphItem>($"drives/{root.DriveId}/items/{Escaped(id)}?$select={ItemFields}", cancellationToken);
        if (item is null || item.Id == root.FolderId)
        {
            return item;
        }

        var parent = item.ParentReference;
        if (parent?.DriveId is { } drive && !string.Equals(drive, root.DriveId, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // The path of the parent, as "/drives/{id}/root:/FiscoProjects/ERP-FIN", from the root of the library.
        var path = parent?.Path is { } full && full.IndexOf("root:", StringComparison.Ordinal) is var at and >= 0
            ? Uri.UnescapeDataString(full[(at + "root:".Length)..])
            : null;
        return path is not null && (path.Equals(root.Path, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(root.Path + "/", StringComparison.OrdinalIgnoreCase))
            ? item
            : null;
    }

    private async Task<GraphVersion?> CurrentVersionAsync(string driveId, string itemId, CancellationToken cancellationToken)
    {
        // Graph lists the versions from the newest (the current one) to the oldest.
        var versions = await GetAsync<GraphList<GraphVersion>>($"drives/{driveId}/items/{itemId}/versions", cancellationToken);
        return versions?.Value.FirstOrDefault();
    }

    private async Task<bool> DeleteAsync(string driveId, GraphItem item, CancellationToken cancellationToken)
    {
        // Only as it was read: a folder that got something meanwhile stays.
        using var response = await SendAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, $"drives/{driveId}/items/{item.Id}");
                if (item.ETag is { } etag)
                {
                    request.Headers.TryAddWithoutValidation("If-Match", etag);
                }

                return request;
            },
            cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.PreconditionFailed)
        {
            return false;
        }

        await EnsureSuccessAsync(response, "remove an item", cancellationToken);
        return true;
    }

    /// <summary>The JSON of a read, or null when the item does not exist (or the id is not one of Graph).</summary>
    private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
        where T : class
    {
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, path), cancellationToken);
        return response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest
            ? null
            : await ReadAsync<T>(response, "read", cancellationToken);
    }

    /// <summary>
    /// Sends a request with the token of the application, again after the wait Graph asks when it throttles or is
    /// busy, a few times. The request is made again for each attempt.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> request, CancellationToken cancellationToken)
    {
        var client = clients.CreateClient(ClientName);
        for (var attempt = 1; ; attempt++)
        {
            using var message = request();
            message.RequestUri = new Uri(MicrosoftGraph.Address, message.RequestUri!.OriginalString);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetAsync(cancellationToken));
            var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (attempt == Attempts || response.StatusCode is not (HttpStatusCode.TooManyRequests
                or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout))
            {
                return response;
            }

            var wait = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(attempt * 2);
            response.Dispose();
            await Task.Delay(wait < LongestWait ? wait : LongestWait, cancellationToken);
        }
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, string action, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, action, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonSerializerOptions.Web, cancellationToken)
            ?? throw new HttpRequestException($"Microsoft Graph answered nothing to {action} in the SharePoint storage.");
    }

    /// <summary>A failure says the status and the code of Graph, never the names of the files and folders.</summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string action, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? code = null;
        try
        {
            code = (await response.Content.ReadFromJsonAsync<GraphFailure>(JsonSerializerOptions.Web, cancellationToken))?.Error?.Code;
        }
        catch (JsonException)
        {
        }

        throw new HttpRequestException(
            $"Microsoft Graph answered {(int)response.StatusCode} ({code ?? "no code"}) to {action} in the SharePoint storage.",
            null,
            response.StatusCode);
    }

    /// <summary>The site of an address: "sites/{host}:/{path}", or "sites/{host}" for the root site.</summary>
    private static string SitePath(string siteUrl)
    {
        var site = new Uri(siteUrl);
        var path = site.AbsolutePath.TrimEnd('/');
        return path.Length == 0 ? $"sites/{site.Host}" : $"sites/{site.Host}:{path}";
    }

    private static string Escaped(string text) => Uri.EscapeDataString(text);

    private sealed record Root(string DriveId, string FolderId, string Path);

    private sealed record GraphSite(string Id);

    private sealed record GraphDrive(string Id, string Name);

    private sealed record GraphFolder(int ChildCount);

    private sealed record GraphFile(string? MimeType);

    private sealed record GraphParent(string? Id, string? DriveId, string? Path);

    private sealed record GraphItem(
        string Id, string Name, long Size, GraphFolder? Folder, GraphFile? File, GraphParent? ParentReference, string? ETag);

    private sealed record GraphVersion(string Id, long? Size);

    private sealed record GraphList<T>(IReadOnlyList<T> Value)
    {
        [JsonPropertyName("@odata.nextLink")]
        public string? NextLink { get; init; }
    }

    /// <summary>A facet without properties, as the "folder": {} of a new folder.</summary>
    private sealed record EmptyFacet;

    private sealed record NewFolder(
        string Name,
        EmptyFacet Folder,
        [property: JsonPropertyName("@microsoft.graph.conflictBehavior")] string ConflictBehavior);

    private sealed record NewName(
        string Name,
        [property: JsonPropertyName("@microsoft.graph.conflictBehavior")] string ConflictBehavior);

    private sealed record GraphError(string? Code);

    private sealed record GraphFailure(GraphError? Error);

    /// <summary>The content of a download, which closes the answer of Graph when it is closed.</summary>
    private sealed class ResponseStream(HttpResponseMessage response, Stream content) : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => content.Read(buffer, offset, count);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            content.ReadAsync(buffer, cancellationToken);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            content.ReadAsync(buffer, offset, count, cancellationToken);

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                content.Dispose();
                response.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
