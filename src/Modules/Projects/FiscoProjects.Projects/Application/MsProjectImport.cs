using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using FiscoProjects.Calendar.Contracts;
using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Application;

/// <summary>Why a file was not read as a schedule of the MS Project (ADR-0135).</summary>
public enum MsProjectRefusal
{
    /// <summary>Not an XML of the MS Project: a .mpp, an XML of another program, with a DTD, or not XML at all.</summary>
    NotMsProjectXml = 1,

    /// <summary>Larger than 5 MB.</summary>
    TooLarge = 2,

    /// <summary>More than 2,000 tasks.</summary>
    TooManyTasks = 3,

    /// <summary>No task to bring.</summary>
    NoTasks = 4,

    /// <summary>Dates beyond the days a schedule may use, from 2000 to 2099.</summary>
    OutOfRange = 5,
}

/// <summary>The file is not read as a schedule of the MS Project, for the reason given (ADR-0135).</summary>
public sealed class MsProjectFileRefusedException(MsProjectRefusal reason)
    : InvalidOperationException($"The file is not read as a schedule of the MS Project: {reason}.")
{
    public MsProjectRefusal Reason { get; } = reason;
}

/// <summary>A dependency of a line on another, as the file has it: the kind (1 is "finish to start") and the lag.</summary>
public sealed record MsProjectLink(int PredecessorUid, int Type, long Lag, bool CrossProject, int LagFormat = 7);

/// <summary>
/// A line of the file, below the summary of the whole project: its number, level, whether it is a milestone, active or
/// an empty line, the name, the days it starts and finishes, the percent complete, the lines it depends on and its
/// deadline (GanttA-2).
/// </summary>
public sealed record MsProjectRow(
    int Uid,
    int Level,
    bool Milestone,
    bool Active,
    bool Empty,
    string Name,
    DateOnly? Start,
    DateOnly? Finish,
    int PercentComplete,
    IReadOnlyList<MsProjectLink> Links,
    DateOnly? Deadline = null);

/// <summary>
/// A resource of the file, with the name and the e-mail when the file has one: a work resource is a person; the others,
/// of material and cost, stay out of the schedule (ADR-0135).
/// </summary>
public sealed record MsProjectResource(int Uid, string Name, string? Email, bool IsWork = true);

/// <summary>A resource on a line, in the order of the file.</summary>
public sealed record MsProjectAssignment(int TaskUid, int ResourceUid);

/// <summary>A file of the MS Project as read: its title, its lines, its resources and the assignments.</summary>
public sealed record MsProjectFile(
    string? Title, IReadOnlyList<MsProjectRow> Rows, IReadOnlyList<MsProjectResource> Resources, IReadOnlyList<MsProjectAssignment> Assignments);

/// <summary>What the conversion changed, for the person to know before creating the project (ADR-0135).</summary>
public enum MsProjectWarningKind
{
    /// <summary>Tasks right in the level 1 went into a phase with the title of the project.</summary>
    LooseTasks = 1,

    /// <summary>Tasks below the level 3 became subtasks of the task of level 2 they are in.</summary>
    DeepTasks = 2,

    /// <summary>Summary tasks below the level 2 went away, their tasks staying.</summary>
    DroppedSummaries = 3,

    /// <summary>Dependencies "start to finish", which the FiscoProjects does not have, became "finish to start" (ADR-0137).</summary>
    OtherDependencies = 4,

    /// <summary>Dependencies of or on a summary task (a phase included) went away.</summary>
    SummaryDependencies = 5,

    /// <summary>
    /// Lags in hours that are not whole days, in calendar days or in percent became business days, rounded, and lags
    /// past -99 or 999 business days stayed at the limit (ADR-0137).
    /// </summary>
    DependencyLags = 6,

    /// <summary>Dependencies that would make a cycle went away.</summary>
    DependencyCycles = 7,

    /// <summary>Tasks longer than 999 business days got 999.</summary>
    LongTasks = 8,

    /// <summary>Names longer than the FiscoProjects keeps were cut.</summary>
    CutNames = 9,

    /// <summary>Tasks with more than one resource got the first.</summary>
    SeveralResources = 10,

    /// <summary>Milestones with resources: a milestone has no owner (ADR-0021).</summary>
    NoOwner = 11,

    /// <summary>Lines without dates went away.</summary>
    NoDates = 12,

    /// <summary>Resources of material and cost stayed out: only people own tasks.</summary>
    OtherResources = 13,

