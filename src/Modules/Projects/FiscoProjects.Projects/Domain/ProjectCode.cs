using System.Text.RegularExpressions;

namespace FiscoProjects.Projects.Domain;

/// <summary>
/// The short code of a project, such as ERP-FIN (ADR-0016): from 2 to 15 characters, letters, digits and hyphens,
/// starting with a letter, without a hyphen at the end or two in a row. It is kept in capitals.
/// </summary>
public static partial class ProjectCode
{
    public const int MinLength = 2;
    public const int MaxLength = 15;

    /// <summary>The code in capitals, without the spaces at the ends; null when it does not follow the rule.</summary>
    public static string? Normalize(string? text)
    {
        var code = text?.Trim().ToUpperInvariant();
        return code is { Length: >= MinLength and <= MaxLength } && Rule().IsMatch(code) ? code : null;
    }

    [GeneratedRegex("^[A-Z][A-Z0-9]*(-[A-Z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex Rule();
}
