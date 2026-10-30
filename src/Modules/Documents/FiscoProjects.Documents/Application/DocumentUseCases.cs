using FiscoProjects.Documents.Domain;
using FiscoProjects.Files.Contracts;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Documents.Application;

/// <summary>The size of a document each installation takes (ADR-0031): 25 MB when it does not say.</summary>
public sealed record DocumentSettings(long MaxBytes)
{
    public const int DefaultMaxMegabytes = 25;
}

/// <summary>
/// Which documents a list asks for: of a folder or, with a search, of all the folders the person sees (ADR-0045).
/// </summary>
public sealed record DocumentQuery(
    Guid ProjectId,
    IReadOnlyCollection<Guid> FolderIds,
    string? Search,
    bool WithObsolete,
    int Page,
    int PageSize);

/// <summary>A page of documents and how many match in all pages.</summary>
public sealed record DocumentRows(IReadOnlyList<Document> Items, int Total);

/// <summary>Where the documents are kept.</summary>
public interface IDocumentStore
{
    /// <summary>The documents of the folders matching the query, in the order of the name.</summary>
    Task<DocumentRows> ListAsync(DocumentQuery query, CancellationToken cancellationToken);

    /// <summary>How many documents not obsolete each folder of the project has.</summary>
    Task<IReadOnlyDictionary<Guid, int>> CountsAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The document of the project, tracked, with its versions when asked.</summary>
    Task<Document?> FindAsync(Guid projectId, Guid id, bool withVersions, CancellationToken cancellationToken);

    /// <summary>The document of the folder kept in the file of the storage, tracked.</summary>
    Task<Document?> FindByFileAsync(Guid folderId, string fileId, CancellationToken cancellationToken);

    void Add(Document document);

    void Remove(Document document);

    /// <summary>Saves; throws <see cref="DocumentChangedException"/> when someone sent the same new file at the same time.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public enum DocumentOutcome
{
    Done = 1,
    NotFound = 2,
    Forbidden = 3,
    ProjectClosed = 4,
    Invalid = 5,
    Refused = 6,
    ReadOnlyFolder = 7,
    Changed = 8,
}

/// <summary>What happened to a document: its id and version, or why nothing was saved.</summary>
public sealed record DocumentResult(DocumentOutcome Outcome, Guid? Id = null, int? Version = null, FileRefusal Refusal = FileRefusal.None);

/// <summary>
/// A project as the person sees it in the documents: the project, the permissions and the folders the person sees; the
/// folder of the terms only for who manages the terms (ADR-0129).
/// </summary>
public sealed record DocumentArea(ProjectAccess Project, DocumentPermissions Permissions, IReadOnlyList<DocumentFolder> Folders)
{
    /// <summary>The folder of the project the person sees; null when it is not one of them.</summary>
    public DocumentFolder? FolderOf(Guid folderId) => Folders.FirstOrDefault(folder => folder.Id == folderId);

