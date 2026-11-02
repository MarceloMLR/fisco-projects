using FiscoProjects.Notifications.Domain;
using FiscoProjects.Persistence.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Notifications.Infrastructure.Persistence;

// The queue of Teams (ADR-0149): technical, out of the audit, as the queue of the e-mails; the reservation is checked
// by the row version.
internal sealed class TeamsNoticeConfiguration : IEntityTypeConfiguration<TeamsNotice>
{
    public void Configure(EntityTypeBuilder<TeamsNotice> notice)
    {
        notice.ToTable("teams_notices");
        notice.Property(n => n.Kind).HasConversion<string>().HasMaxLength(40);
        notice.Property(n => n.Path).HasMaxLength(TeamsNotice.PathMaxLength);
        notice.Property(n => n.Status).HasConversion<string>().HasMaxLength(16);
        notice.Property(n => n.LastError).HasMaxLength(TeamsNotice.ErrorMaxLength);
        notice.Property<byte[]>("Version").IsRowVersion();
        notice.HasIndex(n => new { n.Status, n.NextAttemptAt });
        notice.HasIndex(n => n.UserId);
        notice.HasIndex(n => n.ProjectId);
        notice.Ignore(n => n.NeedsEmail);
        notice.ExcludeFromAudit();
    }
}

// The application of Teams of the installation (ADR-0149): one row, created with the database; the migration gives it
// an id of its own. Turning the notices on or off is a record of the audit; the id and the version are technical.
internal sealed class TeamsAppConfiguration : IEntityTypeConfiguration<TeamsApp>
{
    public void Configure(EntityTypeBuilder<TeamsApp> app)
    {
        app.ToTable("teams_app");
        app.Property(a => a.PackageHash).HasMaxLength(TeamsApp.HashMaxLength);
        app.Ignore(a => a.Version);
        app.HasData(new { Id = TeamsApp.SingleId, ManifestId = Guid.Empty, NoticesOn = false, PackageVersion = 0 });
        app.Property(a => a.ManifestId).ExcludeFromAudit();
        app.Property(a => a.PackageVersion).ExcludeFromAudit();
        app.Property(a => a.PackageHash).ExcludeFromAudit();
    }
}

// The channel each person chose (ADR-0149): a personal preference, out of the audit, as the daily summary.
internal sealed class NoticePreferenceConfiguration : IEntityTypeConfiguration<NoticePreference>
{
    public void Configure(EntityTypeBuilder<NoticePreference> preference)
    {
        preference.ToTable("notice_preferences");
        preference.Property(p => p.Channel).HasConversion<string>().HasMaxLength(16);
        preference.HasIndex(p => p.UserId).IsUnique();
        preference.ExcludeFromAudit();
    }
}
