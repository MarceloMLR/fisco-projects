using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Domain;

namespace FiscoProjects.Tickets.Tests;

// Spec: chamados, "Quem abre chamados", "Times dos chamados" and "Visões e filtros da lista" (ADR-0044, ADR-0068).
public sealed class TicketAccessTests
{
    private static readonly TicketViewer Person = new(Guid.CreateVersion7(), IsAdministrator: false);

    private static ProjectAccess ProjectWith(ProjectMemberRole? role) =>
        new(Guid.CreateVersion7(), "ERP-FIN", "Implantação do ERP financeiro", IsClosed: false, role);

    [Theory(DisplayName = "Quem abre chamados no domínio")]
    [InlineData(ProjectMemberRole.Manager, true)]
    [InlineData(ProjectMemberRole.TechLead, true)]
    [InlineData(ProjectMemberRole.Contributor, true)]
    [InlineData(ProjectMemberRole.Developer, true)]
    [InlineData(ProjectMemberRole.Viewer, false)]
    public void EveryoneButTheViewer_OpensTickets(ProjectMemberRole role, bool opens) =>
        TicketAccess.MayOpen(Person, ProjectWith(role)).Should().Be(opens);

    [Fact(DisplayName = "Administrador abre chamados fora do projeto")]
    public void Administrator_OpensTickets_WithoutARole() =>
        TicketAccess.MayOpen(Person with { IsAdministrator = true }, ProjectWith(null)).Should().BeTrue();

    [Fact(DisplayName = "Times com quem atenda")]
    public void Teams_NeedSomeoneToServe()
    {
        TicketAccess.TeamsServedBy([ProjectMemberRole.Manager, ProjectMemberRole.Viewer]).Should().BeEmpty();
        TicketAccess.TeamsServedBy([ProjectMemberRole.Manager, ProjectMemberRole.TechLead])
            .Should().Equal(TicketTeam.Development);
        TicketAccess.TeamsServedBy([ProjectMemberRole.Developer, ProjectMemberRole.Contributor])
            .Should().Equal(TicketTeam.Functional, TicketTeam.Development);
    }

    [Theory(DisplayName = "Meus times")]
    [InlineData(ProjectMemberRole.Manager, new[] { TicketTeam.Functional, TicketTeam.Development })]
    [InlineData(ProjectMemberRole.TechLead, new[] { TicketTeam.Functional, TicketTeam.Development })]
    [InlineData(ProjectMemberRole.Contributor, new[] { TicketTeam.Functional })]
    [InlineData(ProjectMemberRole.Developer, new[] { TicketTeam.Development })]
    [InlineData(ProjectMemberRole.Viewer, new TicketTeam[0])]
    public void TeamsOfAPerson_FollowTheRole(ProjectMemberRole role, TicketTeam[] teams) =>
        TicketAccess.TeamsOf(role).Should().Equal(teams);

    [Fact(DisplayName = "Dados do chamado")]
    public void Form_TrimsAndChecksTheFields()
    {
        var form = TicketForm.Of(TicketTeam.Functional, TicketPriority.Low, "  Dúvida sobre o CFOP ", "\n Texto \n");
        form.IsValid.Should().BeTrue();
        (form.Title, form.Description).Should().Be(("Dúvida sobre o CFOP", "Texto"));

        TicketForm.Of((TicketTeam)7, null, new string('a', 201), new string('a', 10_001)).Invalid
            .Should().Equal(TicketField.Team, TicketField.Priority, TicketField.Title, TicketField.Description);

        var ticket = Ticket.Open(104, Guid.CreateVersion7(), form, Person.UserId, new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc));
        (ticket.Number, ticket.Status, ticket.AssigneeId, ticket.IsOpen).Should().Be((104, TicketStatus.Open, null, true));
    }
}
