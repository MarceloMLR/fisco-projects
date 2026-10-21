namespace FiscoProjects.Files.Contracts;

/// <summary>What to do when a file is sent with the name of a file already in the folder.</summary>
public enum SameNameAction
{
    /// <summary>The file keeps its id and gets a new version; the previous one can still be opened (ADR-0031).</summary>
    NewVersion = 1,

    /// <summary>The new file is kept beside the other, with " (1)", " (2)"... before the extension.</summary>
    KeepBoth = 2,
}

/// <summary>A folder of the storage. Its id does not change when the folder is renamed, as in SharePoint.</summary>
public sealed record StoredFolder(string Id, string Name);

/// <summary>A file just stored: its id, the final name, the size in bytes and the current version.</summary>
public sealed record StoredFile(string Id, string Name, long Size, string VersionId);

/// <summary>The content of a file and what the browser needs to show or download it. Dispose it to close the content.</summary>
public sealed class FileDownload(Stream content, string name, string contentType, long size, string versionId) : IAsyncDisposable, IDisposable
{
    public Stream Content { get; } = content;

    public string Name { get; } = name;

    public string ContentType { get; } = contentType;

    public long Size { get; } = size;

    public string VersionId { get; } = versionId;

    public ValueTask DisposeAsync() => Content.DisposeAsync();

    public void Dispose() => Content.Dispose();
}

/// <summary>A file was sent to a folder that does not exist (anymore).</summary>
public sealed class StorageFolderNotFoundException(string folderId)
    : Exception($"The folder '{folderId}' does not exist in the file storage.")
{
    public string FolderId { get; } = folderId;
}

/// <summary>
/// Files of the installation (ADR-0004, ADR-0030, ADR-0079): SharePoint in production, a local folder in development
/// and tests. Folders and files are known by opaque ids the modules keep; a null parent folder is the configured root.
/// Names are adjusted to the SharePoint rules, and no path ever leaves the root.
/// </summary>
public interface IFileStorage
{
    /// <summary>The folder at the path (folders separated by "/"), creating the ones missing.</summary>
    Task<StoredFolder> EnsureFolderAsync(string? parentFolderId, string path, CancellationToken cancellationToken);

    /// <summary>The folders directly inside a folder, in alphabetical order.</summary>
    Task<IReadOnlyList<StoredFolder>> ListFoldersAsync(string? parentFolderId, CancellationToken cancellationToken);

    /// <summary>Stores a file in a folder. Throws <see cref="StorageFolderNotFoundException"/> when the folder does not exist.</summary>
    Task<StoredFile> UploadAsync(
        string folderId, string fileName, Stream content, SameNameAction sameName, CancellationToken cancellationToken);

    /// <summary>The current version of a file, or the given one; null when the file or the version does not exist.</summary>
    Task<FileDownload?> OpenAsync(string fileId, string? versionId, CancellationToken cancellationToken);

    /// <summary>Removes a file with all its versions; false when it does not exist.</summary>
    Task<bool> DeleteFileAsync(string fileId, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a folder only when it is empty, so nothing the system did not create is ever removed (ADR-0041);
    /// false when it still has something or does not exist.
    /// </summary>
    Task<bool> DeleteFolderIfEmptyAsync(string folderId, CancellationToken cancellationToken);

    /// <summary>
    /// The id of the folder that holds a file or a folder, so a module can remove the folders left empty above what it
    /// removed (ADR-0111); null when the item is at the root or does not exist.
    /// </summary>
    Task<string?> FolderOfAsync(string itemId, CancellationToken cancellationToken);
}
