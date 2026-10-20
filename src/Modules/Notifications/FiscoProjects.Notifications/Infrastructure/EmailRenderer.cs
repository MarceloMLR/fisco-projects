using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FiscoProjects.Notifications.Application;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>
/// Puts the texts of an e-mail in the layout of the installation (ADR-0036, ADR-0037): the texts come from the
/// translation file of the e-mails (ADR-0013), and the colors, corners and spaces from the tokens of the design system,
/// in the light theme, as e-mail programs do not follow the theme of the person. Every e-mail with a button carries the
/// address of the item, also written out; every line of a summary links to its item.
/// </summary>
internal sealed partial class EmailRenderer(string? siteUrl) : IEmailRenderer
{
    private static readonly Lazy<JsonElement> Texts = new(() => Load("FiscoProjects.Notifications.Emails.pt-BR.json"));
    private static readonly Lazy<IReadOnlyDictionary<string, string>> Tokens = new(LoadTokens);
    private static readonly CultureInfo Brazil = CultureInfo.GetCultureInfo("pt-BR");

    public RenderedEmail Render(EmailContent content, string installationName)
    {
        ArgumentNullException.ThrowIfNull(content);
        var values = Shown(content.Values, installationName);
        var subject = Text(content.SubjectKey, values);
        var paragraphs = content.ParagraphKeys.Select(key => Text(key, values)).ToList();
        var url = UrlOf(content.Path);
        var action = content.ActionKey is { } actionKey && url is not null ? Text(actionKey, values) : null;
        var sections = content.Sections
            .Select(section => new Section(
                Text(section.TitleKey, values),
                [.. section.Lines.Select(line => new Line(Text(line.Key, Shown(line.Values, installationName)), UrlOf(line.Path)))]))
            .ToList();
        var note = content.NoteKey is { } noteKey ? Text(noteKey, values) : null;
        var footer = Text("layout.footer", values);
        var hint = Text("layout.linkHint", values);
        return new RenderedEmail(
            subject,
            Html(subject, installationName, paragraphs, sections, action, url, hint, note, footer),
            Plain(paragraphs, sections, action, url, note, footer));
    }

    /// <summary>The text of a key, with the values in place of {{name}}.</summary>
    internal static string Text(string key, IReadOnlyDictionary<string, string> values)
    {
        if (Find(key) is not { ValueKind: JsonValueKind.String } node)
        {
            throw new KeyNotFoundException($"The text {key} is not in the texts of the e-mails.");
        }

        return Placeholder().Replace(
            node.GetString() ?? string.Empty,
            match => values.TryGetValue(match.Groups[1].Value, out var value) ? value : match.Value);
    }

    /// <summary>The text safe in HTML; the accents stay as they are, as the e-mail is UTF-8.</summary>
    internal static string Encode(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);

    /// <summary>
    /// The values as the reader sees them: a code with a text of its own in the section "values" of the texts (a team,
    /// a status, a role) is translated, a day (2026-10-21) is written as 21/10/2026, and the installation is added.
    /// </summary>
    private static Dictionary<string, string> Shown(IReadOnlyDictionary<string, string> values, string installationName)
    {
        var shown = new Dictionary<string, string>(StringComparer.Ordinal) { ["installation"] = installationName };
        foreach (var (name, value) in values)
        {
            shown[name] = Find($"values.{name}.{value}") is { ValueKind: JsonValueKind.String } text
                ? text.GetString() ?? value
                : DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
                    ? day.ToString("dd/MM/yyyy", Brazil)
                    : value;
        }

        return shown;
    }

    private static JsonElement? Find(string key)
    {
        var node = Texts.Value;
        foreach (var part in key.Split('.'))
        {
            if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty(part, out node))
            {
                return null;
            }
        }

