using System.Data.Common;
using FiscoProjects.Files.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Tickets.Infrastructure;

/// <summary>
/// The tickets of a project being deleted (ADR-0041, ADR-0111): the attachments and the folders of the tickets in the
/// storage, then the tickets with their messages, attachments, events, followers, deadline alerts and registrations at
/// vendors. The numbers are not given again: they come from the sequence (ADR-0029).
/// </summary>
internal sealed class TicketProjectDeletion(TicketsDbContext db, IFileStorage files) : IProjectDeletion
{
    public async Task<IReadOnlyList<ProjectItemCount>> CountAsync(Guid projectId, CancellationToken cancellationToken) =>
        [new(ProjectItemKind.Tickets, await db.Tickets.CountAsync(ticket => ticket.ProjectId == projectId, cancellationToken))];

    public async Task DeleteFilesAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var tickets = db.Tickets.AsNoTracking().Where(ticket => ticket.ProjectId == projectId);
        var fileIds = await db.TicketAttachments.AsNoTracking()
            .Where(attachment => tickets.Select(ticket => ticket.Id).Contains(attachment.TicketId))
            .Select(attachment => attachment.FileId)
            .ToListAsync(cancellationToken);
        var folderIds = await tickets
            .Where(ticket => ticket.AttachmentFolderId != null)
            .Select(ticket => ticket.AttachmentFolderId!)
            .ToListAsync(cancellationToken);
        await files.RemoveAsync(fileIds, folderIds, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectItemCount>> DeleteDataAsync(
        Guid projectId, DbTransaction transaction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        // A context of its own on the connection of the deletion, without the audit: the deletion is one record.
        var options = new DbContextOptionsBuilder<TicketsDbContext>().UseSqlServer(transaction.Connection!).Options;
        await using var tickets = new TicketsDbContext(options);
        await tickets.Database.UseTransactionAsync(transaction, cancellationToken);

        var ids = tickets.Tickets.Where(ticket => ticket.ProjectId == projectId).Select(ticket => ticket.Id);
        // The children before the tickets; the attachments before the messages they belong to.
        await tickets.TicketAttachments.Where(item => ids.Contains(item.TicketId)).ExecuteDeleteAsync(cancellationToken);
        await tickets.TicketMessages.Where(item => ids.Contains(item.TicketId)).ExecuteDeleteAsync(cancellationToken);
        await tickets.TicketEvents.Where(item => ids.Contains(item.TicketId)).ExecuteDeleteAsync(cancellationToken);
        await tickets.TicketFollowers.Where(item => ids.Contains(item.TicketId)).ExecuteDeleteAsync(cancellationToken);
        await tickets.TicketDeadlineAlerts.Where(item => ids.Contains(item.TicketId)).ExecuteDeleteAsync(cancellationToken);
        await tickets.VendorTickets.Where(item => ids.Contains(item.TicketId)).ExecuteDeleteAsync(cancellationToken);
        var removed = await tickets.Tickets.Where(ticket => ticket.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
        return [new(ProjectItemKind.Tickets, removed)];
    }
}
