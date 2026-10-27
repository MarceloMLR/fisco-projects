using System.Data.Common;
using FiscoProjects.Notifications.Infrastructure.Persistence;
using FiscoProjects.Projects.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Notifications.Infrastructure;

/// <summary>
/// The notices of a project being deleted (ADR-0041, ADR-0111): the notifications of the bell and the items waiting for
/// the daily summary; the e-mails already in the queue go out.
/// </summary>
internal sealed class NotificationProjectDeletion : IProjectDeletion
{
    public Task<IReadOnlyList<ProjectItemCount>> CountAsync(Guid projectId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProjectItemCount>>([]);

    public Task DeleteFilesAsync(Guid projectId, string? projectFolderId, CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<IReadOnlyList<ProjectItemCount>> DeleteDataAsync(
        Guid projectId, DbTransaction transaction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        var options = new DbContextOptionsBuilder<NotificationsDbContext>().UseSqlServer(transaction.Connection!).Options;
        await using var notices = new NotificationsDbContext(options);
        await notices.Database.UseTransactionAsync(transaction, cancellationToken);
        await notices.Notifications.Where(item => item.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
        await notices.DigestItems.Where(item => item.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
        return [];
    }
}
