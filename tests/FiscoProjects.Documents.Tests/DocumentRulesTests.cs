using FiscoProjects.Documents.Domain;
using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.Documents.Tests;

// Spec: documentos (domain side): "Pastas padrão", "Pastas do projeto", "Nova versão do documento" and "Permissões nos
// documentos" (ADR-0031, ADR-0045, ADR-0093).
public sealed class DocumentRulesTests
{
    private static readonly DocumentViewer Person = new(Guid.CreateVersion7(), IsAdministrator: false);
    private static readonly DateTime Now = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    private static ProjectAccess ProjectWith(ProjectMemberRole? role) =>
        new(Guid.CreateVersion7(), "ERP-FIN", "Implantação do ERP financeiro", IsClosed: false, role);

    [Theory(DisplayName = "Matriz dos documentos")]
    [InlineData(ProjectMemberRole.Manager, true, true, true, true)]
    [InlineData(ProjectMemberRole.TechLead, true, true, true, false)]
    [InlineData(ProjectMemberRole.Contributor, true, false, false, false)]
    [InlineData(ProjectMemberRole.Developer, true, false, false, false)]
    [InlineData(ProjectMemberRole.Viewer, false, false, false, false)]
    public void Permissions_FollowTheRole(ProjectMemberRole role, bool upload, bool approve, bool delete, bool terms) =>
        DocumentAccess.Of(Person, ProjectWith(role)).Should().Be(new DocumentPermissions(upload, approve, delete, terms));

    [Fact(DisplayName = "Administrador em qualquer projeto")]
    public void Administrator_DoesEverything_WithoutARole() =>
        DocumentAccess.Of(Person with { IsAdministrator = true }, ProjectWith(null))
            .Should().Be(new DocumentPermissions(true, true, true, true));

    [Fact(DisplayName = "Quem aprova e quem muda os outros status")]
    public void Approving_IsOfWhoApproves()
    {
        var contributor = DocumentAccess.Of(Person, ProjectWith(ProjectMemberRole.Contributor));

        contributor.MaySet(DocumentStatus.InReview).Should().BeTrue();
        contributor.MaySet(DocumentStatus.Obsolete).Should().BeTrue();
        contributor.MaySet(DocumentStatus.Approved).Should().BeFalse();
    }

    [Fact(DisplayName = "Nova versão de documento aprovado volta para revisão no domínio")]
    public void NewVersion_OfAnApprovedDocument_GoesBackToReview()
    {
        var beatriz = Guid.CreateVersion7();
        var carla = Guid.CreateVersion7();
        var document = Document.Upload(Guid.CreateVersion7(), Guid.CreateVersion7(), "Regras de CFOP.xlsx", "f", "1", 10, beatriz, Now);
        document.ChangeStatus(DocumentStatus.Approved, beatriz, Now);

        document.AddVersion("2", 12, carla, Now.AddHours(1));

        (document.Version, document.Size, document.Status, document.StatusChangedById, document.UpdatedById)
            .Should().Be((2, 12L, DocumentStatus.InReview, carla, carla));
        document.Versions.Select(version => version.Number).Should().Equal(1, 2);
    }

    [Theory(DisplayName = "Nova versão mantém o status no domínio")]
    [InlineData(DocumentStatus.Draft)]
    [InlineData(DocumentStatus.InReview)]
    [InlineData(DocumentStatus.Obsolete)]
    public void NewVersion_KeepsTheOtherStatuses(DocumentStatus status)
    {
        var who = Guid.CreateVersion7();
        var document = Document.Upload(Guid.CreateVersion7(), Guid.CreateVersion7(), "Ata.docx", "f", "1", 10, who, Now);
        document.ChangeStatus(status, who, Now);

        document.AddVersion("2", 12, Guid.CreateVersion7(), Now.AddHours(1));

        (document.Status, document.StatusChangedById).Should().Be((status, who));
    }

    [Fact(DisplayName = "Documento novo é rascunho")]
    public void NewDocument_IsADraftInVersionOne()
    {
        var who = Guid.CreateVersion7();

        var document = Document.Upload(Guid.CreateVersion7(), Guid.CreateVersion7(), "Ata.docx", "f", "1", 10, who, Now);

        (document.Version, document.Status, document.StatusChangedById, document.CreatedById).Should().Be((1, DocumentStatus.Draft, who, who));
    }

    [Theory(DisplayName = "Nomes de pasta")]
    [InlineData("  07 Treinamentos ", "07 Treinamentos")]
    [InlineData("", null)]
    [InlineData("Atas/Reuniões", null)]
    [InlineData("Contratos: compras", null)]
    [InlineData("Entregas.", null)]
    public void FolderNames_AreTrimmedAndChecked(string name, string? expected) =>
        FolderNames.Of(name).Should().Be(expected);

    [Fact(DisplayName = "Nome de pasta longo")]
    public void FolderNames_HaveUpTo120Characters()
    {
        FolderNames.Of(new string('a', 120)).Should().NotBeNull();
        FolderNames.Of(new string('a', 121)).Should().BeNull();
    }

    [Fact(DisplayName = "Pastas de um projeto novo")]
    public void NewProject_GetsTheActiveDefaultFolders()
    {
        var minutes = new DefaultFolder("04 Atas de reunião");
        minutes.Change("04 Atas de reunião", isActive: false).Should().BeTrue();
        var project = Guid.CreateVersion7();

        var folders = DocumentFolder.For(project, [new DefaultFolder("02 Contratos e compras"), minutes, new DefaultFolder("01 Gestão do projeto")]);

        folders.Select(folder => folder.Name).Should().Equal("01 Gestão do projeto", "02 Contratos e compras");
        folders.Should().OnlyContain(folder => folder.ProjectId == project && !folder.IsAcceptance);
    }

    [Fact(DisplayName = "Nome do arquivo")]
    public void FileNames_LeaveTheFolderOut()
    {
        Document.NameOf(" Mapa de processos fiscais.docx ").Should().Be("Mapa de processos fiscais.docx");
        Document.NameOf("C:/temp/Mapa.docx").Should().Be("Mapa.docx");
        Document.NameOf(string.Empty).Should().BeNull();
        Document.NameOf(new string('a', 256)).Should().BeNull();
    }
}
