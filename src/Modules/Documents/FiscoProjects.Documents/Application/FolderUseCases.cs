using FiscoProjects.Documents.Domain;
using FiscoProjects.Files.Contracts;
using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.Documents.Application;

/// <summary>Another default folder already has the name; nothing was saved.</summary>
public sealed class DefaultFolderNameInUseException(Exception inner) : InvalidOperationException("The name of the folder is in use.", inner);

/// <summary>Someone changed the same documents or folders at the same time; nothing was saved.</summary>
public sealed class DocumentChangedException(Exception inner) : InvalidOperationException("The documents changed at the same time.", inner);

/// <summary>Where the folders are kept: the default ones and the ones of each project.</summary>
public interface IFolderStore
{
    /// <summary>The default folders, in the order of the name.</summary>
    Task<IReadOnlyList<DefaultFolder>> DefaultFoldersAsync(CancellationToken cancellationToken);

    /// <summary>The default folder, tracked.</summary>
    Task<DefaultFolder?> FindDefaultFolderAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> DefaultFolderNameInUseAsync(string name, Guid? except, CancellationToken cancellationToken);

    void AddDefaultFolder(DefaultFolder folder);

    /// <summary>The folders of the project, tracked, in the order of the name.</summary>
    Task<IReadOnlyList<DocumentFolder>> FoldersAsync(Guid projectId, CancellationToken cancellationToken);

    void AddFolders(IEnumerable<DocumentFolder> folders);

    /// <summary>
    /// Saves who sees a folder of a project with, in the same transaction, the record of the change in the audit: the
    /// folders of the projects stay out of the automatic audit (ADR-0148).
    /// </summary>
    Task SaveVisibilityAsync(DocumentFolder folder, FolderViewers before, CancellationToken cancellationToken);

    /// <summary>
    /// Saves; throws <see cref="DefaultFolderNameInUseException"/> when another default folder took the name, and
    /// <see cref="DocumentChangedException"/> when someone made the same folders or documents at the same time.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Forgets what was not saved, after a save failed.</summary>
    void Discard();
}

public enum FolderOutcome
{
    Done = 1,
    NotFound = 2,
    Invalid = 3,
    NameInUse = 4,
    AcceptanceAlwaysActive = 5,
    AcceptanceVisibilityFixed = 6,
}

/// <summary>What happened to a default folder: its id, or why it was not saved.</summary>
public sealed record FolderResult(FolderOutcome Outcome, Guid? Id = null);

/// <summary>The default folders of the installation, for the Administration (ADR-0031, ADR-0046).</summary>
public sealed class ListDefaultFolders(IFolderStore folders)
{
    public Task<IReadOnlyList<DefaultFolder>> ExecuteAsync(CancellationToken cancellationToken) =>
        folders.DefaultFoldersAsync(cancellationToken);
}

/// <summary>
/// The Administrator includes a default folder, active, with a name no other folder has (ADR-0031), seen by the roles
/// chosen, or by all four without a choice (ADR-0148).
/// </summary>
public sealed class AddDefaultFolder(IFolderStore folders)
{
    public async Task<FolderResult> ExecuteAsync(
        string? name, IReadOnlyList<FolderRole>? visibleTo, CancellationToken cancellationToken)
    {
        if (FolderNames.Of(name) is not { } valid || (visibleTo is not null && !FolderVisibility.AreValid(visibleTo)))
        {
            return new(FolderOutcome.Invalid);
        }

        if (await folders.DefaultFolderNameInUseAsync(valid, null, cancellationToken))
        {
            return new(FolderOutcome.NameInUse);
        }

        var folder = new DefaultFolder(valid, visibleTo is null ? FolderViewers.Everyone : FolderVisibility.Of(visibleTo));
        folders.AddDefaultFolder(folder);
        return await SaveAsync(folders, folder.Id, cancellationToken);
    }

    internal static async Task<FolderResult> SaveAsync(IFolderStore folders, Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await folders.SaveChangesAsync(cancellationToken);
        }
        catch (DefaultFolderNameInUseException)
        {
            return new(FolderOutcome.NameInUse);
        }

        return new(FolderOutcome.Done, id);
    }
}

/// <summary>
/// The Administrator renames, deactivates or reactivates a default folder (ADR-0031) and changes who sees it, when the
/// roles come (ADR-0148): the change is for the new projects; the folder of the acceptance terms is never deactivated
/// (ADR-0093) and stays only of the managers.
/// </summary>
public sealed class ChangeDefaultFolder(IFolderStore folders)
{
    public async Task<FolderResult> ExecuteAsync(
        Guid id, string? name, bool isActive, IReadOnlyList<FolderRole>? visibleTo, CancellationToken cancellationToken)
    {
        var folder = await folders.FindDefaultFolderAsync(id, cancellationToken);
        if (folder is null)
        {
            return new(FolderOutcome.NotFound);
        }

        if (FolderNames.Of(name) is not { } valid || (visibleTo is not null && !FolderVisibility.AreValid(visibleTo)))
        {
            return new(FolderOutcome.Invalid);
        }

        if (await folders.DefaultFolderNameInUseAsync(valid, id, cancellationToken))
        {
            return new(FolderOutcome.NameInUse);
        }

        // The folder of the terms takes no roles: an empty list says what it already is.
        if (visibleTo is not null
            && !folder.ChangeVisibility(FolderVisibility.Of(visibleTo))
            && FolderVisibility.Of(visibleTo) != FolderViewers.None)
        {
            return new(FolderOutcome.AcceptanceVisibilityFixed);
        }

        return folder.Change(valid, isActive)
            ? await AddDefaultFolder.SaveAsync(folders, id, cancellationToken)
            : new(FolderOutcome.AcceptanceAlwaysActive);
    }
}

