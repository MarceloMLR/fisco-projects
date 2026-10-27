using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Tickets.Infrastructure;

/// <summary>
/// The attachments of the tickets in the folder of a project (ADR-0122): any of them is a file, so the folder stays;
/// without attachments, a ticket only forgets a folder it kept, which is made again in the new folder with the next one.
/// </summary>
internal sealed class TicketProjectFolderUse(TicketsDbContext db) : IProjectFolderUse
{
    public Task<bool> HasFilesAsync(Guid projectId, CancellationToken cancellationToken) =>
        db.TicketAttachments.AnyAsync(
            attachment => db.Tickets.Any(ticket => ticket.Id == attachment.TicketId && ticket.ProjectId == projectId),
            cancellationToken);

    public Task FolderChangedAsync(Guid projectId, string? previousFolderId, CancellationToken cancellationToken) =>
        db.Tickets
            .Where(ticket => ticket.ProjectId == projectId && ticket.AttachmentFolderId != null)
            .ExecuteUpdateAsync(update => update.SetProperty(ticket => ticket.AttachmentFolderId, (string?)null), cancellationToken);
}
