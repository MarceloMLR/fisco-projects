using FiscoProjects.Projects.Application;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

/// <summary>
/// The folder of each project, written straight to its row, without the tracking nor the audit of the project
/// (ADR-0121): a file of another module never saves the project.
/// </summary>
internal sealed class ProjectFolderStore(ProjectsDbContext db) : IProjectFolderStore
{
    public Task<ProjectFolderInfo?> FindAsync(Guid projectId, CancellationToken cancellationToken) =>
        db.Projects.AsNoTracking()
            .Where(project => project.Id == projectId)
            .Select(project => new ProjectFolderInfo(project.Code, project.Name, project.StorageFolderId, project.StorageFolderName))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<string?> KeepAsync(Guid projectId, string folderId, string folderName, CancellationToken cancellationToken)
    {
        await db.Projects
            .Where(project => project.Id == projectId && project.StorageFolderId == null)
            .ExecuteUpdateAsync(
                update => update
                    .SetProperty(project => project.StorageFolderId, folderId)
                    .SetProperty(project => project.StorageFolderName, folderName),
                cancellationToken);
        return await db.Projects.AsNoTracking()
            .Where(project => project.Id == projectId)
            .Select(project => project.StorageFolderId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task ForgetAsync(Guid projectId, string folderId, CancellationToken cancellationToken) =>
        db.Projects
            .Where(project => project.Id == projectId && project.StorageFolderId == folderId)
            .ExecuteUpdateAsync(
                update => update
                    .SetProperty(project => project.StorageFolderId, (string?)null)
                    .SetProperty(project => project.StorageFolderName, (string?)null)
                    .SetProperty(project => project.StorageFolderChosen, false),
                cancellationToken);

    public async Task<IReadOnlyList<ProjectFolderOwner>> OwnersAsync(CancellationToken cancellationToken) =>
        await db.Projects.AsNoTracking()
            .Where(project => project.StorageFolderId != null)
            .Select(project => new ProjectFolderOwner(project.Id, project.Code, project.StorageFolderId!))
            .ToListAsync(cancellationToken);

    public Task RenamedAsync(Guid projectId, string folderName, CancellationToken cancellationToken) =>
        db.Projects
            .Where(project => project.Id == projectId)
            .ExecuteUpdateAsync(update => update.SetProperty(project => project.StorageFolderName, folderName), cancellationToken);
}
