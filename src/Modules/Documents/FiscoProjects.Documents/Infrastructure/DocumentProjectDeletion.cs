using System.Data.Common;
using FiscoProjects.Documents.Infrastructure.Persistence;
using FiscoProjects.Files.Contracts;
using FiscoProjects.Projects.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Documents.Infrastructure;

/// <summary>
/// The documents, links and acceptance terms of a project being deleted (ADR-0041, ADR-0111): the files of the documents,
/// with their versions, and of the terms to sign, and the folders left empty in the storage; then the terms with their
/// history, the documents with their versions, the folders of the project and the links.
/// </summary>
internal sealed class DocumentProjectDeletion(DocumentsDbContext db, IFileStorage files) : IProjectDeletion
{
    public async Task<IReadOnlyList<ProjectItemCount>> CountAsync(Guid projectId, CancellationToken cancellationToken) =>
    [
        new(ProjectItemKind.Documents, await db.Documents.CountAsync(document => document.ProjectId == projectId, cancellationToken)),
        new(ProjectItemKind.Links, await db.Links.CountAsync(link => link.ProjectId == projectId, cancellationToken)),
        new(ProjectItemKind.AcceptanceTerms, await db.AcceptanceTerms.CountAsync(term => term.ProjectId == projectId, cancellationToken)),
    ];

    public async Task DeleteFilesAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var fileIds = await db.Documents.AsNoTracking()
            .Where(document => document.ProjectId == projectId)
            .Select(document => document.FileId)
            .ToListAsync(cancellationToken);
        fileIds.AddRange(await db.AcceptanceTerms.AsNoTracking()
            .Where(term => term.ProjectId == projectId && term.FileId != null)
            .Select(term => term.FileId!)
            .ToListAsync(cancellationToken));
        var folderIds = await db.Folders.AsNoTracking()
            .Where(folder => folder.ProjectId == projectId && folder.StorageFolderId != null)
            .Select(folder => folder.StorageFolderId!)
            .ToListAsync(cancellationToken);
        await files.RemoveAsync(fileIds, folderIds, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectItemCount>> DeleteDataAsync(
        Guid projectId, DbTransaction transaction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        // A context of its own on the connection of the deletion, without the audit: the deletion is one record.
        var options = new DbContextOptionsBuilder<DocumentsDbContext>().UseSqlServer(transaction.Connection!).Options;
        await using var documents = new DocumentsDbContext(options);
        await documents.Database.UseTransactionAsync(transaction, cancellationToken);

        // The terms before the documents they point to; the versions before their documents; the documents before
        // their folders.
        var terms = documents.AcceptanceTerms.Where(term => term.ProjectId == projectId).Select(term => term.Id);
        await documents.AcceptanceEvents.Where(item => terms.Contains(item.TermId)).ExecuteDeleteAsync(cancellationToken);
        var removedTerms = await documents.AcceptanceTerms.Where(term => term.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
        var ids = documents.Documents.Where(document => document.ProjectId == projectId).Select(document => document.Id);
        await documents.DocumentVersions.Where(version => ids.Contains(version.DocumentId)).ExecuteDeleteAsync(cancellationToken);
        var removedDocuments = await documents.Documents.Where(document => document.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
        await documents.Folders.Where(folder => folder.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
        var removedLinks = await documents.Links.Where(link => link.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
        return
        [
            new(ProjectItemKind.Documents, removedDocuments),
            new(ProjectItemKind.Links, removedLinks),
            new(ProjectItemKind.AcceptanceTerms, removedTerms),
        ];
    }
}
