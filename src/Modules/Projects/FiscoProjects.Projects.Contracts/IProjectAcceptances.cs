namespace FiscoProjects.Projects.Contracts;

/// <summary>
/// The acceptance terms of the projects: for the summary of the closing (ADR-0043), where a pending request warns
/// without keeping the project from being completed, and for the next milestones of Meu trabalho (ADR-0096).
/// </summary>
public interface IProjectAcceptances
{
    /// <summary>How many requests of acceptance of the project wait for the answer.</summary>
    /// <param name="projectId">The project.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    Task<int> PendingAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The milestones of the projects with a request of acceptance waiting for the answer.</summary>
    /// <param name="projectIds">The projects.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    Task<IReadOnlySet<Guid>> RequestedMilestonesAsync(IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken);
}
