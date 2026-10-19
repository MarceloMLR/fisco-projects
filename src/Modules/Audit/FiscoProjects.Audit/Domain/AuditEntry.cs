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
}

/// <summary>
/// One audit record (ADR-0039): who did what to which item and when. Records are only added, never changed or
/// removed; the author is empty when the system did it.
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

    public string? ItemLabel { get; private init; }

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
