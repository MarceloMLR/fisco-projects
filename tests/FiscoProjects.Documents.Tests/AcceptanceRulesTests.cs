using FiscoProjects.Documents.Domain;
using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.Documents.Tests;

// Spec: termos-de-aceite (domain side): "Pedido de aceite", "Resposta ao pedido", "Termo anexado pelo gestor", "Lembrar e
// cancelar" and "Permissões nos termos" (ADR-0035, ADR-0045, ADR-0095).
public sealed class AcceptanceRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Marina = Guid.CreateVersion7();
    private static readonly Guid Carla = Guid.CreateVersion7();
    private static readonly AcceptanceFile ToSign = new("f", "termo-homologacao.pdf", 2_048);

    private static ProjectAccess ProjectWith(ProjectMemberRole? role, bool closed = false) =>
        new(Guid.CreateVersion7(), "ERP-FIN", "Implantação do ERP financeiro", closed, role);

    private static AcceptanceTerm Requested() =>
        AcceptanceTerm.Request(
            Guid.CreateVersion7(), Guid.CreateVersion7(), new AcceptanceForm("Aceite da homologação", null), Carla, ToSign, Marina, Now);

    [Fact(DisplayName = "Pedido pendente com o termo a assinar")]
    public void Request_IsPending_WithTheFileAndTheHistory()
    {
        var term = Requested();

        (term.Status, term.RecipientId, term.RequestedById, term.IsRequest, term.FileName)
            .Should().Be((AcceptanceStatus.Pending, Carla, Marina, true, "termo-homologacao.pdf"));
        term.Events.Select(item => (item.Kind, item.ActorId)).Should().Equal((AcceptanceEventKind.Requested, Marina));
    }

    [Theory(DisplayName = "Título e mensagem do pedido")]
    [InlineData("  Aceite da homologação  ", null, true)]
    [InlineData("", null, false)]
    [InlineData("   ", "Assine até sexta.", false)]
    public void Form_NeedsATitle(string title, string? message, bool valid)
    {
        var (form, invalid) = AcceptanceForm.Of(title, message);

        (form is not null).Should().Be(valid);
        if (valid)
        {
            form!.Title.Should().Be("Aceite da homologação");
        }
        else
        {
            invalid.Should().Equal(AcceptanceField.Title);
        }
    }

    [Fact(DisplayName = "Limites do título e da mensagem")]
    public void Form_HasLimits()
    {
        AcceptanceForm.Of(new string('t', 120), new string('m', 2_000)).Form.Should().NotBeNull();
        AcceptanceForm.Of(new string('t', 121), new string('m', 2_001)).Invalid
            .Should().Equal(AcceptanceField.Title, AcceptanceField.Message);
        AcceptanceForm.Of("Aceite", "   ").Form!.Message.Should().BeNull();
    }

    [Fact(DisplayName = "Termo assinado pela pessoa do pedido")]
    public void Sign_ByThePerson_IsSigned()
    {
        var term = Requested();
        var document = Guid.CreateVersion7();

        term.Sign(document, Carla, Now.AddHours(1));

        (term.Status, term.DocumentId, term.AnsweredById, term.AnsweredAt).Should().Be((AcceptanceStatus.Signed, document, Carla, Now.AddHours(1)));
        term.Events.Select(item => item.Kind).Should().Equal(AcceptanceEventKind.Requested, AcceptanceEventKind.Signed);
    }

    [Fact(DisplayName = "Termo anexado em nome da pessoa do pedido no domínio")]
    public void Sign_BySomeoneElse_IsAttached()
    {
        var term = Requested();

        term.Sign(Guid.CreateVersion7(), Marina, Now);

        (term.Status, term.AnsweredById, term.RecipientId).Should().Be((AcceptanceStatus.Signed, Marina, Carla));
        term.Events[^1].Kind.Should().Be(AcceptanceEventKind.Attached);
    }

    [Fact(DisplayName = "Termo anexado sem pedido no domínio")]
    public void Attach_WithoutARequest_IsSigned_InTheNameOfThePerson()
    {
        var document = Guid.CreateVersion7();

        var term = AcceptanceTerm.Attach(Guid.CreateVersion7(), Guid.CreateVersion7(), "Aceite do escopo", Carla, document, Marina, Now);

        (term.Status, term.RecipientId, term.RequestedById, term.AnsweredById, term.DocumentId, term.IsRequest)
            .Should().Be((AcceptanceStatus.Signed, Carla, Marina, Marina, document, false));
        term.Events.Select(item => (item.Kind, item.ActorId)).Should().Equal((AcceptanceEventKind.Attached, Marina));
    }

    [Fact(DisplayName = "Recusa com motivo no domínio")]
    public void Refuse_KeepsTheReason()
    {
        var term = Requested();

        term.Refuse("Falta o relatório de testes", Carla, Now);

        (term.Status, term.RefusalReason, term.AnsweredById, term.DocumentId)
            .Should().Be((AcceptanceStatus.Refused, "Falta o relatório de testes", Carla, (Guid?)null));
        term.Events[^1].Kind.Should().Be(AcceptanceEventKind.Refused);
    }

    [Theory(DisplayName = "Motivo da recusa")]
    [InlineData("  Falta o relatório  ", "Falta o relatório")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void Reason_IsNeeded(string? reason, string? expected) => AcceptanceTerm.ReasonOf(reason).Should().Be(expected);

    [Fact(DisplayName = "Limite do motivo da recusa")]
    public void Reason_HasALimit()
    {
        AcceptanceTerm.ReasonOf(new string('r', 1_000)).Should().HaveLength(1_000);
        AcceptanceTerm.ReasonOf(new string('r', 1_001)).Should().BeNull();
    }

    [Fact(DisplayName = "Pedido respondido não recebe outra resposta no domínio")]
    public void AnsweredRequest_TakesNoOtherAnswer()
    {
        var term = Requested();
        term.Refuse("Falta o relatório de testes", Carla, Now);

        term.Invoking(item => item.Sign(Guid.CreateVersion7(), Carla, Now)).Should().Throw<InvalidOperationException>();
        term.Invoking(item => item.Refuse("De novo", Carla, Now)).Should().Throw<InvalidOperationException>();
        term.Invoking(item => item.Remind(Marina, Now)).Should().Throw<InvalidOperationException>();
    }

    [Fact(DisplayName = "Lembrete no histórico")]
    public void Remind_EntersTheHistory()
    {
        var term = Requested();

        term.Remind(Marina, Now.AddDays(2));

        term.Status.Should().Be(AcceptanceStatus.Pending);
        term.Events.Select(item => (item.Kind, item.At)).Should().Equal(
            (AcceptanceEventKind.Requested, Now), (AcceptanceEventKind.Reminded, Now.AddDays(2)));
    }

    [Theory(DisplayName = "Matriz dos termos")]
    [InlineData(ProjectMemberRole.Manager, true)]
    [InlineData(ProjectMemberRole.TechLead, false)]
    [InlineData(ProjectMemberRole.Contributor, false)]
    [InlineData(ProjectMemberRole.Developer, false)]
    [InlineData(ProjectMemberRole.Viewer, false)]
    public void Managing_IsOfTheManager(ProjectMemberRole role, bool manages)
    {
        var viewer = new DocumentViewer(Guid.CreateVersion7(), IsAdministrator: false);
        var project = ProjectWith(role);
        var term = Requested();

        AcceptanceAccess.Manages(viewer, project).Should().Be(manages);
        AcceptanceAccess.Sees(viewer, project, term).Should().Be(manages);
        AcceptanceAccess.ActionsOf(viewer, project, term)
            .Should().Be(new AcceptanceActions(CanAnswer: false, CanAttach: manages, CanRemind: manages, CanCancel: manages));
    }

    [Fact(DisplayName = "Administrador gerencia os termos")]
    public void Administrator_Manages_WithoutARole()
    {
        var administrator = new DocumentViewer(Guid.CreateVersion7(), IsAdministrator: true);

        AcceptanceAccess.Manages(administrator, ProjectWith(null)).Should().BeTrue();
    }

    [Theory(DisplayName = "Pessoa do pedido responde em qualquer papel")]
    [InlineData(ProjectMemberRole.TechLead)]
    [InlineData(ProjectMemberRole.Contributor)]
    [InlineData(ProjectMemberRole.Developer)]
    [InlineData(ProjectMemberRole.Viewer)]
    public void ThePerson_SeesAndAnswers_InAnyRole(ProjectMemberRole role)
    {
        var carla = new DocumentViewer(Carla, IsAdministrator: false);
        var project = ProjectWith(role);
        var term = Requested();

        AcceptanceAccess.Sees(carla, project, term).Should().BeTrue();
        AcceptanceAccess.ActionsOf(carla, project, term)
            .Should().Be(new AcceptanceActions(CanAnswer: true, CanAttach: false, CanRemind: false, CanCancel: false));
    }

    [Fact(DisplayName = "Termos de projeto encerrado no domínio")]
    public void ClosedProject_TakesNoAction()
    {
        var term = Requested();

        AcceptanceAccess.ActionsOf(new DocumentViewer(Carla, false), ProjectWith(ProjectMemberRole.Contributor, closed: true), term)
            .Should().Be(new AcceptanceActions(false, false, false, false));
        AcceptanceAccess.ActionsOf(new DocumentViewer(Marina, false), ProjectWith(ProjectMemberRole.Manager, closed: true), term)
            .Should().Be(new AcceptanceActions(false, false, false, false));
    }

    [Fact(DisplayName = "Termo respondido sem ações")]
    public void AnsweredTerm_TakesNoAction()
    {
        var term = Requested();
        term.Sign(Guid.CreateVersion7(), Carla, Now);

        AcceptanceAccess.ActionsOf(new DocumentViewer(Marina, false), ProjectWith(ProjectMemberRole.Manager), term)
            .Should().Be(new AcceptanceActions(false, false, false, false));
    }
}
