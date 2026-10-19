using System.Data;
using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

internal sealed class ProjectStore(ProjectsDbContext db) : IProjectStore
{
    private const string AccentInsensitive = "Latin1_General_100_CI_AI";

    public async Task<ProjectListRows> ListAsync(
        ProjectListQuery query, ProjectViewer viewer, CancellationToken cancellationToken)
    {
        var projects = db.Projects.AsNoTracking();
        if (!viewer.IsAdministrator || query.Mine)
        {
            projects = projects.Where(project => project.Members.Any(member => member.UserId == viewer.UserId));
        }

        if (query.Search is { } search)
        {
            // Without telling uppercase, lowercase and accents apart: "erp" finds "ERP-FIN" and "Implantação do ERP".
            projects = projects.Where(project =>
                EF.Functions.Collate(project.Name, AccentInsensitive).Contains(search)
                || EF.Functions.Collate(project.Code, AccentInsensitive).Contains(search));
        }

        if (query.ClientId is { } clientId)
        {
            projects = projects.Where(project => project.ClientId == clientId);
        }

        projects = query.Situation switch
        {
            ProjectSituation.Active => projects.Where(project =>
                project.Status != ProjectStatus.Completed && project.Status != ProjectStatus.Cancelled),
            ProjectSituation.Closed => projects.Where(project =>
                project.Status == ProjectStatus.Completed || project.Status == ProjectStatus.Cancelled),
            _ => projects,
        };

        var total = await projects.CountAsync(cancellationToken);
        var rows = await projects
            .OrderBy(project => project.Name).ThenBy(project => project.Code)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(project => new
            {
                project.Id,
                project.Code,
                project.Name,
                project.ClientId,
                project.Status,
                Managers = project.Members.Where(member => member.Role == ProjectRole.Manager).Select(member => member.UserId).ToList(),
            })
            .ToListAsync(cancellationToken);

        var clientIds = rows.Where(row => row.ClientId is not null).Select(row => row.ClientId!.Value).Distinct().ToList();
        var clients = await db.Clients.AsNoTracking()
            .Where(client => clientIds.Contains(client.Id))
            .Select(client => new ProjectClient(client.Id, client.Name, client.LogoFileId == null ? null : client.LogoChangedAt))
            .ToDictionaryAsync(client => client.Id, cancellationToken);

        return new ProjectListRows(
            [.. rows.Select(row => new ProjectListRow(
                row.Id,
                row.Code,
                row.Name,
                row.ClientId is { } id && clients.TryGetValue(id, out var client) ? client : null,
                row.Status,
                row.Managers))],
            total);
    }

    public Task<Project?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Projects.Include(project => project.Members).SingleOrDefaultAsync(project => project.Id == id, cancellationToken);

    public Task<string?> NameOfProjectWithCodeAsync(string code, Guid? except, CancellationToken cancellationToken) =>
        db.Projects.AsNoTracking()
            .Where(project => project.Code == code && project.Id != except)
            .Select(project => project.Name)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> AnyWithClientAsync(Guid clientId, CancellationToken cancellationToken) =>
        db.Projects.AnyAsync(project => project.ClientId == clientId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.Projects.AsNoTracking()
            .Where(project => ids.Contains(project.Id))
            .ToDictionaryAsync(project => project.Id, project => project.Name, cancellationToken);

    public void Add(Project project) => db.Projects.Add(project);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 } sql
            && sql.Message.Contains(ProjectConfiguration.CodeIndex, StringComparison.Ordinal))
        {
            // The changes stay as they were tried; the request ends here, with its own context.
            throw new ProjectCodeInUseException(exception);
        }
    }

    public Task<TOutcome> InSerializableTransactionAsync<TOutcome>(
        Func<CancellationToken, Task<TOutcome>> work, CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async token =>
        {
            // A repeated attempt starts from what is in the database, not from the changes of the failed one.
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var result = await work(token);
            await transaction.CommitAsync(token);
            return result;
        }, cancellationToken);
    }
}
