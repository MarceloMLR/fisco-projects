namespace FiscoProjects.Projects.Contracts;

/// <summary>
/// Told when a person leaves a project or gets another role in it (ADR-0042), after the change is saved, so the other
/// modules settle what the person can no longer do, as the tickets the person was in charge of. Each implementation
/// is idempotent: a failure is logged, and the change of the people stays.
/// </summary>
public interface IProjectMemberChanges
{
    /// <summary>A person left the project, with a null role, or got the role.</summary>
    /// <param name="projectId">The project.</param>
    /// <param name="userId">The person.</param>
    /// <param name="role">The role of the person now; null when the person left the project.</param>
    /// <param name="actorId">Who made the change.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    Task MemberChangedAsync(Guid projectId, Guid userId, ProjectMemberRole? role, Guid actorId, CancellationToken cancellationToken);
}
