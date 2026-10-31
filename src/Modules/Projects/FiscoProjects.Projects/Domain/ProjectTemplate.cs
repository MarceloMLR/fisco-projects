using FiscoProjects.Calendar.Contracts;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Projects.Domain;

/// <summary>
/// A model of project saved from the schedule of a project (ADR-0017): its phases, tasks, subtasks, milestones,
/// dependencies and durations, without dates nor owners. Each item keeps how many business days after the first start of
/// the schedule it started, so a project made from the model keeps the shape of the schedule. The Administrator renames,
/// describes, deactivates and removes models (ADR-0046); an inactive one is not offered for new projects.
/// </summary>
public sealed class ProjectTemplate : Entity
{
    public const int NameMaxLength = 150;
    public const int DescriptionMaxLength = 500;

    private readonly List<ProjectTemplatePhase> _phases = [];
    private readonly List<ProjectTemplateItem> _items = [];
    private readonly List<ProjectTemplateDependency> _dependencies = [];

    private ProjectTemplate()
    {
        Name = string.Empty;
    }

    private ProjectTemplate(string name, string? description)
    {
        Name = name;
        Description = description;
        IsActive = true;
    }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyList<ProjectTemplatePhase> Phases => _phases;

    public IReadOnlyList<ProjectTemplateItem> Items => _items;

    public IReadOnlyList<ProjectTemplateDependency> Dependencies => _dependencies;

    /// <summary>A name with 1 to 150 characters, without the spaces around; null otherwise.</summary>
    public static string? ValidName(string? name)
    {
        var trimmed = name?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > NameMaxLength ? null : trimmed;
    }

    /// <summary>A description of up to 500 characters, null when empty; false when longer.</summary>
    public static bool IsValidDescription(string? description) => (description?.Trim().Length ?? 0) <= DescriptionMaxLength;

    private static string? DescriptionOf(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>
    /// The model of a schedule, with its phases in the order of the Lista and its items by the business days from the
    /// first start, each task before its subtasks. Null without tasks nor milestones.
    /// </summary>
    public static ProjectTemplate? From(Schedule schedule, string name, string? description, IBusinessDays days)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(days);
        if (schedule.Items.Count == 0)
        {
            return null;
        }

        var template = new ProjectTemplate(
            ValidName(name) ?? throw new ArgumentException("No valid name.", nameof(name)), DescriptionOf(description));
        var first = schedule.Items.Min(item => item.Start);
        var figures = ScheduleFigures.Of(schedule.Phases, schedule.Items, first);
        var phases = new Dictionary<Guid, Guid>();
        foreach (var (phase, position) in figures.Phases.Select((phase, index) => (phase.Phase, index + 1)))
        {
            var copy = new ProjectTemplatePhase(template.Id, phase.Name, position);
            template._phases.Add(copy);
            phases[phase.Id] = copy.Id;
        }

        var items = new Dictionary<Guid, Guid>();
        int OffsetOf(ScheduleItem item) => Math.Max(0, days.CountBusinessDays(first, item.Start) - 1);
        var sorted = schedule.Items
            .OrderBy(OffsetOf)
            .ThenBy(item => item.ParentId is null ? 0 : 1)
            .ThenBy(item => item.End)
            .ThenBy(item => item.Kind)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        // In that order, each item after its task and the items it depends on, as the project is made again.
        var ordered = new List<ScheduleItem>();
        var placed = new HashSet<Guid>();
        while (ordered.Count < sorted.Count)
        {
            var next = sorted.First(item => !placed.Contains(item.Id)
                && (item.ParentId is not { } parent || placed.Contains(parent))
                && schedule.PredecessorsOf(item.Id).All(predecessor => placed.Contains(predecessor.Id)));
            ordered.Add(next);
            placed.Add(next.Id);
        }

        foreach (var (item, position) in ordered.Select((item, index) => (item, index + 1)))
        {
            var copy = new ProjectTemplateItem(
                template.Id,
                phases[item.PhaseId],
                item.Kind,
                item.Name,
                OffsetOf(item),
                item.Duration,
                position);
            template._items.Add(copy);
            items[item.Id] = copy.Id;
        }

        foreach (var item in schedule.Items.Where(item => item.ParentId is not null))
        {
            template._items.Single(copy => copy.Id == items[item.Id]).PutInto(items[item.ParentId!.Value]);
        }

        foreach (var dependency in schedule.Dependencies)
        {
            template._dependencies.Add(new ProjectTemplateDependency(
                template.Id, items[dependency.PredecessorId], items[dependency.SuccessorId]));
        }

        return template;
    }

