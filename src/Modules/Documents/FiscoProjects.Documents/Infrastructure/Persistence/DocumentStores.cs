using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Documents.Infrastructure.Persistence;

/// <summary>The folders and the documents, in the tables of the module, saved together.</summary>
internal sealed class DocumentStores(DocumentsDbContext db) : IFolderStore, IDocumentStore
{
    public async Task<IReadOnlyList<DefaultFolder>> DefaultFoldersAsync(CancellationToken cancellationToken) =>
        await db.DefaultFolders.OrderBy(folder => folder.Name).ToListAsync(cancellationToken);

    public Task<DefaultFolder?> FindDefaultFolderAsync(Guid id, CancellationToken cancellationToken) =>
        db.DefaultFolders.SingleOrDefaultAsync(folder => folder.Id == id, cancellationToken);

    public Task<bool> DefaultFolderNameInUseAsync(string name, Guid? except, CancellationToken cancellationToken) =>
        db.DefaultFolders.AnyAsync(folder => folder.Name == name && folder.Id != except, cancellationToken);

    public void AddDefaultFolder(DefaultFolder folder) => db.DefaultFolders.Add(folder);

    public async Task<IReadOnlyList<DocumentFolder>> FoldersAsync(Guid projectId, CancellationToken cancellationToken) =>
        await db.Folders.Where(folder => folder.ProjectId == projectId).OrderBy(folder => folder.Name).ToListAsync(cancellationToken);

    public void AddFolders(IEnumerable<DocumentFolder> folders) => db.Folders.AddRange(folders);

    public async Task<DocumentRows> ListAsync(DocumentQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var documents = db.Documents.AsNoTracking()
            .Where(document => document.ProjectId == query.ProjectId && query.FolderIds.Contains(document.FolderId));
        if (!query.WithObsolete)
        {
            documents = documents.Where(document => document.Status != DocumentStatus.Obsolete);
        }

        if (query.Search is { } search)
        {
            documents = documents.Where(document => document.Name.Contains(search));
        }

        var total = await documents.CountAsync(cancellationToken);
        var items = await documents
            .OrderBy(document => document.Name)
            .ThenBy(document => document.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        return new(items, total);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountsAsync(Guid projectId, CancellationToken cancellationToken) =>
        await db.Documents.AsNoTracking()
            .Where(document => document.ProjectId == projectId && document.Status != DocumentStatus.Obsolete)
            .GroupBy(document => document.FolderId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);

    public Task<Document?> FindAsync(Guid projectId, Guid id, bool withVersions, CancellationToken cancellationToken)
    {
        IQueryable<Document> documents = withVersions ? db.Documents.Include(document => document.Versions) : db.Documents;
        return documents.SingleOrDefaultAsync(document => document.Id == id && document.ProjectId == projectId, cancellationToken);
    }

    public Task<Document?> FindByFileAsync(Guid folderId, string fileId, CancellationToken cancellationToken) =>
        db.Documents.SingleOrDefaultAsync(document => document.FolderId == folderId && document.FileId == fileId, cancellationToken);

    public void Add(Document document) => db.Documents.Add(document);

    public void Remove(Document document) => db.Documents.Remove(document);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 } sql)
        {
            if (sql.Message.Contains(DefaultFolderConfiguration.NameIndex, StringComparison.Ordinal))
            {
                throw new DefaultFolderNameInUseException(exception);
            }

            throw new DocumentChangedException(exception);
        }
    }

    public void Discard() => db.ChangeTracker.Clear();
}
