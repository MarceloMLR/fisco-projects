using System.Globalization;
using System.Text;

namespace FiscoProjects.SharedKernel;

/// <summary>A search in a list already in memory, as the database compares text: without telling case or accents apart.</summary>
public static class SearchText
{
    /// <summary>The text trimmed, without accents and in lowercase.</summary>
    public static string Simplify(string? text)
    {
        var decomposed = (text ?? string.Empty).Trim().Normalize(NormalizationForm.FormD);
        var simple = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                simple.Append(char.ToLowerInvariant(character));
            }
        }

        return simple.ToString();
    }

    /// <summary>Whether the text has the search in it, already simplified; an empty search finds everything.</summary>
    public static bool Finds(string? text, string simplifiedSearch) =>
        simplifiedSearch.Length == 0 || Simplify(text).Contains(simplifiedSearch, StringComparison.Ordinal);
}
