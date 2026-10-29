using System.Data.Common;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.TimeTracking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.TimeTracking.Infrastructure;

/// <summary>The hours of a project being deleted (ADR-0111, ADR-0128).</summary>
internal sealed class TimeEntryProjectDeletion(TimeTrackingDbContext db) : IProjectDeletion
{
    public async Task<IReadOnlyList<ProjectItemCount>> CountAsync(Guid projectId, CancellationToken cancellationToken) =>
        [new(ProjectItemKind.TimeEntries, await db.TimeEntries.CountAsync(entry => entry.ProjectId == projectId, cancellationToken))];

    public Task DeleteFilesAsync(Guid projectId, string? projectFolderId, CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<IReadOnlyList<ProjectItemCount>> DeleteDataAsync(
        Guid projectId, DbTransaction transaction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        var options = new DbContextOptionsBuilder<TimeTrackingDbContext>().UseSqlServer(transaction.Connection!).Options;
        await using var hours = new TimeTrackingDbContext(options);
        await hours.Database.UseTransactionAsync(transaction, cancellationToken);
        var removed = await hours.TimeEntries.Where(entry => entry.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
        return [new(ProjectItemKind.TimeEntries, removed)];
    }
}
