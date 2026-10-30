using FiscoProjects.Documents.Domain;
using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.Documents.Tests;

// Spec: termos-de-aceite (domain side): "Pedido de aceite", "Termo assinado", "Termo anexado pelo gestor", "Lembrar e
// cancelar", "Dados dos signatários", "Pedidos anteriores" and "Termos só da gestão" (ADR-0095, ADR-0129).
public sealed class AcceptanceRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Marina = Guid.CreateVersion7();
    private static readonly AcceptanceFile ToSign = new("f", "termo-homologacao.pdf", 2_048);

    private static ProjectAccess ProjectWith(ProjectMemberRole? role, bool closed = false) =>
        new(Guid.CreateVersion7(), "ERP-FIN", "Implantação do ERP financeiro", closed, role);

    private static IReadOnlyList<AcceptanceSignerForm> TwoSigners() =>
        AcceptanceSignerForm.Of([("Ana Souza", "ana.souza@cliente.example"), ("Bruno Lima", "bruno.lima@cliente.example")])!;

    private static AcceptanceTerm Requested() =>
        AcceptanceTerm.Request(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new AcceptanceForm("Aceite da homologação", null),
            TwoSigners(),
            ToSign,
            new AcceptanceDelivery("Local", "doc-1", ["s-1", "s-2"]),
            Marina,
            Now);

    [Fact(DisplayName = "Pedido pendente com os signatários")]
    public void Request_IsPending_WithTheSigners()
    {
        var term = Requested();

        (term.Status, term.RecipientId, term.RequestedById, term.IsRequest, term.IsSignatureRequest, term.ProviderDocumentId)
            .Should().Be((AcceptanceStatus.Pending, (Guid?)null, Marina, true, true, "doc-1"));
        term.Signers.Select(signer => (signer.Position, signer.Name, signer.Email, signer.Status, signer.ProviderSignerId)).Should().Equal(
            (1, "Ana Souza", "ana.souza@cliente.example", AcceptanceSignerStatus.Pending, "s-1"),
            (2, "Bruno Lima", "bruno.lima@cliente.example", AcceptanceSignerStatus.Pending, "s-2"));
        term.Events.Select(item => (item.Kind, item.ActorId)).Should().Equal((AcceptanceEventKind.Requested, (Guid?)Marina));
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

    [Fact(DisplayName = "Signatários do pedido aparados")]
    public void Signers_AreTrimmed()
    {
        var signers = AcceptanceSignerForm.Of([("  Ana Souza ", " ana.souza@cliente.example ")]);

        signers.Should().Equal(new AcceptanceSignerForm("Ana Souza", "ana.souza@cliente.example"));
    }

    [Fact(DisplayName = "Signatários inválidos")]
    public void Signers_AreRefused_WhenNotValid()
    {
        var eleven = Enumerable.Range(1, 11).Select(index => ((string?)$"Pessoa {index}", (string?)$"pessoa{index}@cliente.example")).ToList();

        AcceptanceSignerForm.Of([]).Should().BeNull();
        AcceptanceSignerForm.Of(eleven).Should().BeNull();
        AcceptanceSignerForm.Of(eleven[..10]).Should().HaveCount(10);
        AcceptanceSignerForm.Of([("Ana Souza", "ana.souza")]).Should().BeNull();
        AcceptanceSignerForm.Of([("Ana Souza", "Ana <ana.souza@cliente.example>")]).Should().BeNull();
        AcceptanceSignerForm.Of([("", "ana.souza@cliente.example")]).Should().BeNull();
        AcceptanceSignerForm.Of([(new string('a', 121), "ana.souza@cliente.example")]).Should().BeNull();
        AcceptanceSignerForm.Of([("Ana Souza", "ana.souza@cliente.example"), ("Ana S.", "ANA.SOUZA@cliente.example")])
            .Should().BeNull();
    }

    [Fact(DisplayName = "Assinatura parcial no domínio")]
    public void OneSignature_KeepsTheRequestPending()
    {
        var term = Requested();
        var ana = term.Signers[0];

        term.SignerSigned(ana.Id, Now.AddHours(1)).Should().BeTrue();

        term.Status.Should().Be(AcceptanceStatus.Pending);
        (ana.Status, ana.RespondedAt, ana.Email).Should().Be((AcceptanceSignerStatus.Signed, (DateTime?)Now.AddHours(1), "ana.souza@cliente.example"));
        term.AllSigned.Should().BeFalse();
        term.Events[^1].Should().BeEquivalentTo(new { Kind = AcceptanceEventKind.Signed, ActorId = (Guid?)null, SignerId = (Guid?)ana.Id });
    }

    [Fact(DisplayName = "Mesma assinatura aplicada uma vez")]
    public void TheSameSignature_IsKeptOnce()
    {
        var term = Requested();
        var ana = term.Signers[0];
        term.SignerSigned(ana.Id, Now);

        term.SignerSigned(ana.Id, Now.AddMinutes(15)).Should().BeFalse();

        term.Events.Count(item => item.Kind == AcceptanceEventKind.Signed).Should().Be(1);
        ana.RespondedAt.Should().Be(Now);
    }

    [Fact(DisplayName = "E-mail apagado ao fechar o pedido")]
    public void Emails_AreForgotten_WhenEverySignerSigned()
    {
        var term = Requested();
        var document = Guid.CreateVersion7();
        term.SignerSigned(term.Signers[0].Id, Now);
        term.SignerSigned(term.Signers[1].Id, Now.AddHours(1));

        term.CompleteSigning(document, Now.AddHours(2));

        (term.Status, term.DocumentId, term.AnsweredById, term.AnsweredAt)
            .Should().Be((AcceptanceStatus.Signed, (Guid?)document, (Guid?)null, (DateTime?)Now.AddHours(2)));
        term.Signers.Select(signer => (signer.Name, signer.Email)).Should().Equal(("Ana Souza", (string?)null), ("Bruno Lima", (string?)null));
    }

    [Fact(DisplayName = "Termo assinado só com todos os signatários")]
    public void Signing_NeedsEverySigner()
    {
        var term = Requested();
        term.SignerSigned(term.Signers[0].Id, Now);

        term.Invoking(item => item.CompleteSigning(Guid.CreateVersion7(), Now)).Should().Throw<InvalidOperationException>();
    }

    [Fact(DisplayName = "Recusa de um signatário")]
    public void OneRefusal_RefusesTheTerm()
    {
        var term = Requested();
        var bruno = term.Signers[1];
        term.SignerSigned(term.Signers[0].Id, Now);

        term.SignerRefused(bruno.Id, "  Falta o relatório de testes  ", Now.AddHours(1)).Should().BeTrue();

        (term.Status, term.RefusalReason, term.DocumentId, term.AnsweredById)
            .Should().Be((AcceptanceStatus.Refused, "Falta o relatório de testes", (Guid?)null, (Guid?)null));
        (bruno.Status, bruno.RefusalReason).Should().Be((AcceptanceSignerStatus.Refused, "Falta o relatório de testes"));
        term.Signers.Should().OnlyContain(signer => signer.Email == null);
        term.Events[^1].Should().BeEquivalentTo(new { Kind = AcceptanceEventKind.Refused, SignerId = (Guid?)bruno.Id });
    }

    [Fact(DisplayName = "Motivo longo do provedor cortado")]
    public void ALongReason_IsCut()
    {
        var term = Requested();

        term.SignerRefused(term.Signers[0].Id, new string('m', 1_500), Now);

        term.RefusalReason.Should().HaveLength(AcceptanceTerm.ReasonMaxLength);
    }

    [Fact(DisplayName = "Termo anexado num pedido pendente")]
    public void SignedTerm_AttachedToARequest()
    {
        var term = Requested();
        var document = Guid.CreateVersion7();

        term.Sign(document, Marina, Now.AddDays(1));

        (term.Status, term.DocumentId, term.AnsweredById).Should().Be((AcceptanceStatus.Signed, (Guid?)document, (Guid?)Marina));
        term.Signers.Should().OnlyContain(signer => signer.Email == null && signer.Status == AcceptanceSignerStatus.Pending);
        term.Events[^1].Should().BeEquivalentTo(new { Kind = AcceptanceEventKind.Attached, ActorId = (Guid?)Marina });
    }

    [Fact(DisplayName = "Termo anexado sem pedido")]
    public void Attach_IsSigned_WithoutARequest()
    {
        var document = Guid.CreateVersion7();

        var term = AcceptanceTerm.Attach(Guid.CreateVersion7(), Guid.CreateVersion7(), "Aceite do escopo", document, Marina, Now);

        (term.Status, term.RecipientId, term.RequestedById, term.AnsweredById, term.DocumentId, term.IsRequest, term.Signers.Count)
            .Should().Be((AcceptanceStatus.Signed, (Guid?)null, Marina, (Guid?)Marina, (Guid?)document, false, 0));
        term.Events.Select(item => (item.Kind, item.ActorId)).Should().Equal((AcceptanceEventKind.Attached, (Guid?)Marina));
    }

    [Fact(DisplayName = "Pedido respondido não muda")]
    public void AnsweredRequest_TakesNothingElse()
    {
        var term = Requested();
        term.SignerRefused(term.Signers[0].Id, "Falta o relatório de testes", Now);

        term.Invoking(item => item.Sign(Guid.CreateVersion7(), Marina, Now)).Should().Throw<InvalidOperationException>();
        term.Invoking(item => item.SignerSigned(term.Signers[1].Id, Now)).Should().Throw<InvalidOperationException>();
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

    [Theory(DisplayName = "Permissões nos termos por papel")]
    [InlineData(ProjectMemberRole.Manager, true)]
    [InlineData(ProjectMemberRole.TechLead, false)]
    [InlineData(ProjectMemberRole.Contributor, false)]
    [InlineData(ProjectMemberRole.Developer, false)]
    [InlineData(ProjectMemberRole.Viewer, false)]
    public void Managing_IsOfTheManager(ProjectMemberRole role, bool manages)
    {
        var viewer = new DocumentViewer(Guid.CreateVersion7(), IsAdministrator: false);
        var project = ProjectWith(role);

        AcceptanceAccess.Manages(viewer, project).Should().Be(manages);
        AcceptanceAccess.ActionsOf(viewer, project, Requested(), providerReached: true)
            .Should().Be(new AcceptanceActions(CanAttach: manages, CanRemind: manages, CanCancel: manages));
    }

    [Fact(DisplayName = "Administrador gerencia os termos")]
    public void Administrator_Manages_WithoutARole()
    {
        var administrator = new DocumentViewer(Guid.CreateVersion7(), IsAdministrator: true);

        AcceptanceAccess.Manages(administrator, ProjectWith(null)).Should().BeTrue();
    }

    [Fact(DisplayName = "Lembrete só no provedor do pedido")]
    public void Remind_NeedsTheProviderOfTheRequest()
    {
        var manager = new DocumentViewer(Marina, false);

        AcceptanceAccess.ActionsOf(manager, ProjectWith(ProjectMemberRole.Manager), Requested(), providerReached: false)
            .Should().Be(new AcceptanceActions(CanAttach: true, CanRemind: false, CanCancel: true));
    }

    [Fact(DisplayName = "Termos de projeto encerrado no domínio")]
    public void ClosedProject_TakesNoAction()
    {
        AcceptanceAccess.ActionsOf(new DocumentViewer(Marina, false), ProjectWith(ProjectMemberRole.Manager, closed: true), Requested(), true)
            .Should().Be(new AcceptanceActions(false, false, false));
    }

    [Fact(DisplayName = "Termo respondido sem ações")]
    public void AnsweredTerm_TakesNoAction()
    {
        var term = Requested();
        term.Sign(Guid.CreateVersion7(), Marina, Now);

        AcceptanceAccess.ActionsOf(new DocumentViewer(Marina, false), ProjectWith(ProjectMemberRole.Manager), term, true)
            .Should().Be(new AcceptanceActions(false, false, false));
    }
}