        return node;
    }

    private string? UrlOf(string? path) =>
        path is not null && !string.IsNullOrWhiteSpace(siteUrl) ? siteUrl.TrimEnd('/') + path : null;

    private static string Html(
        string subject,
        string installation,
        IReadOnlyList<string> paragraphs,
        IReadOnlyList<Section> sections,
        string? action,
        string? url,
        string hint,
        string? note,
        string footer)
    {
        var token = Tokens.Value;
        var html = new StringBuilder();
        html.Append(CultureInfo.InvariantCulture, $"""
            <!doctype html>
            <html lang="pt-BR">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{Encode(subject)}</title></head>
            <body style="margin:0;padding:0;background:{token["bg-subtle"]};font-family:{token["font-sans"]};">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:{token["bg-subtle"]};padding:{token["space-6"]} {token["space-3"]};">
            <tr><td align="center">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background:{token["bg-surface"]};border:1px solid {token["border"]};border-radius:{token["radius-lg"]};">
            <tr><td style="padding:{token["space-4"]} {token["space-6"]};border-bottom:1px solid {token["border"]};font-size:14px;line-height:20px;font-weight:500;color:{token["text"]};">{Encode(installation)}</td></tr>
            <tr><td style="padding:{token["space-6"]};font-size:15px;line-height:24px;color:{token["text"]};">
            """);
        foreach (var paragraph in paragraphs)
        {
            html.Append(CultureInfo.InvariantCulture, $"""<p style="margin:0 0 {token["space-4"]};">{Encode(paragraph)}</p>""");
        }

        foreach (var section in sections)
        {
            html.Append(CultureInfo.InvariantCulture, $"""<p style="margin:{token["space-6"]} 0 {token["space-2"]};font-size:14px;line-height:20px;font-weight:500;color:{token["text"]};">{Encode(section.Title)}</p>""");
            html.Append(CultureInfo.InvariantCulture, $"""<ul style="margin:0 0 {token["space-4"]};padding:0 0 0 {token["space-5"]};">""");
            foreach (var line in section.Lines)
            {
                var text = line.Url is null
                    ? Encode(line.Text)
                    : $"""<a href="{Encode(line.Url)}" style="color:{token["action-text"]};">{Encode(line.Text)}</a>""";
                html.Append(CultureInfo.InvariantCulture, $"""<li style="margin:0 0 {token["space-1"]};font-size:14px;line-height:20px;">{text}</li>""");
            }

            html.Append("</ul>");
        }

        if (action is not null && url is not null)
        {
            html.Append(CultureInfo.InvariantCulture, $"""
                <table role="presentation" cellpadding="0" cellspacing="0" style="margin:{token["space-2"]} 0 {token["space-4"]};"><tr><td style="border-radius:{token["radius-md"]};background:{token["action"]};"><a href="{Encode(url)}" style="display:inline-block;padding:10px {token["space-4"]};font-size:14px;line-height:20px;font-weight:500;color:{token["on-action"]};text-decoration:none;">{Encode(action)}</a></td></tr></table>
                <p style="margin:0;font-size:12px;line-height:16px;color:{token["text-secondary"]};">{Encode(hint)} <a href="{Encode(url)}" style="color:{token["action-text"]};">{Encode(url)}</a></p>
                """);
        }

        if (note is not null)
        {
            html.Append(CultureInfo.InvariantCulture, $"""<p style="margin:{token["space-4"]} 0 0;font-size:12px;line-height:16px;color:{token["text-secondary"]};">{Encode(note)}</p>""");
        }

        html.Append(CultureInfo.InvariantCulture, $"""
            </td></tr>
            <tr><td style="padding:{token["space-4"]} {token["space-6"]};border-top:1px solid {token["border"]};font-size:12px;line-height:16px;color:{token["text-secondary"]};">{Encode(footer)}</td></tr>
            </table>
            </td></tr>
            </table>
            </body>
            </html>
            """);
        return html.ToString();
    }

    private static string Plain(
        IReadOnlyList<string> paragraphs, IReadOnlyList<Section> sections, string? action, string? url, string? note, string footer)
    {
        var text = new StringBuilder();
        foreach (var paragraph in paragraphs)
        {
            text.Append(paragraph).Append("\n\n");
        }

        foreach (var section in sections)
        {
            text.Append(section.Title).Append('\n');
            foreach (var line in section.Lines)
            {
                text.Append("- ").Append(line.Text);
                if (line.Url is not null)
                {
                    text.Append(": ").Append(line.Url);
                }

                text.Append('\n');
            }

            text.Append('\n');
        }

        if (action is not null && url is not null)
        {
            text.Append(CultureInfo.InvariantCulture, $"{action}: {url}\n\n");
        }

        if (note is not null)
        {
            text.Append(note).Append("\n\n");
        }

        return text.Append("-- \n").Append(footer).Append('\n').ToString();
    }

    private static JsonElement Load(string name)
    {
        using var stream = typeof(EmailRenderer).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"The resource {name} is missing.");
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.Clone();
    }

    /// <summary>The colors of the light theme, the corners, the spaces and the font of the design system, by name.</summary>
    private static Dictionary<string, string> LoadTokens()
    {
        var tokens = Load("FiscoProjects.Notifications.Emails.tokens.json");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var token in tokens.GetProperty("color").GetProperty("tokens").EnumerateArray())
        {
            values[token.GetProperty("name").GetString()!] = token.GetProperty("value").GetProperty("light").GetString()!;
        }

        foreach (var group in new[] { "radius", "spacing" })
        {
            foreach (var token in tokens.GetProperty(group).GetProperty("tokens").EnumerateArray())
            {
                values[token.GetProperty("name").GetString()!] = token.GetProperty("value").GetString()!;
            }
        }

        values["font-sans"] = tokens.GetProperty("type").GetProperty("families").GetProperty("sans").GetString()!.Replace('"', '\'');
        return values;
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex Placeholder();

    private sealed record Section(string Title, IReadOnlyList<Line> Lines);

    private sealed record Line(string Text, string? Url);
}
