using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

/// <summary>
/// The root of the schedule of a project, with the id of the project. Only the root deletes in cascade: SQL Server
/// refuses two cascade paths to a table, and the domain removes the items of a phase and the dependencies of an item
/// itself, so each removal is audited.
/// </summary>
internal sealed class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> schedule)
    {
        schedule.ToTable("schedules");
        schedule.Property(s => s.Id).ValueGeneratedNever();
        schedule.Ignore(s => s.ProjectId);
        schedule.HasOne<Project>().WithOne().HasForeignKey<Schedule>(s => s.Id).OnDelete(DeleteBehavior.Cascade);
        schedule.HasMany(s => s.Phases).WithOne().HasForeignKey(p => p.ProjectId).OnDelete(DeleteBehavior.Cascade);
        schedule.HasMany(s => s.Items).WithOne().HasForeignKey(i => i.ProjectId).OnDelete(DeleteBehavior.Cascade);
        schedule.HasMany(s => s.Dependencies).WithOne().HasForeignKey(d => d.ProjectId).OnDelete(DeleteBehavior.Cascade);
        schedule.Navigation(s => s.Phases).UsePropertyAccessMode(PropertyAccessMode.Field);
        schedule.Navigation(s => s.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        schedule.Navigation(s => s.Dependencies).UsePropertyAccessMode(PropertyAccessMode.Field);

        // The root has nothing of its own to audit; its phases, items and dependencies are.
        schedule.ExcludeFromAudit();
    }
}

internal sealed class SchedulePhaseConfiguration : IEntityTypeConfiguration<SchedulePhase>
{
    public void Configure(EntityTypeBuilder<SchedulePhase> phase)
    {
        phase.ToTable("schedule_phases");
        phase.Property(p => p.Name).HasMaxLength(Schedule.PhaseNameMaxLength);

        // Audit (ADR-0039, ADR-0084): the phase by its name, in the project, which comes in every change.
        phase.HasAuditLabel(p => p.Name);
        phase.Property(p => p.ProjectId).IsAuditReference(ProjectsAudit.ProjectReference).IsAuditContext();
        phase.Property(p => p.Position).ExcludeFromAudit();
    }
}

internal sealed class ScheduleItemConfiguration : IEntityTypeConfiguration<ScheduleItem>
{
    public void Configure(EntityTypeBuilder<ScheduleItem> item)
    {
        item.ToTable("schedule_items");
        item.Property(i => i.Kind).HasConversion<string>().HasMaxLength(16);
        item.Property(i => i.Name).HasMaxLength(Schedule.ItemNameMaxLength);
        item.HasOne<SchedulePhase>().WithMany().HasForeignKey(i => i.PhaseId).OnDelete(DeleteBehavior.ClientCascade);
        // The tasks of a person are found when the person leaves a project.
        item.HasIndex(i => i.OwnerId);

        // Audit (ADR-0039, ADR-0084): the item by its name, in the project, with its kind in every change, so the
        // Audit screen says whether it is a task or a milestone; the phase and the owner show by name.
        item.HasAuditLabel(i => i.Name);
        item.Property(i => i.ProjectId).IsAuditReference(ProjectsAudit.ProjectReference).IsAuditContext();
        item.Property(i => i.Kind).IsAuditContext();
        item.Property(i => i.PhaseId).IsAuditReference(ProjectsAudit.SchedulePhaseReference);
        item.Property(i => i.OwnerId).IsAuditReference(UserAudit.Reference);
    }
}

internal sealed class ScheduleDependencyConfiguration : IEntityTypeConfiguration<ScheduleDependency>
{
    public void Configure(EntityTypeBuilder<ScheduleDependency> dependency)
    {
        dependency.ToTable("schedule_dependencies");
        dependency.HasOne<ScheduleItem>().WithMany().HasForeignKey(d => d.PredecessorId).OnDelete(DeleteBehavior.ClientCascade);
        dependency.HasOne<ScheduleItem>().WithMany().HasForeignKey(d => d.SuccessorId).OnDelete(DeleteBehavior.ClientCascade);
        // One dependency per pair of items.
        dependency.HasIndex(d => new { d.PredecessorId, d.SuccessorId }).IsUnique();

        // Audit (ADR-0039, ADR-0084): the dependency by the item that comes after, and both items by name.
        dependency.HasAuditLabelReference(d => d.SuccessorId, ProjectsAudit.ScheduleItemReference);
        dependency.Property(d => d.ProjectId).IsAuditReference(ProjectsAudit.ProjectReference).IsAuditContext();
        dependency.Property(d => d.PredecessorId).IsAuditReference(ProjectsAudit.ScheduleItemReference);
        dependency.Property(d => d.SuccessorId).IsAuditReference(ProjectsAudit.ScheduleItemReference);
    }
}
