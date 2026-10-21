using System.Data;
using System.Globalization;
using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

/// <summary>
/// The deletion of a project in the database (ADR-0041, ADR-0111): in one serializable transaction, repeated from scratch
/// when the database picks it as a deadlock victim, each module removes its rows through this connection, then the
/// schedule, the people and the project go, children before parents, and the audit gets one record: the project as it
/// was and how many items of each kind were removed. The rows removed make no records of their own.
/// </summary>
internal sealed class ProjectDeletionStore(
    ProjectsDbContext db, IEnumerable<IProjectDeletion> modules, IEnumerable<IChangeAuditor> auditors) : IProjectDeletionStore
{
    public Task DeleteAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async token =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var removed = new Dictionary<ProjectItemKind, int>();
            foreach (var module in modules)
            {
                foreach (var count in await module.DeleteDataAsync(project.Id, transaction.GetDbTransaction(), token))
                {
                    removed[count.Kind] = removed.GetValueOrDefault(count.Kind) + count.Count;
                }
            }

            var (items, members) = await DeleteOwnAsync(project.Id, token);
            removed[ProjectItemKind.ScheduleItems] = items;
            removed[ProjectItemKind.Members] = members;
            if (auditors.FirstOrDefault() is { } auditor)
            {
                await auditor.RecordAsync(db, [RecordOf(project, removed)], token);
            }

            await transaction.CommitAsync(token);
        }, cancellationToken);
    }

    private async Task<(int Items, int Members)> DeleteOwnAsync(Guid id, CancellationToken cancellationToken)
    {
        await db.ScheduleDependencies.Where(dependency => dependency.ProjectId == id).ExecuteDeleteAsync(cancellationToken);
        await db.Set<ScheduleBaselineItem>().Where(item => item.ProjectId == id).ExecuteDeleteAsync(cancellationToken);
        await db.ScheduleBaselines.Where(baseline => baseline.Id == id).ExecuteDeleteAsync(cancellationToken);
        await db.ScheduleChanges.Where(change => change.ProjectId == id).ExecuteDeleteAsync(cancellationToken);
        var items = await db.ScheduleItems.Where(item => item.ProjectId == id).ExecuteDeleteAsync(cancellationToken);
        await db.SchedulePhases.Where(phase => phase.ProjectId == id).ExecuteDeleteAsync(cancellationToken);
        await db.Schedules.Where(schedule => schedule.Id == id).ExecuteDeleteAsync(cancellationToken);
        var members = await db.ProjectMembers.Where(member => member.ProjectId == id).ExecuteDeleteAsync(cancellationToken);
        await db.Projects.Where(project => project.Id == id).ExecuteDeleteAsync(cancellationToken);
        return (items, members);
    }

    /// <summary>The project before the deletion, and the items removed of each kind, as fields of one record.</summary>
    private static AuditedChange RecordOf(Project project, Dictionary<ProjectItemKind, int> removed) =>
        new(
            ProjectsDbContext.Schema,
            nameof(Project),
            project.Id,
            AuditedAction.Deleted,
            project.Name,
            [
                new AuditedField(nameof(Project.Code), project.Code, null, false),
                new AuditedField(nameof(Project.Name), project.Name, null, false),
                new AuditedField(nameof(Project.ClientId), project.ClientId?.ToString(), null, false, ProjectsAudit.ClientReference),
                new AuditedField(nameof(Project.Status), project.Status.ToString(), null, false),
                .. Enum.GetValues<ProjectItemKind>().Select(kind => new AuditedField(
                    kind.ToString(), null, removed.GetValueOrDefault(kind).ToString(CultureInfo.InvariantCulture), false)),
            ]);
}