    /// <summary>The folder of the document, when the person sees the document; null otherwise.</summary>
    public DocumentFolder? FolderOf(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return FolderOf(document.FolderId);
    }
}

/// <summary>The project, the permissions and the folders of a person, read once for each action (ADR-0045).</summary>
public sealed class DocumentAreas(IProjectAccess projects, ProjectFolders folders)
{
    /// <summary>
    /// The area of the project; null when the person does not see the project (404). The folder of the terms is only of
    /// who manages the terms (ADR-0129).
    /// </summary>
    public async Task<DocumentArea?> OfAsync(Guid projectId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        if (await projects.FindAsync(projectId, viewer.UserId, viewer.IsAdministrator, cancellationToken) is not { } project)
        {
            return null;
        }

        var permissions = DocumentAccess.Of(viewer, project);
        var all = await folders.OfAsync(projectId, cancellationToken);
        return permissions.SeesAcceptanceFolder
            ? new(project, permissions, all)
            : new(project, permissions, [.. all.Where(folder => !folder.IsAcceptance)]);
    }
}

/// <summary>A document with the names a list shows: the folder, the people and the item of the schedule it is linked to.</summary>
public sealed record DocumentView(
    Document Document,
    string FolderName,
    string UpdatedBy,
    string StatusChangedBy,
    ScheduleItemInfo? Link,
    IReadOnlyList<DocumentVersionView> Versions);

/// <summary>A version with the name of who sent it.</summary>
public sealed record DocumentVersionView(DocumentVersion Version, string UploadedBy);

/// <summary>A page of documents of the area, with the names.</summary>
public sealed record DocumentPage(IReadOnlyList<DocumentView> Items, int Total);

/// <summary>Reads the names a list of documents shows, in one call to each source.</summary>
public sealed class DocumentViews(IUserDirectory users, IScheduleItems schedule)
{
    public async Task<IReadOnlyList<DocumentView>> OfAsync(
        DocumentArea area, IReadOnlyList<Document> documents, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(area);
        ArgumentNullException.ThrowIfNull(documents);
        if (documents.Count == 0)
        {
            return [];
        }

        var people = documents
            .SelectMany(document => document.Versions.Select(version => version.UploadedById)
                .Append(document.UpdatedById)
                .Append(document.StatusChangedById))
            .ToHashSet();
        var names = await users.NamesAsync(people, cancellationToken);
        var items = documents.Any(document => document.SchedulePhaseId is not null || document.ScheduleItemId is not null)
            ? (await schedule.ItemsAsync(area.Project.Id, cancellationToken)).ToDictionary(item => item.Id)
            : [];
        var folders = area.Folders.ToDictionary(folder => folder.Id, folder => folder.Name);
        return
        [
            .. documents.Select(document => new DocumentView(
                document,
                folders.GetValueOrDefault(document.FolderId, string.Empty),
                names.GetValueOrDefault(document.UpdatedById, string.Empty),
                names.GetValueOrDefault(document.StatusChangedById, string.Empty),
                (document.SchedulePhaseId ?? document.ScheduleItemId) is { } linked ? items.GetValueOrDefault(linked) : null,
                [.. document.Versions.OrderByDescending(version => version.Number)
                    .Select(version => new DocumentVersionView(version, names.GetValueOrDefault(version.UploadedById, string.Empty)))])),
        ];
    }
}

/// <summary>A folder of the area with how many documents not obsolete it has.</summary>
public sealed record FolderCount(DocumentFolder Folder, int Count);

/// <summary>The folders the person sees in a project, with the counts, the permissions and the size the installation takes.</summary>
public sealed record DocumentFolders(IReadOnlyList<FolderCount> Folders, int Total, DocumentPermissions Permissions, bool IsClosed, long MaxBytes);

/// <summary>
/// The folders of the documents of a project (ADR-0031): every folder the person sees, with how many documents not
/// obsolete each has, the total for the tab, and what the person may do (ADR-0045).
/// </summary>
public sealed class ListDocumentFolders(DocumentAreas areas, IDocumentStore documents, DocumentSettings settings)
{
    public async Task<DocumentFolders?> ExecuteAsync(Guid projectId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        if (await areas.OfAsync(projectId, viewer, cancellationToken) is not { } area)
        {
            return null;
        }

        var counts = await documents.CountsAsync(projectId, cancellationToken);
        var folders = area.Folders.Select(folder => new FolderCount(folder, counts.GetValueOrDefault(folder.Id))).ToList();
        return new(folders, folders.Sum(folder => folder.Count), area.Permissions, area.Project.IsClosed, settings.MaxBytes);
    }
}

/// <summary>Which documents a person asks for: a folder or, with a search, the whole project; with the obsolete ones or not.</summary>
public sealed record DocumentFilter(Guid? FolderId, string? Search, bool WithObsolete, int Page, int PageSize);

/// <summary>
/// The documents of a folder, or the ones whose name has the search in all the folders the person sees (ADR-0031), by
/// name, without the obsolete ones unless asked.
/// </summary>
public sealed class ListDocuments(DocumentAreas areas, IDocumentStore documents, DocumentViews views)
{
    public const int MaxPageSize = 100;

    public async Task<DocumentPage?> ExecuteAsync(
        Guid projectId, DocumentFilter filter, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (await areas.OfAsync(projectId, viewer, cancellationToken) is not { } area)
        {
            return null;
        }

        var search = string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim();
        IReadOnlyCollection<Guid> folderIds = filter.FolderId is { } folderId
            ? area.FolderOf(folderId) is null ? [] : [folderId]
            : search is null ? [] : [.. area.Folders.Select(folder => folder.Id)];
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, MaxPageSize);
        var rows = await documents.ListAsync(
            new DocumentQuery(projectId, folderIds, search, filter.WithObsolete, page, size),
            cancellationToken);
        return new(await views.OfAsync(area, rows.Items, cancellationToken), rows.Total);
    }
}

/// <summary>A document with its versions, as the details show it.</summary>
public sealed record DocumentDetails(DocumentView View, DocumentPermissions Permissions, bool IsClosed);

