namespace FiscoProjects.Projects.Contracts;

/// <summary>
/// Asked when a project is completed or cancelled (ADR-0043), so the other modules say what is still open in it and
/// close it when the project is cancelled, as the tickets. Each implementation is idempotent.
/// </summary>
public interface IProjectClosing
{
    /// <summary>How many tickets of the project are not closed, which keep it from being completed.</summary>
    /// <param name="projectId">The project.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    Task<int> OpenTicketsAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>
    /// Closes the tickets of the project that are not closed, because the project is being cancelled, with the reason
    /// in their conversations; all or nothing.
    /// </summary>
    /// <param name="projectId">The project.</param>
    /// <param name="actorId">Who cancels the project.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    /// <returns>False when someone changed a ticket at the same time and nothing was closed.</returns>
    Task<bool> CloseOpenTicketsAsync(Guid projectId, Guid actorId, CancellationToken cancellationToken);
}
