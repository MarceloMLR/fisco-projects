using FiscoProjects.SharedKernel;

namespace FiscoProjects.Tickets.Domain;

/// <summary>The actions of the flow of a ticket (ADR-0024), named in English in the table.</summary>
public enum TicketAction
{
    StartTriage = 1,
    FirstResponse = 2,
    StartWork = 3,
    AskForInformation = 4,
    Reply = 5,
    Resolve = 6,
    ConfirmAndClose = 7,
    AutoClose = 8,
    Reopen = 9,
    Cancel = 10,
}

/// <summary>Who may take an action of the flow (ADR-0024, ADR-0044); a transition takes several.</summary>
[Flags]
public enum TicketActors
{
    None = 0,

    /// <summary>Who serves the ticket: the Administrator, the managers and who serves its team.</summary>
    Attendant = 1,

    /// <summary>Who opened the ticket.</summary>
    Requester = 2,

    /// <summary>The manager and the tech lead of the project.</summary>
    Manager = 4,

    Administrator = 8,

    /// <summary>The application itself, such as the automatic closing.</summary>
    System = 16,
}

/// <summary>
/// A transition of the flow of the tickets (ADR-0024): from a status to another by an action, who may take it, whether
/// it needs a message and whether only the system takes it, as the first response, the answer of the requester or the
/// automatic closing.
/// </summary>
public sealed class TicketTransition : Entity
{
    private TicketTransition()
    {
    }

    public TicketTransition(
        TicketStatus from, TicketStatus to, TicketAction action, TicketActors actors, bool requiresMessage, bool automatic)
    {
        From = from;
        To = to;
        Action = action;
        Actors = actors;
        RequiresMessage = requiresMessage;
        Automatic = automatic;
    }

    public TicketStatus From { get; private init; }

    public TicketStatus To { get; private init; }

    public TicketAction Action { get; private init; }

    public TicketActors Actors { get; private init; }

    public bool RequiresMessage { get; private init; }

    /// <summary>Taken by the system on its own, never offered as a button.</summary>
    public bool Automatic { get; private init; }
}

/// <summary>The flow read from the table is broken (ADR-0024): no change is made until it is fixed.</summary>
public sealed class TicketFlowBrokenException(string reason) : InvalidOperationException($"The flow of the tickets is broken: {reason}");

/// <summary>
/// The flow of the tickets read from the table and checked (ADR-0024): every status but Closed has a way out, no action
/// repeats in a status, and nothing leaves Closed.
/// </summary>
public sealed class TicketFlow
{
    private TicketFlow(IReadOnlyList<TicketTransition> transitions)
    {
        Transitions = transitions;
    }

    public IReadOnlyList<TicketTransition> Transitions { get; }

    /// <summary>The flow of the transitions; throws <see cref="TicketFlowBrokenException"/> when it is broken.</summary>
    public static TicketFlow From(IEnumerable<TicketTransition> transitions)
    {
        ArgumentNullException.ThrowIfNull(transitions);
        var all = transitions.ToList();
        if (all.Any(transition => transition.From == TicketStatus.Closed))
        {
            throw new TicketFlowBrokenException("a closed ticket is never reopened");
        }

        var dead = Enum.GetValues<TicketStatus>()
            .Where(status => status != TicketStatus.Closed && all.All(transition => transition.From != status))
            .ToList();
        if (dead.Count > 0)
        {
            throw new TicketFlowBrokenException($"no way out of {string.Join(", ", dead)}");
        }

        if (all.GroupBy(transition => (transition.From, transition.Action)).Any(group => group.Count() > 1))
        {
            throw new TicketFlowBrokenException("an action repeats in a status");
        }

        return new TicketFlow([.. all.OrderBy(transition => transition.From).ThenBy(transition => transition.Action)]);
    }

    /// <summary>The transition of an action from a status; null when the action does not leave it.</summary>
    public TicketTransition? Of(TicketStatus from, TicketAction action) =>
        Transitions.SingleOrDefault(transition => transition.From == from && transition.Action == action);

