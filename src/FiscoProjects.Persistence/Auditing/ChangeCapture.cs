using System.Globalization;
using FiscoProjects.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FiscoProjects.Persistence.Auditing;

/// <summary>Reads, before a save, what is about to change in the items of a module context.</summary>
internal static class ChangeCapture
{
    public static IReadOnlyList<AuditedChange> Capture(ChangeTracker changeTracker, string module)
    {
        changeTracker.DetectChanges();
        var changes = new List<AuditedChange>();

        foreach (var entry in changeTracker.Entries())
        {
            if (entry.Entity is not Entity item || entry.Metadata.IsExcludedFromAudit())
            {
                continue;
            }

            var action = entry.State switch
            {
                EntityState.Added => AuditedAction.Created,
                EntityState.Modified => AuditedAction.Updated,
                EntityState.Deleted => AuditedAction.Deleted,
                _ => (AuditedAction?)null,
            };
            if (action is null)
            {
                continue;
            }

            var fields = Fields(entry, action.Value);
            if (action == AuditedAction.Updated && fields.Count == 0)
            {
                continue;
            }

            changes.Add(new AuditedChange(
                module,
                entry.Metadata.ClrType.Name,
                item.Id,
                action.Value,
                Label(entry, action.Value),
                fields,
                entry.Metadata.AuditLabelReference()));
        }

        return changes;
    }

    private static List<AuditedField> Fields(EntityEntry entry, AuditedAction action)
    {
        var fields = new List<AuditedField>();

        foreach (var property in entry.Properties)
        {
            var metadata = property.Metadata;
            if (metadata.IsPrimaryKey() || metadata.IsExcludedFromAudit())
            {
                continue;
            }

            var before = action == AuditedAction.Created ? null : property.OriginalValue;
            var after = action == AuditedAction.Deleted ? null : property.CurrentValue;
            if (Equals(before, after) || (action == AuditedAction.Updated && !property.IsModified))
            {
                continue;
            }

            fields.Add(metadata.IsAuditSecret()
                ? new AuditedField(metadata.Name, null, null, Secret: true)
                : new AuditedField(metadata.Name, Format(before), Format(after), Secret: false, metadata.AuditReference()));
        }

        return fields;
    }

    private static string? Label(EntityEntry entry, AuditedAction action)
    {
        var name = entry.Metadata.AuditLabel();
        if (name is null)
        {
            return null;
        }

        var property = entry.Property(name);
        return Format(action == AuditedAction.Deleted ? property.OriginalValue : property.CurrentValue);
    }

    /// <summary>Invariant text of a value: dates in ISO 8601 UTC, enums by name, booleans as true or false.</summary>
    internal static string? Format(object? value) => value switch
    {
        null => null,
        string text => text,
        DateTime dateTime => (dateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            : dateTime.ToUniversalTime()).ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        TimeOnly time => time.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
        bool flag => flag ? "true" : "false",
        Enum member => member.ToString(),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };
}
