namespace FiscoProjects.Projects.Contracts;

/// <summary>A task not completed with a person as its owner, with its own dates, as the load of the team weighs it (ADR-0142).</summary>
public sealed record OwnedTask(
    Guid OwnerId, Guid ProjectId, string ProjectCode, string ProjectName, Guid ItemId, string Name, DateOnly Start, DateOnly End);

/// <summary>A period a person does not work (ADR-0141), as the capacity of the load of the team takes it (ADR-0142).</summary>
public sealed record AbsenceInfo(Guid UserId, DateOnly Start, DateOnly End);

/// <summary>
/// The work of the people across the open projects, for the load of the team (ADR-0142): the tasks with a person as
/// owner, the absences and the people of the projects. It says nothing about who may see it: the load is only of the
/// Administrator, by the policy of its route.
/// </summary>
public interface ITeamWork
{
    /// <summary>
    /// The tasks below 100%, also the summary tasks and the subtasks, with a person as owner (not the client), whose
    /// dates cross the days (both included), of the projects not completed nor cancelled.
    /// </summary>
    Task<IReadOnlyList<OwnedTask>> TasksAsync(DateOnly first, DateOnly last, CancellationToken cancellationToken);

    /// <summary>The absences that cross the days (both included), of anyone.</summary>
    Task<IReadOnlyList<AbsenceInfo>> AbsencesAsync(DateOnly first, DateOnly last, CancellationToken cancellationToken);

    /// <summary>The people of the projects not completed nor cancelled, in any role, each once.</summary>
    Task<IReadOnlyList<Guid>> PeopleAsync(CancellationToken cancellationToken);
}
