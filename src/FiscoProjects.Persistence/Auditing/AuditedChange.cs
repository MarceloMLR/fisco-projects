namespace FiscoProjects.Persistence.Auditing;

/// <summary>What happened to an item in a save.</summary>
public enum AuditedAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
}

/// <summary>
/// One field of an audited item: the value before and after, as invariant text. A secret field keeps no value,
/// only the fact that it changed (ADR-0033).
/// </summary>
public sealed record AuditedField(string Name, string? Before, string? After, bool Secret);

/// <summary>
/// A change to one item, captured by the base context when the module saves (ADR-0039, ADR-0075): the schema of
/// the module, the item type (such as <c>User</c>) and id, what happened, the label that identifies the item on the
/// Audit screen (such as the person name) and the fields.
/// </summary>
public sealed record AuditedChange(
    string Module,
    string ItemType,
    Guid ItemId,
    AuditedAction Action,
    string? ItemLabel,
    IReadOnlyList<AuditedField> Fields);
