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

    internal static bool IsExcludedFromAudit(this IReadOnlyAnnotatable item) => item.FindAnnotation(Excluded)?.Value is true;

    internal static bool IsAuditSecret(this IReadOnlyProperty property) => property.FindAnnotation(Secret)?.Value is true;

    internal static string? AuditLabel(this IReadOnlyEntityType entityType) => entityType.FindAnnotation(Label)?.Value as string;

    private static string PropertyName<TEntity>(Expression<Func<TEntity, string?>> property) =>
        property.Body is MemberExpression member
            ? member.Member.Name
            : throw new ArgumentException("Use a property of the item, such as x => x.Name.", nameof(property));
}