    /// <summary>Work resources that are the first of no task brought stayed out.</summary>
    UnusedResources = 14,

    /// <summary>Inactive lines, and the lines below them, stayed out.</summary>
    InactiveTasks = 15,

    /// <summary>Blank or nameless lines, and the lines below them, stayed out; only how many, as they have no name.</summary>
    BlankLines = 16,
}

/// <summary>
/// A change of the conversion, with the names of the lines or resources it touched ("A → B" for a dependency) and how
/// many: the names, but for the blank lines, which have none.
/// </summary>
public sealed record MsProjectWarning(MsProjectWarningKind Kind, IReadOnlyList<string> Names, int Count);

/// <summary>
/// How much of the file came: its tasks (the lines with a name, the inactive ones included) and the ones that became a
/// phase, a task, a subtask or a milestone; its resources (those with a name, of any kind) and the ones that own tasks.
/// </summary>
public sealed record MsProjectCounts(int FileTasks, int Tasks, int FileResources, int Resources);

/// <summary>A work resource of the file that is the first of some task: its name, its e-mail and how many tasks it has.</summary>
public sealed record MsProjectImportedResource(string Name, string? Email, int Tasks);

/// <summary>
/// A file converted into the schedule of a new project (ADR-0135): its title, its first day, the outline, the position
/// in <see cref="Resources"/> of the resource of each item (none without one), the changes of the conversion and how
/// much of the file came.
/// </summary>
public sealed record MsProjectSchedule(
    string Title,
    DateOnly FirstDay,
    SchedulePlanOutline Outline,
    IReadOnlyList<int?> ItemResources,
    IReadOnlyList<MsProjectImportedResource> Resources,
    IReadOnlyList<MsProjectWarning> Warnings,
    MsProjectCounts Counts);

