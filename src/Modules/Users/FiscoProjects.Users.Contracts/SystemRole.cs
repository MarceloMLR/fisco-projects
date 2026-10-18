namespace FiscoProjects.Users.Contracts;

/// <summary>
/// Role of a person in the whole system (ADR-0040). The numbering starts at 1, so an unset value is never a
/// valid role.
/// </summary>
public enum SystemRole
{
    /// <summary>Default on the first sign-in: nothing beyond what each project role allows.</summary>
    User = 1,

    /// <summary>Creates projects.</summary>
    ProjectManager = 2,

    /// <summary>Everything, including the Administration.</summary>
    Administrator = 3,
}
