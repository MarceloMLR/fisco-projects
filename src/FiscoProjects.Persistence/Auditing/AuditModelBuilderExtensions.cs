using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Persistence.Auditing;

/// <summary>
/// Audit markers a module puts in its own database configuration. Everything else is audited as it is.
/// </summary>
public static class AuditModelBuilderExtensions
{
    internal const string Excluded = "FiscoProjects:Audit:Excluded";
    internal const string Secret = "FiscoProjects:Audit:Secret";
    internal const string Label = "FiscoProjects:Audit:Label";
    internal const string Reference = "FiscoProjects:Audit:Reference";
    internal const string LabelReference = "FiscoProjects:Audit:LabelReference";
    internal const string Context = "FiscoProjects:Audit:Context";

    /// <summary>The item type stays out of the audit, such as a technical link.</summary>
    public static EntityTypeBuilder<TEntity> ExcludeFromAudit<TEntity>(this EntityTypeBuilder<TEntity> entity)
        where TEntity : class => entity.HasAnnotation(Excluded, true);

    /// <summary>The field stays out of the audit, such as the last sign-in; a save that changes only it is not audited.</summary>
    public static PropertyBuilder<TProperty> ExcludeFromAudit<TProperty>(this PropertyBuilder<TProperty> property) =>
        property.HasAnnotation(Excluded, true);

    /// <summary>The audit records only that the field changed, never its value, not even encrypted (ADR-0033).</summary>
    public static PropertyBuilder<TProperty> IsAuditSecret<TProperty>(this PropertyBuilder<TProperty> property) =>
        property.HasAnnotation(Secret, true);

    /// <summary>The field that identifies the item on the Audit screen, such as the person name.</summary>
    public static EntityTypeBuilder<TEntity> HasAuditLabel<TEntity>(
        this EntityTypeBuilder<TEntity> entity, Expression<Func<TEntity, string?>> property)
        where TEntity : class =>
        entity.HasAnnotation(Label, PropertyName(property));

    /// <summary>
    /// The field points to another item of the given type, such as <c>User</c>: the audit keeps the id and the type,
    /// and the Audit screen shows the current name of the item (ADR-0084).
    /// </summary>
    public static PropertyBuilder<TProperty> IsAuditReference<TProperty>(this PropertyBuilder<TProperty> property, string kind) =>
        property.HasAnnotation(Reference, kind);

    /// <summary>
    /// The item is identified on the Audit screen by another item it points to, such as the person included in a
    /// project, shown by the current name (ADR-0084).
    /// </summary>
    public static EntityTypeBuilder<TEntity> HasAuditLabelReference<TEntity>(
        this EntityTypeBuilder<TEntity> entity, Expression<Func<TEntity, Guid>> property, string kind)
        where TEntity : class =>
        entity.HasAnnotation(Label, PropertyName(property)).HasAnnotation(LabelReference, kind);

    /// <summary>
    /// The field says where the item is, such as the project of a person in it: the audit records it in every creation,
    /// change and removal of the item, also when it does not change, with the same value before and after. The Audit
    /// screen uses it to say where the change happened, out of the table of the changes.
    /// </summary>
    public static PropertyBuilder<TProperty> IsAuditContext<TProperty>(this PropertyBuilder<TProperty> property) =>
        property.HasAnnotation(Context, true);

    internal static bool IsExcludedFromAudit(this IReadOnlyAnnotatable item) => item.FindAnnotation(Excluded)?.Value is true;

    internal static bool IsAuditSecret(this IReadOnlyProperty property) => property.FindAnnotation(Secret)?.Value is true;

    internal static bool IsAuditContext(this IReadOnlyProperty property) => property.FindAnnotation(Context)?.Value is true;

    internal static string? AuditLabel(this IReadOnlyEntityType entityType) => entityType.FindAnnotation(Label)?.Value as string;

    internal static string? AuditLabelReference(this IReadOnlyEntityType entityType) =>
        entityType.FindAnnotation(LabelReference)?.Value as string;

    internal static string? AuditReference(this IReadOnlyProperty property) => property.FindAnnotation(Reference)?.Value as string;

    private static string PropertyName<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> property) =>
        property.Body is MemberExpression member
            ? member.Member.Name
            : throw new ArgumentException("Use a property of the item, such as x => x.Name.", nameof(property));
}
