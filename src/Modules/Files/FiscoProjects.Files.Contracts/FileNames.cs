namespace FiscoProjects.Files.Contracts;

/// <summary>
/// Names of files and folders adjusted to the SharePoint rules (ADR-0030, ADR-0079), and paths that never leave the
/// root. Every implementation of the storage uses these rules, so a name is stored the same way everywhere, and a
/// module checks with them a path someone types, as the folder of the projects (ADR-0121).
/// </summary>
public static class FileNames
{
    /// <summary>Folder of the storage itself (versions, temporary files); never listed and never a user name.</summary>
    public const string InternalFolder = ".fiscoprojects";

    /// <summary>Longest name kept, extension included.</summary>
    public const int MaxLength = 200;

    /// <summary>Longest path someone may type for a folder, as the folder of the projects.</summary>
    public const int MaxPathLength = 400;

    /// <summary>Name of a file or folder whose name is empty after the adjustments.</summary>
    public const string Fallback = "file";

    private static readonly char[] NotAccepted = ['"', '*', ':', '<', '>', '?', '/', '\\', '|'];

    private static readonly HashSet<string> Reserved = new(
        ["CON", "PRN", "AUX", "NUL", .. Numbered("COM"), .. Numbered("LPT")], StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ReservedNames = new(
        [".lock", "desktop.ini", InternalFolder], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The name as it is stored: characters SharePoint does not accept become "_", no spaces at the ends nor dot at the
    /// end, reserved names get "_" in front and long names are cut, keeping the extension.
    /// </summary>
    public static string Normalize(string? name)
    {
        var text = new string([.. (name ?? string.Empty).Select(c => NotAccepted.Contains(c) || char.IsControl(c) ? '_' : c)]);
        text = TrimEnds(text).Replace("_vti_", "vti_", StringComparison.OrdinalIgnoreCase);
        if (text.Length == 0)
        {
            return Fallback;
        }

        var stem = text.Split('.')[0];
        if (Reserved.Contains(stem) || ReservedNames.Contains(text))
        {
            text = "_" + text;
        }

        return Shorten(text);
    }

    /// <summary>
    /// The folders of a path separated by "/", each one adjusted. Empty parts are ignored; "." and ".." are refused,
    /// since a path never goes up or out of the root.
    /// </summary>
    public static IReadOnlyList<string> Segments(string path)
    {
        var parts = (path ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Any(part => part is "." or ".."))
        {
            throw new ArgumentException($"The path '{path}' goes out of its folder.", nameof(path));
        }

        if (parts.Length == 0)
        {
            throw new ArgumentException("A path needs at least one folder.", nameof(path));
        }

        return [.. parts.Select(Normalize)];
    }

    /// <summary>
    /// True when the storage keeps the path as typed: folders separated by "/", none empty, "." or "..", and each one
    /// already within the SharePoint rules, up to <see cref="MaxPathLength"/> characters.
    /// </summary>
    public static bool IsKeptAsTyped(string path) =>
        !string.IsNullOrEmpty(path) && path.Length <= MaxPathLength
        && path.Split('/').All(part => part is not ("." or "..") && part.Length > 0 && Normalize(part) == part);

    /// <summary>The name of the n-th file kept beside another of the same name: "print (1).png".</summary>
    public static string Numbered(string name, int number)
    {
        var extension = Path.GetExtension(name);
        var stem = name[..^extension.Length];
        if (stem.Length == 0)
        {
            (stem, extension) = (name, string.Empty);
        }

        var suffix = $" ({number}){extension}";
        return Shorten(stem.Length + suffix.Length > MaxLength ? stem[..(MaxLength - suffix.Length)] + suffix : stem + suffix);
    }

    private static string Shorten(string name)
    {
        if (name.Length <= MaxLength)
        {
            return name;
        }

        var extension = Path.GetExtension(name);
        if (extension.Length >= MaxLength / 4)
        {
            extension = string.Empty;
        }

        return TrimEnds(name[..(MaxLength - extension.Length)]) + extension;
    }

    private static string TrimEnds(string text) => text.Trim().TrimEnd('.').TrimEnd();

    private static IEnumerable<string> Numbered(string prefix) => Enumerable.Range(0, 10).Select(n => $"{prefix}{n}");
}
