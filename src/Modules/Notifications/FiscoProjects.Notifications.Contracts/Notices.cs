namespace FiscoProjects.Notifications.Contracts;

/// <summary>What happened, for the notices (ADR-0037, ADR-0038): one kind per event of the table of ADR-0037.</summary>
public enum NoticeKind
{
    /// <summary>A ticket was opened for the team the person serves.</summary>
    TicketOpened = 1,

    /// <summary>A message in a ticket the person follows.</summary>
    TicketMessage = 2,

    /// <summary>The person was mentioned in a message.</summary>
    TicketMentioned = 3,

    /// <summary>Who serves the ticket asked the requester for information.</summary>
    TicketInformationRequested = 4,

    /// <summary>The ticket was resolved and waits for the requester to confirm it.</summary>
    TicketResolved = 5,

    /// <summary>The person became the assignee.</summary>
    TicketAssigned = 6,

    /// <summary>The person became the requester.</summary>
    TicketRequesterChanged = 7,

    /// <summary>The ticket went to the team the person serves.</summary>
    TicketTransferred = 8,

    /// <summary>A deadline of the ticket is due today or on the next business day.</summary>
    TicketDueSoon = 9,

    /// <summary>A deadline of the ticket is overdue.</summary>
    TicketOverdue = 10,

    /// <summary>The status of a ticket the person follows changed.</summary>
    TicketStatusChanged = 11,

    /// <summary>A ticket at a vendor was registered or updated in a ticket the person follows.</summary>
    VendorTicketUpdated = 12,

    /// <summary>The person was included in a project.</summary>
    ProjectMemberAdded = 13,

    /// <summary>The role of the person in a project changed.</summary>
    ProjectRoleChanged = 14,

    /// <summary>The status of a project of the person changed.</summary>
    ProjectStatusChanged = 15,

    /// <summary>The dates of a task of the person changed, also pushed by another one.</summary>
    ScheduleDatesChanged = 16,
}

/// <summary>
/// Something that happened, told by the module that owns the item (ADR-0038): the kind, who should hear about it, who
/// did it (null for the system), the project of the item, the address of the item in the screens and its data. The
/// values carry only data of the item, never names of people: the channels show the current name of who did it. The
/// key, when there is one, stands for the item, so a later notice of the same key replaces an older one not yet sent,
/// as the new dates of a task.
/// </summary>
public sealed record Notice(
    NoticeKind Kind,
    IReadOnlyCollection<Guid> Recipients,
    Guid? ActorId,
    Guid? ProjectId,
    string Path,
    IReadOnlyDictionary<string, string> Values,
    string? Key = null);

/// <summary>
/// Where the modules tell what happened, after saving it (ADR-0037, ADR-0038). Who hears about it, and by which
/// channel, is decided by the Notifications module. It never throws: a failure is logged, and the action stays.
/// </summary>
public interface INoticeBoard
{
    Task PostAsync(IReadOnlyCollection<Notice> notices, CancellationToken cancellationToken);
}

/// <summary>Posts on every board registered: none when the Notifications module is not there, as in the tests of a module.</summary>
public static class NoticeBoards
{
    public static async Task PostAsync(
        this IEnumerable<INoticeBoard> boards, IReadOnlyCollection<Notice> notices, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(boards);
        ArgumentNullException.ThrowIfNull(notices);
        if (notices.Count == 0)
        {
            return;
        }

        foreach (var board in boards)
        {
            await board.PostAsync(notices, cancellationToken);
        }
    }
}

/// <summary>
/// The names of the values of the notices, shared by the modules that post them and the channels that show them. Codes
/// (a team, a status, a role) go as the names of the enums, days as yyyy-MM-dd.
/// </summary>
public static class NoticeValues
{
    /// <summary>The number of a ticket.</summary>
    public const string Number = "number";

    /// <summary>The title of a ticket.</summary>
    public const string Title = "title";

    /// <summary>The code of the project.</summary>
    public const string Project = "project";

    /// <summary>The team of a ticket: Functional or Development.</summary>
    public const string Team = "team";

    /// <summary>The status a ticket left.</summary>
    public const string From = "from";

    /// <summary>The status a ticket went to.</summary>
    public const string To = "to";

    /// <summary>The deadline of a ticket: FirstResponse or Resolution.</summary>
    public const string Deadline = "deadline";

    /// <summary>The day a deadline is due.</summary>
    public const string DueOn = "dueOn";

    /// <summary>The name of a vendor.</summary>
    public const string Vendor = "vendor";

    /// <summary>The status of a ticket at a vendor.</summary>
    public const string VendorStatus = "vendorStatus";

    /// <summary>The role of a person in a project.</summary>
    public const string Role = "role";

    /// <summary>The status of a project.</summary>
    public const string Status = "status";

    /// <summary>The name of an item of a schedule.</summary>
    public const string Item = "item";

    /// <summary>The first day of an item of a schedule.</summary>
    public const string Start = "start";

    /// <summary>The last day of an item of a schedule.</summary>
    public const string End = "end";

    /// <summary>Present when a ticket was closed because its project was cancelled (ADR-0043).</summary>
    public const string ClosedWithProject = "closedWithProject";
}
