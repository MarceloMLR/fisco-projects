namespace GerenWare.Projects.Contracts;

/// <summary>A status report kept in the documents of the project: the document, its folder, its name and its version.</summary>
public sealed record SavedStatusReport(Guid DocumentId, Guid FolderId, string FolderName, string Name, int Version);

/// <summary>
/// Keeps each status report generated in the folder of the status reports of the project (ADR-0165), in any status of
/// the project; the Documents module implements it. The one who generates was already checked by the caller.
/// </summary>
public interface IStatusReportDocuments
{
    /// <summary>
    /// Keeps the file as a document of the folder of the status reports, made for the project when it does not have it,
    /// or as a new version of the document with the same name.
    /// </summary>
    Task<SavedStatusReport> SaveAsync(Guid projectId, string name, Stream content, Guid userId, CancellationToken cancellationToken);
}
