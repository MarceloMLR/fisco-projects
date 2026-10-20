namespace FiscoProjects.Projects.Contracts;

/// <summary>
/// The acceptance terms of a project, for the summary of its closing (ADR-0043): a pending request warns, without
/// keeping the project from being completed.
/// </summary>
public interface IProjectAcceptances
{
    /// <summary>How many requests of acceptance of the project wait for the answer.</summary>
    /// <param name="projectId">The project.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    Task<int> PendingAsync(Guid projectId, CancellationToken cancellationToken);
}
