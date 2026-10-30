using FiscoProjects.Administration.Contracts;
using FiscoProjects.Calendar.Contracts;
using FiscoProjects.MyWork.Contracts;
using FiscoProjects.MyWork.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.MyWork.Application;

/// <summary>
/// Meu trabalho of a person (ADR-0070, ADR-0096): the days of the window, the three views and the names of the people
/// the items show (who asked, who is in charge, for whom).
/// </summary>
public sealed record MyWorkResult(
    MyWorkWindow Window, IReadOnlyList<MyWorkViewEntries> Views, IReadOnlyDictionary<Guid, string> Names)
{
    public string NameOf(Guid id) => Names.TryGetValue(id, out var name) ? name : string.Empty;
}

/// <summary>
/// Puts together the items of the person from every module (<see cref="IMyWorkSource"/>) and groups them by urgency
/// (<see cref="MyWorkQueue"/>), today in the time zone and the calendar of the installation.
/// </summary>
public sealed class GetMyWork(
    IEnumerable<IMyWorkSource> sources,
    IWorkCalendar calendar,
    IInstallationSettings installation,
    TimeProvider clock,
    IUserDirectory users)
{
    public async Task<MyWorkResult> ExecuteAsync(Guid userId, bool isAdministrator, CancellationToken cancellationToken)
    {
        var info = await installation.GetAsync(cancellationToken);
        var days = await calendar.GetAsync(cancellationToken);
        var window = MyWorkWindow.Of(info.DayOf(clock.GetUtcNow().UtcDateTime), days);
        var query = new MyWorkQuery(userId, isAdministrator, window.Today, window.Until);
        var items = new List<MyWorkItem>();
        foreach (var source in sources)
        {
            items.AddRange(await source.ItemsAsync(query, cancellationToken));
        }

        var views = MyWorkQueue.ViewsOf(items, window);
        var people = views
            .SelectMany(view => view.Groups)
            .SelectMany(group => group.Entries)
            .SelectMany(entry => PeopleOf(entry.Item))
            .Distinct()
            .ToList();
        var names = people.Count == 0 ? new Dictionary<Guid, string>() : await users.NamesAsync(people, cancellationToken);
        return new MyWorkResult(window, views, names);
    }

    private static IEnumerable<Guid> PeopleOf(MyWorkItem item)
    {
        if (item.Ticket is { } ticket)
        {
            yield return ticket.RequesterId;
            if (ticket.AssigneeId is { } assignee)
            {
                yield return assignee;
            }
        }

        if (item.Acceptance is { } acceptance)
        {
            if (acceptance.RecipientId is { } recipient)
            {
                yield return recipient;
            }

            yield return acceptance.RequestedById;
        }
    }
}