public enum FolderVisibilityOutcome
{
    Done = 1,
    NotFound = 2,
    Forbidden = 3,
    AcceptanceFixed = 4,
    Closed = 5,
    Invalid = 6,
}

/// <summary>
/// The Administrator changes who sees a folder of a project (ADR-0148), with the project open (ADR-0016): the other people
/// of the project are refused (403) and who does not see the project gets 404; the folder of the terms stays only of the
/// managers. The change goes to the audit with the project.
/// </summary>
public sealed class ChangeFolderVisibility(IProjectAccess projects, ProjectFolders projectFolders, IFolderStore folders)
{
    public async Task<FolderVisibilityOutcome> ExecuteAsync(
        Guid projectId, Guid folderId, IReadOnlyList<FolderRole> visibleTo, DocumentViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visibleTo);
        ArgumentNullException.ThrowIfNull(viewer);
        if (await projects.FindAsync(projectId, viewer.UserId, viewer.IsAdministrator, cancellationToken) is not { } project)
        {
            return FolderVisibilityOutcome.NotFound;
        }

        if (!DocumentAccess.MayChangeFolderVisibility(viewer))
        {
            return FolderVisibilityOutcome.Forbidden;
        }

        var folder = (await projectFolders.OfAsync(projectId, cancellationToken)).FirstOrDefault(item => item.Id == folderId);
        if (folder is null)
        {
            return FolderVisibilityOutcome.NotFound;
        }

        if (!FolderVisibility.AreValid(visibleTo))
        {
            return FolderVisibilityOutcome.Invalid;
        }

        if (folder.IsAcceptance)
        {
            return FolderVisibilityOutcome.AcceptanceFixed;
        }

        if (project.IsClosed)
        {
            return FolderVisibilityOutcome.Closed;
        }

        var before = folder.VisibleTo;
        folder.ChangeVisibility(FolderVisibility.Of(visibleTo));
        if (folder.VisibleTo != before)
        {
            await folders.SaveVisibilityAsync(folder, before, cancellationToken);
        }

        return FolderVisibilityOutcome.Done;
    }
}

/// <summary>
/// The folders of the documents of each project (ADR-0030, ADR-0031): made from the active default folders with the
/// project, or on the first need for a project made before the module; and their folders in the storage, made on the
/// first upload, at "{code} - {name}/Documentos/{folder}" under the root.
/// </summary>
public sealed class ProjectFolders(IFolderStore folders, IProjectStorage storage, IFileStorage files)
{
    /// <summary>The folder of the documents in the folder of a project, as "Chamados" for the tickets.</summary>
    public const string DocumentsFolder = "Documentos";

    /// <summary>The folders of the project, made from the active default folders when it has none.</summary>
    public async Task<IReadOnlyList<DocumentFolder>> OfAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var current = await folders.FoldersAsync(projectId, cancellationToken);
        if (current.Count > 0)
        {
            return current;
        }

        var made = DocumentFolder.For(projectId, await folders.DefaultFoldersAsync(cancellationToken));
        folders.AddFolders(made);
        try
        {
            await folders.SaveChangesAsync(cancellationToken);
            return made;
        }
        catch (DocumentChangedException)
        {
            // Someone made them at the same time: the ones saved stand.
            folders.Discard();
            return await folders.FoldersAsync(projectId, cancellationToken);
        }
    }

    /// <summary>The id of the folder in the storage, made now when it has none; the caller saves it with its work.</summary>
    public async Task<string> StorageOfAsync(DocumentFolder folder, ProjectAccess project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(project);
        if (folder.StorageFolderId is { } id)
        {
            return id;
        }

        // Inside the folder of the project, whatever its name (ADR-0121).
        var stored = await storage.EnsureFolderAsync(project.Id, $"{DocumentsFolder}/{folder.Name}", cancellationToken);
        folder.KeepStorage(stored);
        return stored;
    }

    /// <summary>
    /// Stores a file in the folder of the storage of a folder of documents; a folder removed straight in the storage is
    /// made again, once (ADR-0121). The caller saves the folder with its work.
    /// </summary>
    public async Task<StoredFile> UploadAsync(
        DocumentFolder folder, ProjectAccess project, string name, Stream content, SameNameAction sameName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(content);
        var storageFolder = await StorageOfAsync(folder, project, cancellationToken);
        try
        {
            return await files.UploadAsync(storageFolder, name, content, sameName, cancellationToken);
        }
        catch (StorageFolderNotFoundException) when (content.CanSeek)
        {
            folder.ForgetStorage();
            storageFolder = await StorageOfAsync(folder, project, cancellationToken);
            content.Position = 0;
            return await files.UploadAsync(storageFolder, name, content, sameName, cancellationToken);
        }
    }
}

/// <summary>
/// Sets up the folders of a new project (ADR-0092), told by the Projects module after it saves the project: the
/// folders from the active default folders, and their folders in the storage. Running it again changes nothing.
/// </summary>
public sealed class DocumentProjectSetup(ProjectFolders projectFolders, IFolderStore folders, IProjectAccess projects) : IProjectSetup
{
    public async Task ProjectCreatedAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var made = await projectFolders.OfAsync(projectId, cancellationToken);
        if (await projects.FindAsync(projectId, Guid.Empty, isAdministrator: true, cancellationToken) is not { } project)
        {
            return;
        }

        foreach (var folder in made)
        {
            await projectFolders.StorageOfAsync(folder, project, cancellationToken);
        }

        await folders.SaveChangesAsync(cancellationToken);
    }
}
