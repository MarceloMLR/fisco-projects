namespace FiscoProjects.Files.Contracts;

/// <summary>Why a file someone sends is refused: none, empty, larger than the use takes, or a program or a script.</summary>
public enum FileRefusal
{
    None = 0,
    Empty = 1,
    TooLarge = 2,
    Dangerous = 3,
}

/// <summary>
/// What every use of files checks the same way (ADR-0026, ADR-0031, ADR-0079): never a program or a script, told by the
/// extension and by the start of the file, and the type the browser gets. The size each use takes is its own rule.
/// </summary>
public static class FileTypes
{
    /// <summary>How many bytes of the start of a file <see cref="Check"/> looks at.</summary>
    public const int StartLength = 8;

    private const string Bytes = "application/octet-stream";

    private static readonly HashSet<string> Blocked = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".msi", ".msp", ".bat", ".cmd", ".com", ".scr", ".pif", ".cpl", ".ps1", ".psm1", ".vbs", ".vbe",
        ".js", ".jse", ".wsf", ".wsh", ".hta", ".jar", ".sh", ".bash", ".app", ".lnk", ".reg", ".msc", ".gadget", ".apk",
    };

    private static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".pdf"] = "application/pdf",
        [".xml"] = "application/xml",
        [".txt"] = "text/plain",
        [".csv"] = "text/csv",
        [".zip"] = "application/zip",
        [".doc"] = "application/msword",
        [".xls"] = "application/vnd.ms-excel",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
    };

    /// <summary>Why the file is refused, from its name, size and first bytes; None when it is taken.</summary>
    public static FileRefusal Check(string name, long size, ReadOnlySpan<byte> start, long maxBytes)
    {
        if (size <= 0)
        {
            return FileRefusal.Empty;
        }

        if (size > maxBytes)
        {
            return FileRefusal.TooLarge;
        }

        var program = start.StartsWith("MZ"u8) || start.StartsWith("\u007fELF"u8) || start.StartsWith("#!"u8);
        return Blocked.Contains(Path.GetExtension(name)) || program ? FileRefusal.Dangerous : FileRefusal.None;
    }

    /// <summary>
    /// Why the file is refused, reading its first bytes and going back to the start of the content, which must be
    /// seekable; None when it is taken.
    /// </summary>
    public static async Task<FileRefusal> CheckAsync(
        string name, long size, Stream content, long maxBytes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var start = new byte[StartLength];
        var read = await content.ReadAtLeastAsync(start, start.Length, throwOnEndOfStream: false, cancellationToken);
        content.Position = 0;
        return Check(name, size, start.AsSpan(0, read), maxBytes);
    }

    /// <summary>The type of the file by its extension, for the browser; a download of bytes when unknown.</summary>
    public static string ContentTypeOf(string name) =>
        Types.TryGetValue(Path.GetExtension(name), out var type) ? type : Bytes;

    /// <summary>Whether the browser shows the file in a tab as it is: images and PDF.</summary>
    public static bool OpensInBrowser(string contentType) =>
        contentType.StartsWith("image/", StringComparison.Ordinal) || contentType == "application/pdf";

    /// <summary>Whether the file is text the browser shows as plain text: text and CSV.</summary>
    public static bool IsText(string contentType) => contentType is "text/plain" or "text/csv";
}
