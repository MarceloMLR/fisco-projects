using FiscoProjects.Audit.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Audit.Infrastructure.Persistence;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> entry)
    {
        entry.ToTable("entries");
        entry.Property(e => e.Action).HasConversion<string>().HasMaxLength(32);
        entry.Property(e => e.ItemType).HasMaxLength(AuditEntry.ItemTypeMaxLength);
        entry.Property(e => e.ItemLabel).HasMaxLength(AuditEntry.ItemLabelMaxLength);
        entry.Property(e => e.Detail).HasMaxLength(AuditEntry.DetailMaxLength);
        entry.Property(e => e.Module).HasMaxLength(AuditEntry.ModuleMaxLength);
        entry.Property(e => e.ItemLabelReference).HasMaxLength(AuditEntry.ReferenceMaxLength);

        // The changed fields of a record, before and after, in a JSON column.
        entry.Ignore(e => e.Changes);
        entry.ComplexCollection<List<AuditField>, AuditField>("_changes", changes => changes.ToJson(nameof(AuditEntry.Changes)));

        // The Audit screen lists by period, newest first, optionally by author or item type.
        entry.HasIndex(e => e.OccurredAt);
        entry.HasIndex(e => new { e.ActorId, e.OccurredAt });
        entry.HasIndex(e => new { e.ItemType, e.OccurredAt });
    }
}
