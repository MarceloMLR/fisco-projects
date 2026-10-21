namespace FiscoProjects.Projects.Contracts;

/// <summary>A request of acceptance waiting for the answer, for the overview of the project (ADR-0073).</summary>
public sealed record PendingProjectAcceptance(Guid Id, string Title, Guid MilestoneId, Guid RecipientId, DateTime RequestedAt);

/// <summary>
/// The acceptance terms of the projects: for the summary of the closing (ADR-0043), where a pending request warns
/// without keeping the project from being completed, for the next milestones of Meu trabalho (ADR-0096) and for the
/// overview of the project (ADR-0073).
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

    /// <summary>
    /// The requests of the project waiting for the answer that the person sees (ADR-0045), the oldest first, for the
    /// overview of the project.
    /// </summary>
    /// <param name="projectId">The project.</param>
    /// <param name="userId">The person.</param>
    /// <param name="isAdministrator">Whether the person is the Administrator.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    Task<IReadOnlyList<PendingProjectAcceptance>> PendingForAsync(
        Guid projectId, Guid userId, bool isAdministrator, CancellationToken cancellationToken);
}
