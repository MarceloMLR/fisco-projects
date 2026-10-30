using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Contracts;
using FiscoProjects.TimeTracking.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.TimeTracking.Infrastructure.Persistence;

// The time entries (ADR-0128): in the audit by the description, with the project in every record and the person, the
// task and the ticket by their current names (ADR-0084); when they were made stays out, as the audit records it. Only
// the id of the person is kept, never the name or the e-mail, so the anonymization has nothing to erase here.
internal sealed class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> entry)
    {
        entry.ToTable("time_entries");
        entry.Property(e => e.Hours).HasPrecision(4, 2);
        entry.Property(e => e.Description).HasMaxLength(TimeEntry.DescriptionMaxLength);
        entry.HasIndex(e => new { e.UserId, e.Day });
        entry.HasIndex(e => e.ProjectId);

        entry.HasAuditLabel(e => e.Description);
        entry.Property(e => e.ProjectId).IsAuditReference(ProjectsAudit.ProjectReference).IsAuditContext();
        entry.Property(e => e.UserId).IsAuditReference(UserAudit.Reference);
        entry.Property(e => e.ScheduleItemId).IsAuditReference(ProjectsAudit.ScheduleItemReference);
        entry.Property(e => e.TicketId).IsAuditReference(TicketsAudit.TicketReference);
        entry.Property(e => e.CreatedAt).ExcludeFromAudit();
        entry.Property(e => e.UpdatedAt).ExcludeFromAudit();
    }
}
