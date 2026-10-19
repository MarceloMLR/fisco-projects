using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public const string CodeIndex = "IX_projects_Code";

    public void Configure(EntityTypeBuilder<Project> project)
    {
        project.ToTable("projects");
        project.Property(p => p.Code).HasMaxLength(ProjectCode.MaxLength);
        project.Property(p => p.Name).HasMaxLength(Project.NameMaxLength);
        project.Property(p => p.Description).HasMaxLength(Project.DescriptionMaxLength);
        project.Property(p => p.Status).HasConversion<string>().HasMaxLength(16);
        project.Ignore(p => p.IsClosed);
        // One project per code, open or closed (ADR-0016).
        project.HasIndex(p => p.Code).IsUnique().HasDatabaseName(CodeIndex);
        project.HasIndex(p => p.Name);
        // A client with projects is only made inactive, never removed (ADR-0015).
        project.HasOne<Client>().WithMany().HasForeignKey(p => p.ClientId).OnDelete(DeleteBehavior.Restrict);
        project.HasMany(p => p.Members).WithOne().HasForeignKey(m => m.ProjectId).OnDelete(DeleteBehavior.Cascade);
        project.Navigation(p => p.Members).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Audit (ADR-0039, ADR-0084): the project is named by its name, and the client by its current name.
        project.HasAuditLabel(p => p.Name);
        project.Property(p => p.ClientId).IsAuditReference(ProjectsAudit.ClientReference);
        project.Property(p => p.CreatedAt).ExcludeFromAudit();
    }
}

internal sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> member)
    {
        member.ToTable("project_members");
        member.Property(m => m.Role).HasConversion<string>().HasMaxLength(16);
        // One role per person in each project (ADR-0042); the projects of a person are found by the person.
        member.HasIndex(m => new { m.ProjectId, m.UserId }).IsUnique();
        member.HasIndex(m => m.UserId);

        // Audit (ADR-0039, ADR-0084): the record is named by the person, and the project and the person show by name.
        // The project comes in every record of the person, also in a change of the role, to say where it happened.
        member.HasAuditLabelReference(m => m.UserId, UserAudit.Reference);
        member.Property(m => m.UserId).IsAuditReference(UserAudit.Reference);
        member.Property(m => m.ProjectId).IsAuditReference(ProjectsAudit.ProjectReference).IsAuditContext();
        member.Property(m => m.AddedAt).ExcludeFromAudit();
    }
}
