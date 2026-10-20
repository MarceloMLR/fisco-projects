using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.Tickets.Domain;

/// <summary>Who asks: the person of the session and whether the person is the Administrator of the system.</summary>
public sealed record TicketViewer(Guid UserId, bool IsAdministrator);

/// <summary>
/// The part a person plays in a ticket, which gives the color and the tag of the messages (ADR-0069): the requester,
/// the assignee (even being of a team), the manager, the tech lead, the teams, the Administrator outside the project,
/// or anyone else.
/// </summary>
public enum TicketParticipantRole
{
    Requester = 1,
    Owner = 2,
    Manager = 3,
    TechLead = 4,
    Development = 5,
    Functional = 6,
    Administrator = 7,
    Other = 8,
}

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

    /// <summary>
    /// Sends messages and attachments in the ticket (ADR-0044): the Administrator, the managers, who serves the team
    /// of the ticket and the requester; the other people of the project only follow it.
    /// </summary>
    public static bool MaySend(TicketViewer viewer, ProjectMemberRole? role, Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(ticket);
        return viewer.IsAdministrator
            || role is ProjectMemberRole.Manager or ProjectMemberRole.TechLead
            || (role is { } member && Serves(ticket.Team, member))
            || ticket.RequesterId == viewer.UserId;
    }

    /// <summary>
    /// Who may be mentioned in the ticket (ADR-0026): the managers of the project, in any ticket, and the people
    /// involved in it: the requester, who serves its team and the assignee; only people of the project.
    /// </summary>
    public static IReadOnlyList<Guid> Mentionable(Ticket ticket, IEnumerable<ProjectMemberInfo> members)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        return [.. members
            .Where(member => member.Role is ProjectMemberRole.Manager or ProjectMemberRole.TechLead
                || Serves(ticket.Team, member.Role)
                || member.UserId == ticket.RequesterId
                || member.UserId == ticket.AssigneeId)
            .Select(member => member.UserId)
            .Distinct()];
    }

    /// <summary>The part of a person in the ticket, from the role in the project (ADR-0069).</summary>
    public static TicketParticipantRole RoleIn(Ticket ticket, Guid userId, ProjectMemberRole? role, bool isAdministrator)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        if (userId == ticket.RequesterId)
        {
            return TicketParticipantRole.Requester;
        }

        if (userId == ticket.AssigneeId)
        {
            return TicketParticipantRole.Owner;
        }

        return role switch
        {
            ProjectMemberRole.Manager => TicketParticipantRole.Manager,
            ProjectMemberRole.TechLead => TicketParticipantRole.TechLead,
            ProjectMemberRole.Developer => TicketParticipantRole.Development,
            ProjectMemberRole.Contributor => TicketParticipantRole.Functional,
            null when isAdministrator => TicketParticipantRole.Administrator,
            _ => TicketParticipantRole.Other,
        };
    }

    /// <summary>
    /// The parts of a person in the flow of the ticket (ADR-0024, ADR-0044): who serves it is the Administrator, the
    /// managers and who serves its team; the requester; the managers (the manager and the tech lead); the
    /// Administrator.
    /// </summary>
    public static TicketActors ActorsOf(TicketViewer viewer, ProjectMemberRole? role, Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(ticket);
        var actors = TicketActors.None;
        if (viewer.IsAdministrator)
        {
            actors |= TicketActors.Administrator | TicketActors.Attendant;
        }

        if (role is ProjectMemberRole.Manager or ProjectMemberRole.TechLead)
        {
            actors |= TicketActors.Manager | TicketActors.Attendant;
        }

        if (role is { } member && Serves(ticket.Team, member))
        {
            actors |= TicketActors.Attendant;
        }

        if (ticket.RequesterId == viewer.UserId)
        {
            actors |= TicketActors.Requester;
        }

        return actors;
    }
}
