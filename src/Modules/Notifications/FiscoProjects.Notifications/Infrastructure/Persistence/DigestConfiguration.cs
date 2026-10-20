using FiscoProjects.Notifications.Domain;
using FiscoProjects.Persistence.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Notifications.Infrastructure.Persistence;

// The items waiting for the daily summary (ADR-0037): technical, out of the audit, removed once sent.
internal sealed class DigestItemConfiguration : IEntityTypeConfiguration<DigestItem>
{
    public void Configure(EntityTypeBuilder<DigestItem> item)
    {
        item.ToTable("digest_items");
        item.Property(i => i.Kind).HasConversion<string>().HasMaxLength(40);
        item.Property(i => i.Path).HasMaxLength(DigestItem.PathMaxLength);
        item.Property(i => i.Key).HasMaxLength(DigestItem.KeyMaxLength);
        item.HasIndex(i => new { i.UserId, i.Key });
        item.HasIndex(i => i.CreatedAt);
        item.ExcludeFromAudit();
    }
}

// The day of each daily summary: the key keeps it to one process.
internal sealed class DailySummaryRunConfiguration : IEntityTypeConfiguration<DailySummaryRun>
{
    public void Configure(EntityTypeBuilder<DailySummaryRun> run)
    {
        run.ToTable("daily_summaries");
        run.HasKey(r => r.Day);
        run.Property(r => r.Day).ValueGeneratedNever();
        run.ExcludeFromAudit();
    }
}
