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

    /// <summary>The plain text of the description, with its line breaks, for the search.</summary>
    public string Description { get; private set; }

    /// <summary>
    /// The description with its formatting (<see cref="RichText"/>), as JSON; null for the ones opened before the editor,
    /// shown as plain text.
    /// </summary>
    public string? DescriptionBody { get; private set; }

    /// <summary>The folder of the attachments of the ticket in the file storage, made with the first one (ADR-0030).</summary>
    public string? AttachmentFolderId { get; private set; }

    public TicketStatus Status { get; private set; }

    public Guid RequesterId { get; private set; }

    public Guid? AssigneeId { get; private set; }

    public DateTime OpenedAt { get; private init; }

    /// <summary>The last time anything changed in the ticket.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>When it was resolved the last time; the automatic closing counts from it (ADR-0024).</summary>
    public DateTime? ResolvedAt { get; private set; }

    public DateTime? ClosedAt { get; private set; }

    /// <summary>The business days of the priority for the first answer, kept from the opening (ADR-0025).</summary>
    public int FirstResponseDays { get; private set; }

    /// <summary>The business days of the priority to resolve, kept from the opening (ADR-0025).</summary>
    public int ResolutionDays { get; private set; }

    /// <summary>The day the first answer is due; null for a ticket opened before the deadlines.</summary>
    public DateOnly? FirstResponseDueOn { get; private set; }

    /// <summary>The day the resolution is due; null while it waits for the requester, and for a ticket without deadlines.</summary>
    public DateOnly? ResolutionDueOn { get; private set; }

    /// <summary>When someone who serves the team answered the first time (ADR-0025).</summary>
    public DateTime? FirstRespondedAt { get; private set; }

    /// <summary>When the ticket started to wait for the requester, while it waits.</summary>
    public DateTime? PausedSince { get; private set; }

    /// <summary>The business days it waited for the requester before, which push the resolution further.</summary>
    public int PausedBusinessDays { get; private set; }

    public bool IsOpen => OpenStatuses.Contains(Status);

    /// <summary>Something changed in the ticket now, such as a new message.</summary>
    public void Touch(DateTime now) => UpdatedAt = now;

    /// <summary>Takes a transition of the flow from the status the ticket is in (ADR-0024).</summary>
    public void Apply(TicketTransition transition, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(transition);
        if (transition.From != Status)
        {
            throw new InvalidOperationException($"The ticket is {Status}, not {transition.From}.");
        }

        if (transition.To == TicketStatus.WaitingForRequester)
        {
            // The resolution stops while the ticket waits for the requester (ADR-0025).
            PausedSince = now;
            ResolutionDueOn = null;
        }

        Status = transition.To;
        // Closing keeps when it was resolved, for the deadline; reopening forgets it.
        ResolvedAt = transition.To switch
        {
            TicketStatus.Resolved => now,
            TicketStatus.Closed => ResolvedAt,
            _ => null,
        };
        ClosedAt = transition.To == TicketStatus.Closed ? now : null;
        UpdatedAt = now;
    }

    /// <summary>
    /// The project was cancelled (ADR-0043): the ticket closes from any status, without the flow of the table; a
    /// resolved one keeps when it was resolved.
    /// </summary>
    public void CloseWithProject(DateTime now)
    {
        Status = TicketStatus.Closed;
        ClosedAt = now;
        UpdatedAt = now;
    }

    /// <summary>The deadlines of the priority, and the days they are due (ADR-0025).</summary>
    public void UseDeadlines(int firstResponseDays, int resolutionDays, DateOnly firstResponseDueOn, DateOnly? resolutionDueOn)
    {
        FirstResponseDays = firstResponseDays;
        ResolutionDays = resolutionDays;
        FirstResponseDueOn = firstResponseDueOn;
        ResolutionDueOn = resolutionDueOn;
    }

    /// <summary>The ticket stops waiting for the requester: the paused days push the resolution (ADR-0025).</summary>
    public void Resume(int pausedBusinessDays, DateOnly resolutionDueOn)
    {
        PausedBusinessDays += pausedBusinessDays;
        PausedSince = null;
        ResolutionDueOn = resolutionDueOn;
    }

    /// <summary>The first answer of who serves the team; the later ones change nothing.</summary>
    public void RespondFirst(DateTime now) => FirstRespondedAt ??= now;

    /// <summary>A new priority, whose deadlines the caller sets again from the opening (ADR-0025).</summary>
    public void ChangePriority(TicketPriority priority, DateTime now)
    {
        Priority = priority;
        UpdatedAt = now;
    }

    /// <summary>Nobody is in charge of the ticket any more, as when the assignee leaves the project (ADR-0042).</summary>
    public void Unassign(DateTime now)
    {
        AssigneeId = null;
        UpdatedAt = now;
    }

    /// <summary>Another person of the project answers for the ticket as the requester (ADR-0027).</summary>
    public void ChangeRequester(Guid requesterId, DateTime now)
    {
        RequesterId = requesterId;
        UpdatedAt = now;
    }

    /// <summary>The ticket goes to the other team, without an assignee until someone of it answers (ADR-0068).</summary>
    public void Transfer(TicketTeam team, DateTime now)
    {
        Team = team;
        AssigneeId = null;
        UpdatedAt = now;
    }

    /// <summary>The person in charge of the ticket from now on (ADR-0068).</summary>
    public void Assign(Guid assigneeId, DateTime now)
    {
        AssigneeId = assigneeId;
        UpdatedAt = now;
    }

    /// <summary>Keeps the folder of the attachments, made with the first one.</summary>
    public void KeepAttachmentFolder(string folderId) => AttachmentFolderId ??= folderId;

    /// <summary>A new ticket, Open and without an assignee, from data already checked by <see cref="TicketForm"/>.</summary>
    public static Ticket Open(int number, Guid projectId, TicketForm form, Guid requesterId, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(form);
        if (!form.IsValid)
        {
            throw new ArgumentException("The ticket is not valid.", nameof(form));
        }

        return new Ticket(
            number, projectId, form.Team!.Value, form.Priority!.Value, form.Title!, form.Description!, requesterId, now)
        {
            DescriptionBody = form.Body?.Json,
        };
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
    private TicketForm(
        TicketTeam? team, TicketPriority? priority, string? title, string? description, RichText? body, List<TicketField> invalid)
    {
        Team = team;
        Priority = priority;
        Title = title;
        Description = description;
        Body = body;
        Invalid = invalid;
    }

    public TicketTeam? Team { get; }

    public TicketPriority? Priority { get; }

    public string? Title { get; }

    public string? Description { get; }

    /// <summary>The description with its formatting, when it came from the editor.</summary>
    public RichText? Body { get; }

    /// <summary>The fields that are missing or wrong, in the order of the form.</summary>
    public IReadOnlyList<TicketField> Invalid { get; }

    public bool IsValid => Invalid.Count == 0;

    /// <summary>
    /// The data of a new ticket; the description comes with its formatting (<paramref name="body"/>, the JSON of the
    /// editor) or as plain text.
    /// </summary>
    public static TicketForm Of(
        TicketTeam? team, TicketPriority? priority, string? title, string? description, string? body = null)
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

        var rich = body is null ? null : RichText.Parse(body);
        var trimmedDescription = rich?.Text ?? description?.Trim();
        // Mentions go in the messages, where who is mentioned starts to follow the ticket (ADR-0026).
        if ((body is not null && (rich is null || rich.Mentions.Count > 0))
            || string.IsNullOrEmpty(trimmedDescription) || trimmedDescription.Length > Ticket.DescriptionMaxLength)
        {
            invalid.Add(TicketField.Description);
        }

        return new TicketForm(team, priority, trimmedTitle, trimmedDescription, rich, invalid);
    }
}
