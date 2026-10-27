using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Infrastructure.Persistence;
using FiscoProjects.Files.Contracts;
using FiscoProjects.Projects.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Documents.Infrastructure;

/// <summary>
/// The documents and the acceptance terms in the folder of a project (ADR-0122): any of them is a file, so the folder
/// stays; after a change of the folder, the folders of the documents are forgotten, the empty ones leave the previous
/// folder, and they are made again in the new one.
/// </summary>
internal sealed class DocumentProjectFolderUse(
    DocumentsDbContext db, IFileStorage files, ProjectFolders projectFolders, IProjectAccess projects) : IProjectFolderUse
{
    public async Task<bool> HasFilesAsync(Guid projectId, CancellationToken cancellationToken) =>
        await db.Documents.AnyAsync(document => document.ProjectId == projectId, cancellationToken)
        || await db.AcceptanceTerms.AnyAsync(term => term.ProjectId == projectId && term.FileId != null, cancellationToken);

    public async Task FolderChangedAsync(Guid projectId, string? previousFolderId, CancellationToken cancellationToken)
    {
        var folders = await db.Folders.Where(folder => folder.ProjectId == projectId).ToListAsync(cancellationToken);
        var previous = folders.Select(folder => folder.StorageFolderId).OfType<string>().ToList();
        foreach (var folder in folders)
        {
            folder.ForgetStorage();
        }

        await db.SaveChangesAsync(cancellationToken);
        await files.RemoveAsync([], previous, previousFolderId, cancellationToken);

        if (await projects.FindAsync(projectId, Guid.Empty, isAdministrator: true, cancellationToken) is not { } project)
        {
            return;
        }

        foreach (var folder in folders)
        {
            await projectFolders.StorageOfAsync(folder, project, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
