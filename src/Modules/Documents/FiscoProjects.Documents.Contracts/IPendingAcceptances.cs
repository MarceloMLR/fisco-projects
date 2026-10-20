namespace FiscoProjects.Documents.Contracts;

/// <summary>A request of acceptance waiting for the answer of the person, for the daily summary (ADR-0037).</summary>
public sealed record PendingAcceptance(
    Guid RecipientId, Guid ProjectId, string ProjectCode, Guid TermId, string Title, DateTime RequestedAt);

/// <summary>The requests of acceptance waiting for the answer, for the daily summary of each person (ADR-0035, ADR-0037).</summary>
public interface IPendingAcceptances
{
    /// <summary>
    /// The requests pending in projects not completed nor cancelled, of the people still in the project, oldest first.
    /// </summary>
    Task<IReadOnlyList<PendingAcceptance>> PendingAsync(CancellationToken cancellationToken);
}
