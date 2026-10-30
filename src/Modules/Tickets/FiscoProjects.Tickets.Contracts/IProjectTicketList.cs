namespace FiscoProjects.Tickets.Contracts;

/// <summary>A ticket as the other modules point to it: its project, number, current title and whether it is closed.</summary>
public sealed record ProjectTicketInfo(Guid Id, Guid ProjectId, int Number, string Title, bool IsClosed);

/// <summary>
/// The tickets of the projects, for the modules whose items point to one, as the hours worked on a ticket (ADR-0128).
/// Everyone in a project sees its tickets (ADR-0044), so the project is what the caller checks.
/// </summary>
public interface IProjectTicketList
{
    /// <summary>The tickets of the project, first the ones not closed, each group from the newest number.</summary>
    Task<IReadOnlyList<ProjectTicketInfo>> OfProjectAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The tickets with these ids that exist, of any project.</summary>
    Task<IReadOnlyList<ProjectTicketInfo>> FindAsync(IReadOnlyCollection<Guid> ticketIds, CancellationToken cancellationToken);
}
