using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Contracts;
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
        // The moments of the status repeat the status, which the audit already has.
        ticket.Property(t => t.ResolvedAt).ExcludeFromAudit();
        ticket.Property(t => t.ClosedAt).ExcludeFromAudit();
        ticket.HasIndex(t => new { t.Status, t.ResolvedAt });
        // The deadlines follow the priority and the status, which the audit already has (ADR-0025).
        ticket.Property(t => t.FirstResponseDays).ExcludeFromAudit();
        ticket.Property(t => t.ResolutionDays).ExcludeFromAudit();
        ticket.Property(t => t.FirstResponseDueOn).ExcludeFromAudit();
        ticket.Property(t => t.ResolutionDueOn).ExcludeFromAudit();
        ticket.Property(t => t.FirstRespondedAt).ExcludeFromAudit();
        ticket.Property(t => t.PausedSince).ExcludeFromAudit();
        ticket.Property(t => t.PausedBusinessDays).ExcludeFromAudit();
        // Two changes at once never overwrite each other: the second finds the ticket changed (TicketChangedException).
        ticket.Property<byte[]>("Version").IsRowVersion().ExcludeFromAudit();
    }
}

// The alerts of the deadlines told (ADR-0037): technical, out of the audit, apart from the ticket, so telling them never
// finds a ticket changed by a person at the same time.
internal sealed class TicketDeadlineAlertConfiguration : IEntityTypeConfiguration<TicketDeadlineAlert>
{
    public void Configure(EntityTypeBuilder<TicketDeadlineAlert> alert)
    {
        alert.ToTable("ticket_deadline_alerts");
        alert.HasKey(a => new { a.TicketId, a.Deadline });
        alert.Property(a => a.Deadline).HasConversion<string>().HasMaxLength(16);
        alert.Property(a => a.State).HasConversion<string>().HasMaxLength(16);
        alert.HasOne<Ticket>().WithMany().HasForeignKey(a => a.TicketId).OnDelete(DeleteBehavior.Cascade);
        alert.ExcludeFromAudit();
    }
}

// The vendors of the installation (ADR-0028): changed by the Administrator, with the change in the audit; never removed.
internal sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    /// <summary>The unique index of the names, told apart from other errors when saving.</summary>
    public const string NameIndex = "IX_vendors_Name";

    public void Configure(EntityTypeBuilder<Vendor> vendor)
    {
        vendor.ToTable("vendors");
        vendor.Property(v => v.Name).HasMaxLength(Vendor.NameMaxLength);
        vendor.Property(v => v.PortalUrl).HasMaxLength(Vendor.UrlMaxLength);
        vendor.HasIndex(v => v.Name).IsUnique().HasDatabaseName(NameIndex);
        vendor.HasAuditLabel(v => v.Name);
    }
}

// The tickets at vendors, registered in our tickets (ADR-0028); each registration and update is also an event.
internal sealed class VendorTicketConfiguration : IEntityTypeConfiguration<VendorTicket>
{
    public void Configure(EntityTypeBuilder<VendorTicket> vendorTicket)
    {
        vendorTicket.ToTable("vendor_tickets");
        vendorTicket.Property(v => v.Code).HasMaxLength(VendorTicket.CodeMaxLength);
        vendorTicket.Property(v => v.Status).HasConversion<string>().HasMaxLength(16);
        vendorTicket.Property(v => v.Link).HasMaxLength(Vendor.UrlMaxLength);
        vendorTicket.Property(v => v.Note).HasMaxLength(VendorTicket.NoteMaxLength);
        vendorTicket.Ignore(v => v.IsOpen);
        vendorTicket.HasOne<Ticket>().WithMany().HasForeignKey(v => v.TicketId).OnDelete(DeleteBehavior.Cascade);
        vendorTicket.HasOne<Vendor>().WithMany().HasForeignKey(v => v.VendorId).OnDelete(DeleteBehavior.Restrict);
        vendorTicket.HasIndex(v => new { v.TicketId, v.RegisteredAt });
        // Audit (ADR-0039, ADR-0084): named by its number at the vendor, with the ticket and the vendor in every record.
        vendorTicket.HasAuditLabel(v => v.Code);
        vendorTicket.Property(v => v.TicketId).IsAuditReference(TicketsAudit.TicketReference).IsAuditContext();
        vendorTicket.Property(v => v.VendorId).IsAuditReference(TicketsAudit.VendorReference).IsAuditContext();
        vendorTicket.Property(v => v.RegisteredById).IsAuditReference(UserAudit.Reference);
        vendorTicket.Property(v => v.RegisteredAt).ExcludeFromAudit();
        vendorTicket.Property(v => v.UpdatedAt).ExcludeFromAudit();
    }
}

