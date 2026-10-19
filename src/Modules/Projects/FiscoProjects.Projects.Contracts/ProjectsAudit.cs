namespace FiscoProjects.Projects.Contracts;

/// <summary>How other modules point to a project or a client in the audit (ADR-0084).</summary>
public static class ProjectsAudit
{
    /// <summary>The type of a field that holds the id of a project, shown on the Audit screen by the project name.</summary>
    public const string ProjectReference = "Project";

    /// <summary>The type of a field that holds the id of a client, shown on the Audit screen by the client name.</summary>
    public const string ClientReference = "Client";

    /// <summary>The type of a field that holds the id of a phase of a schedule, shown by the phase name.</summary>
    public const string SchedulePhaseReference = "SchedulePhase";

    /// <summary>The type of a field that holds the id of a task or milestone of a schedule, shown by its name.</summary>
    public const string ScheduleItemReference = "ScheduleItem";
}
