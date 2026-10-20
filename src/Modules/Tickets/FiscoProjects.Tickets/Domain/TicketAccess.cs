using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.Tickets.Domain;

/// <summary>Who asks: the person of the session and whether the person is the Administrator of the system.</summary>
public sealed record TicketViewer(Guid UserId, bool IsAdministrator);

/// <summary>
/// What a person may do with the tickets of a project, from the system role and the role in the project (ADR-0044,
/// ADR-0068). This is the only place with the rule.
/// </summary>
public static class TicketAccess
{
    private static readonly IReadOnlyList<TicketTeam> BothTeams = [TicketTeam.Functional, TicketTeam.Development];

    /// <summary>
    /// Opens tickets in the project: the Administrator and everyone in it but the viewer (ADR-0044). A completed or
    /// cancelled project takes no tickets from anyone (ADR-0022), which the opening tells apart.
    /// </summary>
    public static bool MayOpen(TicketViewer viewer, ProjectAccess project)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(project);
        return viewer.IsAdministrator || project.Role is ProjectMemberRole.Manager or ProjectMemberRole.TechLead
            or ProjectMemberRole.Contributor or ProjectMemberRole.Developer;
    }

    /// <summary>
    /// Whether a person with the role serves the team (ADR-0068): Funcional, the contributors; Desenvolvimento, the tech
    /// lead and the developers.
    /// </summary>
    public static bool Serves(TicketTeam team, ProjectMemberRole role) => team switch
    {
        TicketTeam.Functional => role == ProjectMemberRole.Contributor,
        TicketTeam.Development => role is ProjectMemberRole.TechLead or ProjectMemberRole.Developer,
        _ => false,
    };

    /// <summary>The teams with someone to serve them, among the roles of the people of a project: the ones a ticket may go to.</summary>
    public static IReadOnlyList<TicketTeam> TeamsServedBy(IEnumerable<ProjectMemberRole> roles)
    {
        var all = roles.ToList();
        return [.. BothTeams.Where(team => all.Any(role => Serves(team, role)))];
    }

    /// <summary>
    /// The teams of a person in a project, for "Sem responsável nos meus times": the manager and the tech lead hand out
    /// the tickets of both teams (ADR-0068); the contributor serves Funcional and the developer, Desenvolvimento; the
    /// viewer and who is not in the project, none.
    /// </summary>
    public static IReadOnlyList<TicketTeam> TeamsOf(ProjectMemberRole? role) => role switch
    {
        ProjectMemberRole.Manager or ProjectMemberRole.TechLead => BothTeams,
        ProjectMemberRole.Contributor => [TicketTeam.Functional],
        ProjectMemberRole.Developer => [TicketTeam.Development],
        _ => [],
    };
}
