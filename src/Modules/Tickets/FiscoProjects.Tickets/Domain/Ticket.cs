using FiscoProjects.SharedKernel;

namespace FiscoProjects.Tickets.Domain;

/// <summary>The team a ticket goes to (ADR-0068): only these two, without queues.</summary>
public enum TicketTeam
{
    Functional = 1,
    Development = 2,
}

/// <summary>How urgent a ticket is, from the lowest to the highest.</summary>
public enum TicketPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
}

/// <summary>The status of a ticket (ADR-0024). A ticket is opened as Open; the flow between them comes from a table.</summary>
public enum TicketStatus
{
    Open = 1,
    Triage = 2,
    InProgress = 3,
    WaitingForRequester = 4,
    Resolved = 5,
    Closed = 6,
}

/// <summary>Which tickets a list shows by status: the ones still in progress, the resolved and closed ones, or all.</summary>
public enum TicketSituation
{
    Open = 1,
    Finished = 2,
    All = 3,
}

/// <summary>
/// A ticket of a project (ADR-0022): a number unique in the installation (ADR-0029), the team it goes to, the priority,
/// the title and the description, the person who opened it (the requester) and the person in charge (the assignee),
/// none when it is opened (ADR-0068).
/// </summary>
public sealed class Ticket : Entity
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 10_000;

    /// <summary>The statuses of a ticket still in progress: everything but Resolved and Closed.</summary>
    public static readonly IReadOnlyList<TicketStatus> OpenStatuses =
        [TicketStatus.Open, TicketStatus.Triage, TicketStatus.InProgress, TicketStatus.WaitingForRequester];

    private Ticket()
    {
        Title = string.Empty;
        Description = string.Empty;
    }

    private Ticket(
        int number, Guid projectId, TicketTeam team, TicketPriority priority, string title, string description,
        Guid requesterId, DateTime openedAt)
    {
        Number = number;
        ProjectId = projectId;
        Team = team;
        Priority = priority;
        Title = title;
        Description = description;
        Status = TicketStatus.Open;
        RequesterId = requesterId;
        OpenedAt = openedAt;
        UpdatedAt = openedAt;
    }

    public int Number { get; private init; }

    public Guid ProjectId { get; private init; }

    public TicketTeam Team { get; private set; }

    public TicketPriority Priority { get; private set; }

    public string Title { get; private set; }

    /// <summary>Plain text with its line breaks.</summary>
    public string Description { get; private set; }

    public TicketStatus Status { get; private set; }

    public Guid RequesterId { get; private set; }

    public Guid? AssigneeId { get; private set; }

    public DateTime OpenedAt { get; private init; }

    /// <summary>The last time anything changed in the ticket.</summary>
    public DateTime UpdatedAt { get; private set; }

    public bool IsOpen => OpenStatuses.Contains(Status);

    /// <summary>A new ticket, Open and without an assignee, from data already checked by <see cref="TicketForm"/>.</summary>
    public static Ticket Open(int number, Guid projectId, TicketForm form, Guid requesterId, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(form);
        if (!form.IsValid)
        {
            throw new ArgumentException("The ticket is not valid.", nameof(form));
        }

        return new Ticket(
            number, projectId, form.Team!.Value, form.Priority!.Value, form.Title!, form.Description!, requesterId, now);
    }
}

/// <summary>The fields of a new ticket that may be wrong.</summary>
public enum TicketField
{
    Project = 1,
    Team = 2,
    Priority = 3,
    Title = 4,
    Description = 5,
}

/// <summary>
/// The data of a new ticket, checked: the team and the priority among the known ones, the title with 1 to 200 characters
/// and the description with 1 to 10,000, both without the spaces around.
/// </summary>
public sealed class TicketForm
{
    private TicketForm(TicketTeam? team, TicketPriority? priority, string? title, string? description, List<TicketField> invalid)
    {
        Team = team;
        Priority = priority;
        Title = title;
        Description = description;
        Invalid = invalid;
    }

    public TicketTeam? Team { get; }

    public TicketPriority? Priority { get; }

    public string? Title { get; }

    public string? Description { get; }

    /// <summary>The fields that are missing or wrong, in the order of the form.</summary>
    public IReadOnlyList<TicketField> Invalid { get; }

    public bool IsValid => Invalid.Count == 0;

    public static TicketForm Of(TicketTeam? team, TicketPriority? priority, string? title, string? description)
    {
        var invalid = new List<TicketField>();
        if (team is not { } chosenTeam || !Enum.IsDefined(chosenTeam))
        {
            invalid.Add(TicketField.Team);
            team = null;
        }

        if (priority is not { } chosenPriority || !Enum.IsDefined(chosenPriority))
        {
            invalid.Add(TicketField.Priority);
            priority = null;
        }

        var trimmedTitle = title?.Trim();
        if (string.IsNullOrEmpty(trimmedTitle) || trimmedTitle.Length > Ticket.TitleMaxLength)
        {
            invalid.Add(TicketField.Title);
        }

        var trimmedDescription = description?.Trim();
        if (string.IsNullOrEmpty(trimmedDescription) || trimmedDescription.Length > Ticket.DescriptionMaxLength)
        {
            invalid.Add(TicketField.Description);
        }

        return new TicketForm(team, priority, trimmedTitle, trimmedDescription, invalid);
    }
}
