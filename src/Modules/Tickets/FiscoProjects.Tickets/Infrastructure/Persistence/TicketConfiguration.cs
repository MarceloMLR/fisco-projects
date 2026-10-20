using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Tickets.Infrastructure.Persistence;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> ticket)
    {
        ticket.ToTable("tickets");
        ticket.Property(t => t.Title).HasMaxLength(Ticket.TitleMaxLength);
        ticket.Property(t => t.Description).HasMaxLength(Ticket.DescriptionMaxLength);
        ticket.Property(t => t.Team).HasConversion<string>().HasMaxLength(16);
        ticket.Property(t => t.Priority).HasConversion<string>().HasMaxLength(16);
        ticket.Property(t => t.Status).HasConversion<string>().HasMaxLength(24);
        ticket.Ignore(t => t.IsOpen);
        // One number per ticket in the installation (ADR-0029); the lists look tickets up by project and by person.
        ticket.HasIndex(t => t.Number).IsUnique();
        ticket.HasIndex(t => t.ProjectId);
        ticket.HasIndex(t => t.RequesterId);
        ticket.HasIndex(t => t.AssigneeId);

        // Audit (ADR-0039, ADR-0084): the ticket is named by its title, with the number among the fields, and the project
        // comes in every record, to say where it happened.
        ticket.HasAuditLabel(t => t.Title);
        ticket.Property(t => t.ProjectId).IsAuditReference(ProjectsAudit.ProjectReference).IsAuditContext();
        ticket.Property(t => t.RequesterId).IsAuditReference(UserAudit.Reference);
        ticket.Property(t => t.AssigneeId).IsAuditReference(UserAudit.Reference);
        ticket.Property(t => t.OpenedAt).ExcludeFromAudit();
        ticket.Property(t => t.UpdatedAt).ExcludeFromAudit();
    }
}
