using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Domain;

namespace FiscoProjects.Tickets.Tests;

// Spec: chamados, "Trocar o responsável", "Trocar o solicitante" and "Transferir de time" (ADR-0027, ADR-0044, ADR-0068).
public sealed class TicketChangeAccessTests
{
    private static readonly Guid Requester = Guid.CreateVersion7();

    private static Ticket DevelopmentTicket() =>
        Ticket.Open(
            104,
            Guid.CreateVersion7(),
            TicketForm.Of(TicketTeam.Development, TicketPriority.High, "Rejeição 539", "Texto"),
            Requester,
            new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc));

    public static TheoryData<string, ProjectMemberRole?, bool, bool, bool, bool, bool, bool> Matrix => new()
    {
        // Who, role, Administrator, requester; changes the assignee, the requester alone, the requester in bulk, transfers.
        { "administrador", null, true, false, true, true, true, true },
        { "gerente", ProjectMemberRole.Manager, false, false, true, true, true, true },
        { "líder técnico", ProjectMemberRole.TechLead, false, false, true, true, true, true },
        { "desenvolvedor", ProjectMemberRole.Developer, false, false, false, false, false, true },
        { "colaborador fora do time", ProjectMemberRole.Contributor, false, false, false, false, false, false },
        { "solicitante", ProjectMemberRole.Contributor, false, true, false, true, false, false },
        { "leitor", ProjectMemberRole.Viewer, false, false, false, false, false, false },
    };

    [Theory(DisplayName = "Quem troca e transfere")]
    [MemberData(nameof(Matrix))]
    public void Changes_FollowWhoThePersonIs(
        string who, ProjectMemberRole? role, bool isAdministrator, bool isRequester,
        bool assigns, bool changesRequester, bool changesRequesterInBulk, bool transfers)
    {
        var ticket = DevelopmentTicket();
        var viewer = new TicketViewer(isRequester ? Requester : Guid.CreateVersion7(), isAdministrator);

        TicketAccess.MayAssign(viewer, role).Should().Be(assigns, who);
        TicketAccess.MayChangeRequester(viewer, role, ticket, inBulk: false).Should().Be(changesRequester, who);
        TicketAccess.MayChangeRequester(viewer, role, ticket, inBulk: true).Should().Be(changesRequesterInBulk, who);
        TicketAccess.MayTransfer(viewer, role, ticket).Should().Be(transfers, who);
    }

    [Theory(DisplayName = "Quem pode ser responsável e solicitante")]
    [InlineData(ProjectMemberRole.Manager, true, true, true)]
    [InlineData(ProjectMemberRole.TechLead, false, true, true)]
    [InlineData(ProjectMemberRole.Contributor, true, false, true)]
    [InlineData(ProjectMemberRole.Developer, false, true, true)]
    [InlineData(ProjectMemberRole.Viewer, false, false, false)]
    public void People_WhoMayBeChosen(ProjectMemberRole role, bool functional, bool development, bool requester)
    {
        TicketAccess.CanBeAssignee(TicketTeam.Functional, role).Should().Be(functional);
        TicketAccess.CanBeAssignee(TicketTeam.Development, role).Should().Be(development);
        TicketAccess.CanBeRequester(role).Should().Be(requester);
        TicketAccess.CanBeAssignee(TicketTeam.Development, null).Should().BeFalse();
    }

    [Fact(DisplayName = "Transferência deixa sem responsável")]
    public void Transfer_LeavesTheTicketWithoutAssignee()
    {
        var ticket = DevelopmentTicket();
        var now = new DateTime(2026, 10, 19, 13, 0, 0, DateTimeKind.Utc);
        ticket.Assign(Guid.CreateVersion7(), now);

        ticket.Transfer(TicketTeam.Functional, now);

        (ticket.Team, ticket.AssigneeId, ticket.UpdatedAt).Should().Be((TicketTeam.Functional, null, now));
    }
}
