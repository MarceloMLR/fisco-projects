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
        ticket.Property(t => t.DescriptionBody).HasMaxLength(RichText.MaxJsonLength);
        ticket.Property(t => t.AttachmentFolderId).HasMaxLength(512);
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
        // The formatted description repeats the plain one, and the folder is technical.
        ticket.Property(t => t.DescriptionBody).ExcludeFromAudit();
        ticket.Property(t => t.AttachmentFolderId).ExcludeFromAudit();
    }
}

// The conversation is the history of the ticket and never changes: it stays out of the audit, which would get a record
// per message (ADR-0039).
internal sealed class TicketMessageConfiguration : IEntityTypeConfiguration<TicketMessage>
{
    public void Configure(EntityTypeBuilder<TicketMessage> message)
    {
        message.ToTable("ticket_messages");
        message.Property(m => m.Body).HasMaxLength(RichText.MaxJsonLength);
        message.Property(m => m.Text).HasMaxLength(RichText.MaxTextLength);
        message.HasOne<Ticket>().WithMany().HasForeignKey(m => m.TicketId).OnDelete(DeleteBehavior.Cascade);
        message.HasIndex(m => new { m.TicketId, m.SentAt });
        message.ExcludeFromAudit();
    }
}

internal sealed class TicketAttachmentConfiguration : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> attachment)
    {
        attachment.ToTable("ticket_attachments");
        attachment.Property(a => a.FileId).HasMaxLength(512);
        attachment.Property(a => a.Name).HasMaxLength(260);
        attachment.Property(a => a.ContentType).HasMaxLength(128);
        attachment.HasOne<Ticket>().WithMany().HasForeignKey(a => a.TicketId).OnDelete(DeleteBehavior.Cascade);
        attachment.HasOne<TicketMessage>().WithMany().HasForeignKey(a => a.MessageId).OnDelete(DeleteBehavior.NoAction);
        attachment.HasIndex(a => a.TicketId);
        attachment.ExcludeFromAudit();
    }
}

internal sealed class TicketFollowerConfiguration : IEntityTypeConfiguration<TicketFollower>
{
    public void Configure(EntityTypeBuilder<TicketFollower> follower)
    {
        follower.ToTable("ticket_followers");
        follower.HasOne<Ticket>().WithMany().HasForeignKey(f => f.TicketId).OnDelete(DeleteBehavior.Cascade);
        // One line per person and ticket; the lists look the tickets of a person up.
        follower.HasIndex(f => new { f.TicketId, f.UserId }).IsUnique();
        follower.HasIndex(f => f.UserId);
        follower.ExcludeFromAudit();
    }
}
