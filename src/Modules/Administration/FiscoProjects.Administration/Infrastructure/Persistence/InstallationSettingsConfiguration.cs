using FiscoProjects.Administration.Domain;
using FiscoProjects.Files.Contracts;
using FiscoProjects.Persistence.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Administration.Infrastructure.Persistence;

internal sealed class InstallationSettingsConfiguration : IEntityTypeConfiguration<InstallationSettings>
{
    public void Configure(EntityTypeBuilder<InstallationSettings> settings)
    {
        settings.ToTable("installation_settings");
        settings.Property(s => s.Name).HasMaxLength(InstallationSettings.NameMaxLength);
        // The installations created before the time zone existed show dates in the default one (ADR-0013).
        settings.Property(s => s.TimeZone).HasMaxLength(InstallationSettings.TimeZoneMaxLength)
            .HasDefaultValue(InstallationSettings.DefaultTimeZone);
        settings.Property(s => s.DailySummaryAt).HasDefaultValue(InstallationSettings.DefaultDailySummaryAt);
        settings.Property(s => s.NotificationRetentionDays).HasDefaultValue(InstallationSettings.DefaultNotificationRetentionDays);
        settings.Property(s => s.AuditRetentionYears).HasDefaultValue(InstallationSettings.DefaultAuditRetentionYears);
        settings.Property(s => s.ProjectsFolder).HasMaxLength(FileNames.MaxPathLength).HasDefaultValue(string.Empty);
        settings.Property(s => s.MainColor).HasMaxLength(7);
        settings.Property(s => s.LogoFileId).HasMaxLength(400);
        settings.Property(s => s.LogoFileName).HasMaxLength(200);
        settings.Property(s => s.LogoContentType).HasMaxLength(64);
        settings.Ignore(s => s.HasLogo);
        settings.Property(s => s.DarkLogoFileId).HasMaxLength(400);
        settings.Property(s => s.DarkLogoFileName).HasMaxLength(200);
        settings.Property(s => s.DarkLogoContentType).HasMaxLength(64);
        settings.Ignore(s => s.HasDarkLogo);

        // The one row, created with the database, so reading never has to create it.
        settings.HasData(new
        {
            Id = InstallationSettings.SingleId,
            Name = InstallationSettings.DefaultName,
            TimeZone = InstallationSettings.DefaultTimeZone,
            DailySummaryAt = InstallationSettings.DefaultDailySummaryAt,
            NotificationRetentionDays = InstallationSettings.DefaultNotificationRetentionDays,
            AuditRetentionYears = InstallationSettings.DefaultAuditRetentionYears,
            ProjectsFolder = string.Empty,
        });

        // Audit (ADR-0039): the name and the name of the logo file sent tell what changed; the item is always the
        // same, so it has no label. Ids, type and date of the logo are technical.
        settings.Property(s => s.LogoFileId).ExcludeFromAudit();
        settings.Property(s => s.LogoContentType).ExcludeFromAudit();
        settings.Property(s => s.LogoChangedAt).ExcludeFromAudit();
        settings.Property(s => s.DarkLogoFileId).ExcludeFromAudit();
        settings.Property(s => s.DarkLogoContentType).ExcludeFromAudit();
        settings.Property(s => s.DarkLogoChangedAt).ExcludeFromAudit();
    }
}
