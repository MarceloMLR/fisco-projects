using System.Text;
using System.Xml;

namespace FiscoProjects.Administration.Application;

/// <summary>Why a logo was refused.</summary>
public enum LogoRefusal
{
    /// <summary>Neither a PNG nor an SVG.</summary>
    Format = 1,

    /// <summary>More than 1 MB.</summary>
    TooLarge = 2,

    /// <summary>An SVG that could run a script or read other files.</summary>
    Unsafe = 3,
}

/// <summary>A logo that may be kept: the format found in the content, with its extension and type.</summary>
public sealed record LogoFormat(string Extension, string ContentType)
{
    public static readonly LogoFormat Png = new("png", "image/png");

    public static readonly LogoFormat Svg = new("svg", "image/svg+xml");
}

/// <summary>The result of checking a logo: the format, or why it was refused.</summary>
public sealed record LogoCheck(LogoFormat? Format, LogoRefusal? Refusal)
{
    public static LogoCheck Accepted(LogoFormat format) => new(format, null);

    public static LogoCheck Refused(LogoRefusal refusal) => new(null, refusal);
}

/// <summary>
/// What a logo of the installation may be, as in the canvas: a PNG or an SVG of up to 1 MB. The format comes from the
/// content, never from the extension or the type the browser said. An SVG is refused when it has a DTD, a script,
/// foreign content, event attributes or a javascript: link; it is also served so that it never runs scripts.
/// </summary>
public static class LogoFile
{
    public const int MaxBytes = 1024 * 1024;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly string[] UnsafeElements = ["script", "foreignObject"];

    public static LogoCheck Check(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length > MaxBytes)
        {
            return LogoCheck.Refused(LogoRefusal.TooLarge);
        }

        if (content.AsSpan().StartsWith(PngSignature))
        {
            return LogoCheck.Accepted(LogoFormat.Png);
        }

        return CheckSvg(content);
    }

    private static LogoCheck CheckSvg(byte[] content)
    {
        // A DTD can define entities that read other files or grow without end; an SVG for an image never needs one.
        var text = Encoding.UTF8.GetString(content);
        if (text.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
            || text.Contains("<!ENTITY", StringComparison.OrdinalIgnoreCase))
        {
            return LogoCheck.Refused(LogoRefusal.Unsafe);
        }

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
        };

        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var reader = XmlReader.Create(stream, settings);
            var root = true;
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                if (root)
                {
                    if (!string.Equals(reader.LocalName, "svg", StringComparison.Ordinal))
                    {
                        return LogoCheck.Refused(LogoRefusal.Format);
                    }

                    root = false;
                }

                if (IsUnsafe(reader))
                {
                    return LogoCheck.Refused(LogoRefusal.Unsafe);
                }
            }

            return root ? LogoCheck.Refused(LogoRefusal.Format) : LogoCheck.Accepted(LogoFormat.Svg);
        }
        catch (XmlException)
        {
            return LogoCheck.Refused(LogoRefusal.Format);
        }
    }

    private static bool IsUnsafe(XmlReader element)
    {
        if (UnsafeElements.Any(name => string.Equals(element.LocalName, name, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        for (var moved = element.MoveToFirstAttribute(); moved; moved = element.MoveToNextAttribute())
        {
            if (element.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(element.LocalName, "href", StringComparison.OrdinalIgnoreCase)
                    && IsScriptLink(element.Value)))
            {
                return true;
            }
        }

        element.MoveToElement();
        return false;
    }

    // Browsers ignore spaces and control characters inside the scheme, so "java\tscript:" counts too.
    private static bool IsScriptLink(string value) =>
        new string([.. value.Where(c => !char.IsWhiteSpace(c) && !char.IsControl(c))])
            .StartsWith("javascript:", StringComparison.OrdinalIgnoreCase);
}
