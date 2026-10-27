using FiscoProjects.SharedKernel;

namespace FiscoProjects.Documents.Domain;

/// <summary>The rules of the names of the folders, the same for the default ones and the ones of the projects.</summary>
public static class FolderNames
{
    public const int MaxLength = 120;

    /// <summary>What SharePoint does not take in a name, and what would make a path of folders (ADR-0030).</summary>
    private static readonly char[] Refused = ['"', '*', ':', '<', '>', '?', '/', '\\', '|'];

    /// <summary>
    /// A name with something in it, up to 120 characters, trimmed, without the characters SharePoint refuses and not
    /// ending in a dot; null when it is not valid.
    /// </summary>
    public static string? Of(string? name) =>
        name?.Trim() is { Length: > 0 and <= MaxLength } trimmed
            && trimmed.IndexOfAny(Refused) < 0
            && !trimmed.EndsWith('.')
            && !trimmed.Any(char.IsControl)
            ? trimmed
            : null;
}

/// <summary>
/// A folder every new project gets in its documents (ADR-0031), set by the Administrator: none is fixed in the code
/// (ADR-0003). The folder of the acceptance terms is one of them, and it is never deactivated (ADR-0093). An inactive
/// folder is left out of the new projects; the projects that have it keep it.
/// </summary>
public sealed class DefaultFolder : Entity
{
    private DefaultFolder()
    {
        Name = string.Empty;
    }

    public DefaultFolder(string name)
    {
        Name = name;
        IsActive = true;
    }

    public string Name { get; private set; }

    /// <summary>The folder of the acceptance terms, with the signed terms of the project (ADR-0035, ADR-0093).</summary>
    public bool IsAcceptance { get; private init; }

    public bool IsActive { get; private set; }

    /// <summary>Renames and activates or deactivates the folder; false, changing nothing, to deactivate the folder of the terms.</summary>
    public bool Change(string name, bool isActive)
    {
        if (IsAcceptance && !isActive)
        {
            return false;
        }

        Name = name;
        IsActive = isActive;
        return true;
    }
}

/// <summary>
/// A folder of the documents of a project, copied from a default folder when the project was created (ADR-0031): later
/// changes of the default folders leave it as it is. Its folder in the storage is made on the first need (ADR-0030).
/// </summary>
public sealed class DocumentFolder : Entity
{
    private DocumentFolder()
    {
        Name = string.Empty;
    }

    public DocumentFolder(Guid projectId, string name, bool isAcceptance)
    {
        ProjectId = projectId;
        Name = name;
        IsAcceptance = isAcceptance;
    }

    public Guid ProjectId { get; private init; }

    public string Name { get; private init; }

    /// <summary>The folder of the acceptance terms: only the terms flow puts documents in it (ADR-0093).</summary>
    public bool IsAcceptance { get; private init; }

    /// <summary>The id of the folder in the storage; null until it is made.</summary>
    public string? StorageFolderId { get; private set; }

    public void KeepStorage(string folderId) => StorageFolderId = folderId;

    /// <summary>Forgets the folder in the storage, removed outside the system, so it is made again (ADR-0121).</summary>
    public void ForgetStorage() => StorageFolderId = null;

    /// <summary>The folders of a new project: a copy of the active default folders, in the order of the name.</summary>
    public static IReadOnlyList<DocumentFolder> For(Guid projectId, IEnumerable<DefaultFolder> defaults) =>
        [.. defaults.Where(folder => folder.IsActive)
            .OrderBy(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(folder => new DocumentFolder(projectId, folder.Name, folder.IsAcceptance))];
}
