using System.Globalization;
using System.Xml.Linq;
using FiscoProjects.Calendar.Contracts;
using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Application;

/// <summary>
/// The schedule of a project in the XML format of the MS Project (MSPDI), built on the server (ADR-0021, ADR-0051): the
/// phases as summary tasks of level 1, the tasks and milestones in level 2 and the subtasks in level 3, in the order
/// of the Lista, with their dates, durations in business days, progress, owners (with their e-mails, ADR-0136), "finish
/// to start" dependencies and baseline, and the calendar of the installation for the period. Every item with dates of its own keeps its start by
/// a "start no earlier than" constraint, so the MS Project opens it with the same dates. The elements follow the order
/// of the schema of the MS Project.
/// </summary>
/// <summary>A person in charge of tasks, as a resource of the file: the name and the e-mail (none for an anonymized person).</summary>
public sealed record MsProjectOwner(string Name, string? Email);

public static class MsProjectExport
{
    public static readonly XNamespace Ns = "http://schemas.microsoft.com/project";

    private const int HoursPerDay = 8;

    /// <summary>A day of lag in the unit of the file: tenths of a minute of a day of 8 hours.</summary>
    public const int LagUnitsPerDay = HoursPerDay * 60 * 10;

    private static readonly TimeSpan DayStart = new(8, 0, 0);
    private static readonly TimeSpan DayEnd = new(17, 0, 0);

    /// <summary>The document of a schedule, as the Schedule tab shows it on the day.</summary>
    public static XDocument Of(
        string code,
        string name,
        Schedule schedule,
        ScheduleFigures figures,
        IBusinessDays days,
        IReadOnlyDictionary<Guid, MsProjectOwner> owners,
        DateTime createdAt,
        string? clientName = null)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(figures);
        ArgumentNullException.ThrowIfNull(days);
        ArgumentNullException.ThrowIfNull(owners);
        var start = figures.Start ?? DateOnly.FromDateTime(createdAt);
        var end = figures.End ?? start;
        // The numbers first: a dependency may point to an item further on.
        var uids = new Dictionary<Guid, int>();
        var lines = new List<(PhaseFigures Phase, ScheduleItem? Item, string Outline)>();
        var phaseNumber = 0;
        foreach (var phase in figures.Phases.Where(phase => phase.Start is not null))
        {
            phaseNumber++;
            lines.Add((phase, null, phaseNumber.ToString(CultureInfo.InvariantCulture)));
            var itemNumber = 0;
            var subtaskNumber = 0;
            foreach (var item in phase.Items)
            {
                if (item.ParentId is null)
                {
                    itemNumber++;
                    subtaskNumber = 0;
                }
                else
                {
                    subtaskNumber++;
                }

                uids[item.Id] = lines.Count + 1;
                lines.Add((phase, item, item.ParentId is null
                    ? $"{phaseNumber}.{itemNumber}"
                    : $"{phaseNumber}.{itemNumber}.{subtaskNumber}"));
            }
        }

        var tasks = new List<XElement> { Summary(0, "0", 0, $"{code} - {name}", start, end, figures.Progress, days) };
        tasks.AddRange(lines.Select((line, index) => line.Item is { } item
            ? ItemOf(item, index + 1, line.Outline, schedule, figures, uids, days)
            : Summary(index + 1, line.Outline, 1, line.Phase.Phase.Name, line.Phase.Start!.Value, line.Phase.End!.Value,
                line.Phase.Progress, days)));

        var ownerIds = schedule.Items.Where(item => item.OwnerId is not null).Select(item => item.OwnerId!.Value).Distinct().ToList();
        // The client in charge of tasks is one more resource, after the people, with the name only (ADR-0102).
        var clientInCharge = clientName is not null && schedule.Items.Any(item => item.OwnedByClient);
        var people = ownerIds.Select(owner => owners.TryGetValue(owner, out var found) ? found : new MsProjectOwner(string.Empty, null)).ToList();
        if (clientInCharge)
        {
            people.Add(new MsProjectOwner(clientName!, null));
        }