/// <summary>A document of a folder the person sees, with its versions (ADR-0031).</summary>
public sealed class GetDocument(DocumentAreas areas, IDocumentStore documents, DocumentViews views)
{
    public async Task<DocumentDetails?> ExecuteAsync(
        Guid projectId, Guid documentId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        if (await areas.OfAsync(projectId, viewer, cancellationToken) is not { } area
            || await documents.FindAsync(projectId, documentId, withVersions: true, cancellationToken) is not { } document
            || area.FolderOf(document) is null)
        {
            return null;
        }

        return new((await views.OfAsync(area, [document], cancellationToken))[0], area.Permissions, area.Project.IsClosed);
    }
}

/// <summary>A file someone sends: the name, the size and the content, which must be seekable.</summary>
public sealed record DocumentFile(string Name, long Size, Stream Content);

/// <summary>
/// Sends a file to a folder of the project (ADR-0031, ADR-0045): who sends documents, in an open project, up to the size
/// of the installation, never a program or a script. A file with the name of a document of the folder is its next
/// version (ADR-0093); the folder of the terms takes nothing here.
/// </summary>
public sealed class UploadDocument(
    DocumentAreas areas, ProjectFolders folders, IDocumentStore documents, IFileStorage files, DocumentSettings settings, TimeProvider clock)
{
    public async Task<DocumentResult> ExecuteAsync(
        Guid projectId, Guid folderId, DocumentFile file, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(viewer);
        if (await areas.OfAsync(projectId, viewer, cancellationToken) is not { } area || area.FolderOf(folderId) is not { } folder)
        {
            return new(DocumentOutcome.NotFound);
        }

        if (!area.Permissions.CanUpload)
        {
            return new(DocumentOutcome.Forbidden);
        }

        if (area.Project.IsClosed)
        {
            return new(DocumentOutcome.ProjectClosed);
        }

        if (folder.IsAcceptance)
        {
            return new(DocumentOutcome.ReadOnlyFolder);
        }

        if (Document.NameOf(file.Name) is not { } name)
        {
            return new(DocumentOutcome.Invalid);
        }

        var refusal = await FileTypes.CheckAsync(name, file.Size, file.Content, settings.MaxBytes, cancellationToken);
        if (refusal != FileRefusal.None)
        {
            return new(DocumentOutcome.Refused, Refusal: refusal);
        }

        var stored = await folders.UploadAsync(folder, area.Project, name, file.Content, SameNameAction.NewVersion, cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;
        var document = await documents.FindByFileAsync(folder.Id, stored.Id, cancellationToken);
        var isNew = document is null;
        if (document is null)
        {
            document = Document.Upload(projectId, folder.Id, stored.Name, stored.Id, stored.VersionId, stored.Size, viewer.UserId, now);
            documents.Add(document);
        }
        else
        {
            document.AddVersion(stored.VersionId, stored.Size, viewer.UserId, now);
        }

        try
        {
            await documents.SaveChangesAsync(cancellationToken);
        }
        catch (DocumentChangedException)
        {
            return new(DocumentOutcome.Changed);
        }
        catch
        {
            // A new file nobody can reach does not stay in the storage; a version too many of a document is never opened.
            if (isNew)
            {
                await files.DeleteFileAsync(stored.Id, CancellationToken.None);
            }

            throw;
        }

        return new(DocumentOutcome.Done, document.Id, document.Version);
    }
}

/// <summary>The file of a version of a document, with the name and the type the browser gets.</summary>
public sealed record OpenedDocument(FileDownload File, string Name, string ContentType);

/// <summary>Opens the current version of a document, or another one, of a folder the person sees (ADR-0031).</summary>
public sealed class OpenDocument(DocumentAreas areas, IDocumentStore documents, IFileStorage files)
{
    public async Task<OpenedDocument?> ExecuteAsync(
        Guid projectId, Guid documentId, int? version, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        if (await areas.OfAsync(projectId, viewer, cancellationToken) is not { } area
            || await documents.FindAsync(projectId, documentId, withVersions: true, cancellationToken) is not { } document
            || area.FolderOf(document) is null
            || document.Versions.FirstOrDefault(item => item.Number == (version ?? document.Version)) is not { } chosen
            || await files.OpenAsync(document.FileId, chosen.StorageVersionId, cancellationToken) is not { } file)
        {
            return null;
        }

        return new(file, document.Name, FileTypes.ContentTypeOf(document.Name));
    }
}

/// <summary>
/// Sets the status of a document (ADR-0031, ADR-0045): who sends documents sets Rascunho, Em revisão and Obsoleto; only
/// the managers approve. Who and when are kept.
/// </summary>
public sealed class ChangeDocumentStatus(DocumentAreas areas, IDocumentStore documents, TimeProvider clock)
{
    public async Task<DocumentResult> ExecuteAsync(
        Guid projectId, Guid documentId, DocumentStatus status, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var (outcome, document) = await DocumentChanges.FindAsync(areas, documents, projectId, documentId, viewer, cancellationToken);
        if (outcome != DocumentOutcome.Done)
        {
            return new(outcome);
        }

        if (!Enum.IsDefined(status))
        {
            return new(DocumentOutcome.Invalid);
        }

        if (!document!.Area.Permissions.MaySet(status))
        {
            return new(DocumentOutcome.Forbidden);
        }

        if (document.Document.Status != status)
        {
            document.Document.ChangeStatus(status, viewer.UserId, clock.GetUtcNow().UtcDateTime);
            await documents.SaveChangesAsync(cancellationToken);
        }

        return new(DocumentOutcome.Done, documentId, document.Document.Version);
    }
}

/// <summary>
/// Links a document to a phase or to a task or milestone of the schedule of its project, or takes the link away
/// (ADR-0031): who sends documents.
/// </summary>
public sealed class LinkDocument(DocumentAreas areas, IDocumentStore documents, IScheduleItems schedule)
{
    public async Task<DocumentResult> ExecuteAsync(
        Guid projectId, Guid documentId, Guid? phaseId, Guid? itemId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        var (outcome, found) = await DocumentChanges.FindAsync(areas, documents, projectId, documentId, viewer, cancellationToken);
        if (outcome != DocumentOutcome.Done)
        {
            return new(outcome);
        }

        if (!found!.Area.Permissions.CanUpload)
        {
            return new(DocumentOutcome.Forbidden);
        }

        if (phaseId is not null && itemId is not null)
        {
            return new(DocumentOutcome.Invalid);
        }

        if ((phaseId ?? itemId) is { } linked)
        {
            var items = await schedule.ItemsAsync(projectId, cancellationToken);
            var expected = phaseId is null ? (ScheduleItemType[])[ScheduleItemType.Task, ScheduleItemType.Milestone] : [ScheduleItemType.Phase];
            if (!items.Any(item => item.Id == linked && expected.Contains(item.Type)))
            {
                return new(DocumentOutcome.Invalid);
            }
        }

        found.Document.Link(phaseId, itemId);
        await documents.SaveChangesAsync(cancellationToken);
        return new(DocumentOutcome.Done, documentId, found.Document.Version);
    }
}

/// <summary>
/// Removes a document with its file and all the versions (ADR-0031, ADR-0045): only the managers. The file goes from
/// the storage once the removal is saved; a failure there is left to the storage.
/// </summary>
public sealed class RemoveDocument(DocumentAreas areas, IDocumentStore documents, IFileStorage files)
{
    public async Task<DocumentResult> ExecuteAsync(
        Guid projectId, Guid documentId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        var (outcome, found) = await DocumentChanges.FindAsync(areas, documents, projectId, documentId, viewer, cancellationToken);
        if (outcome != DocumentOutcome.Done)
        {
            return new(outcome);
        }

        if (!found!.Area.Permissions.CanDelete)
        {
            return new(DocumentOutcome.Forbidden);
        }

        documents.Remove(found.Document);
        await documents.SaveChangesAsync(cancellationToken);
        await files.DeleteFileAsync(found.Document.FileId, CancellationToken.None);
        return new(DocumentOutcome.Done, documentId);
    }
}

/// <summary>A document found for a change, with the area of the person.</summary>
internal sealed record FoundDocument(DocumentArea Area, Document Document);

/// <summary>The checks every change of a document shares: the project, the folder, the project open and the folder of the terms.</summary>
internal static class DocumentChanges
{
    public static async Task<(DocumentOutcome Outcome, FoundDocument? Found)> FindAsync(
        DocumentAreas areas, IDocumentStore documents, Guid projectId, Guid documentId, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        if (await areas.OfAsync(projectId, viewer, cancellationToken) is not { } area
            || await documents.FindAsync(projectId, documentId, withVersions: false, cancellationToken) is not { } document
            || area.FolderOf(document) is not { } folder)
        {
            return (DocumentOutcome.NotFound, null);
        }

        if (!area.Permissions.CanUpload)
        {
            return (DocumentOutcome.Forbidden, null);
        }

        if (area.Project.IsClosed)
        {
            return (DocumentOutcome.ProjectClosed, null);
        }

        // The documents of the folder of the terms are signed terms, which do not change here (ADR-0093).
        return folder.IsAcceptance ? (DocumentOutcome.ReadOnlyFolder, null) : (DocumentOutcome.Done, new FoundDocument(area, document));
    }
}
