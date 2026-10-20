using FiscoProjects.Documents.Contracts;
using FiscoProjects.Documents.Domain;
using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Projects.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Documents.Infrastructure.Persistence;

// The categories of links (ADR-0032): the table starts with the categories of the MVP, and the Administrator changes
// them, with the change in the audit (ADR-0039, ADR-0046).
internal sealed class LinkCategoryConfiguration : IEntityTypeConfiguration<LinkCategory>
{
    /// <summary>The unique index of the names, told apart from other errors when saving.</summary>
    public const string NameIndex = "IX_link_categories_Name";

    private static readonly string[] Seed = ["Sistemas", "Ambientes", "Repositórios", "Painéis", "Outros"];

    public void Configure(EntityTypeBuilder<LinkCategory> category)
    {
        category.ToTable("link_categories");
        category.Property(c => c.Name).HasMaxLength(LinkCategory.NameMaxLength);
        category.HasIndex(c => c.Name).IsUnique().HasDatabaseName(NameIndex);
        category.HasAuditLabel(c => c.Name);
        category.HasData(Seed.Select((name, index) => new
        {
            Id = Guid.Parse($"019a0a11-0000-7000-8000-{index + 1:D12}"),
            Name = name,
            IsActive = true,
        }));
    }
}

// The links of the projects (ADR-0032): in the audit by the title, with the project in every record and the category
// by its current name (ADR-0084); who included or changed it and when stay out, as the audit records them.
internal sealed class ProjectLinkConfiguration : IEntityTypeConfiguration<ProjectLink>
{
    public void Configure(EntityTypeBuilder<ProjectLink> link)
    {
        link.ToTable("links");
        link.Property(l => l.Title).HasMaxLength(ProjectLink.TitleMaxLength);
        link.Property(l => l.Url).HasMaxLength(ProjectLink.UrlMaxLength);
        link.Property(l => l.Description).HasMaxLength(ProjectLink.DescriptionMaxLength);
        link.HasOne<LinkCategory>().WithMany().HasForeignKey(l => l.CategoryId).OnDelete(DeleteBehavior.Restrict);
        link.HasIndex(l => l.ProjectId);

        link.HasAuditLabel(l => l.Title);
        link.Property(l => l.ProjectId).IsAuditReference(ProjectsAudit.ProjectReference).IsAuditContext();
        link.Property(l => l.CategoryId).IsAuditReference(DocumentsAudit.LinkCategoryReference);
        link.Property(l => l.CreatedById).ExcludeFromAudit();
        link.Property(l => l.CreatedAt).ExcludeFromAudit();
        link.Property(l => l.UpdatedById).ExcludeFromAudit();
        link.Property(l => l.UpdatedAt).ExcludeFromAudit();
    }
}
