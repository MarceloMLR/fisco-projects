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
/// only the fact that it changed (ADR-0033). A field that points to another item keeps its id and the type of the
/// item in <paramref name="Reference"/> (ADR-0084). A context field says where the item is and comes in every change
/// of the item, with the same value before and after when it does not change (<paramref name="IsContext"/>).
/// </summary>
public sealed record AuditedField(
    string Name, string? Before, string? After, bool Secret, string? Reference = null, bool IsContext = false);

/// <summary>
/// A change to one item, captured by the base context when the module saves (ADR-0039, ADR-0075): the schema of
/// the module, the item type (such as <c>User</c>) and id, what happened, the label that identifies the item on the
/// Audit screen (such as the person name) and the fields. A label that points to another item keeps its id, with the
/// type of the item in <paramref name="ItemLabelReference"/> (ADR-0084).
/// </summary>
public sealed record AuditedChange(
    string Module,
    string ItemType,
    Guid ItemId,
    AuditedAction Action,
    string? ItemLabel,
    IReadOnlyList<AuditedField> Fields,
    string? ItemLabelReference = null);
