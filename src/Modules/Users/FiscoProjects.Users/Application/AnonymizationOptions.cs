using System.Globalization;

namespace FiscoProjects.Users.Application;

/// <summary>
/// The identifier that takes the place of the name of an anonymized person, with the number of the anonymization in
/// <c>{0}</c> (ADR-0058, ADR-0110), such as "Pessoa removida nº {0}"; configuration of the installation, in its
/// language.
/// </summary>
public sealed class AnonymizationOptions
{
    public const string Section = "Users";

    public string AnonymousName { get; set; } = string.Empty;

    public bool IsValid
    {
        get
        {
            if (string.IsNullOrWhiteSpace(AnonymousName) || !AnonymousName.Contains("{0}", StringComparison.Ordinal)
                || AnonymousName.Length > 120)
            {
                return false;
            }

            try
            {
                return IdentifierFor(int.MaxValue).Length > 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }

    public string IdentifierFor(int number) => string.Format(CultureInfo.InvariantCulture, AnonymousName, number).Trim();
}
