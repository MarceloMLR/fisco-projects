using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Documents.Infrastructure.Persistence;

/// <summary>The categories of links and the links of the projects, in the tables of the module.</summary>
internal sealed class LinkStore(DocumentsDbContext db) : ILinkStore
{
    public async Task<IReadOnlyList<LinkCategory>> CategoriesAsync(bool activeOnly, CancellationToken cancellationToken) =>
        await db.LinkCategories.AsNoTracking()
            .Where(category => !activeOnly || category.IsActive)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

    public Task<LinkCategory?> FindCategoryAsync(Guid id, CancellationToken cancellationToken) =>
        db.LinkCategories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task<bool> CategoryNameInUseAsync(string name, Guid? except, CancellationToken cancellationToken) =>
        db.LinkCategories.AnyAsync(category => category.Name == name && category.Id != except, cancellationToken);

    public void AddCategory(LinkCategory category) => db.LinkCategories.Add(category);

    public async Task<IReadOnlyList<ProjectLink>> LinksAsync(Guid projectId, CancellationToken cancellationToken) =>
        await db.Links.AsNoTracking().Where(link => link.ProjectId == projectId).ToListAsync(cancellationToken);

    public Task<ProjectLink?> FindLinkAsync(Guid projectId, Guid id, CancellationToken cancellationToken) =>
        db.Links.SingleOrDefaultAsync(link => link.Id == id && link.ProjectId == projectId, cancellationToken);

    public void AddLink(ProjectLink link) => db.Links.Add(link);

    public void RemoveLink(ProjectLink link) => db.Links.Remove(link);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 } sql
            && sql.Message.Contains(LinkCategoryConfiguration.NameIndex, StringComparison.Ordinal))
        {
            throw new LinkCategoryNameInUseException(exception);
        }
    }
}
