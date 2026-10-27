using System.Text.Json;

namespace FiscoProjects.Files.Infrastructure;

/// <summary>
/// The ids of the local storage, so they do not change when a folder is renamed, as in SharePoint (ADR-0121): a file
/// in the internal folder maps each id to the path of the item from the root. Items created by the storage get a new
/// id; an item that was there before, whose id was its path, keeps the path as its id, and is written down before a
/// folder above it is renamed, so the modules that kept it still find it. Everything is guarded by one lock, and the
/// file is written whole, through a temporary file, after each change.
/// </summary>
internal sealed class LocalFileIds
{
    private readonly Lock _gate = new();
    private readonly string _file;
    private readonly Dictionary<string, string> _paths = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _ids = new(StringComparer.OrdinalIgnoreCase);

    public LocalFileIds(string file)
    {
        _file = file;
        if (File.Exists(file))
        {
            var kept = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file)) ?? [];
            foreach (var (id, path) in kept)
            {
                _paths[id] = path;
                _ids[path] = id;
            }
        }
    }

    /// <summary>The path of an id written down, or null for an id that is not, whose path is the id itself.</summary>
    public string? PathOf(string id)
    {
        lock (_gate)
        {
            return _paths.GetValueOrDefault(id);
        }
    }

    /// <summary>The id of an item that already exists: the one written down, or its path, unless another uses it.</summary>
    public string IdOf(string path)
    {
        lock (_gate)
        {
            if (_ids.TryGetValue(path, out var id))
            {
                return id;
            }

            return _paths.ContainsKey(path) ? Add(path) : path;
        }
    }

    /// <summary>A new id for an item the storage just created.</summary>
    public string Created(string path)
    {
        lock (_gate)
        {
            return _ids.TryGetValue(path, out var id) ? id : Add(path);
        }
    }

    /// <summary>
    /// Moves the ids of a folder and of everything in it to the new path, writing down before the ones that were known
    /// by their path, so they keep the id they had.
    /// </summary>
    public void Moved(string from, string to, IEnumerable<string> pathsInside)
    {
        lock (_gate)
        {
            foreach (var path in pathsInside.Prepend(from))
            {
                if (!_ids.ContainsKey(path))
                {
                    // Known by its path, unless another item already took that id.
                    var id = _paths.ContainsKey(path) ? Guid.CreateVersion7().ToString("N") : path;
                    _paths[id] = path;
                    _ids[path] = id;
                }
            }

            foreach (var (id, path) in _paths.ToList())
            {
                if (Inside(path, from) is { } rest)
                {
                    _ids.Remove(path);
                    _paths[id] = to + rest;
                    _ids[to + rest] = id;
                }
            }

            Save();
        }
    }

    /// <summary>Forgets the id of an item removed.</summary>
    public void Removed(string path)
    {
        lock (_gate)
        {
            if (_ids.Remove(path, out var id))
            {
                _paths.Remove(id);
                Save();
            }
        }
    }

    /// <summary>The rest of a path after the folder ("" for the folder itself), or null when it is not inside it.</summary>
    private static string? Inside(string path, string folder) =>
        path.Equals(folder, StringComparison.OrdinalIgnoreCase) ? string.Empty
        : path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase) ? path[folder.Length..]
        : null;

    private string Add(string path)
    {
        var id = Guid.CreateVersion7().ToString("N");
        _paths[id] = path;
        _ids[path] = id;
        Save();
        return id;
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
        var temporary = $"{_file}.{Guid.NewGuid():N}";
        File.WriteAllText(temporary, JsonSerializer.Serialize(_paths));
        File.Move(temporary, _file, overwrite: true);
    }
}
