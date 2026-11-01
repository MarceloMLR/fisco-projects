using System.Text.Json;
using System.Text.Json.Serialization;
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
        schedule.HasOne(s => s.Baseline).WithOne().HasForeignKey<ScheduleBaseline>(b => b.Id).OnDelete(DeleteBehavior.Cascade);
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
        // The subtasks of a task (ADR-0088); the schedule removes them with the task.
        item.HasOne<ScheduleItem>().WithMany().HasForeignKey(i => i.ParentId).OnDelete(DeleteBehavior.ClientCascade);
        // The tasks of a person are found when the person leaves a project.
        item.HasIndex(i => i.OwnerId);

        // Audit (ADR-0039, ADR-0084): the item by its name, in the project, with its kind in every change, so the
        // Audit screen says whether it is a task or a milestone; the phase, the owner and the task it is in show by name.
        item.HasAuditLabel(i => i.Name);
        item.Property(i => i.ProjectId).IsAuditReference(ProjectsAudit.ProjectReference).IsAuditContext();
        item.Property(i => i.Kind).IsAuditContext();
        item.Property(i => i.PhaseId).IsAuditReference(ProjectsAudit.SchedulePhaseReference);
        item.Property(i => i.OwnerId).IsAuditReference(UserAudit.Reference);
        item.Property(i => i.ParentId).IsAuditReference(ProjectsAudit.ScheduleItemReference);
    }
}

/// <summary>
/// The baseline of a project (ADR-0021), with the id of the project, and the dates of its items. The dates of the items
/// are a copy, without a foreign key to the items: an item removed later stays in the copy unread.
/// </summary>
internal sealed class ScheduleBaselineConfiguration : IEntityTypeConfiguration<ScheduleBaseline>
{
    public void Configure(EntityTypeBuilder<ScheduleBaseline> baseline)
    {
        baseline.ToTable("schedule_baselines");
        baseline.Property(b => b.Id).ValueGeneratedNever();
        baseline.HasMany(b => b.Items).WithOne().HasForeignKey(i => i.ProjectId).OnDelete(DeleteBehavior.Cascade);
        baseline.Navigation(b => b.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Audit (ADR-0039, ADR-0084): saving and updating the baseline, by the name of the project, with the day and the
        // dates of the project before and after; the copy of the dates of each item stays out.
        baseline.HasAuditLabelReference(b => b.Id, ProjectsAudit.ProjectReference);
    }
}

internal sealed class ScheduleBaselineItemConfiguration : IEntityTypeConfiguration<ScheduleBaselineItem>
{
    public void Configure(EntityTypeBuilder<ScheduleBaselineItem> item)
    {
        item.ToTable("schedule_baseline_items");
        item.HasIndex(i => new { i.ProjectId, i.ItemId }).IsUnique();
        item.ExcludeFromAudit();
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
        dependency.Property(d => d.Type).HasConversion<string>().HasMaxLength(16);

        // Audit (ADR-0039, ADR-0084): the dependency by the item that comes after, and both items by name.
        dependency.HasAuditLabelReference(d => d.SuccessorId, ProjectsAudit.ScheduleItemReference);
        dependency.Property(d => d.ProjectId).IsAuditReference(ProjectsAudit.ProjectReference).IsAuditContext();
        dependency.Property(d => d.PredecessorId).IsAuditReference(ProjectsAudit.ScheduleItemReference);
        dependency.Property(d => d.SuccessorId).IsAuditReference(ProjectsAudit.ScheduleItemReference);
    }
}

/// <summary>
/// The changes of the schedule each person keeps to undo and redo (ADR-0091): what changed goes as JSON, read and
/// written whole. They stay out of the audit: undoing and redoing are audited by the records that change.
/// </summary>
internal sealed class ScheduleChangeConfiguration : IEntityTypeConfiguration<ScheduleChange>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public void Configure(EntityTypeBuilder<ScheduleChange> change)
    {
        change.ToTable("schedule_changes");
        change.HasOne<Schedule>().WithMany().HasForeignKey(c => c.ProjectId).OnDelete(DeleteBehavior.Cascade);
        change.HasIndex(c => new { c.ProjectId, c.ActorId, c.Number }).IsUnique();
        change.Property(c => c.Action).HasConversion<string>().HasMaxLength(32);
        change.Property(c => c.Name).HasMaxLength(Math.Max(Schedule.ItemNameMaxLength, Schedule.PhaseNameMaxLength));
        change.Property(c => c.Diff).HasConversion(diff => Write(diff), json => Read(json));
        change.Ignore(c => c.Label);
        change.ExcludeFromAudit();
    }

    private static string Write(ScheduleDiff diff) => JsonSerializer.Serialize(diff, Json);

    private static ScheduleDiff Read(string json) =>
        JsonSerializer.Deserialize<ScheduleDiff>(json, Json) ?? new ScheduleDiff([], [], []);
}
