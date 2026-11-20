using GerenWare.Persistence.Auditing;
using GerenWare.Projects.Application;
using GerenWare.Projects.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GerenWare.Projects.Infrastructure.Persistence;

/// <summary>The notes of the last status report of each project, in the tables of the module (ADR-0165).</summary>
internal sealed class StatusReportStore(ProjectsDbContext db) : IStatusReportStore
{
    public Task<StatusReportNotes?> FindAsync(Guid projectId, CancellationToken cancellationToken) =>
        db.StatusReportNotes.Include(notes => notes.Risks).SingleOrDefaultAsync(notes => notes.ProjectId == projectId, cancellationToken);

    public void Add(StatusReportNotes notes) => db.StatusReportNotes.Add(notes);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

// The notes of the last report, one per project: out of the audit, because the document of the report is in it
// (ADR-0039, ADR-0165); removed with the project.
internal sealed class StatusReportNotesConfiguration : IEntityTypeConfiguration<StatusReportNotes>
{
    public void Configure(EntityTypeBuilder<StatusReportNotes> notes)
    {
        notes.ToTable("status_report_notes");
        notes.HasIndex(n => n.ProjectId).IsUnique();
        notes.HasOne<Project>().WithMany().HasForeignKey(n => n.ProjectId).OnDelete(DeleteBehavior.Cascade);
        notes.Property(n => n.Summary).HasMaxLength(StatusReportLimits.TextMaxLength);
        notes.Property(n => n.Focus).HasMaxLength(StatusReportLimits.TextMaxLength);
        notes.Property(n => n.NextSteps).HasMaxLength(StatusReportLimits.TextMaxLength);
        notes.Property(n => n.ClientNeeds).HasMaxLength(StatusReportLimits.TextMaxLength);
        notes.HasMany(n => n.Risks).WithOne().HasForeignKey(r => r.NotesId).OnDelete(DeleteBehavior.Cascade);
        notes.Navigation(n => n.Risks).UsePropertyAccessMode(PropertyAccessMode.Field);
        notes.ExcludeFromAudit();
    }
}

internal sealed class StatusReportRiskNoteConfiguration : IEntityTypeConfiguration<StatusReportRiskNote>
{
    public void Configure(EntityTypeBuilder<StatusReportRiskNote> risk)
    {
        risk.ToTable("status_report_risks");
        risk.Property(r => r.Level).HasConversion<string>().HasMaxLength(16);
        risk.Property(r => r.Attention).HasMaxLength(StatusReportLimits.AttentionMaxLength);
        risk.ExcludeFromAudit();
    }
}
