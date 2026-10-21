using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Domain;
using FiscoProjects.Documents.Infrastructure.Persistence;
using FiscoProjects.MyWork.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Documents.Infrastructure;

/// <summary>
/// The reads of the tabs Documentos, Links and Termos de aceite (ADR-0106), as the manager of the first project with
/// folders sees them, and the terms of Meu trabalho. Only a project that already has its folders, so the warm-up never
/// makes them (ProjectFolders).
/// </summary>
internal sealed class DocumentsWarmUp(
    DocumentsDbContext db,
    IProjectAccess projects,
    ListDocumentFolders folders,
    ListDocuments documents,
    ListLinks links,
    ListAcceptances acceptances,
    TimeProvider clock,
    IEnumerable<IMyWorkSource> work) : IWarmUp
{
    public string Module => "Documents";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var projectId = await db.Folders.AsNoTracking()
            .OrderBy(folder => folder.ProjectId)
            .Select(folder => (Guid?)folder.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);
        if (projectId is not { } project)
        {
            return;
        }

        var people = await projects.MembersAsync([project], cancellationToken);
        if (people.FirstOrDefault(person => person.Role == ProjectMemberRole.Manager) is not { } manager)
        {
            return;
        }

        var viewer = new DocumentViewer(manager.UserId, IsAdministrator: false);
        var shown = await folders.ExecuteAsync(project, viewer, cancellationToken);
        if (shown?.Folders.FirstOrDefault() is { } first)
        {
            await documents.ExecuteAsync(project, new DocumentFilter(first.Folder.Id, null, false, 1, 50), viewer, cancellationToken);
        }

        await links.ExecuteAsync(project, null, null, viewer, cancellationToken);
        await acceptances.ExecuteAsync(project, null, viewer, cancellationToken);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        foreach (var source in work.OfType<AcceptanceWorkSource>())
        {
            await source.ItemsAsync(new MyWorkQuery(manager.UserId, false, today, today.AddDays(7)), cancellationToken);
        }
    }
}
