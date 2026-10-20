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
/// address of the item, also written out.
/// </summary>
internal sealed partial class EmailRenderer(string? siteUrl) : IEmailRenderer
{
    private static readonly Lazy<JsonElement> Texts = new(() => Load("FiscoProjects.Notifications.Emails.pt-BR.json"));
    private static readonly Lazy<IReadOnlyDictionary<string, string>> Tokens = new(LoadTokens);

    public RenderedEmail Render(EmailContent content, string installationName)
    {
        ArgumentNullException.ThrowIfNull(content);
        var values = new Dictionary<string, string>(content.Values) { ["installation"] = installationName };
        var subject = Text(content.SubjectKey, values);
        var paragraphs = content.ParagraphKeys.Select(key => Text(key, values)).ToList();
        var url = content.Path is { } path && !string.IsNullOrWhiteSpace(siteUrl)
            ? siteUrl.TrimEnd('/') + path
            : null;
        var action = content.ActionKey is { } actionKey && url is not null ? Text(actionKey, values) : null;
        var footer = Text("layout.footer", values);
        var hint = Text("layout.linkHint", values);
        return new RenderedEmail(subject, Html(subject, installationName, paragraphs, action, url, hint, footer), Plain(paragraphs, action, url, footer));
    }

    /// <summary>The text of a key, with the values in place of {{name}}.</summary>
    internal static string Text(string key, IReadOnlyDictionary<string, string> values)
    {
        var node = Texts.Value;
        foreach (var part in key.Split('.'))
        {
            if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty(part, out node))
            {
                throw new KeyNotFoundException($"The text {key} is not in the texts of the e-mails.");
            }
        }

        return Placeholder().Replace(
            node.GetString() ?? string.Empty,
            match => values.TryGetValue(match.Groups[1].Value, out var value) ? value : match.Value);
    }

    private static string Html(
        string subject, string installation, IReadOnlyList<string> paragraphs, string? action, string? url, string hint, string footer)
    {
        var color = Tokens.Value;
        var html = new StringBuilder();
        html.Append(CultureInfo.InvariantCulture, $"""
            <!doctype html>
            <html lang="pt-BR">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{Encode(subject)}</title></head>
            <body style="margin:0;padding:0;background:{color["bg-subtle"]};font-family:{color["font-sans"]};">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:{color["bg-subtle"]};padding:{color["space-6"]} {color["space-3"]};">
            <tr><td align="center">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background:{color["bg-surface"]};border:1px solid {color["border"]};border-radius:{color["radius-lg"]};">
            <tr><td style="padding:{color["space-4"]} {color["space-6"]};border-bottom:1px solid {color["border"]};font-size:14px;line-height:20px;font-weight:500;color:{color["text"]};">{Encode(installation)}</td></tr>
            <tr><td style="padding:{color["space-6"]};font-size:15px;line-height:24px;color:{color["text"]};">
            """);
        foreach (var paragraph in paragraphs)
        {
            html.Append(CultureInfo.InvariantCulture, $"""<p style="margin:0 0 {color["space-4"]};">{Encode(paragraph)}</p>""");
        }

        if (action is not null && url is not null)
        {
            html.Append(CultureInfo.InvariantCulture, $"""
                <table role="presentation" cellpadding="0" cellspacing="0" style="margin:{color["space-2"]} 0 {color["space-4"]};"><tr><td style="border-radius:{color["radius-md"]};background:{color["action"]};"><a href="{Encode(url)}" style="display:inline-block;padding:10px {color["space-4"]};font-size:14px;line-height:20px;font-weight:500;color:{color["on-action"]};text-decoration:none;">{Encode(action)}</a></td></tr></table>
                <p style="margin:0;font-size:12px;line-height:16px;color:{color["text-secondary"]};">{Encode(hint)} <a href="{Encode(url)}" style="color:{color["action-text"]};">{Encode(url)}</a></p>
                """);
        }

        html.Append(CultureInfo.InvariantCulture, $"""
            </td></tr>
            <tr><td style="padding:{color["space-4"]} {color["space-6"]};border-top:1px solid {color["border"]};font-size:12px;line-height:16px;color:{color["text-secondary"]};">{Encode(footer)}</td></tr>
            </table>
            </td></tr>
            </table>
            </body>
            </html>
            """);
        return html.ToString();
    }

    /// <summary>The text safe in HTML; the accents stay as they are, as the e-mail is UTF-8.</summary>
    internal static string Encode(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);

    private static string Plain(IReadOnlyList<string> paragraphs, string? action, string? url, string footer)
    {
        var text = new StringBuilder();
        foreach (var paragraph in paragraphs)
        {
            text.Append(paragraph).Append("\n\n");
        }

        if (action is not null && url is not null)
        {
            text.Append(CultureInfo.InvariantCulture, $"{action}: {url}\n\n");
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
}
