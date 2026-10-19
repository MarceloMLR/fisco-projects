using FiscoProjects.Administration.Domain;
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
        settings.Property(s => s.LogoFileId).HasMaxLength(400);
        settings.Property(s => s.LogoFileName).HasMaxLength(200);
        settings.Property(s => s.LogoContentType).HasMaxLength(64);
        settings.Ignore(s => s.HasLogo);

        // The one row, created with the database, so reading never has to create it.
        settings.HasData(new
        {
            Id = InstallationSettings.SingleId,
            Name = InstallationSettings.DefaultName,
            TimeZone = InstallationSettings.DefaultTimeZone,
        });

        // Audit (ADR-0039): the name and the name of the logo file sent tell what changed; the item is always the
        // same, so it has no label. Ids, type and date of the logo are technical.
        settings.Property(s => s.LogoFileId).ExcludeFromAudit();
        settings.Property(s => s.LogoContentType).ExcludeFromAudit();
        settings.Property(s => s.LogoChangedAt).ExcludeFromAudit();
    }
}
