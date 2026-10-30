namespace FiscoProjects.MyWork.Contracts;

/// <summary>The views of Meu trabalho an item is in (ADR-0070, ADR-0096): for the person, opened by the person, followed.</summary>
[Flags]
public enum MyWorkViews
{
    None = 0,
    ForMe = 1,
    OpenedByMe = 2,
    Following = 4,
}

/// <summary>What an item of the queue is: a ticket, a task of a schedule or a request of acceptance.</summary>
public enum MyWorkKind
{
    Ticket = 1,
    Task = 2,
    Acceptance = 3,
}

/// <summary>The statuses of a ticket the queue shows: the open ones (ADR-0024).</summary>
public enum MyWorkTicketStatus
{
    Open = 1,
    Triage = 2,
    InProgress = 3,
    WaitingForRequester = 4,
}

/// <summary>The team of a ticket (ADR-0068).</summary>
public enum MyWorkTeam
{
    Functional = 1,
    Development = 2,
}

/// <summary>The deadline of a ticket the queue counts (ADR-0025): the first answer, while not given, then the resolution.</summary>
public enum MyWorkDeadline
{
    FirstResponse = 1,
    Resolution = 2,
}

/// <summary>
/// Whose items are asked for: the person and whether the person is the Administrator, today in the time zone of the
/// installation and the last day of the window of Meu trabalho (ADR-0096).
/// </summary>
public sealed record MyWorkQuery(Guid UserId, bool IsAdministrator, DateOnly Today, DateOnly Until);

/// <summary>
/// A ticket in the queue: the number, where it stands, the team, who asked, who is in charge, whether the person changes
/// the assignee in the project (ADR-0068), how many messages and events it has and which deadline counts.
/// </summary>
public sealed record MyWorkTicket(
    int Number,
    MyWorkTicketStatus Status,
    MyWorkTeam Team,
    Guid RequesterId,
    Guid? AssigneeId,
    bool CanAssign,
    int Interactions,
    MyWorkDeadline? Deadline);

/// <summary>A task in the queue: the name of its phase and its progress, from 0 to 99%.</summary>
public sealed record MyWorkTask(string Phase, int Progress);

/// <summary>
/// A pending request of acceptance in the queue: the signers (ADR-0129), or the person a request was asked of before,
/// who asked and when, and the milestone.
/// </summary>
public sealed record MyWorkAcceptance(
    Guid? RecipientId, IReadOnlyList<string> Signers, Guid RequestedById, DateTime RequestedAt, string? Milestone);

/// <summary>
/// An item of a person for Meu trabalho (ADR-0070, ADR-0096): what it is, its project, the title, the views it is in,
/// the day it is due (none while it waits for someone), the business days of delay as its module counts them, and
/// whether it waits for the person, since when.
/// </summary>
public sealed record MyWorkItem(MyWorkKind Kind, Guid Id, Guid ProjectId, string ProjectCode, string Title, MyWorkViews Views)
{
    public DateOnly? DueOn { get; init; }

    public int? DaysLate { get; init; }

    /// <summary>The item waits for the answer of the person: a ticket the person opened.</summary>
    public bool WaitsForYou { get; init; }

    public DateTime? WaitingSince { get; init; }

    public MyWorkTicket? Ticket { get; init; }

    public MyWorkTask? Task { get; init; }

    public MyWorkAcceptance? Acceptance { get; init; }
}

/// <summary>
/// The items of a person in a module, for Meu trabalho (ADR-0096): each module tells which of its items are of the
/// person, with its own rules of who sees and who serves them.
/// </summary>
public interface IMyWorkSource
{
    /// <summary>
    /// The items of the person in projects the person sees that are not completed nor cancelled: the late ones, the ones
    /// waiting for the person and the ones due up to the last day of the window.
    /// </summary>
    Task<IReadOnlyList<MyWorkItem>> ItemsAsync(MyWorkQuery query, CancellationToken cancellationToken);
}
