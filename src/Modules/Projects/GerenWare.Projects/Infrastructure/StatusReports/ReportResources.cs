using System.Globalization;
using System.Text.Json;

namespace GerenWare.Projects.Infrastructure.StatusReports;

/// <summary>
/// The texts of the status report, by key, from the translation file of the report (ADR-0013): the labels of the
/// default template and the words of the values. Nothing of a company is in them (ADR-0003).
/// </summary>
internal sealed class ReportTexts
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private readonly JsonElement _texts;

    private ReportTexts(JsonElement texts)
    {
        _texts = texts;
    }

    public static ReportTexts Default { get; } = new(ReportResource.Load("GerenWare.Projects.StatusReports.pt-BR.json"));

    /// <summary>The text of a key such as <c>labels.progress</c>.</summary>
    public string this[string key]
    {
        get
        {
            var element = _texts;
            foreach (var part in key.Split('.'))
            {
                if (!element.TryGetProperty(part, out element))
                {
                    throw new KeyNotFoundException($"The status report has no text {key}.");
                }
            }

            return element.GetString() ?? throw new KeyNotFoundException($"The text {key} is not a text.");
        }
    }

    /// <summary>The text of a key with its places filled, as <c>{0}</c> and <c>{1}</c>.</summary>
    public string Format(string key, params object[] values) => string.Format(PtBr, this[key], values);

    /// <summary>The text of a key with one or many, as "1 dia útil" and "3 dias úteis".</summary>
    public string Count(string key, int count) =>
        count == 1 ? this[$"{key}One"] : Format($"{key}Many", count.ToString(PtBr));

    /// <summary>A day as the screens show it, 13/10/2026.</summary>
    public static string Day(DateOnly day) => day.ToString("dd/MM/yyyy", PtBr);

    /// <summary>Hours as the screens show them, with up to two decimals: 1.234,5.</summary>
    public static string Hours(decimal hours) => hours.ToString("#,##0.##", PtBr);

    public static string Number(int value) => value.ToString(PtBr);
}

/// <summary>
/// The light colors and the corners of the design system (ADR-0062 to ADR-0067, ADR-0159), from the same tokens file the
/// screens use, for the default template.
/// </summary>
internal sealed class ReportTokens
{
    private readonly Dictionary<string, string> _values;

    private ReportTokens(Dictionary<string, string> values)
    {
        _values = values;
    }

    public static ReportTokens Default { get; } = Load();

    /// <summary>A color as <c>RRGGBB</c>, as the PowerPoint file keeps it.</summary>
    public string Color(string name) =>
        _values.TryGetValue(name, out var value) && value.StartsWith('#')
            ? value[1..].ToUpperInvariant()
            : throw new KeyNotFoundException($"The design system has no color {name}.");

    /// <summary>A corner, in pixels.</summary>
    public int Radius(string name) =>
        _values.TryGetValue(name, out var value)
            ? int.Parse(value.Replace("px", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture)
            : throw new KeyNotFoundException($"The design system has no corner {name}.");

    private static ReportTokens Load()
    {
        var tokens = ReportResource.Load("GerenWare.Projects.StatusReports.tokens.json");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var token in tokens.GetProperty("color").GetProperty("tokens").EnumerateArray())
        {
            var value = token.GetProperty("value");
            values[token.GetProperty("name").GetString()!] = value.ValueKind == JsonValueKind.Object
                ? value.GetProperty("light").GetString()!
                : value.GetString()!;
        }

        foreach (var token in tokens.GetProperty("radius").GetProperty("tokens").EnumerateArray())
        {
            values[token.GetProperty("name").GetString()!] = token.GetProperty("value").GetString()!;
        }

        return new ReportTokens(values);
    }
}

internal static class ReportResource
{
    public static JsonElement Load(string name)
    {
        using var stream = typeof(ReportResource).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"The resource {name} is missing.");
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.Clone();
    }
}
