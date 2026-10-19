using FiscoProjects.Files.Contracts;

namespace FiscoProjects.Projects.Tests;

/// <summary>A file storage in memory, enough for the logo: folders by path, files by name, versions counted.</summary>
internal sealed class MemoryFileStorage : IFileStorage
{
    private readonly Dictionary<string, StoredFolder> _folders = [];
    private readonly Dictionary<string, (string FolderId, string Name, byte[] Content, int Version)> _files = [];

    public IReadOnlyDictionary<string, (string FolderId, string Name, byte[] Content, int Version)> Files => _files;

    public string? FolderNameOf(string fileId) =>
        _files.TryGetValue(fileId, out var file) ? _folders.Values.Single(folder => folder.Id == file.FolderId).Name : null;

    public Task<StoredFolder> EnsureFolderAsync(string? parentFolderId, string path, CancellationToken cancellationToken)
    {
        var key = $"{parentFolderId}/{path}";
        if (!_folders.TryGetValue(key, out var folder))
        {
            folder = new StoredFolder($"folder-{_folders.Count + 1}", path);
            _folders[key] = folder;
        }

        return Task.FromResult(folder);
    }

    public Task<IReadOnlyList<StoredFolder>> ListFoldersAsync(string? parentFolderId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StoredFolder>>([.. _folders.Values]);

    public async Task<StoredFile> UploadAsync(
        string folderId, string fileName, Stream content, SameNameAction sameName, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var id = $"{folderId}/{fileName}";
        var version = _files.TryGetValue(id, out var existing) ? existing.Version + 1 : 1;
        _files[id] = (folderId, fileName, buffer.ToArray(), version);
        return new StoredFile(id, fileName, buffer.Length, version.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public Task<FileDownload?> OpenAsync(string fileId, string? versionId, CancellationToken cancellationToken) =>
        Task.FromResult(_files.TryGetValue(fileId, out var file)
            ? new FileDownload(new MemoryStream(file.Content), file.Name, "application/octet-stream", file.Content.Length, $"{file.Version}")
            : null);

    public Task<bool> DeleteFileAsync(string fileId, CancellationToken cancellationToken) =>
        Task.FromResult(_files.Remove(fileId));

    public Task<bool> DeleteFolderIfEmptyAsync(string folderId, CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
