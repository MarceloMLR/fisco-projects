using System.Data.Common;

namespace FiscoProjects.Projects.Contracts;

/// <summary>The kinds of items of a project, counted before and when it is deleted (ADR-0041).</summary>
public enum ProjectItemKind
{
    Tickets = 1,
    Documents = 2,
    Links = 3,
    Credentials = 4,
    AcceptanceTerms = 5,
    ScheduleItems = 6,
    Members = 7,
}

/// <summary>How many items of a kind a module has, or removed, of a project.</summary>
public sealed record ProjectItemCount(ProjectItemKind Kind, int Count);

/// <summary>
/// Asked when the Administrator deletes a project (ADR-0041, ADR-0111), so each module that keeps something of it says
/// how much it has and removes it: first the files it stored, then its rows, in the transaction of the deletion, which
/// the Projects module commits with the project. Each implementation is idempotent.
/// </summary>
public interface IProjectDeletion
{
    /// <summary>What the module has of the project, by kind, for the summary shown before the deletion.</summary>
    /// <param name="projectId">The project.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    Task<IReadOnlyList<ProjectItemCount>> CountAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the files the module stored for the project and the folders left empty, before the rows; a file already
    /// gone is skipped.
    /// </summary>
    /// <param name="projectId">The project.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    Task DeleteFilesAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>Removes the rows of the project, through the connection and the transaction of the deletion.</summary>
    /// <param name="projectId">The project.</param>
    /// <param name="transaction">The transaction of the deletion, with its connection.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    /// <returns>How many items of each kind were removed.</returns>
    Task<IReadOnlyList<ProjectItemCount>> DeleteDataAsync(
        Guid projectId, DbTransaction transaction, CancellationToken cancellationToken);
}
