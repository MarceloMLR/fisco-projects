using System.Data.Common;
using FiscoProjects.Credentials.Infrastructure.Persistence;
using FiscoProjects.Projects.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Credentials.Infrastructure;

/// <summary>The credentials of a project being deleted (ADR-0041, ADR-0111); the keys of the passwords stay (ADR-0033).</summary>
internal sealed class CredentialProjectDeletion(CredentialsDbContext db) : IProjectDeletion
{
    public async Task<IReadOnlyList<ProjectItemCount>> CountAsync(Guid projectId, CancellationToken cancellationToken) =>
        [new(ProjectItemKind.Credentials, await db.PortalCredentials.CountAsync(item => item.ProjectId == projectId, cancellationToken))];

    public Task DeleteFilesAsync(Guid projectId, CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<IReadOnlyList<ProjectItemCount>> DeleteDataAsync(
        Guid projectId, DbTransaction transaction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        var options = new DbContextOptionsBuilder<CredentialsDbContext>().UseSqlServer(transaction.Connection!).Options;
        await using var credentials = new CredentialsDbContext(options);
        await credentials.Database.UseTransactionAsync(transaction, cancellationToken);
        var removed = await credentials.PortalCredentials.Where(item => item.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
        return [new(ProjectItemKind.Credentials, removed)];
    }
}