// The deadlines of each priority (ADR-0025): the table starts with the ones of the MVP, and the Administrator changes
// them, with the change in the audit (ADR-0039, ADR-0046).
internal sealed class TicketDeadlineSettingConfiguration : IEntityTypeConfiguration<TicketDeadlineSetting>
{
    public void Configure(EntityTypeBuilder<TicketDeadlineSetting> setting)
    {
        setting.ToTable("ticket_deadlines");
        setting.Property(d => d.Priority).HasConversion<string>().HasMaxLength(16);
        setting.HasIndex(d => d.Priority).IsUnique();
        setting.HasAuditLabel(d => d.Priority);
        setting.HasData(TicketDeadlineSetting.Defaults.Select((item, index) => new
        {
            Id = Guid.Parse($"0199f5a3-0000-7000-8000-{index + 1:D12}"),
            item.Priority,
            item.FirstResponseDays,
            item.ResolutionDays,
        }));
    }
}

// The flow of the tickets (ADR-0024): the table starts with the flow of the MVP, and has no screen to change it in the
// first version, so it stays out of the audit.
internal sealed class TicketTransitionConfiguration : IEntityTypeConfiguration<TicketTransition>
{
    public void Configure(EntityTypeBuilder<TicketTransition> transition)
    {
        transition.ToTable("ticket_transitions");
        transition.Property(t => t.From).HasConversion<string>().HasMaxLength(24);
        transition.Property(t => t.To).HasConversion<string>().HasMaxLength(24);
        transition.Property(t => t.Action).HasConversion<string>().HasMaxLength(32);
        transition.Property(t => t.Actors).HasConversion<string>().HasMaxLength(128);
        transition.ExcludeFromAudit();
        transition.HasData(TicketFlow.Default.Select((item, index) => new
        {
            Id = Guid.Parse($"0199f5a2-0000-7000-8000-{index + 1:D12}"),
            item.From,
            item.To,
            item.Action,
            item.Actors,
            item.RequiresMessage,
            item.Automatic,
        }));
    }
}

// The events are part of the conversation, which never changes: out of the audit, as the messages.
internal sealed class TicketEventConfiguration : IEntityTypeConfiguration<TicketEvent>
{
    public void Configure(EntityTypeBuilder<TicketEvent> ticketEvent)
    {
        ticketEvent.ToTable("ticket_events");
        ticketEvent.Property(e => e.Kind).HasConversion<string>().HasMaxLength(24);
        ticketEvent.Property(e => e.FromStatus).HasConversion<string>().HasMaxLength(24);
        ticketEvent.Property(e => e.ToStatus).HasConversion<string>().HasMaxLength(24);
        ticketEvent.Property(e => e.Action).HasConversion<string>().HasMaxLength(32);
        ticketEvent.Property(e => e.FromPriority).HasConversion<string>().HasMaxLength(16);
        ticketEvent.Property(e => e.ToPriority).HasConversion<string>().HasMaxLength(16);
        ticketEvent.Property(e => e.FromTeam).HasConversion<string>().HasMaxLength(16);
        ticketEvent.Property(e => e.ToTeam).HasConversion<string>().HasMaxLength(16);
        ticketEvent.Property(e => e.FromVendorStatus).HasConversion<string>().HasMaxLength(16);
        ticketEvent.Property(e => e.ToVendorStatus).HasConversion<string>().HasMaxLength(16);
        ticketEvent.Property(e => e.Note).HasMaxLength(VendorTicket.NoteMaxLength);
        ticketEvent.HasOne<Ticket>().WithMany().HasForeignKey(e => e.TicketId).OnDelete(DeleteBehavior.Cascade);
        ticketEvent.HasIndex(e => new { e.TicketId, e.At });
        ticketEvent.ExcludeFromAudit();
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
