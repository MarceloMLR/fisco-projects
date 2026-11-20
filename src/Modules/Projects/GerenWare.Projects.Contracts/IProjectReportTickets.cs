namespace GerenWare.Projects.Contracts;

/// <summary>The status of an open ticket (ADR-0024), as the status report shows it: never resolved nor closed.</summary>
public enum ReportTicketStatus
{
    Open = 1,
    Triage = 2,
    InProgress = 3,
    WaitingForRequester = 4,
}

/// <summary>The priority of a ticket (ADR-0025), as the status report shows it.</summary>
public enum ReportTicketPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
}

/// <summary>The status of a ticket at a vendor (ADR-0028), as the status report shows it.</summary>
public enum ReportVendorStatus
{
    Open = 1,
    InAnalysis = 2,
    WaitingForUs = 3,
    Resolved = 4,
    Closed = 5,
}

/// <summary>The ticket at a vendor a list shows for a ticket (ADR-0028): the vendor, the number there and the status.</summary>
public sealed record ReportVendorTicket(string Vendor, string Code, ReportVendorStatus Status);

/// <summary>
/// An open ticket (neither resolved nor closed) of a project for the status report (ADR-0165): the number, the title,
/// the status, the priority, whether it is late now (ADR-0025), the ticket at a vendor a list shows for it, if any, and
/// the items of the schedule it is linked to (ADR-0164).
/// </summary>
public sealed record ReportTicket(
    int Number,
    string Title,
    ReportTicketStatus Status,
    ReportTicketPriority Priority,
    bool Overdue,
    ReportVendorTicket? Vendor,
    IReadOnlyList<Guid> ItemIds);

/// <summary>The open tickets of a project for the status report (ADR-0165); the Tickets module implements it.</summary>
public interface IProjectReportTickets
{
    /// <summary>The open tickets of the project, by the number.</summary>
    Task<IReadOnlyList<ReportTicket>> OpenAsync(Guid projectId, CancellationToken cancellationToken);
}
