using System.Globalization;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Audit.Domain;

/// <summary>What an audit record says happened.</summary>
public enum AuditAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    SignedIn = 4,
    SignInRefused = 5,

    /// <summary>The Administrator exported the audit to a spreadsheet (ADR-0099).</summary>
    Exported = 6,

    /// <summary>The system removed the records older than the retention of the installation (ADR-0058, ADR-0110).</summary>
    Cleaned = 7,
}

/// <summary>
/// One field of a change: the value before and after, as invariant text, or only that a secret changed. A field that
/// points to another item keeps its id, with the type of the item in <paramref name="Reference"/> (ADR-0084).
/// </summary>
public sealed record AuditField(string Field, string? Before, string? After, bool Secret, string? Reference = null);

/// <summary>Item types of the audit that are not an entity of a module.</summary>
public static class AuditItemTypes
{
    public const string SignIn = "SignIn";

    /// <summary>An export of the audit to a spreadsheet, with its filters and how many records left (ADR-0099).</summary>
    public const string AuditExport = "AuditExport";

    /// <summary>A monthly cleanup of the audit: the day it kept from and how many records it removed (ADR-0110).</summary>
    public const string AuditCleanup = "AuditCleanup";
}

/// <summary>The fields of a cleanup record of the audit (ADR-0110).</summary>
public static class AuditCleanupFields
{
    /// <summary>The first day kept, in the time zone of the installation; the records before it were removed.</summary>
    public const string KeptFrom = "KeptFrom";

    public const string Removed = "Removed";
}

/// <summary>
/// One audit record (ADR-0039): who did what to which item and when; the author is empty when the system did it.
/// Records are only added; the anonymization of a person erases the person's name and e-mail from them, and the
/// retention of the installation removes the old ones (ADR-0058, ADR-0110).
/// </summary>
public sealed class AuditEntry : Entity
{
    public const int ItemTypeMaxLength = 64;
    public const int ItemLabelMaxLength = 256;
    public const int DetailMaxLength = 64;
    public const int ModuleMaxLength = 64;
    public const int ReferenceMaxLength = 64;

    private AuditEntry()
    {
    }

    public DateTime OccurredAt { get; private init; }

    public Guid? ActorId { get; private init; }

    public AuditAction Action { get; private init; }

    public string ItemType { get; private init; } = string.Empty;

    public Guid? ItemId { get; private init; }

    public string? ItemLabel { get; private set; }

    /// <summary>The type of item the label points to, such as <c>User</c>; the label is then its id (ADR-0084).</summary>
    public string? ItemLabelReference { get; private init; }

    /// <summary>The identity provider of a sign-in, or why a sign-in was refused.</summary>
    public string? Detail { get; private init; }

    /// <summary>Schema of the module whose save made the change.</summary>
    public string? Module { get; private init; }

    // The changed fields, kept in a list the mapping can fill; outside, they are read-only.
    private List<AuditField> _changes = [];

    public IReadOnlyList<AuditField> Changes => _changes;

    public static AuditEntry ForChange(
        DateTime occurredAt,
        Guid? actorId,
        string module,
        string itemType,
        Guid itemId,
        AuditAction action,
        string? itemLabel,
        IEnumerable<AuditField> changes,
        string? itemLabelReference = null)
    {
        if (action is not (AuditAction.Created or AuditAction.Updated or AuditAction.Deleted))
        {
            throw new ArgumentOutOfRangeException(nameof(action), action, "A change is a creation, an update or a deletion.");
        }

        return new AuditEntry
        {
            OccurredAt = occurredAt,
            ActorId = actorId,
            Module = module,
            ItemType = itemType,
            ItemId = itemId,
            Action = action,
            ItemLabel = Shorten(itemLabel, ItemLabelMaxLength),
            ItemLabelReference = itemLabelReference,
            _changes = [.. changes],
        };
    }

    public static AuditEntry SignedIn(DateTime occurredAt, Guid userId, string provider) => new()
    {
        OccurredAt = occurredAt,
        ActorId = userId,
        Action = AuditAction.SignedIn,
        ItemType = AuditItemTypes.SignIn,
        ItemId = userId,
        Detail = Shorten(provider, DetailMaxLength),
    };

    /// <summary>
    /// An export of the audit by the Administrator (ADR-0099): the filters and how many records left, as the fields of
    /// the record, with only the value after.
    /// </summary>
    public static AuditEntry Exported(DateTime occurredAt, Guid actorId, IEnumerable<AuditField> filters) => new()
    {
        OccurredAt = occurredAt,
        ActorId = actorId,
        Action = AuditAction.Exported,
        ItemType = AuditItemTypes.AuditExport,
        _changes = [.. filters],
    };

    /// <summary>
    /// A monthly cleanup, by the system (ADR-0110): the first day kept, as an ISO date, and how many records were removed.
    /// </summary>
    public static AuditEntry Cleaned(DateTime occurredAt, DateOnly keptFrom, int removed) => new()
    {
        OccurredAt = occurredAt,
        Action = AuditAction.Cleaned,
        ItemType = AuditItemTypes.AuditCleanup,
        _changes =
        [
            new AuditField(AuditCleanupFields.KeptFrom, null, keptFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), false),
            new AuditField(AuditCleanupFields.Removed, null, removed.ToString(CultureInfo.InvariantCulture), false),
        ],
    };

    /// <summary>
    /// Takes an anonymized person out of a record of the person's own item (ADR-0058, ADR-0110): the label becomes the
    /// identifier, and the fields with personal data keep only that they changed, as a secret (ADR-0033).
    /// </summary>
    public void EraseOwner(string identifier, IReadOnlyCollection<string> personalFields)
    {
        if (ItemLabel is not null)
        {
            ItemLabel = Shorten(identifier, ItemLabelMaxLength);
        }

        if (_changes.Any(change => personalFields.Contains(change.Field) && !IsErased(change)))
        {
            _changes = [.. _changes.Select(change => personalFields.Contains(change.Field)
                ? change with { Before = null, After = null, Secret = true }
                : change)];
        }
    }

    /// <summary>A refused sign-in with the e-mail of an anonymized person names the identifier instead (ADR-0110).</summary>
    public void EraseIdentity(string identifier)
    {
        if (Action == AuditAction.SignInRefused)
        {
            ItemLabel = Shorten(identifier, ItemLabelMaxLength);
        }
    }

    private static bool IsErased(AuditField change) => change.Secret && change.Before is null && change.After is null;

    /// <summary>A refused sign-in, by the system: the identity used, trimmed and shortened, and the reason.</summary>
    public static AuditEntry SignInRefused(DateTime occurredAt, string identity, string reason) => new()
    {
        OccurredAt = occurredAt,
        Action = AuditAction.SignInRefused,
        ItemType = AuditItemTypes.SignIn,
        ItemLabel = Shorten(identity.Trim(), ItemLabelMaxLength),
        Detail = Shorten(reason, DetailMaxLength),
    };

    private static string? Shorten(string? text, int maxLength) =>
        text is null || text.Length <= maxLength ? text : text[..maxLength];
}
