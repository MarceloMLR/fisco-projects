using FiscoProjects.Projects.Contracts;
using FiscoProjects.Tickets.Domain;

namespace FiscoProjects.Tickets.Tests;

// Spec: chamados, "Fluxo de status em tabela" and "Mudar o status" (ADR-0024, ADR-0044).
public sealed class TicketFlowTests
{
    private static readonly DateTime Now = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid Requester = Guid.CreateVersion7();

    private static Ticket DevelopmentTicket() =>
        Ticket.Open(104, Guid.CreateVersion7(), TicketForm.Of(TicketTeam.Development, TicketPriority.High, "Rejeição 539", "Texto"), Requester, Now);

    [Fact(DisplayName = "Fluxo padrão no domínio")]
    public void DefaultFlow_IsSound()
    {
        var flow = TicketFlow.From(TicketFlow.Default);

        flow.Transitions.Should().HaveCount(11);
        flow.Transitions.Should().NotContain(transition => transition.From == TicketStatus.Closed);
        flow.Of(TicketStatus.InProgress, TicketAction.AskForInformation)!.RequiresMessage.Should().BeTrue();
        flow.Of(TicketStatus.InProgress, TicketAction.Resolve)!.To.Should().Be(TicketStatus.Resolved);
        flow.Of(TicketStatus.Open, TicketAction.Resolve).Should().BeNull();
        flow.Transitions.Where(transition => transition.Automatic).Select(transition => transition.Action)
            .Should().BeEquivalentTo([TicketAction.FirstResponse, TicketAction.Reply, TicketAction.AutoClose]);
    }

    [Fact(DisplayName = "Fluxo quebrado")]
    public void BrokenFlow_IsRefused()
    {
        // Em triagem without a way out.
        var deadEnd = TicketFlow.Default.Where(transition => transition.From != TicketStatus.Triage);
        FluentActions.Invoking(() => TicketFlow.From(deadEnd)).Should().Throw<TicketFlowBrokenException>().WithMessage("*Triage*");

        var reopened = TicketFlow.Default.Append(
            new TicketTransition(TicketStatus.Closed, TicketStatus.InProgress, TicketAction.Reopen, TicketActors.Requester, true, false));
        FluentActions.Invoking(() => TicketFlow.From(reopened)).Should().Throw<TicketFlowBrokenException>();

        var repeated = TicketFlow.Default.Append(
            new TicketTransition(TicketStatus.Open, TicketStatus.Resolved, TicketAction.StartTriage, TicketActors.Attendant, false, false));
        FluentActions.Invoking(() => TicketFlow.From(repeated)).Should().Throw<TicketFlowBrokenException>();
    }

    [Fact(DisplayName = "Transição aplicada no chamado")]
    public void Apply_ChangesTheStatus_AndKeepsWhenItWasResolvedAndClosed()
    {
        var flow = TicketFlow.From(TicketFlow.Default);
        var ticket = DevelopmentTicket();

        ticket.Apply(flow.Of(TicketStatus.Open, TicketAction.FirstResponse)!, Now.AddHours(1));
        ticket.Apply(flow.Of(TicketStatus.InProgress, TicketAction.Resolve)!, Now.AddHours(2));
        (ticket.Status, ticket.ResolvedAt, ticket.IsOpen).Should().Be((TicketStatus.Resolved, Now.AddHours(2), false));

        ticket.Apply(flow.Of(TicketStatus.Resolved, TicketAction.Reopen)!, Now.AddHours(3));
        (ticket.Status, ticket.ResolvedAt).Should().Be((TicketStatus.InProgress, null));

        ticket.Apply(flow.Of(TicketStatus.InProgress, TicketAction.Resolve)!, Now.AddHours(4));
        ticket.Apply(flow.Of(TicketStatus.Resolved, TicketAction.ConfirmAndClose)!, Now.AddHours(5));
        (ticket.Status, ticket.ClosedAt, ticket.UpdatedAt).Should().Be((TicketStatus.Closed, Now.AddHours(5), Now.AddHours(5)));

        FluentActions.Invoking(() => ticket.Apply(flow.Of(TicketStatus.InProgress, TicketAction.Resolve)!, Now.AddHours(6)))
            .Should().Throw<InvalidOperationException>();
    }

    public static TheoryData<string, ProjectMemberRole?, bool, bool, TicketStatus, TicketAction[]> Matrix => new()
    {
        // Who, role in the project, Administrator, requester, status, the actions open now.
        { "administrador", null, true, false, TicketStatus.Open, [TicketAction.StartTriage, TicketAction.Cancel] },
        { "gerente", ProjectMemberRole.Manager, false, false, TicketStatus.Open, [TicketAction.StartTriage, TicketAction.Cancel] },
        { "líder técnico", ProjectMemberRole.TechLead, false, false, TicketStatus.Triage, [TicketAction.StartWork, TicketAction.Cancel] },
        { "desenvolvedor", ProjectMemberRole.Developer, false, false, TicketStatus.Open, [TicketAction.StartTriage] },
        { "colaborador fora do time", ProjectMemberRole.Contributor, false, false, TicketStatus.Open, [] },
        { "leitor", ProjectMemberRole.Viewer, false, false, TicketStatus.InProgress, [] },
        { "solicitante", ProjectMemberRole.Contributor, false, true, TicketStatus.Open, [TicketAction.Cancel] },
        { "desenvolvedor em andamento", ProjectMemberRole.Developer, false, false, TicketStatus.InProgress, [TicketAction.AskForInformation, TicketAction.Resolve] },
        { "solicitante em andamento", ProjectMemberRole.Contributor, false, true, TicketStatus.InProgress, [] },
        { "desenvolvedor resolvido", ProjectMemberRole.Developer, false, false, TicketStatus.Resolved, [] },
        { "solicitante resolvido", ProjectMemberRole.Contributor, false, true, TicketStatus.Resolved, [TicketAction.ConfirmAndClose, TicketAction.Reopen] },
        { "gerente resolvido", ProjectMemberRole.Manager, false, false, TicketStatus.Resolved, [TicketAction.ConfirmAndClose] },
        { "administrador resolvido", null, true, false, TicketStatus.Resolved, [TicketAction.ConfirmAndClose] },
        { "desenvolvedor aguardando", ProjectMemberRole.Developer, false, false, TicketStatus.WaitingForRequester, [] },
        { "administrador fechado", null, true, false, TicketStatus.Closed, [] },
    };

    [Theory(DisplayName = "Quem faz cada ação do fluxo")]
    [MemberData(nameof(Matrix))]
    public void ActionsOpenNow_FollowWhoThePersonIs(
        string who, ProjectMemberRole? role, bool isAdministrator, bool isRequester, TicketStatus status, TicketAction[] actions)
    {
        var flow = TicketFlow.From(TicketFlow.Default);
        var ticket = DevelopmentTicket();
        var viewer = new TicketViewer(isRequester ? Requester : Guid.CreateVersion7(), isAdministrator);

        var open = flow.AvailableTo(status, TicketAccess.ActorsOf(viewer, role, ticket)).Select(transition => transition.Action);

        open.Should().BeEquivalentTo(actions, who);
    }
}
