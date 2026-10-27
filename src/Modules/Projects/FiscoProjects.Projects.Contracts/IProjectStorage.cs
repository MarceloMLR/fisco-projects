namespace FiscoProjects.Projects.Contracts;

/// <summary>
/// The folder of each project in the file storage (ADR-0030, ADR-0121): one per project, "{code} - {name}" inside the
/// folder of the projects of the installation, created with the first file and renamed with the project. Every module
/// keeps the files of a project inside it, through this contract, and never builds the path by the name.
/// </summary>
public interface IProjectStorage
{
    /// <summary>
    /// The id, in the file storage, of the folder at the path inside the folder of the project (folders separated by
    /// "/"), creating what is missing, the folder of the project included.
    /// </summary>
    /// <param name="projectId">The project, which exists.</param>
    /// <param name="path">The folders inside the folder of the project, as "Chamados/#104".</param>
    /// <param name="cancellationToken">Stops the work.</param>
    /// <exception cref="KeyNotFoundException">The project does not exist.</exception>
    Task<string> EnsureFolderAsync(Guid projectId, string path, CancellationToken cancellationToken);
}
