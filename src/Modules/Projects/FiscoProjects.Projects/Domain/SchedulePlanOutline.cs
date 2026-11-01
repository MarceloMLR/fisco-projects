namespace FiscoProjects.Projects.Domain;

/// <summary>
/// The shape of the schedule of a new project, which <c>Schedule.ApplyTemplate</c> makes (ADR-0017, ADR-0135): the
/// phases in order, the tasks and milestones in the order they are made, and the
/// dependencies between them, by their positions in the lists. A model of project gives it (<see cref="ProjectTemplate.ToOutline"/>),
/// and so does a file of the MS Project.
/// </summary>
public sealed record SchedulePlanOutline(
    IReadOnlyList<OutlinePhase> Phases,
    IReadOnlyList<OutlineItem> Items,
    IReadOnlyList<OutlineDependency> Dependencies);

/// <summary>A phase of an outline.</summary>
public sealed record OutlinePhase(string Name);

/// <summary>
/// A task or milestone of an outline: the position of its phase, the position of the task it is in (an item before it;
/// none when it is right in the phase), its kind, name, how many business days after the first start it starts and its
/// duration in business days (zero for a milestone). Optional, the progress of a task without subtasks, from 0 to 100,
/// or of a milestone, 100 when it was reached; the person in charge of the task or the client of the project in its
/// place (ADR-0102); and the deadline, in business days after the first start, before it when negative (GanttA-2).
/// </summary>
public sealed record OutlineItem(
    int Phase,
    int? Parent,
    ScheduleItemKind Kind,
    string Name,
    int StartOffset,
    int Duration,
    int Progress = 0,
    Guid? OwnerId = null,
    bool OwnedByClient = false,
    int? DeadlineOffset = null);

/// <summary>An item of an outline that starts only after an item before it ends, by their positions.</summary>
public sealed record OutlineDependency(
    int Predecessor,
    int Successor,
    ScheduleLinkType Type = ScheduleLinkType.FinishToStart,
    int Lag = 0);
