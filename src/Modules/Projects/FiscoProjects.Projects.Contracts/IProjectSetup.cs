namespace FiscoProjects.Projects.Contracts;

/// <summary>
/// Told when a project is created, after it is saved, so the other modules set up what every new project has, as the
/// folders of the documents (ADR-0030, ADR-0092). Each implementation is idempotent: a failure is logged, and the
/// project stays.
/// </summary>
public interface IProjectSetup
{
    /// <summary>A project was created.</summary>
    /// <param name="projectId">The project.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    Task ProjectCreatedAsync(Guid projectId, CancellationToken cancellationToken);
}
