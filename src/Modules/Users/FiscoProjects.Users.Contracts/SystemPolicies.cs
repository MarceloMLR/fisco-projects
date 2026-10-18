namespace FiscoProjects.Users.Contracts;

/// <summary>Authorization policies by system role, for any module's routes (ADR-0040, ADR-0046).</summary>
public static class SystemPolicies
{
    /// <summary>Only the Administrator, checked against the database on every request.</summary>
    public const string Administrator = "Administrator";
}