    /// <summary>The actions a person may take now: from the status, not automatic, for the actors of the person.</summary>
    public IReadOnlyList<TicketTransition> AvailableTo(TicketStatus status, TicketActors actors) =>
        [.. Transitions.Where(transition => transition.From == status && !transition.Automatic && (transition.Actors & actors) != 0)];

    /// <summary>The flow of the MVP (ADR-0024), the one the table starts with.</summary>
    public static IReadOnlyList<TicketTransition> Default =>
    [
        new(TicketStatus.Open, TicketStatus.Triage, TicketAction.StartTriage, TicketActors.Attendant, false, false),
        new(TicketStatus.Open, TicketStatus.InProgress, TicketAction.FirstResponse, TicketActors.Attendant, true, true),
        new(TicketStatus.Triage, TicketStatus.InProgress, TicketAction.StartWork, TicketActors.Attendant, false, false),
        new(TicketStatus.InProgress, TicketStatus.WaitingForRequester, TicketAction.AskForInformation, TicketActors.Attendant, true, false),
        new(TicketStatus.WaitingForRequester, TicketStatus.InProgress, TicketAction.Reply, TicketActors.Requester | TicketActors.Attendant, true, true),
        new(TicketStatus.InProgress, TicketStatus.Resolved, TicketAction.Resolve, TicketActors.Attendant, false, false),
        new(TicketStatus.Resolved, TicketStatus.Closed, TicketAction.ConfirmAndClose, TicketActors.Requester | TicketActors.Manager | TicketActors.Administrator, false, false),
        new(TicketStatus.Resolved, TicketStatus.Closed, TicketAction.AutoClose, TicketActors.System, false, true),
        new(TicketStatus.Resolved, TicketStatus.InProgress, TicketAction.Reopen, TicketActors.Requester, true, false),
        new(TicketStatus.Open, TicketStatus.Closed, TicketAction.Cancel, TicketActors.Requester | TicketActors.Manager | TicketActors.Administrator, true, false),
        new(TicketStatus.Triage, TicketStatus.Closed, TicketAction.Cancel, TicketActors.Requester | TicketActors.Manager | TicketActors.Administrator, true, false),
    ];
}

/// <summary>What happened in a ticket besides the messages (ADR-0069): a change of status or the assignee set.</summary>
public enum TicketEventKind
{
    StatusChanged = 1,
    AssigneeSet = 2,

    /// <summary>Who serves the ticket changed its priority, and its deadlines with it (ADR-0025).</summary>
    PriorityChanged = 3,

    /// <summary>The assignee left the project or the team of the ticket, which is left without one (ADR-0042).</summary>
    AssigneeCleared = 4,

    /// <summary>Another person of the project answers for the ticket as the requester (ADR-0027).</summary>
    RequesterChanged = 5,

    /// <summary>The ticket went to the other team, without an assignee (ADR-0068).</summary>
    TeamChanged = 6,

    /// <summary>A ticket opened at a vendor was registered in it (ADR-0028).</summary>
    VendorTicketRegistered = 7,

    /// <summary>The status of a ticket at a vendor was updated, with a note (ADR-0028).</summary>
    VendorStatusChanged = 8,

    /// <summary>The project was cancelled and closed the ticket, from the status it was in (ADR-0043).</summary>
    ClosedWithProject = 9,
}

/// <summary>An event of the conversation: who did it (none for the system), what and when.</summary>
public sealed class TicketEvent : Entity
{
    private TicketEvent()
    {
    }

    private TicketEvent(Guid ticketId, Guid? actorId, TicketEventKind kind, DateTime at)
    {
        TicketId = ticketId;
        ActorId = actorId;
        Kind = kind;
        At = at;
    }

    public Guid TicketId { get; private init; }

    public Guid? ActorId { get; private init; }

