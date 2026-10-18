using FiscoProjects.SharedKernel;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Users.Domain;

/// <summary>
/// A person who uses the system. Created on the first sign-in from the identity provider data (ADR-0005); the
/// link to the provider is a separate record, so other providers can be added later (ADR-0009).
/// </summary>
public sealed class User : Entity
{
    private User(string name, string email, SystemRole role, bool isActive, DateTime createdAt, DateTime lastSignInAt)
    {
        Name = name;
        Email = email;
        Role = role;
        IsActive = isActive;
        CreatedAt = createdAt;
        LastSignInAt = lastSignInAt;
    }

    public string Name { get; private set; }

    public string Email { get; private set; }

    /// <summary>Role in the system (ADR-0040); User, the least privileged, on creation (ADR-0006).</summary>
    public SystemRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private init; }

    public DateTime LastSignInAt { get; private set; }

    public static User Create(string name, string email, DateTime now) =>
        new(name.Trim(), email.Trim(), SystemRole.User, isActive: true, createdAt: now, lastSignInAt: now);

    public bool IsActiveAdministrator => IsActive && Role == SystemRole.Administrator;

    /// <summary>The provider is the source of truth for name and e-mail, so each sign-in refreshes them.</summary>
    public void RecordSignIn(string name, string email, DateTime now)
    {
        Name = name.Trim();
        Email = email.Trim();
        LastSignInAt = now;
    }

    /// <summary>
    /// Changes the role. Keeping at least one active administrator is checked by the use case, which sees all
    /// users (ADR-0006).
    /// </summary>
    public void ChangeRole(SystemRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown system role.");
        }

        Role = role;
    }

    /// <summary>Blocks the access from the next request on; name, e-mail, role and history stay (ADR-0008).</summary>
    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