    /// <summary>Renames and describes the model.</summary>
    public void Change(string name, string? description)
    {
        Name = ValidName(name) ?? throw new ArgumentException("No valid name.", nameof(name));
        Description = DescriptionOf(description);
    }

    public void SetActive(bool active) => IsActive = active;

    /// <summary>
    /// The outline a new project is made from (ADR-0017): the phases and the items in their order, without progress nor
    /// owners, each task lasting from 1 to 999 business days.
    /// </summary>
    public SchedulePlanOutline ToOutline()
    {
        var phases = _phases.OrderBy(phase => phase.Position).ToList();
        var phaseIndex = phases.Select((phase, index) => (phase.Id, index)).ToDictionary(pair => pair.Id, pair => pair.index);
        var items = _items.OrderBy(item => item.Position).ToList();
        var itemIndex = items.Select((item, index) => (item.Id, index)).ToDictionary(pair => pair.Id, pair => pair.index);
        int? ParentOf(ProjectTemplateItem item, int index) =>
            item.ParentId is { } parent && itemIndex.TryGetValue(parent, out var position) && position < index ? position : null;
        return new SchedulePlanOutline(
            [.. phases.Select(phase => new OutlinePhase(phase.Name))],
            [.. items.Select((item, index) => new OutlineItem(
                phaseIndex[item.PhaseId],
                ParentOf(item, index),
                item.Kind,
                item.Name,
                item.StartOffset,
                item.Kind == ScheduleItemKind.Milestone ? 0 : Math.Clamp(item.Duration, 1, Schedule.MaxDuration)))],
            [.. _dependencies
                .Where(dependency => itemIndex.ContainsKey(dependency.PredecessorId) && itemIndex.ContainsKey(dependency.SuccessorId))
                .Select(dependency => new OutlineDependency(itemIndex[dependency.PredecessorId], itemIndex[dependency.SuccessorId]))
                .Where(dependency => dependency.Predecessor < dependency.Successor)]);
    }

    /// <summary>The items of a phase, in order.</summary>
    public IEnumerable<ProjectTemplateItem> ItemsOf(Guid phaseId) =>
        _items.Where(item => item.PhaseId == phaseId).OrderBy(item => item.Position);
}

/// <summary>A phase of a model, in its order.</summary>
public sealed class ProjectTemplatePhase : Entity
{
    private ProjectTemplatePhase()
    {
        Name = string.Empty;
    }

    internal ProjectTemplatePhase(Guid templateId, string name, int position)
    {
        TemplateId = templateId;
        Name = name;
        Position = position;
    }

    public Guid TemplateId { get; private init; }

    public string Name { get; private init; }

    public int Position { get; private init; }
}

/// <summary>
/// A task or milestone of a model: its phase, the task it is in, its kind, name and duration in business days, and how
/// many business days after the first start of the schedule it starts.
/// </summary>
public sealed class ProjectTemplateItem : Entity
{
    private ProjectTemplateItem()
    {
        Name = string.Empty;
    }

    internal ProjectTemplateItem(
        Guid templateId, Guid phaseId, ScheduleItemKind kind, string name, int startOffset, int duration, int position)
    {
        TemplateId = templateId;
        PhaseId = phaseId;
        Kind = kind;
        Name = name;
        StartOffset = startOffset;
        Duration = duration;
        Position = position;
    }

    public Guid TemplateId { get; private init; }

    public Guid PhaseId { get; private init; }

    public Guid? ParentId { get; private set; }

    public ScheduleItemKind Kind { get; private init; }

    public string Name { get; private init; }

    /// <summary>Business days from the first start of the schedule to the start of the item; zero for the first ones.</summary>
    public int StartOffset { get; private init; }

    public int Duration { get; private init; }

    /// <summary>The order the items are made in: by the distance, each task before its subtasks.</summary>
    public int Position { get; private init; }

    internal void PutInto(Guid parentId) => ParentId = parentId;
}

/// <summary>An item of a model that starts only after another ends.</summary>
public sealed class ProjectTemplateDependency : Entity
{
    private ProjectTemplateDependency()
    {
    }

    internal ProjectTemplateDependency(Guid templateId, Guid predecessorId, Guid successorId)
    {
        TemplateId = templateId;
        PredecessorId = predecessorId;
        SuccessorId = successorId;
    }

    public Guid TemplateId { get; private init; }

    public Guid PredecessorId { get; private init; }

    public Guid SuccessorId { get; private init; }
}
