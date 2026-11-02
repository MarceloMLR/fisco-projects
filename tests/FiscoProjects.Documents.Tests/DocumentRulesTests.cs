using FiscoProjects.Documents.Domain;
using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.Documents.Tests;

// Spec: documentos (domain side): "Pastas padrão", "Pastas do projeto", "Nova versão do documento", "Permissões nos
// documentos" and who sees the folders (ADR-0031, ADR-0045, ADR-0093, ADR-0148).
public sealed class DocumentRulesTests
{
    private static readonly DocumentViewer Person = new(Guid.CreateVersion7(), IsAdministrator: false);
    private static readonly DateTime Now = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    private static ProjectAccess ProjectWith(ProjectMemberRole? role) =>
        new(Guid.CreateVersion7(), "ERP-FIN", "Implantação do ERP financeiro", IsClosed: false, role);

    [Theory(DisplayName = "Matriz dos documentos")]
    [InlineData(ProjectMemberRole.Manager, true, true, true)]
    [InlineData(ProjectMemberRole.TechLead, true, true, true)]
    [InlineData(ProjectMemberRole.Contributor, true, false, false)]
    [InlineData(ProjectMemberRole.Developer, true, false, false)]
    [InlineData(ProjectMemberRole.Viewer, false, false, false)]
    public void Permissions_FollowTheRole(ProjectMemberRole role, bool upload, bool approve, bool delete) =>
        DocumentAccess.Of(Person, ProjectWith(role)).Should().Be(new DocumentPermissions(upload, approve, delete));

    [Fact(DisplayName = "Administrador em qualquer projeto")]
    public void Administrator_DoesEverything_WithoutARole() =>
        DocumentAccess.Of(Person with { IsAdministrator = true }, ProjectWith(null))
            .Should().Be(new DocumentPermissions(true, true, true));

    // Spec: documentos, "Permissões nos documentos" and "Quem vê as pastas do projeto" (ADR-0148).
    [Theory(DisplayName = "Quem vê a pasta por papel")]
    [InlineData(ProjectMemberRole.Manager, true)]
    [InlineData(ProjectMemberRole.TechLead, true)]
    [InlineData(ProjectMemberRole.Contributor, false)]
    [InlineData(ProjectMemberRole.Developer, false)]
    [InlineData(ProjectMemberRole.Viewer, false)]
    public void TheFolder_IsSeenByItsRoles_AndTheManagers(ProjectMemberRole role, bool sees)
    {
        var folder = new DocumentFolder(Guid.CreateVersion7(), "01 Gestão do projeto", isAcceptance: false, FolderViewers.TechLead);

        DocumentAccess.SeesFolder(Person, ProjectWith(role), folder).Should().Be(sees);
        DocumentAccess.SeesFolder(Person with { IsAdministrator = true }, ProjectWith(null), folder).Should().BeTrue();
    }

    [Fact(DisplayName = "Gestão vê a pasta sem papéis")]
    public void AFolderOfNoRole_IsOnlyTheManagers()
    {
        var folder = new DocumentFolder(Guid.CreateVersion7(), "01 Gestão do projeto", isAcceptance: false, FolderViewers.None);

        DocumentAccess.SeesFolder(Person, ProjectWith(ProjectMemberRole.Manager), folder).Should().BeTrue();
        foreach (var role in new[] { ProjectMemberRole.TechLead, ProjectMemberRole.Contributor, ProjectMemberRole.Developer, ProjectMemberRole.Viewer })
        {
            DocumentAccess.SeesFolder(Person, ProjectWith(role), folder).Should().BeFalse();
        }
    }

    [Fact(DisplayName = "Pasta de termos fixa no domínio")]
    public void TheFolderOfTheTerms_StaysOnlyTheManagers()
    {
        var terms = new DocumentFolder(Guid.CreateVersion7(), "06 Termos de aceite", isAcceptance: true, FolderViewers.Everyone);

        terms.VisibleTo.Should().Be(FolderViewers.None);
        terms.ChangeVisibility(FolderViewers.TechLead).Should().BeFalse();
        terms.VisibleTo.Should().Be(FolderViewers.None);
        DocumentAccess.SeesFolder(Person, ProjectWith(ProjectMemberRole.TechLead), terms).Should().BeFalse();
        DocumentAccess.SeesFolder(Person, ProjectWith(ProjectMemberRole.Manager), terms).Should().BeTrue();
    }

    [Fact(DisplayName = "Papéis de quem vê a pasta")]
    public void TheRoles_AreKeptAsFlags_InTheirOrder()
    {
        FolderVisibility.Of([FolderRole.Viewer, FolderRole.TechLead]).Should().Be(FolderViewers.TechLead | FolderViewers.Viewer);
        FolderVisibility.Of([]).Should().Be(FolderViewers.None);
        FolderVisibility.RolesOf(FolderViewers.Viewer | FolderViewers.Contributor)
            .Should().Equal(FolderRole.Contributor, FolderRole.Viewer);
        FolderVisibility.RolesOf(FolderViewers.Everyone).Should().HaveCount(4);
        FolderVisibility.AreValid([FolderRole.Developer, (FolderRole)9]).Should().BeFalse();
    }

    [Fact(DisplayName = "Só o administrador muda quem vê")]
    public void OnlyTheAdministrator_ChangesWhoSeesAFolder()
    {
        DocumentAccess.MayChangeFolderVisibility(Person with { IsAdministrator = true }).Should().BeTrue();
        DocumentAccess.MayChangeFolderVisibility(Person).Should().BeFalse();
    }

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

    [Fact(DisplayName = "Pastas de um projeto novo com quem vê")]
    public void NewProject_GetsWhoSeesEachDefaultFolder()
    {
        var management = new DefaultFolder("01 Gestão do projeto");
        management.ChangeVisibility(FolderViewers.TechLead).Should().BeTrue();

        var folders = DocumentFolder.For(Guid.CreateVersion7(), [management, new DefaultFolder("02 Contratos e compras")]);

        folders.Select(folder => folder.VisibleTo).Should().Equal(FolderViewers.TechLead, FolderViewers.Everyone);
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
