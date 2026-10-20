using FiscoProjects.Documents.Contracts;
using FiscoProjects.Documents.Domain;
using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Projects.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Documents.Infrastructure.Persistence;

// The default folders (ADR-0031): the table starts with the folders of the MVP, the last one for the acceptance terms
// (ADR-0093), and the Administrator changes them, with the change in the audit (ADR-0039, ADR-0046).
internal sealed class DefaultFolderConfiguration : IEntityTypeConfiguration<DefaultFolder>
{
    /// <summary>The unique index of the names, told apart from other errors when saving.</summary>
    public const string NameIndex = "IX_default_folders_Name";

    private static readonly string[] Seed =
    [
        "01 Gestão do projeto",
        "02 Contratos e compras",
        "03 Requisitos e especificações",
        "04 Atas de reunião",
        "05 Entregas e manuais",
        "06 Termos de aceite",
    ];

    public void Configure(EntityTypeBuilder<DefaultFolder> folder)
    {
        folder.ToTable("default_folders");
        folder.Property(f => f.Name).HasMaxLength(FolderNames.MaxLength);
        folder.HasIndex(f => f.Name).IsUnique().HasDatabaseName(NameIndex);
        folder.HasAuditLabel(f => f.Name);
        folder.HasData(Seed.Select((name, index) => new
        {
            Id = Guid.Parse($"019a0a10-0000-7000-8000-{index + 1:D12}"),
            Name = name,
            IsAcceptance = index == Seed.Length - 1,
            IsActive = true,
        }));
    }
}

// The folders of each project, copies of the default folders: technical, out of the audit.
internal sealed class DocumentFolderConfiguration : IEntityTypeConfiguration<DocumentFolder>
{
    public void Configure(EntityTypeBuilder<DocumentFolder> folder)
    {
        folder.ToTable("folders");
        folder.Property(f => f.Name).HasMaxLength(FolderNames.MaxLength);
        folder.Property(f => f.StorageFolderId).HasMaxLength(512);
        folder.HasIndex(f => new { f.ProjectId, f.Name }).IsUnique();
        folder.ExcludeFromAudit();
    }
}

// The documents (ADR-0031): in the audit by the name, with the project in every record and the folder and the link by
// their current names (ADR-0084); the file and the technical moments stay out.
internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> document)
    {
        document.ToTable("documents");
        document.Property(d => d.Name).HasMaxLength(Document.NameMaxLength);
        document.Property(d => d.FileId).HasMaxLength(512);
        document.Property(d => d.Status).HasConversion<string>().HasMaxLength(16);
        document.HasOne<DocumentFolder>().WithMany().HasForeignKey(d => d.FolderId).OnDelete(DeleteBehavior.Restrict);
        // One document per file of the storage in a folder: a file sent again is a new version of it.
        document.HasIndex(d => new { d.FolderId, d.FileId }).IsUnique();
        document.HasIndex(d => new { d.ProjectId, d.Name });
        document.HasMany(d => d.Versions).WithOne().HasForeignKey(v => v.DocumentId).OnDelete(DeleteBehavior.Cascade);
        document.Navigation(d => d.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);

        document.HasAuditLabel(d => d.Name);
        document.Property(d => d.ProjectId).IsAuditReference(ProjectsAudit.ProjectReference).IsAuditContext();
        document.Property(d => d.FolderId).IsAuditReference(DocumentsAudit.FolderReference);
        document.Property(d => d.SchedulePhaseId).IsAuditReference(ProjectsAudit.SchedulePhaseReference);
        document.Property(d => d.ScheduleItemId).IsAuditReference(ProjectsAudit.ScheduleItemReference);
        document.Property(d => d.StatusChangedById).ExcludeFromAudit();
        document.Property(d => d.StatusChangedAt).ExcludeFromAudit();
        document.Property(d => d.CreatedById).ExcludeFromAudit();
        document.Property(d => d.CreatedAt).ExcludeFromAudit();
        document.Property(d => d.UpdatedById).ExcludeFromAudit();
        document.Property(d => d.UpdatedAt).ExcludeFromAudit();
        document.Property(d => d.FileId).ExcludeFromAudit();
        document.Property(d => d.Size).ExcludeFromAudit();
    }
}

// The versions: the number of the document already says, in the audit, that a version came.
internal sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> version)
    {
        version.ToTable("document_versions");
        version.Property(v => v.StorageVersionId).HasMaxLength(512);
        version.HasIndex(v => new { v.DocumentId, v.Number }).IsUnique();
        version.ExcludeFromAudit();
    }
}