    public TicketEventKind Kind { get; private init; }

    public TicketStatus? FromStatus { get; private init; }

    public TicketStatus? ToStatus { get; private init; }

    public TicketAction? Action { get; private init; }

    public Guid? AssigneeId { get; private init; }

    public TicketPriority? FromPriority { get; private init; }

    public TicketPriority? ToPriority { get; private init; }

    public TicketTeam? FromTeam { get; private init; }

    public TicketTeam? ToTeam { get; private init; }

    /// <summary>The requester before, or the assignee who left.</summary>
    public Guid? FromPersonId { get; private init; }

    /// <summary>The new requester.</summary>
    public Guid? ToPersonId { get; private init; }

    /// <summary>The ticket at a vendor registered or updated.</summary>
    public Guid? VendorTicketId { get; private init; }

    public VendorTicketStatus? FromVendorStatus { get; private init; }

    public VendorTicketStatus? ToVendorStatus { get; private init; }

    /// <summary>The note of the update at the vendor.</summary>
    public string? Note { get; private init; }

    public DateTime At { get; private init; }

    public static TicketEvent StatusChanged(Guid ticketId, Guid? actorId, TicketTransition transition, DateTime at)
    {
        ArgumentNullException.ThrowIfNull(transition);
        return new TicketEvent(ticketId, actorId, TicketEventKind.StatusChanged, at)
        {
            FromStatus = transition.From,
            ToStatus = transition.To,
            Action = transition.Action,
        };
    }

    public static TicketEvent ClosedWithProject(Guid ticketId, Guid actorId, TicketStatus from, DateTime at) =>
        new(ticketId, actorId, TicketEventKind.ClosedWithProject, at) { FromStatus = from, ToStatus = TicketStatus.Closed };

    public static TicketEvent PriorityChanged(Guid ticketId, Guid actorId, TicketPriority from, TicketPriority to, DateTime at) =>
        new(ticketId, actorId, TicketEventKind.PriorityChanged, at) { FromPriority = from, ToPriority = to };

    public static TicketEvent AssigneeCleared(Guid ticketId, Guid actorId, Guid formerAssigneeId, DateTime at) =>
        new(ticketId, actorId, TicketEventKind.AssigneeCleared, at) { FromPersonId = formerAssigneeId };

    public static TicketEvent RequesterChanged(Guid ticketId, Guid actorId, Guid from, Guid to, DateTime at) =>
        new(ticketId, actorId, TicketEventKind.RequesterChanged, at) { FromPersonId = from, ToPersonId = to };

    public static TicketEvent TeamChanged(Guid ticketId, Guid actorId, TicketTeam from, TicketTeam to, DateTime at) =>
        new(ticketId, actorId, TicketEventKind.TeamChanged, at) { FromTeam = from, ToTeam = to };

    public static TicketEvent VendorTicketRegistered(Guid ticketId, Guid actorId, VendorTicket vendorTicket, DateTime at)
    {
        ArgumentNullException.ThrowIfNull(vendorTicket);
        return new TicketEvent(ticketId, actorId, TicketEventKind.VendorTicketRegistered, at)
        {
            VendorTicketId = vendorTicket.Id,
            ToVendorStatus = vendorTicket.Status,
        };
    }

    public static TicketEvent VendorStatusChanged(
        Guid ticketId, Guid actorId, Guid vendorTicketId, VendorTicketStatus from, VendorTicketStatus to, string? note, DateTime at) =>
        new(ticketId, actorId, TicketEventKind.VendorStatusChanged, at)
        {
            VendorTicketId = vendorTicketId,
            FromVendorStatus = from,
            ToVendorStatus = to,
            Note = note,
        };

    public static TicketEvent AssigneeSet(Guid ticketId, Guid actorId, Guid assigneeId, DateTime at) =>
        new(ticketId, actorId, TicketEventKind.AssigneeSet, at) { AssigneeId = assigneeId };
}
