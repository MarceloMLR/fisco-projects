namespace FiscoProjects.Projects.Domain;

/// <summary>
/// The CNPJ of a company (ADR-0015): 12 letters or digits and 2 check digits. Since July 2026 the first 12 may hold
/// letters (IN RFB nº 2.229/2024); the check digits count each character as its ASCII code minus 48, which keeps the
/// rule of the CNPJ made only of digits.
/// </summary>
public static class Cnpj
{
    public const int Length = 14;

    private static readonly int[] FirstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] SecondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>The letters and digits of a text, in capitals: "12.abc.345/01de-35" becomes "12ABC34501DE35".</summary>
    public static string Clean(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return string.Concat(text.Where(char.IsAsciiLetterOrDigit)).ToUpperInvariant();
    }

    /// <summary>
    /// The CNPJ as it is kept: 14 characters without punctuation when the text is a CNPJ, null for an empty text (no
    /// CNPJ). False for anything else.
    /// </summary>
    public static bool TryNormalize(string? text, out string? cnpj)
    {
        cnpj = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var clean = Clean(text);
        if (!IsValid(clean))
        {
            return false;
        }

        cnpj = clean;
        return true;
    }

    /// <summary>Whether 14 characters without punctuation are a CNPJ: the format and the two check digits.</summary>
    public static bool IsValid(string clean)
    {
        ArgumentNullException.ThrowIfNull(clean);
        if (clean.Length != Length
            || !clean[..12].All(character => char.IsAsciiDigit(character) || char.IsAsciiLetterUpper(character))
            || !clean[12..].All(char.IsAsciiDigit)
            || clean.All(character => character == clean[0]))
        {
            return false;
        }

        return CheckDigit(clean[..12], FirstWeights) == clean[12] - '0'
            && CheckDigit(clean[..13], SecondWeights) == clean[13] - '0';
    }

    /// <summary>The CNPJ with its punctuation: 12.ABC.345/01DE-35.</summary>
    public static string Format(string cnpj)
    {
        ArgumentNullException.ThrowIfNull(cnpj);
        return cnpj.Length == Length
            ? $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..]}"
            : cnpj;
    }

    private static int CheckDigit(string characters, int[] weights)
    {
        var sum = 0;
        for (var index = 0; index < characters.Length; index++)
        {
            sum += (characters[index] - '0') * weights[index];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
