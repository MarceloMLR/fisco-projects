using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Projects.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

/// <summary>
/// The models of project (ADR-0017), with their phases, items and dependencies, removed with the model. The model is
/// audited by its name; its parts are a copy of a schedule and stay out of the audit (ADR-0039).
/// </summary>
internal sealed class ProjectTemplateConfiguration : IEntityTypeConfiguration<ProjectTemplate>
{
    public void Configure(EntityTypeBuilder<ProjectTemplate> template)
    {
        template.ToTable("project_templates");
        template.Property(t => t.Name).HasMaxLength(ProjectTemplate.NameMaxLength);
        template.Property(t => t.Description).HasMaxLength(ProjectTemplate.DescriptionMaxLength);
        template.HasIndex(t => t.Name).IsUnique();
        template.HasMany(t => t.Phases).WithOne().HasForeignKey(p => p.TemplateId).OnDelete(DeleteBehavior.Cascade);
        template.HasMany(t => t.Items).WithOne().HasForeignKey(i => i.TemplateId).OnDelete(DeleteBehavior.Cascade);
        template.HasMany(t => t.Dependencies).WithOne().HasForeignKey(d => d.TemplateId).OnDelete(DeleteBehavior.Cascade);
        template.Navigation(t => t.Phases).UsePropertyAccessMode(PropertyAccessMode.Field);
        template.Navigation(t => t.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        template.Navigation(t => t.Dependencies).UsePropertyAccessMode(PropertyAccessMode.Field);
        template.HasAuditLabel(t => t.Name);
    }
}

internal sealed class ProjectTemplatePhaseConfiguration : IEntityTypeConfiguration<ProjectTemplatePhase>
{
    public void Configure(EntityTypeBuilder<ProjectTemplatePhase> phase)
    {
        phase.ToTable("project_template_phases");
        phase.Property(p => p.Name).HasMaxLength(Schedule.PhaseNameMaxLength);
        phase.ExcludeFromAudit();
    }
}

internal sealed class ProjectTemplateItemConfiguration : IEntityTypeConfiguration<ProjectTemplateItem>
{
    public void Configure(EntityTypeBuilder<ProjectTemplateItem> item)
    {
        item.ToTable("project_template_items");
        item.Property(i => i.Kind).HasConversion<string>().HasMaxLength(16);
        item.Property(i => i.Name).HasMaxLength(Schedule.ItemNameMaxLength);
        item.ExcludeFromAudit();
    }
}

internal sealed class ProjectTemplateDependencyConfiguration : IEntityTypeConfiguration<ProjectTemplateDependency>
{
    public void Configure(EntityTypeBuilder<ProjectTemplateDependency> dependency)
    {
        dependency.ToTable("project_template_dependencies");
        dependency.Property(d => d.Type).HasConversion<string>().HasMaxLength(16);
        dependency.ExcludeFromAudit();
    }
}
