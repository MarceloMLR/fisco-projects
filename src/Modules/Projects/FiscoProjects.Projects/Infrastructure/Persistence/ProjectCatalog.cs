using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

/// <summary>Every project with its status and client, read from the tables of the module for the other modules.</summary>
internal sealed class ProjectCatalog(ProjectsDbContext db) : IProjectCatalog
{
    public async Task<IReadOnlyList<ProjectCard>> AllAsync(CancellationToken cancellationToken) =>
        [.. (await Rows(db.Projects.AsNoTracking()).ToListAsync(cancellationToken))
            .OrderBy(row => row.Code, StringComparer.Ordinal)
            .Select(CardOf)];

    public async Task<ProjectCard?> FindAsync(Guid projectId, CancellationToken cancellationToken) =>
        await Rows(db.Projects.AsNoTracking().Where(project => project.Id == projectId)).SingleOrDefaultAsync(cancellationToken) is { } row
            ? CardOf(row)
            : null;

    private IQueryable<Row> Rows(IQueryable<Project> projects) =>
        from project in projects
        join client in db.Clients on project.ClientId equals client.Id into clients
        from client in clients.DefaultIfEmpty()
        select new Row(project.Id, project.Code, project.Name, project.Status, client == null ? null : client.Name);

    private static ProjectCard CardOf(Row row) => new(row.Id, row.Code, row.Name, (ProjectCardStatus)row.Status, row.ClientName);

    private sealed record Row(Guid Id, string Code, string Name, ProjectStatus Status, string? ClientName);
}
