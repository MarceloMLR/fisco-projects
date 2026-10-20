using FiscoProjects.SharedKernel;

namespace FiscoProjects.Documents.Domain;

/// <summary>Where a document stands, set by hand (ADR-0031); the obsolete ones leave the list by default.</summary>
public enum DocumentStatus
{
    Draft = 1,
    InReview = 2,
    Approved = 3,
    Obsolete = 4,
}

/// <summary>
/// A file of a project, in one of its folders (ADR-0031): the file is in the storage, with its versions, and the status
/// and the link with the schedule are here (ADR-0030). The name is the name of the file, unique in the folder: a file
/// sent again with it is a new version.
/// </summary>
public sealed class Document : Entity
{
    public const int NameMaxLength = 255;

    private readonly List<DocumentVersion> _versions = [];

    private Document()
    {
        Name = string.Empty;
        FileId = string.Empty;
    }

    private Document(Guid projectId, Guid folderId, string name, string fileId, Guid userId, DateTime now)
    {
        ProjectId = projectId;
        FolderId = folderId;
        Name = name;
        FileId = fileId;
        Status = DocumentStatus.Draft;
        StatusChangedById = userId;
        StatusChangedAt = now;
        CreatedById = userId;
        CreatedAt = now;
        UpdatedById = userId;
        UpdatedAt = now;
    }

    public Guid ProjectId { get; private init; }

    public Guid FolderId { get; private init; }

    public string Name { get; private init; }

    /// <summary>The id of the file in the storage, the same for all its versions.</summary>
    public string FileId { get; private init; }

    /// <summary>The number of the current version, from 1.</summary>
    public int Version { get; private set; }

    /// <summary>The size of the current version, in bytes.</summary>
    public long Size { get; private set; }

    public DocumentStatus Status { get; private set; }

    /// <summary>Who changed the status last, and when (ADR-0031); at first, who sent the document.</summary>
    public Guid StatusChangedById { get; private set; }

    public DateTime StatusChangedAt { get; private set; }

    /// <summary>The phase of the schedule the document is linked to; at most one of the phase and the item.</summary>
    public Guid? SchedulePhaseId { get; private set; }

    /// <summary>The task or milestone of the schedule the document is linked to.</summary>
    public Guid? ScheduleItemId { get; private set; }

    public Guid CreatedById { get; private init; }

    public DateTime CreatedAt { get; private init; }

    /// <summary>Who sent the current version, and when.</summary>
    public Guid UpdatedById { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>The versions, when read with the document.</summary>
    public IReadOnlyList<DocumentVersion> Versions => _versions;

    /// <summary>A document sent for the first time: version 1, as a draft (ADR-0031).</summary>
    public static Document Upload(
        Guid projectId, Guid folderId, string name, string fileId, string versionId, long size, Guid userId, DateTime now)
    {
        var document = new Document(projectId, folderId, name, fileId, userId, now);
        document.AddVersion(versionId, size, userId, now);
        return document;
    }

    /// <summary>
    /// The file sent again: the next version. An approved document goes back to review, since the approval was of the
    /// version before (ADR-0093); the other statuses stay.
    /// </summary>
    public DocumentVersion AddVersion(string versionId, long size, Guid userId, DateTime now)
    {
        Version++;
        Size = size;
        UpdatedById = userId;
        UpdatedAt = now;
        if (Status == DocumentStatus.Approved)
        {
            ChangeStatus(DocumentStatus.InReview, userId, now);
        }

        var version = new DocumentVersion(Id, Version, versionId, size, userId, now);
        _versions.Add(version);
        return version;
    }

    public void ChangeStatus(DocumentStatus status, Guid userId, DateTime now)
    {
        Status = status;
        StatusChangedById = userId;
        StatusChangedAt = now;
    }

    /// <summary>Links the document to a phase or to a task or milestone of the schedule; both null take the link away.</summary>
    public void Link(Guid? phaseId, Guid? itemId)
    {
        if (phaseId is not null && itemId is not null)
        {
            throw new ArgumentException("A document is linked to a phase or to an item, not both.", nameof(itemId));
        }

        SchedulePhaseId = phaseId;
        ScheduleItemId = itemId;
    }

    /// <summary>A name of a file with something in it, up to 255 characters, without a folder; null when it is not valid.</summary>
    public static string? NameOf(string? fileName) =>
        Path.GetFileName(fileName?.Trim()) is { Length: > 0 and <= NameMaxLength } name && !name.Any(char.IsControl) ? name : null;
}

/// <summary>A version of a document: its number, the version of the file in the storage, the size and who sent it.</summary>
public sealed class DocumentVersion : Entity
{
    private DocumentVersion()
    {
        StorageVersionId = string.Empty;
    }

    internal DocumentVersion(Guid documentId, int number, string storageVersionId, long size, Guid uploadedById, DateTime uploadedAt)
    {
        DocumentId = documentId;
        Number = number;
        StorageVersionId = storageVersionId;
        Size = size;
        UploadedById = uploadedById;
        UploadedAt = uploadedAt;
    }

    public Guid DocumentId { get; private init; }

    public int Number { get; private init; }

    /// <summary>The id of the version in the storage, which opens it.</summary>
    public string StorageVersionId { get; private init; }

    public long Size { get; private init; }

    public Guid UploadedById { get; private init; }

    public DateTime UploadedAt { get; private init; }
}
