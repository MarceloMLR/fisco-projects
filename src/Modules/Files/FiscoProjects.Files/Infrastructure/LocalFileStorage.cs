using System.Globalization;
using FiscoProjects.Files.Contracts;

namespace FiscoProjects.Files.Infrastructure;

/// <summary>
/// Files in a folder of the disk, for development and tests (ADR-0004, ADR-0079). It behaves as SharePoint does for
/// the modules: folders, versions of a file and ids kept by whoever uses them, which do not change when a folder is
/// renamed (<see cref="LocalFileIds"/>, ADR-0121). The ids given before were the paths from the root, with "/", and
/// still work. Previous versions, temporary files and the ids live in the internal folder, never listed.
/// </summary>
internal sealed class LocalFileStorage : IFileStorage
{
    private const string VersionsFolder = "versions";
    private const string TemporaryFolder = "tmp";
    private const string IdsFile = "ids.json";

    private static readonly StringComparer Alphabetical = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true);

    // One writer at a time: versions and "keep both" read and change several files in a row.
    private readonly SemaphoreSlim _writing = new(1, 1);
    private readonly string _root;
    private readonly LocalFileIds _ids;

    public LocalFileStorage(string rootPath)
    {
        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
        Directory.CreateDirectory(_root);
        _ids = new LocalFileIds(Path.Combine(_root, FileNames.InternalFolder, IdsFile));
    }

    public Task<StoredFolder> EnsureFolderAsync(string? parentFolderId, string path, CancellationToken cancellationToken)
    {
        var parent = ExistingFolder(parentFolderId);
        var segments = FileNames.Segments(path);
        var relative = string.Join('/', [.. Split(parent), .. segments]);
        var existed = Directory.Exists(FullPath(relative));
        Directory.CreateDirectory(FullPath(relative));
        var id = existed ? _ids.IdOf(relative) : _ids.Created(relative);
        return Task.FromResult(new StoredFolder(id, segments[^1]));
    }

    public async Task<StoredFolder?> RenameFolderAsync(string folderId, string name, CancellationToken cancellationToken)
    {
        var relative = string.IsNullOrEmpty(folderId)
            ? throw new ArgumentException("The root of the storage is never renamed.", nameof(folderId))
            : ExistingFolder(folderId);
        var newName = FileNames.Normalize(name);
        var separator = relative.LastIndexOf('/');
        var parent = separator < 0 ? string.Empty : relative[..separator];
        var target = Join(parent, newName);
        await _writing.WaitAsync(cancellationToken);
        try
        {
            if (target == relative)
            {
                return new StoredFolder(folderId, newName);
            }

            // Another item with the name beside it, in any case, as in SharePoint; the folder itself is not another.
            var current = FullPath(relative);
            if (new DirectoryInfo(FullPath(parent)).EnumerateFileSystemInfos().Any(entry =>
                string.Equals(entry.Name, newName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(entry.FullName, current, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            var inside = Directory.EnumerateFileSystemEntries(FullPath(relative), "*", SearchOption.AllDirectories)
                .Select(entry => Join(relative, Path.GetRelativePath(FullPath(relative), entry).Replace('\\', '/')))
                .ToList();
            Move(FullPath(relative), FullPath(target));
            if (Directory.Exists(VersionsPath(relative)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(VersionsPath(target))!);
                Move(VersionsPath(relative), VersionsPath(target));
            }

            _ids.Moved(relative, target, inside);
            return new StoredFolder(folderId, newName);
        }
        finally
        {
            _writing.Release();
        }
    }

    public Task<IReadOnlyList<StoredFolder>> ListFoldersAsync(string? parentFolderId, CancellationToken cancellationToken)
    {
        var parent = ExistingFolder(parentFolderId);
        IReadOnlyList<StoredFolder> folders =
        [
            .. new DirectoryInfo(FullPath(parent)).EnumerateDirectories()
                .Where(folder => parent.Length > 0 || !string.Equals(folder.Name, FileNames.InternalFolder, StringComparison.OrdinalIgnoreCase))
                .Select(folder => new StoredFolder(_ids.IdOf(Join(parent, folder.Name)), folder.Name))
                .OrderBy(folder => folder.Name, Alphabetical),
        ];
        return Task.FromResult(folders);
    }

    public async Task<StoredFile> UploadAsync(
        string folderId, string fileName, Stream content, SameNameAction sameName, CancellationToken cancellationToken)
    {
        var folder = ExistingFolder(folderId);
        var name = FileNames.Normalize(fileName);

        // The content goes to a temporary file first, so an interrupted upload never leaves half a file in the folder.
        var temporary = Path.Combine(InternalPath(TemporaryFolder), $"{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.GetDirectoryName(temporary)!);
        long size;
        try
        {
            await using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await content.CopyToAsync(file, cancellationToken);
                size = file.Length;
            }

            await _writing.WaitAsync(cancellationToken);
            try
            {
                return sameName == SameNameAction.NewVersion
                    ? StoreAsNewVersion(temporary, folder, name, size)
                    : StoreBeside(temporary, folder, name, size);
            }
            finally
            {
                _writing.Release();
            }
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    public Task<FileDownload?> OpenAsync(string fileId, string? versionId, CancellationToken cancellationToken)
    {
        var relative = PathOf(fileId);
        var current = FullPath(relative);
        if (relative.Length == 0 || !File.Exists(current))
        {
            return Task.FromResult<FileDownload?>(null);
        }

        var currentVersion = CurrentVersion(relative);
        var version = versionId is null ? currentVersion : VersionNumber(versionId);
        var path = version == currentVersion
            ? current
            : Path.Combine(VersionsPath(relative), version.ToString(CultureInfo.InvariantCulture));
        if (version < 1 || version > currentVersion || !File.Exists(path))
        {
            return Task.FromResult<FileDownload?>(null);
        }

        var name = Path.GetFileName(current);
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult<FileDownload?>(new FileDownload(
            stream, name, ContentTypes.Of(name), stream.Length, version.ToString(CultureInfo.InvariantCulture)));
    }

    public async Task<bool> DeleteFileAsync(string fileId, CancellationToken cancellationToken)
    {
        var relative = PathOf(fileId);
        var path = FullPath(relative);
        await _writing.WaitAsync(cancellationToken);
        try
        {
            if (relative.Length == 0 || !File.Exists(path))
            {
                return false;
            }

            File.Delete(path);
            if (Directory.Exists(VersionsPath(relative)))
            {
                Directory.Delete(VersionsPath(relative), recursive: true);
            }

            _ids.Removed(relative);
            return true;
        }
        finally
        {
            _writing.Release();
        }
    }

    public async Task<bool> DeleteFolderIfEmptyAsync(string folderId, CancellationToken cancellationToken)
    {
        var relative = PathOf(folderId);
        var path = FullPath(relative);
        await _writing.WaitAsync(cancellationToken);
        try
        {
            // The root is never removed, and a folder with anything in it stays (ADR-0041).
            if (relative.Length == 0 || !Directory.Exists(path) || Directory.EnumerateFileSystemEntries(path).Any())
            {
                return false;
            }

            Directory.Delete(path);
            _ids.Removed(relative);
            return true;
        }
        finally
        {
            _writing.Release();
        }
    }

    public Task<string?> FolderOfAsync(string itemId, CancellationToken cancellationToken)
    {
        var relative = PathOf(itemId);
        var path = FullPath(relative);
        if (relative.Length == 0 || (!File.Exists(path) && !Directory.Exists(path)))
        {
            return Task.FromResult<string?>(null);
        }

        var separator = relative.LastIndexOf('/');
        return Task.FromResult<string?>(separator < 0 ? null : _ids.IdOf(relative[..separator]));
    }

    private StoredFile StoreAsNewVersion(string temporary, string folder, string name, long size)
    {
        var relative = Join(folder, name);
        var target = FullPath(relative);
        var version = 1;
        var existed = File.Exists(target);
        if (existed)
        {
            var previous = CurrentVersion(relative);
            Directory.CreateDirectory(VersionsPath(relative));
            File.Move(target, Path.Combine(VersionsPath(relative), previous.ToString(CultureInfo.InvariantCulture)));
            version = previous + 1;
        }

        File.Move(temporary, target);
        var id = existed ? _ids.IdOf(relative) : _ids.Created(relative);
        return new StoredFile(id, name, size, version.ToString(CultureInfo.InvariantCulture));
    }

    private StoredFile StoreBeside(string temporary, string folder, string name, long size)
    {
        for (var number = 0; ; number++)
        {
            var candidate = number == 0 ? name : FileNames.Numbered(name, number);
            var relative = Join(folder, candidate);
            var target = FullPath(relative);
            if (File.Exists(target) || Directory.Exists(target))
            {
                continue;
            }

            File.Move(temporary, target);
            return new StoredFile(_ids.Created(relative), candidate, size, "1");
        }
    }

    /// <summary>The number of a version id, or 0 when it is not one.</summary>
    private static int VersionNumber(string versionId) =>
        int.TryParse(versionId, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : 0;

    /// <summary>The current version of a file: one more than the previous versions kept.</summary>
    private int CurrentVersion(string relative) =>
        Directory.Exists(VersionsPath(relative)) ? Directory.EnumerateFiles(VersionsPath(relative)).Count() + 1 : 1;

    private string ExistingFolder(string? folderId)
    {
        var relative = PathOf(folderId);
        return Directory.Exists(FullPath(relative))
            ? relative
            : throw new StorageFolderNotFoundException(folderId ?? string.Empty);
    }

    /// <summary>The path of an id: the one written down, or the id itself, as the ids given before were.</summary>
    private string PathOf(string? id) =>
        string.IsNullOrEmpty(id) ? string.Empty : _ids.PathOf(id) ?? Relative(id);

    /// <summary>Moves a folder, through another name when only the upper and lower case change.</summary>
    private static void Move(string from, string to)
    {
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            var between = $"{from}.{Guid.NewGuid():N}";
            Directory.Move(from, between);
            from = between;
        }

        Directory.Move(from, to);
    }

    /// <summary>
    /// The path of an id given before the ids were written down, checked: no "." or "..", no rooted path, nothing in
    /// the internal folder and nothing that resolves outside the root. Anything else is refused, never adjusted.
    /// </summary>
    private string Relative(string id)
    {
        var parts = id.Split('/');
        if (Path.IsPathRooted(id) || id.Contains('\\', StringComparison.Ordinal)
            || parts.Any(part => part.Length == 0 || part is "." or "..")
            || string.Equals(parts[0], FileNames.InternalFolder, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"'{id}' is not an id of this file storage.", nameof(id));
        }

        var full = Path.GetFullPath(Path.Combine(_root, Path.Combine(parts)));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException($"'{id}' is not an id of this file storage.", nameof(id));
        }

        return id;
    }

    private string FullPath(string relative) =>
        relative.Length == 0 ? _root : Path.Combine(_root, Path.Combine(relative.Split('/')));

    private string InternalPath(string folder) => Path.Combine(_root, FileNames.InternalFolder, folder);

    private string VersionsPath(string relative) => Path.Combine(InternalPath(VersionsFolder), Path.Combine(relative.Split('/')));

    private static string[] Split(string relative) => relative.Length == 0 ? [] : relative.Split('/');

    private static string Join(string folder, string name) => folder.Length == 0 ? name : $"{folder}/{name}";
}
