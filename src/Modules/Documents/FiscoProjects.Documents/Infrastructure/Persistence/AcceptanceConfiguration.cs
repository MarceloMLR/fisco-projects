using FiscoProjects.Documents.Domain;
using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Users.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Documents.Infrastructure.Persistence;

// The acceptance terms (ADR-0035, ADR-0095): in the audit by the title, with the project in every record and the
// milestone and the person by their current names (ADR-0084); a cancelled request stays only there. The file, the
// signed document and the moments are technical. Two answers at the same time are told apart by the version.
internal sealed class AcceptanceTermConfiguration : IEntityTypeConfiguration<AcceptanceTerm>
{
    public void Configure(EntityTypeBuilder<AcceptanceTerm> term)
    {
        term.ToTable("acceptance_terms");
        term.Property(t => t.Title).HasMaxLength(AcceptanceTerm.TitleMaxLength);
        term.Property(t => t.Message).HasMaxLength(AcceptanceTerm.MessageMaxLength);
        term.Property(t => t.RefusalReason).HasMaxLength(AcceptanceTerm.ReasonMaxLength);
        term.Property(t => t.Status).HasConversion<string>().HasMaxLength(16);
        term.Property(t => t.FileId).HasMaxLength(512);
        term.Property(t => t.FileName).HasMaxLength(Document.NameMaxLength);
        term.HasOne<Document>().WithMany().HasForeignKey(t => t.DocumentId).OnDelete(DeleteBehavior.Restrict);
        term.HasIndex(t => new { t.ProjectId, t.RecipientId });
        term.HasIndex(t => t.Status);
        term.HasMany(t => t.Events).WithOne().HasForeignKey(e => e.TermId).OnDelete(DeleteBehavior.Cascade);
        term.Navigation(t => t.Events).UsePropertyAccessMode(PropertyAccessMode.Field);
        term.Property<byte[]>("Version").IsRowVersion().ExcludeFromAudit();

        term.HasAuditLabel(t => t.Title);
        term.Property(t => t.ProjectId).IsAuditReference(ProjectsAudit.ProjectReference).IsAuditContext();
        term.Property(t => t.MilestoneId).IsAuditReference(ProjectsAudit.ScheduleItemReference);
        term.Property(t => t.RecipientId).IsAuditReference(UserAudit.Reference);
        term.Property(t => t.RequestedById).ExcludeFromAudit();
        term.Property(t => t.RequestedAt).ExcludeFromAudit();
        term.Property(t => t.AnsweredById).ExcludeFromAudit();
        term.Property(t => t.AnsweredAt).ExcludeFromAudit();
        term.Property(t => t.FileId).ExcludeFromAudit();
        term.Property(t => t.FileSize).ExcludeFromAudit();
        term.Property(t => t.DocumentId).ExcludeFromAudit();
    }
}

// The history of each term, as the events of a ticket: it never changes and stays out of the audit (ADR-0039).
internal sealed class AcceptanceEventConfiguration : IEntityTypeConfiguration<AcceptanceEvent>
{
    public void Configure(EntityTypeBuilder<AcceptanceEvent> acceptanceEvent)
    {
        acceptanceEvent.ToTable("acceptance_events");
        acceptanceEvent.Property(e => e.Kind).HasConversion<string>().HasMaxLength(16);
        acceptanceEvent.HasIndex(e => new { e.TermId, e.At });
        acceptanceEvent.ExcludeFromAudit();
    }
}
