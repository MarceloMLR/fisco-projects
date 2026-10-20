using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;

namespace FiscoProjects.Tickets.Domain;

/// <summary>
/// A text with basic formatting (ADR-0026): the document of the editor (Tiptap, ProseMirror) as JSON, with only
/// paragraphs, line breaks, lists, mentions of people, bold and links to http, https or mailto. Anything else is
/// refused, so the screen draws it without HTML. Keeps the plain text, for the search and the e-mails, and the people
/// mentioned.
/// </summary>
public sealed class RichText
{
    /// <summary>The most characters of plain text in one message.</summary>
    public const int MaxTextLength = 20_000;

    /// <summary>The most characters of JSON, with the room the structure takes.</summary>
    public const int MaxJsonLength = 200_000;

    private static readonly HashSet<string> Blocks = ["paragraph", "bulletList", "orderedList", "listItem"];

    // Accents and other letters as they are; what HTML would take apart is still escaped.
    private static readonly JsonSerializerOptions Writing = new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };
    private static readonly HashSet<string> Schemes = ["http", "https", "mailto"];

    private RichText(string json, string text, IReadOnlyList<Guid> mentions)
    {
        Json = json;
        Text = text;
        Mentions = mentions;
    }

    /// <summary>The document as JSON, written again by the server: only what is allowed.</summary>
    public string Json { get; }

    /// <summary>The plain text, the paragraphs and the items of the lists in lines of their own.</summary>
    public string Text { get; }

    /// <summary>The people mentioned, each once, in the order they appear.</summary>
    public IReadOnlyList<Guid> Mentions { get; }

    /// <summary>A plain text as a document: one paragraph per line, for the descriptions written before the editor.</summary>
    public static RichText OfPlain(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var content = new JsonArray();
        foreach (var line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var paragraph = new JsonObject { ["type"] = "paragraph" };
            if (line.Length > 0)
            {
                paragraph["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = line });
            }

            content.Add(paragraph);
        }

        return new RichText(new JsonObject { ["type"] = "doc", ["content"] = content }.ToJsonString(Writing), text.Trim(), []);
    }

    /// <summary>The document checked and written again; null when it is not valid JSON, has anything not allowed or is empty.</summary>
    public static RichText? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > MaxJsonLength)
        {
            return null;
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }

        if (root is not JsonObject { } document || TypeOf(document) != "doc" || document["content"] is not JsonArray blocks || blocks.Count == 0)
        {
            return null;
        }

        var text = new StringBuilder();
        var mentions = new List<Guid>();
        var clean = new JsonArray();
        foreach (var block in blocks)
        {
            if (TypeOf(block) == "listItem" || Block(block, text, mentions, depth: 0) is not { } copy)
            {
                return null;
            }

            clean.Add(copy);
        }

        var plain = text.ToString().Trim();
        if ((plain.Length == 0 && mentions.Count == 0) || plain.Length > MaxTextLength)
        {
            return null;
        }

        return new RichText(
            new JsonObject { ["type"] = "doc", ["content"] = clean }.ToJsonString(Writing), plain, [.. mentions.Distinct()]);
    }

    private static string? TypeOf(JsonNode? node) =>
        node is JsonObject item && item["type"] is JsonValue type && type.TryGetValue<string>(out var name) ? name : null;

    private static JsonObject? Block(JsonNode? node, StringBuilder text, List<Guid> mentions, int depth)
    {
        var type = TypeOf(node);
        if (type is null || !Blocks.Contains(type) || depth > 4 || node is not JsonObject item)
        {
            return null;
        }

        var copy = new JsonObject { ["type"] = type };
        if (type == "orderedList" && item["attrs"] is JsonObject attrs && attrs["start"] is JsonValue start
            && start.TryGetValue<int>(out var first) && first is > 0 and < 10_000)
        {
            copy["attrs"] = new JsonObject { ["start"] = first };
        }

        if (item["content"] is null)
        {
            if (type == "paragraph")
            {
                text.Append('\n');
                return copy;
            }

            return null;
        }

        if (item["content"] is not JsonArray children)
        {
            return null;
        }

        var content = new JsonArray();
        foreach (var child in children)
        {
            var copied = type == "paragraph"
                ? Inline(child, text, mentions)
                : type == "listItem"
                    ? Block(child, text, mentions, depth + 1)
                    : TypeOf(child) == "listItem" ? Block(child, text, mentions, depth + 1) : null;
            if (copied is null)
            {
                return null;
            }

            content.Add(copied);
        }

        if (type == "paragraph")
        {
            text.Append('\n');
        }

        copy["content"] = content;
        return copy;
    }

    private static JsonObject? Inline(JsonNode? node, StringBuilder text, List<Guid> mentions)
    {
        switch (TypeOf(node))
        {
            case "hardBreak":
                text.Append('\n');
                return new JsonObject { ["type"] = "hardBreak" };
            case "mention" when node!["attrs"] is JsonObject attrs
                && attrs["id"] is JsonValue id && id.TryGetValue<string>(out var value) && Guid.TryParse(value, out var person)
                && attrs["label"] is JsonValue label && label.TryGetValue<string>(out var name)
                && name.Length is > 0 and <= 150:
                mentions.Add(person);
                text.Append('@').Append(name);
                return new JsonObject
                {
                    ["type"] = "mention",
                    ["attrs"] = new JsonObject { ["id"] = person.ToString(), ["label"] = name },
                };
            case "text" when node!["text"] is JsonValue content && content.TryGetValue<string>(out var words) && words.Length > 0:
                var copy = new JsonObject { ["type"] = "text", ["text"] = words };
                if (node["marks"] is JsonArray marks)
                {
                    if (Marks(marks) is not { } clean)
                    {
                        return null;
                    }

                    if (clean.Count > 0)
                    {
                        copy["marks"] = clean;
                    }
                }

                text.Append(words);
                return copy;
            default:
                return null;
        }
    }

    private static JsonArray? Marks(JsonArray marks)
    {
        var clean = new JsonArray();
        foreach (var mark in marks)
        {
            switch (TypeOf(mark))
            {
                case "bold":
                    clean.Add(new JsonObject { ["type"] = "bold" });
                    break;
                case "link" when mark!["attrs"] is JsonObject attrs && attrs["href"] is JsonValue href
                    && href.TryGetValue<string>(out var address) && IsSafeLink(address):
                    clean.Add(new JsonObject { ["type"] = "link", ["attrs"] = new JsonObject { ["href"] = address } });
                    break;
                default:
                    return null;
            }
        }

        return clean;
    }

    /// <summary>An address of a link the screen may open: http, https or mailto, up to 2,000 characters.</summary>
    public static bool IsSafeLink(string address) =>
        address.Length <= 2_000
        && Uri.TryCreate(address, UriKind.Absolute, out var uri)
        && Schemes.Contains(uri.Scheme);
}
