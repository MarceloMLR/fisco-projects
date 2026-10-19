namespace FiscoProjects.Users.Contracts;

/// <summary>How other modules point to a person in the audit (ADR-0084).</summary>
public static class UserAudit
{
    /// <summary>The type of a field that holds the id of a person, shown on the Audit screen by the person's name.</summary>
    public const string Reference = "User";
}
