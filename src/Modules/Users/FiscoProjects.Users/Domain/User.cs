using FiscoProjects.SharedKernel;

namespace FiscoProjects.Users.Domain;

/// <summary>
/// A person who uses the system. Created on the first sign-in from the identity provider data (ADR-0005); the
/// link to the provider is a separate record, so other providers can be added later (ADR-0009).
/// </summary>
public sealed class User : Entity
{
    private User(string name, string email, bool isActive, DateTime createdAt, DateTime lastSignInAt)
    {
        Name = name;
        Email = email;
        IsActive = isActive;
        CreatedAt = createdAt;
        LastSignInAt = lastSignInAt;
    }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private init; }

    public DateTime LastSignInAt { get; private set; }

    public static User Create(string name, string email, DateTime now) =>
        new(name.Trim(), email.Trim(), isActive: true, createdAt: now, lastSignInAt: now);

    /// <summary>The provider is the source of truth for name and e-mail, so each sign-in refreshes them.</summary>
    public void RecordSignIn(string name, string email, DateTime now)
    {
        Name = name.Trim();
        Email = email.Trim();
        LastSignInAt = now;
    }
}
