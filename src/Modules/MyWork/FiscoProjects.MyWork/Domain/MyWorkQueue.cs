using FiscoProjects.Calendar.Contracts;
using FiscoProjects.MyWork.Contracts;

namespace FiscoProjects.MyWork.Domain;

/// <summary>The views of Meu trabalho (ADR-0070), in the order of the screen.</summary>
public enum MyWorkView
{
    ForMe = 1,
    OpenedByMe = 2,
    Following = 3,
}

/// <summary>The groups of the queue (ADR-0070), in the order of the screen.</summary>
public enum MyWorkGroup
{
    /// <summary>Due before today.</summary>
    Late = 1,

    /// <summary>Waiting for the answer of the person, whatever the deadline.</summary>
    Waiting = 2,

    /// <summary>Due today or up to the next business day.</summary>
    Soon = 3,

    /// <summary>Due after the next business day, up to 7 days after today (ADR-0096).</summary>
    Week = 4,
}

/// <summary>The one action of a line of the queue (ADR-0070, ADR-0096).</summary>
public enum MyWorkAction
{
    Open = 1,
    Reply = 2,
    Assign = 3,
    Start = 4,
    Update = 5,
    Complete = 6,
    Remind = 7,
}

/// <summary>
/// The days of the queue: today, the next business day of the calendar of the installation and the last day of the
/// window, 7 calendar days after today (ADR-0096).
/// </summary>
public sealed record MyWorkWindow(DateOnly Today, DateOnly NextBusinessDay, DateOnly Until)
{
    public const int Days = 7;

    public static MyWorkWindow Of(DateOnly today, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(days);
        return new MyWorkWindow(today, days.AddBusinessDays(today, 1), today.AddDays(Days));
    }
}

/// <summary>An item in its group, with its action.</summary>
public sealed record MyWorkEntry(MyWorkItem Item, MyWorkGroup Group, MyWorkAction Action);

/// <summary>A group of a view, with its items in order.</summary>
public sealed record MyWorkGroupEntries(MyWorkGroup Group, IReadOnlyList<MyWorkEntry> Entries);

/// <summary>A view of the queue: how many items it has and the groups that have some.</summary>
public sealed record MyWorkViewEntries(MyWorkView View, int Count, IReadOnlyList<MyWorkGroupEntries> Groups);

/// <summary>
/// The rule of urgency of Meu trabalho in one place (ADR-0070, ADR-0096): the group of each item by its deadline, the
/// action of each line and the order of the items.
/// </summary>
public static class MyWorkQueue
{
    private static readonly IReadOnlyList<(MyWorkView View, MyWorkViews Flag)> AllViews =
    [
        (MyWorkView.ForMe, MyWorkViews.ForMe),
        (MyWorkView.OpenedByMe, MyWorkViews.OpenedByMe),
        (MyWorkView.Following, MyWorkViews.Following),
    ];

    /// <summary>
    /// The group of an item: waiting for the person; late, before today; soon, up to the next business day; the week,
    /// up to the last day of the window. Null for an item out of the queue: due after the window, or without a deadline
    /// while it waits for someone else.
    /// </summary>
    public static MyWorkGroup? GroupOf(MyWorkItem item, MyWorkWindow window)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(window);
        if (item.WaitsForYou)
        {
            return MyWorkGroup.Waiting;
        }

        return item.DueOn switch
        {
            null => null,
            { } due when due < window.Today => MyWorkGroup.Late,
            { } due when due <= window.NextBusinessDay => MyWorkGroup.Soon,
            { } due when due <= window.Until => MyWorkGroup.Week,
            _ => null,
        };
    }

    /// <summary>
    /// The action of an item in its group: Responder, what waits for the person; Atribuir, a ticket without an assignee
    /// for who changes the assignee; Abrir, the other tickets; Iniciar, a task at 0%; Concluir, a task late, of today or
    /// of the next business day; Atualizar, the other tasks; Lembrar, a request the person made.
    /// </summary>
    public static MyWorkAction ActionOf(MyWorkItem item, MyWorkGroup group)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Kind switch
        {
            MyWorkKind.Ticket when item.WaitsForYou => MyWorkAction.Reply,
            MyWorkKind.Ticket when item.Ticket is { AssigneeId: null, CanAssign: true } => MyWorkAction.Assign,
            MyWorkKind.Ticket => MyWorkAction.Open,
            MyWorkKind.Task when item.Task is { Progress: 0 } => MyWorkAction.Start,
            MyWorkKind.Task when group is MyWorkGroup.Late or MyWorkGroup.Soon => MyWorkAction.Complete,
            MyWorkKind.Task => MyWorkAction.Update,
            _ when item.WaitsForYou => MyWorkAction.Reply,
            _ => MyWorkAction.Remind,
        };
    }

    /// <summary>
    /// The three views of the items, each with the groups that have items, in the order of the screen, and in each group
    /// the items by deadline, the oldest first (in Aguardando você, the longest waiting first), then by kind and title.
    /// </summary>
    public static IReadOnlyList<MyWorkViewEntries> ViewsOf(IEnumerable<MyWorkItem> items, MyWorkWindow window)
    {
        ArgumentNullException.ThrowIfNull(items);
        var entries = items
            .Select(item => (Item: item, Group: GroupOf(item, window)))
            .Where(entry => entry.Group is not null)
            .Select(entry => new MyWorkEntry(entry.Item, entry.Group!.Value, ActionOf(entry.Item, entry.Group.Value)))
            .ToList();
        return
        [
            .. AllViews.Select(view =>
            {
                var inView = entries.Where(entry => entry.Item.Views.HasFlag(view.Flag)).ToList();
                return new MyWorkViewEntries(
                    view.View,
                    inView.Count,
                    [.. inView
                        .GroupBy(entry => entry.Group)
                        .OrderBy(group => group.Key)
                        .Select(group => new MyWorkGroupEntries(group.Key, InOrder(group)))]);
            }),
        ];
    }

    private static List<MyWorkEntry> InOrder(IEnumerable<MyWorkEntry> entries) =>
    [
        .. entries
            .OrderBy(entry => entry.Group == MyWorkGroup.Waiting ? entry.Item.WaitingSince ?? DateTime.MaxValue : DateTime.MinValue)
            .ThenBy(entry => entry.Item.DueOn ?? DateOnly.MaxValue)
            .ThenBy(entry => entry.Item.Kind)
            .ThenBy(entry => entry.Item.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => entry.Item.Id),
    ];
}
