namespace FiscoProjects.Projects.Contracts;

/// <summary>The team of a ticket (ADR-0068), as the overview of a project shows it.</summary>
public enum ProjectTicketTeam
{
    Functional = 1,
    Development = 2,
}

/// <summary>The deadline of a ticket that counts now (ADR-0025): the first answer, while not given, then the resolution.</summary>
public enum ProjectTicketDeadline
{
    FirstResponse = 1,
    Resolution = 2,
}

/// <summary>
/// A ticket that needs attention in the overview (ADR-0073): late, due up to the next business day, or without an
/// assignee; with its team, who is in charge, how many messages and events it has and its deadline.
/// </summary>
public sealed record ProjectTicketAttention(
    int Number,
    string Title,
    ProjectTicketTeam Team,
    Guid? AssigneeId,
    int Interactions,
    ProjectTicketDeadline? Deadline,
    DateOnly? DueOn,
    int? DaysLate);

/// <summary>A ticket at a vendor without an update for the business days of the setting (ADR-0028), of a ticket of the project.</summary>
public sealed record ProjectVendorAttention(int TicketNumber, string Vendor, string Code, int StaleDays);

/// <summary>
/// The tickets of a project for its overview (ADR-0073): the open ones (neither resolved nor closed) of all, the late
/// ones, the ones waiting for the requester, the open ones of each team, the ones that need attention and the stale
/// tickets at vendors.
/// </summary>
public sealed record ProjectTicketOverview(
    int Open,
    int Total,
    int Overdue,
    int WaitingForRequester,
    int OpenFunctional,
    int OpenDevelopment,
    IReadOnlyList<ProjectTicketAttention> Attention,
    IReadOnlyList<ProjectVendorAttention> Vendors)
{
    public static ProjectTicketOverview None { get; } = new(0, 0, 0, 0, 0, 0, [], []);
}

/// <summary>The tickets of a project, for its overview (ADR-0073); the Tickets module implements it.</summary>
public interface IProjectTickets
{
    /// <summary>The numbers and the attention of the tickets of the project, on the day of the installation.</summary>
    Task<ProjectTicketOverview> OverviewAsync(Guid projectId, CancellationToken cancellationToken);
}
