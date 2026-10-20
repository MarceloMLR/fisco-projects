using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

internal sealed class TemplateStore(ProjectsDbContext db) : ITemplateStore
{
    public Task<ProjectTemplate?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        WithParts().SingleOrDefaultAsync(template => template.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProjectTemplate>> ListAsync(bool activeOnly, CancellationToken cancellationToken) =>
        await WithParts()
            .AsNoTracking()
            .Where(template => !activeOnly || template.IsActive)
            .OrderBy(template => template.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> NameInUseAsync(string name, Guid? except, CancellationToken cancellationToken) =>
        db.ProjectTemplates.AnyAsync(template => template.Name == name && template.Id != except, cancellationToken);

    public void Add(ProjectTemplate template) => db.ProjectTemplates.Add(template);

    public void Remove(ProjectTemplate template) => db.ProjectTemplates.Remove(template);

    private IQueryable<ProjectTemplate> WithParts() =>
        db.ProjectTemplates
            .Include(template => template.Phases)
            .Include(template => template.Items)
            .Include(template => template.Dependencies)
            .AsSplitQuery();
}
