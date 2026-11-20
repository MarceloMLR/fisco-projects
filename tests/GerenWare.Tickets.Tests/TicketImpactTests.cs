using GerenWare.Projects.Contracts;
using GerenWare.Tickets.Domain;

namespace GerenWare.Tickets.Tests;

// Spec: chamados, "Chamado no cronograma", "Quem liga o chamado ao cronograma" and "Itens que podem ser ligados"
// (ADR-0044, ADR-0068, ADR-0164).
public sealed class TicketImpactTests
{
    private static readonly Guid Requester = Guid.CreateVersion7();

    private static Ticket TicketOf(TicketTeam team) =>
        Ticket.Open(
            104,
            Guid.CreateVersion7(),
            TicketForm.Of(team, TicketPriority.High, "Rejeição 539", "Texto"),
            Requester,
            new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc));

    public static TheoryData<string, ProjectMemberRole?, bool, bool, TicketTeam, bool> Matrix => new()
    {
        // Who, role, Administrator, requester, team of the ticket; links it to the schedule.
        { "administrador fora do projeto", null, true, false, TicketTeam.Development, true },
        { "gerente", ProjectMemberRole.Manager, false, false, TicketTeam.Functional, true },
        { "gerente", ProjectMemberRole.Manager, false, false, TicketTeam.Development, true },
        { "líder técnico", ProjectMemberRole.TechLead, false, false, TicketTeam.Functional, true },
        { "líder técnico", ProjectMemberRole.TechLead, false, false, TicketTeam.Development, true },
        { "desenvolvedor no time Desenvolvimento", ProjectMemberRole.Developer, false, false, TicketTeam.Development, true },
        { "desenvolvedor no time Funcional", ProjectMemberRole.Developer, false, false, TicketTeam.Functional, false },
        { "colaborador no time Funcional", ProjectMemberRole.Contributor, false, false, TicketTeam.Functional, true },
        { "colaborador no time Desenvolvimento", ProjectMemberRole.Contributor, false, false, TicketTeam.Development, false },
        { "solicitante que não atende o time", ProjectMemberRole.Contributor, false, true, TicketTeam.Development, false },
        { "solicitante que atende o time", ProjectMemberRole.Contributor, false, true, TicketTeam.Functional, true },
        { "leitor", ProjectMemberRole.Viewer, false, false, TicketTeam.Functional, false },
        { "leitor", ProjectMemberRole.Viewer, false, false, TicketTeam.Development, false },
    };

    [Theory(DisplayName = "Quem liga o chamado ao cronograma")]
    [MemberData(nameof(Matrix))]
    public void WhoServesTheTicket_LinksItToTheSchedule(
        string who, ProjectMemberRole? role, bool isAdministrator, bool isRequester, TicketTeam team, bool links)
    {
        var ticket = TicketOf(team);
        var viewer = new TicketViewer(isRequester ? Requester : Guid.CreateVersion7(), isAdministrator);

        TicketAccess.MayLinkToSchedule(viewer, role, ticket).Should().Be(links, who);
        TicketAccess.MayLinkToSchedule(viewer, role, ticket).Should().Be(TicketAccess.MayRegisterAtVendor(viewer, role, ticket), "the same rule");
    }

    [Fact(DisplayName = "Item que não pode ser ligado")]
    public void Items_AreUpToTen_NoneRepeated()
    {
        var one = Guid.CreateVersion7();
        Guid[] ten = [.. Enumerable.Range(0, TicketImpact.MaxItems).Select(_ => Guid.CreateVersion7())];

        TicketImpact.ItemIdsOf([]).Should().BeEmpty("unlinking all is a choice");
        TicketImpact.ItemIdsOf(ten).Should().Equal(ten);
        TicketImpact.ItemIdsOf([.. ten, one]).Should().BeNull("11 items");
        TicketImpact.ItemIdsOf([one, one]).Should().BeNull("an item repeated");
        TicketImpact.ItemIdsOf(null).Should().BeNull();
    }
}
