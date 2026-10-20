using FiscoProjects.Notifications.Domain;
using FiscoProjects.Persistence.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Notifications.Infrastructure.Persistence;

// The queue of the e-mails (ADR-0036): technical, out of the audit; the reservation is checked by the row version.
internal sealed class OutboxEmailConfiguration : IEntityTypeConfiguration<OutboxEmail>
{
    public void Configure(EntityTypeBuilder<OutboxEmail> email)
    {
        email.ToTable("outbox_emails");
        email.Property(e => e.Kind).HasMaxLength(OutboxEmail.KindMaxLength);
        email.Property(e => e.ToAddress).HasMaxLength(OutboxEmail.AddressMaxLength);
        email.Property(e => e.ToName).HasMaxLength(OutboxEmail.NameMaxLength);
        email.Property(e => e.Subject).HasMaxLength(OutboxEmail.SubjectMaxLength);
        email.Property(e => e.Status).HasConversion<string>().HasMaxLength(16);
        email.Property(e => e.LastError).HasMaxLength(OutboxEmail.ErrorMaxLength);
        email.Property<byte[]>("Version").IsRowVersion();
        email.HasIndex(e => new { e.Status, e.NextAttemptAt });
        email.HasIndex(e => new { e.Kind, e.CreatedAt });
        email.ExcludeFromAudit();
    }
}
