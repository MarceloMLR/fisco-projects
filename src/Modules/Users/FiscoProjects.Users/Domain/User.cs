using FiscoProjects.SharedKernel;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Users.Domain;

/// <summary>
/// A person who uses the system. Created on the first sign-in from the identity provider data, or from the company
/// directory when someone includes the person in a project before the first sign-in (ADR-0005); the link to the
/// provider is a separate record, so other providers can be added later (ADR-0009).
/// </summary>
public sealed class User : Entity
{
    private User(string name, string email, SystemRole role, bool isActive, DateTime createdAt, DateTime? lastSignInAt)
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

    /// <summary>The last sign-in; null for a person included from the directory who never signed in.</summary>
    public DateTime? LastSignInAt { get; private set; }

    /// <summary>Theme of the person's screens, kept in the profile so it follows them to any browser (ADR-0014).</summary>
    public ThemePreference Theme { get; private set; }

    /// <summary>Whether the person receives the daily summary of e-mails (ADR-0037); the e-mails sent now are never off.</summary>
    public bool ReceivesDailySummary { get; private set; } = true;

    /// <summary>The number of the anonymization, in sequence in the installation (ADR-0110); null for everyone else.</summary>
    public int? AnonymousNumber { get; private set; }

    /// <summary>When the person was anonymized (ADR-0058); null for everyone else.</summary>
    public DateTime? AnonymizedAt { get; private set; }

    public bool IsAnonymized => AnonymizedAt is not null;

    public static User Create(string name, string email, DateTime now) =>
        new(name.Trim(), email.Trim(), SystemRole.User, isActive: true, createdAt: now, lastSignInAt: now);

    /// <summary>
    /// A person of the company directory included in a project before the first sign-in (ADR-0005): active, with the
    /// least privileged role and no sign-in yet.
    /// </summary>
    public static User FromDirectory(string name, string email, DateTime now) =>
        new(name.Trim(), email.Trim(), SystemRole.User, isActive: true, createdAt: now, lastSignInAt: null);

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

    public void ChooseTheme(ThemePreference theme)
    {
        if (!Enum.IsDefined(theme))
        {
            throw new ArgumentOutOfRangeException(nameof(theme), theme, "Unknown theme.");
        }

        Theme = theme;
    }

    public void ChooseDailySummary(bool receives) => ReceivesDailySummary = receives;

    /// <summary>Blocks the access from the next request on; name, e-mail, role and history stay (ADR-0008).</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Gives the access back; an anonymized person never comes back (ADR-0110).</summary>
    public void Reactivate()
    {
        if (IsAnonymized)
        {
            throw new InvalidOperationException("An anonymized person is not reactivated.");
        }

        IsActive = true;
    }

    /// <summary>
    /// Takes the name and the e-mail away for good (ADR-0058, ADR-0110): the name becomes the identifier of the
    /// anonymization, the e-mail is left empty and the role goes back to User. Only a deactivated person is anonymized.
    /// </summary>
    public void Anonymize(string identifier, int number, DateTime now)
    {
        if (IsActive || IsAnonymized)
        {
            throw new InvalidOperationException("Only a deactivated person, not anonymized yet, is anonymized.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        Name = identifier;
        Email = string.Empty;
        Role = SystemRole.User;
        ReceivesDailySummary = false;
        AnonymousNumber = number;
        AnonymizedAt = now;
    }
}
