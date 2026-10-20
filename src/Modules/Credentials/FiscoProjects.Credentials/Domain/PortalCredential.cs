using FiscoProjects.SharedKernel;

namespace FiscoProjects.Credentials.Domain;

/// <summary>The fields of a credential, to tell which ones are not valid.</summary>
public enum CredentialField
{
    Portal = 1,
    Environment = 2,
    Url = 3,
    Username = 4,
    Password = 5,
    Notes = 6,
}

/// <summary>
/// The user and password of a portal of a project (ADR-0034), as a system of the client in an environment: the
/// password is kept only protected (ADR-0033), and the environment is the text the team uses, as "Produção".
/// </summary>
public sealed class PortalCredential : Entity
{
    public const int PortalMaxLength = 120;
    public const int EnvironmentMaxLength = 60;
    public const int UrlMaxLength = 2_000;
    public const int UsernameMaxLength = 200;
    public const int PasswordMaxLength = 500;
    public const int NotesMaxLength = 500;

    private PortalCredential()
    {
        Portal = string.Empty;
        Environment = string.Empty;
        Username = string.Empty;
        ProtectedPassword = string.Empty;
    }

    public PortalCredential(Guid projectId, CredentialForm form, string protectedPassword, Guid userId, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(form);
        ProjectId = projectId;
        Portal = form.Portal;
        Environment = form.Environment;
        Url = form.Url;
        Username = form.Username;
        Notes = form.Notes;
        ProtectedPassword = protectedPassword;
        CreatedById = userId;
        CreatedAt = now;
        UpdatedById = userId;
        UpdatedAt = now;
    }

    public Guid ProjectId { get; private init; }

    public string Portal { get; private set; }

    public string Environment { get; private set; }

    public string? Url { get; private set; }

    public string Username { get; private set; }

    /// <summary>The password protected by the Data Protection of the credentials (ADR-0033); never the password itself.</summary>
    public string ProtectedPassword { get; private set; }

    public string? Notes { get; private set; }

    public Guid CreatedById { get; private init; }

    public DateTime CreatedAt { get; private init; }

    public Guid UpdatedById { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>Changes the credential; a null protected password keeps the one it has.</summary>
    public void Change(CredentialForm form, string? protectedPassword, Guid userId, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(form);
        Portal = form.Portal;
        Environment = form.Environment;
        Url = form.Url;
        Username = form.Username;
        Notes = form.Notes;
        ProtectedPassword = protectedPassword ?? ProtectedPassword;
        UpdatedById = userId;
        UpdatedAt = now;
    }
}

/// <summary>
/// A credential as the person filled it in, already checked: the portal, the environment, the address, the user, the
/// password (null to keep the one a credential has) and the notes.
/// </summary>
public sealed record CredentialForm(string Portal, string Environment, string? Url, string Username, string? Password, string? Notes)
{
    /// <summary>
    /// The credential, trimmed but for the password, which is kept as typed, or the fields that are not valid: the
    /// portal up to 120 characters, the environment up to 60, the address empty or http or https up to 2,000, the user
    /// up to 200, the password up to 500 (needed for a new credential, empty to keep it on a change) and the notes
    /// empty or up to 500.
    /// </summary>
    public static (CredentialForm? Form, IReadOnlyList<CredentialField> Invalid) Of(
        string? portal, string? environment, string? url, string? username, string? password, string? notes, bool passwordNeeded)
    {
        var invalid = new List<CredentialField>();
        var validPortal = Text(portal, PortalCredential.PortalMaxLength);
        if (validPortal is null)
        {
            invalid.Add(CredentialField.Portal);
        }

        var validEnvironment = Text(environment, PortalCredential.EnvironmentMaxLength);
        if (validEnvironment is null)
        {
            invalid.Add(CredentialField.Environment);
        }

        if (!WebAddress.TryOf(url, PortalCredential.UrlMaxLength, out var validUrl))
        {
            invalid.Add(CredentialField.Url);
        }

        var validUsername = Text(username, PortalCredential.UsernameMaxLength);
        if (validUsername is null)
        {
            invalid.Add(CredentialField.Username);
        }

        var validPassword = string.IsNullOrEmpty(password) ? null : password;
        if ((validPassword is null && passwordNeeded) || validPassword?.Length > PortalCredential.PasswordMaxLength)
        {
            invalid.Add(CredentialField.Password);
        }

        var validNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (validNotes?.Length > PortalCredential.NotesMaxLength)
        {
            invalid.Add(CredentialField.Notes);
        }

        return invalid.Count > 0
            ? (null, invalid)
            : (new CredentialForm(validPortal!, validEnvironment!, validUrl, validUsername!, validPassword, validNotes), invalid);
    }

    private static string? Text(string? text, int maxLength) =>
        text?.Trim() is { Length: > 0 } trimmed && trimmed.Length <= maxLength && !trimmed.Any(char.IsControl) ? trimmed : null;
}