/// <summary>
/// Reads an XML of the MS Project (MSPDI), as it saves with "Save as", "XML", and converts it into the outline of a new
/// schedule by the rules of ADR-0135. The file is not trusted: no DTD nor external entity, up to 5 MB and 2,000 tasks.
/// </summary>
public static class MsProjectImport
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const int MaxTasks = 2_000;

    private const int FinishToStart = 1;
    private const int StartToStart = 3;
    private const int FinishToFinish = 0;

    /// <summary>The formats of a lag (LagFormat): in days, in percent, and the ones in calendar time.</summary>
    private const int DaysFormat = 7;
    private const int PercentFormat = 19;
    private const int ElapsedPercentFormat = 20;
    private static readonly HashSet<int> ElapsedFormats = [4, 6, 8, 10, 12, 36, 38, 40, 42, 44];

    /// <summary>Reads the file; never returns without a valid file.</summary>
    /// <exception cref="MsProjectFileRefusedException">The file is not read, with the reason.</exception>
    public static MsProjectFile Read(Stream content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var buffer = new MemoryStream();
        var chunk = new byte[81_920];
        int read;
        while ((read = content.Read(chunk, 0, chunk.Length)) > 0)
        {
            if (buffer.Length + read > MaxBytes)
            {
                throw new MsProjectFileRefusedException(MsProjectRefusal.TooLarge);
            }

            buffer.Write(chunk, 0, read);
        }

        buffer.Position = 0;
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 0,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
        };
        XDocument document;
        try
        {
            using var reader = XmlReader.Create(buffer, settings);
            document = XDocument.Load(reader);
        }
        catch (XmlException)
        {
            throw new MsProjectFileRefusedException(MsProjectRefusal.NotMsProjectXml);
        }

        var ns = MsProjectExport.Ns;
        if (document.Root is not { } project || project.Name != ns + "Project")
        {
            throw new MsProjectFileRefusedException(MsProjectRefusal.NotMsProjectXml);
        }

        // A blank line of the MS Project (IsNull) may come without a level; the summary of the whole project is at level 0.
        var tasks = project.Element(ns + "Tasks")?.Elements(ns + "Task")
            .Where(task => Number(task.Element(ns + "OutlineLevel")) is >= 1 || Flag(task.Element(ns + "IsNull")) == true)
            .ToList() ?? [];
        if (tasks.Count > MaxTasks)
        {
            throw new MsProjectFileRefusedException(MsProjectRefusal.TooManyTasks);
        }

        var rows = tasks
            .Where(task => Number(task.Element(ns + "UID")) is not null)
            .Select(task => new MsProjectRow(
                Number(task.Element(ns + "UID"))!.Value,
                Number(task.Element(ns + "OutlineLevel")) ?? 1,
                Flag(task.Element(ns + "Milestone")) ?? false,
                Flag(task.Element(ns + "Active")) ?? true,
                Flag(task.Element(ns + "IsNull")) ?? false,
                ((string?)task.Element(ns + "Name"))?.Trim() ?? string.Empty,
                Day(task.Element(ns + "Start")),
                Day(task.Element(ns + "Finish")),
                Math.Clamp(Number(task.Element(ns + "PercentComplete")) ?? 0, 0, 100),
                [.. task.Elements(ns + "PredecessorLink")
                    .Where(link => Number(link.Element(ns + "PredecessorUID")) is not null)
                    .Select(link => new MsProjectLink(
                        Number(link.Element(ns + "PredecessorUID"))!.Value,
                        Number(link.Element(ns + "Type")) ?? FinishToStart,
                        long.TryParse((string?)link.Element(ns + "LinkLag"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var lag) ? lag : 0,
                        Flag(link.Element(ns + "CrossProject")) ?? false,
                        Number(link.Element(ns + "LagFormat")) ?? DaysFormat))],
                Day(task.Element(ns + "Deadline"))))
            .ToList();
        if (rows.Count == 0)
        {
            throw new MsProjectFileRefusedException(MsProjectRefusal.NoTasks);
        }

        // The MS Project never repeats the number of a line.
        if (rows.Select(row => row.Uid).Distinct().Count() < rows.Count)
        {
            throw new MsProjectFileRefusedException(MsProjectRefusal.NotMsProjectXml);
        }

        // A work resource is of type 1, the one the MS Project takes when the file says none; 0 is material and 2 cost.
        var resources = project.Element(ns + "Resources")?.Elements(ns + "Resource")
            .Select(resource => (Uid: Number(resource.Element(ns + "UID")), Name: ((string?)resource.Element(ns + "Name"))?.Trim(),
                Email: ((string?)resource.Element(ns + "EmailAddress"))?.Trim(), IsWork: (Number(resource.Element(ns + "Type")) ?? 1) == 1))
            .Where(resource => resource.Uid is not null && !string.IsNullOrEmpty(resource.Name))
            .Select(resource => new MsProjectResource(
                resource.Uid!.Value, resource.Name!, string.IsNullOrEmpty(resource.Email) ? null : resource.Email, resource.IsWork))
            .ToList() ?? [];
        var assignments = project.Element(ns + "Assignments")?.Elements(ns + "Assignment")
            .Select(assignment => (Task: Number(assignment.Element(ns + "TaskUID")), Resource: Number(assignment.Element(ns + "ResourceUID"))))
            .Where(assignment => assignment.Task is not null && assignment.Resource is not null)
            .Select(assignment => new MsProjectAssignment(assignment.Task!.Value, assignment.Resource!.Value))
            .ToList() ?? [];
        var title = ((string?)project.Element(ns + "Title"))?.Trim();
        return new MsProjectFile(string.IsNullOrEmpty(title) ? null : title, rows, resources, assignments);
    }

    /// <summary>
    /// Converts the file into the outline of a schedule by the rules of ADR-0135, the days counted in the calendar of the
    /// installation; <paramref name="fallbackTitle"/> names the phase of the loose tasks when the file has no title.
    /// </summary>
    /// <exception cref="MsProjectFileRefusedException">No task to bring.</exception>
    public static MsProjectSchedule Convert(MsProjectFile file, string fallbackTitle, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(days);
        return new Conversion(file, file.Title ?? fallbackTitle, days).Run();
    }

    private static int? Number(XElement? element) =>
        int.TryParse((string?)element, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null;

    private static bool? Flag(XElement? element) => ((string?)element)?.Trim() switch
    {
        "1" or "true" => true,
        "0" or "false" => false,
        _ => null,
    };

    private static DateOnly? Day(XElement? element) =>
        DateTime.TryParse((string?)element, CultureInfo.InvariantCulture, DateTimeStyles.None, out var moment)
            ? DateOnly.FromDateTime(moment)
            : null;

    /// <summary>What a kept line became: a phase, a task (of level 2 or a subtask), a milestone or a summary that went away.</summary>
    private enum Role
    {
        Phase,
        Item,
        DroppedSummary,
    }

    private sealed record Line(MsProjectRow Row, bool HasChildren);

    /// <summary>One conversion, with the warnings it gathers.</summary>
    private sealed class Conversion(MsProjectFile file, string title, IBusinessDays days)
    {
        private readonly Dictionary<MsProjectWarningKind, List<string>> _warnings = [];
        private readonly List<string> _phases = [];
        private readonly List<Draft> _items = [];
        private readonly Dictionary<int, Role> _roles = [];
        private readonly Dictionary<int, string> _names = [];
        private int _blankLines;

        public MsProjectSchedule Run()
        {
            Place(Kept());
            // A task of level 2 whose lines went away keeps its own dates, or goes away without them.
            foreach (var summary in _items.Where(item => item.IsSummary).ToList())
            {
                var subtasks = _items.Where(item => item.Parent == summary).ToList();
                summary.IsSummary = subtasks.Count > 0;
                summary.Start = summary.IsSummary ? subtasks.Min(item => item.Start) : summary.Row.Start;
                if (summary.Start is null)
                {
                    Warn(MsProjectWarningKind.NoDates, summary.Name);
                    _items.Remove(summary);
                    _roles.Remove(summary.Row.Uid);
                }
            }

            if (_items.Count == 0)
            {
                throw new MsProjectFileRefusedException(MsProjectRefusal.NoTasks);
            }

            var first = _items.Min(item => item.Start!.Value);
            var dependencies = Dependencies();
            var ordered = Ordered(dependencies);
            var position = ordered.Select((item, index) => (item, index)).ToDictionary(pair => pair.item, pair => pair.index);
            var (resources, itemResources, fileResources) = Resources(ordered);
            var outline = new SchedulePlanOutline(
                [.. _phases.Select(name => new OutlinePhase(name))],
                [.. ordered.Select(item => new OutlineItem(
                    item.Phase,
                    item.Parent is { } parent ? position[parent] : null,
                    item.Kind,
                    item.Name,
                    OffsetOf(first, item.Start!.Value),
                    item.Kind == ScheduleItemKind.Milestone ? 0 : DurationOf(item),
                    item.IsSummary ? 0 : item.Progress,
                    DeadlineOffset: DeadlineOffsetOf(first, item.Row.Deadline)))],
                [.. dependencies
                    .Where(pair => !pair.Dropped)
                    .Select(pair => new OutlineDependency(position[pair.Predecessor], position[pair.Successor], pair.Type, pair.Lag))
                    .Distinct()
                    .OrderBy(dependency => dependency.Successor)
                    .ThenBy(dependency => dependency.Predecessor)]);
            var warnings = _warnings.Select(pair => new MsProjectWarning(pair.Key, pair.Value, pair.Value.Count)).ToList();
            if (_blankLines > 0)
            {
                warnings.Add(new MsProjectWarning(MsProjectWarningKind.BlankLines, [], _blankLines));
            }

            return new MsProjectSchedule(
                title,
                first,
                outline,
                itemResources,
                resources,
                [.. warnings.OrderBy(warning => warning.Kind)],
                new MsProjectCounts(
                    file.Rows.Count(row => !IsBlank(row)),
                    _roles.Values.Count(role => role is Role.Phase or Role.Item),
                    fileResources,
                    resources.Count));
        }

        /// <summary>
        /// The lines to bring, in the order of the file, each saying whether lines of the file are under it: a blank line
        /// goes alone, and an inactive or nameless line goes with the lines under it, each with its warning.
        /// </summary>
        private List<Line> Kept()
        {
            var rows = file.Rows.Where(row => !row.Empty).ToList();
            _blankLines += file.Rows.Count - rows.Count;
            var kept = new List<Line>();
            int? skipBelow = null;
            var belowInactive = false;
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                if (skipBelow is { } level && row.Level > level)
                {
                    Skip(row, belowInactive);
                    continue;
                }

                skipBelow = null;
                if (IsBlank(row) || !row.Active)
                {
                    skipBelow = row.Level;
                    belowInactive = !IsBlank(row);
                    Skip(row, belowInactive);
                    continue;
                }

                kept.Add(new Line(row, index + 1 < rows.Count && rows[index + 1].Level > row.Level));
            }

            return kept;
        }

        /// <summary>A line that stays out: by its name, when inactive or below an inactive one, or counted as blank.</summary>
        private void Skip(MsProjectRow row, bool belowInactive)
        {
            if (belowInactive && !IsBlank(row))
            {
                Warn(MsProjectWarningKind.InactiveTasks, row.Name);
            }
            else
            {
                _blankLines++;
            }
        }

        private static bool IsBlank(MsProjectRow row) => row.Empty || row.Name.Length == 0;

        /// <summary>Each line in its place: the phases, the items and their tasks, as the levels say (ADR-0088, ADR-0135).</summary>
        private void Place(List<Line> lines)
        {
            int? phase = null;
            int? loosePhase = null;
            Draft? top = null;
            foreach (var (row, hasChildren) in lines)
            {
                if (row.Level == 1 && hasChildren)
                {
                    phase = AddPhase(row.Name);
                    top = null;
                    _roles[row.Uid] = Role.Phase;
                    continue;
                }

                // A summary below the level 2 goes away; its tasks go into the task of level 2 (ADR-0135).
                if (row.Level >= 3 && hasChildren && top is not null)
                {
                    Warn(MsProjectWarningKind.DroppedSummaries, row.Name);
                    _roles[row.Uid] = Role.DroppedSummary;
                    _names[row.Uid] = row.Name;
                    continue;
                }

                // Right in the level 1, or without a phase above: the phase with the title of the project.
                var inPhase = row.Level >= 2 && phase is not null;
                if (!inPhase)
                {
                    loosePhase ??= AddPhase(title);
                    Warn(MsProjectWarningKind.LooseTasks, row.Name);
                }

                var itemPhase = inPhase ? phase!.Value : loosePhase!.Value;
                phase = row.Level == 1 ? null : itemPhase;
                var parent = row.Level >= 3 ? top : null;
                var summary = hasChildren && parent is null;
                top = parent;
                if (row.Level >= 4 && parent is not null)
                {
                    Warn(MsProjectWarningKind.DeepTasks, row.Name);
                }

                if (row.Start is null && !summary)
                {
                    Warn(MsProjectWarningKind.NoDates, row.Name);
                    continue;
                }

                var item = new Draft(
                    row,
                    itemPhase,
                    parent,
                    row.Milestone && !hasChildren ? ScheduleItemKind.Milestone : ScheduleItemKind.Task,
                    Cut(row.Name, Schedule.ItemNameMaxLength),
                    summary);
                _items.Add(item);
                _roles[row.Uid] = Role.Item;
                _names[row.Uid] = item.Name;
                if (summary)
                {
                    top = item;
                }
            }
        }

        private int AddPhase(string name)
        {
            _phases.Add(Cut(name, Schedule.PhaseNameMaxLength));
            return _phases.Count - 1;
        }

        private string Cut(string name, int length)
        {
            if (name.Length <= length)
            {
                return name;
            }

            var cut = name[..length].TrimEnd();
            Warn(MsProjectWarningKind.CutNames, cut);
            return cut;
        }

        /// <summary>
        /// The dependencies between items with dates of their own, with their kinds and lags in business days (ADR-0137);
        /// "start to finish" becomes "finish to start", and the ones of a summary go away, each with its warning (ADR-0088).
        /// </summary>
        private List<Dependency> Dependencies()
        {
            var byUid = _items.ToDictionary(item => item.Row.Uid);
            var found = new List<Dependency>();
            foreach (var successor in file.Rows)
            {
                foreach (var link in successor.Links)
                {
                    if (link.CrossProject || !_roles.TryGetValue(link.PredecessorUid, out var before) || !_roles.TryGetValue(successor.Uid, out var after))
                    {
                        continue;
                    }

                    var pair = $"{NameOf(link.PredecessorUid)} → {NameOf(successor.Uid)}";
                    if (before != Role.Item || after != Role.Item || byUid[link.PredecessorUid].IsSummary || byUid[successor.Uid].IsSummary)
                    {
                        Warn(MsProjectWarningKind.SummaryDependencies, pair);
                    }
                    else if (link.PredecessorUid != successor.Uid)
                    {
                        var type = TypeOf(link.Type);
                        if (type is null)
                        {
                            Warn(MsProjectWarningKind.OtherDependencies, pair);
                        }

                        var predecessor = byUid[link.PredecessorUid];
                        var (lag, exact) = LagOf(link, predecessor);
                        if (!exact)
                        {
                            Warn(MsProjectWarningKind.DependencyLags, pair);
                        }

                        found.Add(new Dependency(predecessor, byUid[successor.Uid], pair, type ?? ScheduleLinkType.FinishToStart, lag));
                    }
                }
            }

            return found;
        }

        /// <summary>The kind of a link of the file: 1 finish to start, 3 start to start, 0 finish to finish; none for the others.</summary>
        private static ScheduleLinkType? TypeOf(int type) => type switch
        {
            FinishToStart => ScheduleLinkType.FinishToStart,
            StartToStart => ScheduleLinkType.StartToStart,
            FinishToFinish => ScheduleLinkType.FinishToFinish,
            _ => null,
        };

        /// <summary>
        /// The lag of a link in business days and whether it came exact (ADR-0137): in work time (minutes, hours, days,
        /// weeks, months) by the day of 8 hours, in calendar time by 5 business days in 7, and in percent of the
        /// duration of the item before; rounded, between -99 and 999.
        /// </summary>
        private (int Lag, bool Exact) LagOf(MsProjectLink link, Draft predecessor)
        {
            if (link.Lag == 0)
            {
                return (0, true);
            }

            double days;
            var exact = false;
            if (link.LagFormat is PercentFormat or ElapsedPercentFormat)
            {
                days = link.Lag / 10.0 / 100.0 * (predecessor.Kind == ScheduleItemKind.Milestone ? 0 : DurationOf(predecessor));
            }
            else if (ElapsedFormats.Contains(link.LagFormat))
            {
                days = link.Lag / (10.0 * 60 * 24) * 5 / 7;
            }
            else
            {
                days = link.Lag / (double)MsProjectExport.LagUnitsPerDay;
                exact = link.Lag % MsProjectExport.LagUnitsPerDay == 0;
            }

            var rounded = (int)Math.Clamp(Math.Round(days, MidpointRounding.AwayFromZero), ScheduleLinks.MinLag, ScheduleLinks.MaxLag);
            return (rounded, exact && rounded == days);
        }

        private string NameOf(int uid) =>
            _names.TryGetValue(uid, out var name) ? name : file.Rows.First(row => row.Uid == uid).Name;

        /// <summary>
        /// The items in the order they are made: each task before its subtasks and each item after the ones it depends
        /// on, otherwise in the order of the file. A dependency that would make a cycle goes away (ADR-0019).
        /// </summary>
        private List<Draft> Ordered(List<Dependency> dependencies)
        {
            var waitingFor = dependencies.ToLookup(dependency => dependency.Successor);
            var ordered = new List<Draft>();
            var placed = new HashSet<Draft>();
            bool Ready(Draft item) =>
                (item.Parent is null || placed.Contains(item.Parent))
                && waitingFor[item].All(dependency => dependency.Dropped || placed.Contains(dependency.Predecessor));
            while (ordered.Count < _items.Count)
            {
                var next = _items.FirstOrDefault(item => !placed.Contains(item) && Ready(item));
                if (next is null)
                {
                    // A cycle: the first item that waits only for dependencies loses the ones still waiting.
                    next = _items.First(item => !placed.Contains(item) && (item.Parent is null || placed.Contains(item.Parent)));
                    foreach (var dependency in waitingFor[next].Where(dependency => !dependency.Dropped && !placed.Contains(dependency.Predecessor)))
                    {
                        dependency.Dropped = true;
                        Warn(MsProjectWarningKind.DependencyCycles, dependency.Pair);
                    }
                }

                ordered.Add(next);
                placed.Add(next);
            }

            return ordered;
        }

        /// <summary>
        /// The first work resource of each task, the resources that are the first of some task and how many resources the
        /// file has; the others stay out, each with its warning (ADR-0135).
        /// </summary>
        private (List<MsProjectImportedResource> Resources, List<int?> ItemResources, int FileResources) Resources(List<Draft> ordered)
        {
            var named = file.Resources.GroupBy(resource => resource.Uid).Select(group => group.First()).ToList();
            foreach (var other in named.Where(resource => !resource.IsWork))
            {
                Warn(MsProjectWarningKind.OtherResources, other.Name);
            }

            var resources = named.Where(resource => resource.IsWork).ToDictionary(resource => resource.Uid);
            var byTask = file.Assignments
                .Where(assignment => resources.ContainsKey(assignment.ResourceUid))
                .GroupBy(assignment => assignment.TaskUid)
                .ToDictionary(group => group.Key, group => group.Select(assignment => assignment.ResourceUid).Distinct().ToList());
            var used = new List<int>();
            var tasks = new Dictionary<int, int>();
            var itemResources = new List<int?>();
            foreach (var item in ordered)
            {
                if (!byTask.TryGetValue(item.Row.Uid, out var assigned))
                {
                    itemResources.Add(null);
                    continue;
                }

                if (item.Kind == ScheduleItemKind.Milestone)
                {
                    Warn(MsProjectWarningKind.NoOwner, item.Name);
                    itemResources.Add(null);
                    continue;
                }

                if (assigned.Count > 1)
                {
                    Warn(MsProjectWarningKind.SeveralResources, item.Name);
                }

                var uid = assigned[0];
                if (!used.Contains(uid))
                {
                    used.Add(uid);
                }

                tasks[uid] = tasks.GetValueOrDefault(uid) + 1;
                itemResources.Add(used.IndexOf(uid));
            }

            foreach (var unused in named.Where(resource => resource.IsWork && !used.Contains(resource.Uid)))
            {
                Warn(MsProjectWarningKind.UnusedResources, unused.Name);
            }

            return ([.. used.Select(uid => new MsProjectImportedResource(resources[uid].Name, resources[uid].Email, tasks[uid]))], itemResources, named.Count);
        }

        /// <summary>The business days of the calendar before the start, since the first day of the file (ADR-0135).</summary>
        private int OffsetOf(DateOnly first, DateOnly start) =>
            start <= first ? 0 : days.CountBusinessDays(first, start.AddDays(-1));

        /// <summary>
        /// The business days of the calendar from the first day of the file to the deadline, negative before it, so the
        /// deadline moves with the start (GanttA-2); one on a day off goes to the next business day, as the starts, and
        /// one too far from the schedule stays out.
        /// </summary>
        private int? DeadlineOffsetOf(DateOnly first, DateOnly? deadline)
        {
            if (deadline is not { } day)
            {
                return null;
            }

            var offset = day >= first ? OffsetOf(first, day) : -days.CountBusinessDays(day, first.AddDays(-1));
            return Math.Abs(offset) <= Schedule.MaxStartOffset ? offset : null;
        }

        /// <summary>The business days from the start, on a business day, to the finish, from 1 to 999 (ADR-0135).</summary>
        private int DurationOf(Draft item)
        {
            if (item.IsSummary)
            {
                return 1;
            }

            var start = item.Start!.Value;
            var finish = item.Row.Finish is { } end && end >= start ? end : start;
            var duration = days.CountBusinessDays(days.IsBusinessDay(start) ? start : days.AddBusinessDays(start, 1), finish);
            if (duration > Schedule.MaxDuration)
            {
                Warn(MsProjectWarningKind.LongTasks, item.Name);
                return Schedule.MaxDuration;
            }

            return Math.Max(1, duration);
        }

        private void Warn(MsProjectWarningKind kind, string name)
        {
            if (!_warnings.TryGetValue(kind, out var names))
            {
                _warnings[kind] = names = [];
            }

            if (!names.Contains(name))
            {
                names.Add(name);
            }
        }
    }

    /// <summary>An item while it is converted.</summary>
    private sealed class Draft(MsProjectRow row, int phase, Draft? parent, ScheduleItemKind kind, string name, bool isSummary)
    {
        public MsProjectRow Row { get; } = row;

        public int Phase { get; } = phase;

        public Draft? Parent { get; } = parent;

        public ScheduleItemKind Kind { get; } = kind;

        public string Name { get; } = name;

        /// <summary>A task of level 2 with subtasks: its dates and progress come from them.</summary>
        public bool IsSummary { get; set; } = isSummary;

        public DateOnly? Start { get; set; } = row.Start;

        /// <summary>The percent complete of a task; 100 or 0 for a milestone, reached or not.</summary>
        public int Progress => Kind == ScheduleItemKind.Milestone ? (Row.PercentComplete == 100 ? 100 : 0) : Row.PercentComplete;
    }

    /// <summary>A dependency kept, unless it makes a cycle.</summary>
    private sealed class Dependency(Draft predecessor, Draft successor, string pair, ScheduleLinkType type, int lag)
    {
        public Draft Predecessor { get; } = predecessor;

        public Draft Successor { get; } = successor;

        public string Pair { get; } = pair;

        public ScheduleLinkType Type { get; } = type;

        public int Lag { get; } = lag;

        public bool Dropped { get; set; }
    }
}
