using FiscoProjects.SharedKernel;

namespace FiscoProjects.Calendar.Domain;

/// <summary>
/// The choice of the installation for a national holiday or an optional holiday the system calculates (ADR-0083): one
/// row per holiday, created with the database, and the choice holds in every year.
/// </summary>
public sealed class CalculatedHoliday : Entity
{
    public const int KeyMaxLength = 40;

    private CalculatedHoliday()
    {
        Key = string.Empty;
    }

    /// <summary>The holiday, as the national calendar names it, such as CarnivalMonday.</summary>
    public string Key { get; private set; }

    public bool IsConsidered { get; private set; }

    public void Consider(bool considered) => IsConsidered = considered;
}
