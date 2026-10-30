using System.Data.Common;
using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Domain;
using FiscoProjects.Documents.Infrastructure.Persistence;
using FiscoProjects.Files.Contracts;
using FiscoProjects.Projects.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Documents.Infrastructure;

/// <summary>
/// The documents, links and acceptance terms of a project being deleted (ADR-0041, ADR-0111, ADR-0129): the documents of
/// the terms at the provider of signatures, cancelled when open and deleted when the provider lets, a failure there only
/// in the log; the files of the documents, with their versions, and of the terms to sign, and the folders left empty in
/// the storage; then the terms with their signers and history, the documents with their versions, the folders of the
/// project and the links.
/// </summary>
internal sealed partial class DocumentProjectDeletion(
    DocumentsDbContext db, IFileStorage files, SignatureProviders providers, ILogger<DocumentProjectDeletion> logger) : IProjectDeletion
{
    public async Task<IReadOnlyList<ProjectItemCount>> CountAsync(Guid projectId, CancellationToken cancellationToken) =>
    [
        new(ProjectItemKind.Documents, await db.Documents.CountAsync(document => document.ProjectId == projectId, cancellationToken)),
        new(ProjectItemKind.Links, await db.Links.CountAsync(link => link.ProjectId == projectId, cancellationToken)),
        new(ProjectItemKind.AcceptanceTerms, await db.AcceptanceTerms.CountAsync(term => term.ProjectId == projectId, cancellationToken)),
    ];

    public async Task DeleteFilesAsync(Guid projectId, string? projectFolderId, CancellationToken cancellationToken)
    {
        await ForgetAtProviderAsync(projectId, cancellationToken);
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
        await files.RemoveAsync(fileIds, folderIds, projectFolderId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectItemCount>> DeleteDataAsync(
        Guid projectId, DbTransaction transaction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        // A context of its own on the connection of the deletion, without the audit: the deletion is one record.
        var options = new DbContextOptionsBuilder<DocumentsDbContext>().UseSqlServer(transaction.Connection!).Options;
        await using var documents = new DocumentsDbContext(options);
        await documents.Database.UseTransactionAsync(transaction, cancellationToken);

        // The terms before the documents they point to, with their signers and history; the versions before their
        // documents; the documents before their folders.
        var terms = documents.AcceptanceTerms.Where(term => term.ProjectId == projectId).Select(term => term.Id);
        await documents.AcceptanceEvents.Where(item => terms.Contains(item.TermId)).ExecuteDeleteAsync(cancellationToken);
        await documents.AcceptanceSigners.Where(signer => terms.Contains(signer.TermId)).ExecuteDeleteAsync(cancellationToken);
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

    // The provider keeps the documents of the requests by its own contract: the open ones are cancelled, so nobody
    // signs them, and every one is deleted when the provider lets. The deletion of the project never waits for it.
    private async Task ForgetAtProviderAsync(Guid projectId, CancellationToken cancellationToken)
    {
        // The provider of the installation and, outside production, the one of the signers of the demonstration.
        foreach (var provider in providers.All)
        {
            var requests = await db.AcceptanceTerms.AsNoTracking()
                .Where(term => term.ProjectId == projectId && term.SignatureProvider == provider.Name && term.ProviderDocumentId != null)
                .Select(term => new { term.ProviderDocumentId, term.Status })
                .ToListAsync(cancellationToken);
            var failed = 0;
            foreach (var request in requests)
            {
                try
                {
                    if (request.Status == AcceptanceStatus.Pending)
                    {
                        await provider.CancelAsync(request.ProviderDocumentId!, cancellationToken);
                    }

                    await provider.DeleteAsync(request.ProviderDocumentId!, cancellationToken);
                }
                catch (SignatureProviderException)
                {
                    failed++;
                }
            }

            if (failed > 0)
            {
                LogNotForgotten(logger, failed, provider.Name);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Count} documents of a deleted project stay at the provider of signatures {Provider}.")]
    private static partial void LogNotForgotten(ILogger logger, int count, string provider);
}