        // The e-mail lets the import suggest the person again (ADR-0135, ADR-0136).
        var resources = people.Select((person, index) => new XElement(Ns + "Resource",
            new XElement(Ns + "UID", index + 1),
            new XElement(Ns + "ID", index + 1),
            new XElement(Ns + "Name", person.Name),
            new XElement(Ns + "Type", 1),
            string.IsNullOrWhiteSpace(person.Email) ? null : new XElement(Ns + "EmailAddress", person.Email)));
        int? ResourceOf(ScheduleItem item) => item.OwnerId is { } ownerId
            ? ownerIds.IndexOf(ownerId) + 1
            : item.OwnedByClient && clientInCharge ? people.Count : null;
        var assignments = schedule.Items
            .Where(item => ResourceOf(item) is not null && uids.ContainsKey(item.Id))
            .Select((item, index) => new XElement(Ns + "Assignment",
                new XElement(Ns + "UID", index + 1),
                new XElement(Ns + "TaskUID", uids[item.Id]),
                new XElement(Ns + "ResourceUID", ResourceOf(item)),
                new XElement(Ns + "Units", 1)));

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(Ns + "Project",
                new XElement(Ns + "SaveVersion", 14),
                new XElement(Ns + "Name", $"{code}.xml"),
                new XElement(Ns + "Title", name),
                new XElement(Ns + "CreationDate", Moment(createdAt)),
                new XElement(Ns + "ScheduleFromStart", 1),
                new XElement(Ns + "StartDate", Moment(start, DayStart)),
                new XElement(Ns + "FinishDate", Moment(end, DayEnd)),
                new XElement(Ns + "CalendarUID", 1),
                new XElement(Ns + "DefaultStartTime", "08:00:00"),
                new XElement(Ns + "DefaultFinishTime", "17:00:00"),
                new XElement(Ns + "MinutesPerDay", HoursPerDay * 60),
                new XElement(Ns + "MinutesPerWeek", HoursPerDay * 60 * 5),
                new XElement(Ns + "DaysPerMonth", 20),
                new XElement(Ns + "Calendars", CalendarOf(start, end, days)),
                new XElement(Ns + "Tasks", tasks),
                new XElement(Ns + "Resources", resources),
                new XElement(Ns + "Assignments", assignments)));
    }

    private static XElement Summary(
        int uid, string outline, int level, string name, DateOnly start, DateOnly end, int progress, IBusinessDays days) =>
        new(Ns + "Task",
            new XElement(Ns + "UID", uid),
            new XElement(Ns + "ID", uid),
            new XElement(Ns + "Name", name),
            new XElement(Ns + "OutlineNumber", outline),
            new XElement(Ns + "OutlineLevel", level),
            new XElement(Ns + "Start", Moment(start, DayStart)),
            new XElement(Ns + "Finish", Moment(end, DayEnd)),
            new XElement(Ns + "Duration", Duration(days.CountBusinessDays(start, end))),
            new XElement(Ns + "DurationFormat", 7),
            new XElement(Ns + "Milestone", 0),
            new XElement(Ns + "Summary", 1),
            new XElement(Ns + "PercentComplete", progress),
            new XElement(Ns + "Active", 1),
            new XElement(Ns + "Manual", 0));

    private static XElement ItemOf(
        ScheduleItem item,
        int uid,
        string outline,
        Schedule schedule,
        ScheduleFigures figures,
        IReadOnlyDictionary<Guid, int> uids,
        IBusinessDays days)
    {
        var milestone = item.Kind == ScheduleItemKind.Milestone;
        var summary = figures.Summaries.Contains(item.Id);
        // A milestone sits at the end of its day, as the MS Project puts one right after a task.
        var start = milestone ? Moment(item.Start, DayEnd) : Moment(item.Start, DayStart);
        var task = new XElement(Ns + "Task",
            new XElement(Ns + "UID", uid),
            new XElement(Ns + "ID", uid),
            new XElement(Ns + "Name", item.Name),
            new XElement(Ns + "OutlineNumber", outline),
            new XElement(Ns + "OutlineLevel", item.ParentId is null ? 2 : 3),
            new XElement(Ns + "Start", start),
            new XElement(Ns + "Finish", Moment(item.End, DayEnd)),
            new XElement(Ns + "Duration", Duration(milestone ? 0 : item.Duration)),
            new XElement(Ns + "DurationFormat", 7),
            new XElement(Ns + "Milestone", milestone ? 1 : 0),
            new XElement(Ns + "Summary", summary ? 1 : 0),
            new XElement(Ns + "PercentComplete", milestone ? (item.ReachedOn is null ? 0 : 100) : item.Progress));
        if (!summary)
        {
            // Start no earlier than: the MS Project keeps the dates the server computed.
            task.Add(
                new XElement(Ns + "ConstraintType", 4),
                new XElement(Ns + "ConstraintDate", start));
        }

        if (item.Deadline is { } deadline)
        {
            // The deadline at the end of its day, as the MS Project marks one (GanttA-2).
            task.Add(new XElement(Ns + "Deadline", Moment(deadline, DayEnd)));
        }

        // The kind and the lag in days (ADR-0137): the lag in tenths of a minute of a day of 8 hours, shown in days.
        foreach (var dependency in schedule.Dependencies.Where(d => d.SuccessorId == item.Id && uids.ContainsKey(d.PredecessorId)))
        {
            task.Add(new XElement(Ns + "PredecessorLink",
                new XElement(Ns + "PredecessorUID", uids[dependency.PredecessorId]),
                new XElement(Ns + "Type", LinkTypeOf(dependency.Type)),
                new XElement(Ns + "CrossProject", 0),
                new XElement(Ns + "LinkLag", dependency.Lag * LagUnitsPerDay),
                new XElement(Ns + "LagFormat", 7)));
        }

        if (schedule.Baseline?.ItemOf(item.Id) is { } baseline)
        {
            task.Add(new XElement(Ns + "Baseline",
                new XElement(Ns + "Number", 0),
                new XElement(Ns + "Start", milestone ? Moment(baseline.Start, DayEnd) : Moment(baseline.Start, DayStart)),
                new XElement(Ns + "Finish", Moment(baseline.End, DayEnd)),
                new XElement(Ns + "Duration", Duration(milestone ? 0 : days.CountBusinessDays(baseline.Start, baseline.End)))));
        }

        task.Add(new XElement(Ns + "Active", 1), new XElement(Ns + "Manual", 0));
        return task;
    }

    /// <summary>
    /// The calendar of the installation for the period: the days of the week with business days in it, from 08:00 to
    /// 12:00 and from 13:00 to 17:00, and the days off in those days of the week as exceptions.
    /// </summary>
    private static XElement CalendarOf(DateOnly start, DateOnly end, IBusinessDays days)
    {
        var first = start.AddDays(-7);
        var last = end.AddDays(7);
        var period = Enumerable.Range(0, last.DayNumber - first.DayNumber + 1).Select(first.AddDays).ToList();
        var working = period.Where(days.IsBusinessDay).Select(day => day.DayOfWeek).ToHashSet();
        var weekDays = Enum.GetValues<DayOfWeek>().Select(day => working.Contains(day)
            ? new XElement(Ns + "WeekDay",
                new XElement(Ns + "DayType", (int)day + 1),
                new XElement(Ns + "DayWorking", 1),
                new XElement(Ns + "WorkingTimes",
                    WorkingTime("08:00:00", "12:00:00"),
                    WorkingTime("13:00:00", "17:00:00")))
            : new XElement(Ns + "WeekDay",
                new XElement(Ns + "DayType", (int)day + 1),
                new XElement(Ns + "DayWorking", 0)));
        var exceptions = period
            .Where(day => working.Contains(day.DayOfWeek) && !days.IsBusinessDay(day))
            .Select(day => new XElement(Ns + "WeekDay",
                new XElement(Ns + "DayType", 0),
                new XElement(Ns + "DayWorking", 0),
                new XElement(Ns + "TimePeriod",
                    new XElement(Ns + "FromDate", Moment(day, TimeSpan.Zero)),
                    new XElement(Ns + "ToDate", Moment(day, new TimeSpan(23, 59, 0))))));
        return new XElement(Ns + "Calendar",
            new XElement(Ns + "UID", 1),
            new XElement(Ns + "Name", "Calendário da instalação"),
            new XElement(Ns + "IsBaseCalendar", 1),
            new XElement(Ns + "BaseCalendarUID", -1),
            new XElement(Ns + "WeekDays", weekDays.Concat(exceptions)));
    }

    private static XElement WorkingTime(string from, string to) =>
        new(Ns + "WorkingTime", new XElement(Ns + "FromTime", from), new XElement(Ns + "ToTime", to));

    /// <summary>The kind of a dependency as the MS Project numbers it: 0 ends with, 1 finishes then starts, 3 starts with.</summary>
    public static int LinkTypeOf(ScheduleLinkType type) => type switch
    {
        ScheduleLinkType.StartToStart => 3,
        ScheduleLinkType.FinishToFinish => 0,
        _ => 1,
    };

    private static string Duration(int businessDays) =>
        string.Create(CultureInfo.InvariantCulture, $"PT{businessDays * HoursPerDay}H0M0S");

    private static string Moment(DateOnly day, TimeSpan time) =>
        day.ToDateTime(TimeOnly.FromTimeSpan(time)).ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

    private static string Moment(DateTime moment) =>
        moment.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
}
