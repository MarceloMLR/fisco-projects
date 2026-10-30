using FiscoProjects.Documents.Contracts;
using FiscoProjects.Documents.Infrastructure.Persistence;
using FiscoProjects.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Documents.Infrastructure;

/// <summary>The names of the folders of the projects that audit records point to (ADR-0084).</summary>
internal sealed class FolderReferenceNames(DocumentsDbContext db) : IAuditReferenceNames
{
    public string Kind => DocumentsAudit.FolderReference;

    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.Folders.AsNoTracking()
            .Where(folder => ids.Contains(folder.Id))
            .ToDictionaryAsync(folder => folder.Id, folder => folder.Name, cancellationToken);
}

/// <summary>The names of the categories of links that audit records point to (ADR-0084).</summary>
internal sealed class LinkCategoryReferenceNames(DocumentsDbContext db) : IAuditReferenceNames
{
    public string Kind => DocumentsAudit.LinkCategoryReference;

    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.LinkCategories.AsNoTracking()
            .Where(category => ids.Contains(category.Id))
            .ToDictionaryAsync(category => category.Id, category => category.Name, cancellationToken);
}

/// <summary>The titles of the acceptance terms that audit records point to, as the signers of a request (ADR-0084).</summary>
internal sealed class AcceptanceTermReferenceNames(DocumentsDbContext db) : IAuditReferenceNames
{
    public string Kind => DocumentsAudit.AcceptanceTermReference;

    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.AcceptanceTerms.AsNoTracking()
            .Where(term => ids.Contains(term.Id))
            .ToDictionaryAsync(term => term.Id, term => term.Title, cancellationToken);
}
