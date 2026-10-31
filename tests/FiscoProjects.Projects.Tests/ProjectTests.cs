using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: projetos, requirements "Código do projeto", "Dados do projeto", "Status do projeto" and "Quem altera o
// projeto" (module side).
public sealed class ProjectTests
{
    private static readonly DateTime Now = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

    [Theory(DisplayName = "Código do projeto em maiúsculas")]
    [InlineData("erp-fin", "ERP-FIN")]
    [InlineData(" SPED-27 ", "SPED-27")]
    [InlineData("Portal-nf", "PORTAL-NF")]
    [InlineData("AB", "AB")]
    [InlineData("A1B2C3D4E5F6G7H", "A1B2C3D4E5F6G7H")]
    public void Code_IsKeptInCapitals(string text, string expected) =>
        ProjectCode.Normalize(text).Should().Be(expected);

    [Theory(DisplayName = "Código inválido")]
    [InlineData("1-ERP")]
    [InlineData("ERP-")]
    [InlineData("ERP--FIN")]
    [InlineData("-ERP")]
    [InlineData("E")]
    [InlineData("A1B2C3D4E5F6G7H8")]
    [InlineData("ERP FIN")]
    [InlineData("ERP_FIN")]
    [InlineData("ÇÃO")]
    [InlineData("")]
    [InlineData(null)]
    public void WrongCode_IsRefused(string? text) =>
        ProjectCode.Normalize(text).Should().BeNull();

    [Fact(DisplayName = "Projeto começa em Planejamento")]
    public void NewProject_IsInPlanning_WithItsManagerAndTechLead()
    {
        var manager = Guid.CreateVersion7();
        var techLead = Guid.CreateVersion7();

        var project = Project.Create("ERP-FIN", " Implantação do ERP financeiro ", "  ", null, manager, techLead, Now);

        project.Status.Should().Be(ProjectStatus.Planning);
        project.Name.Should().Be("Implantação do ERP financeiro");
        project.Description.Should().BeNull();
        project.RoleOf(manager).Should().Be(ProjectRole.Manager);
        project.RoleOf(techLead).Should().Be(ProjectRole.TechLead);
        project.RoleOf(Guid.CreateVersion7()).Should().BeNull();
        project.Members.Should().OnlyContain(member => member.ProjectId == project.Id && member.AddedAt == Now);
    }

    [Fact(DisplayName = "Líder técnico diferente do gerente")]
    public void TechLead_IsAnotherPerson()
    {
        var manager = Guid.CreateVersion7();

        var create = () => Project.Create("ERP-FIN", "ERP", null, null, manager, manager, Now);

        create.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Dados fora da regra no domínio")]
    public void WrongData_IsRefusedByTheProject()
    {
        var manager = Guid.CreateVersion7();

        ((Action)(() => Project.Create("erp-fin", "ERP", null, null, manager, null, Now))).Should().Throw<ArgumentException>();
        ((Action)(() => Project.Create("ERP-FIN", " ", null, null, manager, null, Now))).Should().Throw<ArgumentException>();
        ((Action)(() => Project.Create("ERP-FIN", "ERP", new string('a', 4001), null, manager, null, Now)))
            .Should().Throw<ArgumentException>();
    }

    [Theory(DisplayName = "Projeto encerrado somente leitura")]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Cancelled)]
    public void ClosedProject_RefusesChanges_UntilReopened(ProjectStatus closed)
    {
        var project = Project.Create("ERP-FIN", "ERP", null, null, Guid.CreateVersion7(), null, Now);
        project.ChangeStatus(closed);

        var change = () => project.Change("ERP-FIN", "Outro nome", null, null);

        project.IsClosed.Should().BeTrue();
        change.Should().Throw<ProjectClosedException>();
        project.ChangeStatus(ProjectStatus.InProgress);
        change.Should().NotThrow();
        project.Name.Should().Be("Outro nome");
    }

    [Fact(DisplayName = "Pausar e retomar")]
    public void Project_IsPaused_AndResumed()
    {
        var project = Project.Create("ERP-FIN", "ERP", null, null, Guid.CreateVersion7(), null, Now);

        project.ChangeStatus(ProjectStatus.InProgress);
        project.ChangeStatus(ProjectStatus.Paused);
        project.Status.Should().Be(ProjectStatus.Paused);
        project.IsClosed.Should().BeFalse();
        project.ChangeStatus(ProjectStatus.InProgress);

        project.Status.Should().Be(ProjectStatus.InProgress);
    }

    // The matrix of ADR-0043 for the project: viewing, changing the data, changing the status, managing the people,
    // editing the schedule, updating the progress of one's own tasks, deleting it (ADR-0041), choosing its folder
    // (ADR-0122) and downloading the XML of the MS Project with the e-mails (ADR-0136).
    [Theory(DisplayName = "Permissões no projeto")]
    [InlineData(false, ProjectRole.Manager, true, true, true, true, true, true, true, false, true, true)]
    [InlineData(false, ProjectRole.TechLead, true, true, false, true, false, false, true, false, false, false)]
    [InlineData(false, ProjectRole.Contributor, true, false, false, false, false, false, true, false, false, false)]
    [InlineData(false, ProjectRole.Developer, true, false, false, false, false, false, true, false, false, false)]
    [InlineData(false, ProjectRole.Viewer, true, false, false, false, false, false, false, false, false, false)]
    [InlineData(false, null, false, false, false, false, false, false, false, false, false, false)]
    [InlineData(true, null, true, true, true, true, true, true, true, true, true, true)]
    [InlineData(true, ProjectRole.Viewer, true, true, true, true, true, true, true, true, true, true)]
    public void Permissions_FollowTheMatrix(
        bool administrator,
        ProjectRole? role,
        bool canView,
        bool canEditData,
        bool canChangeStatus,
        bool canManagePeople,
        bool canManageManagers,
        bool canEditSchedule,
        bool canUpdateOwnProgress,
        bool canDelete,
        bool canChooseFolder,
        bool canExportSchedule)
    {
        var permissions = ProjectPermissions.Of(new ProjectViewer(Guid.CreateVersion7(), administrator), role);

        permissions.Should().Be(new ProjectPermissions(
            canView, canEditData, canChangeStatus, canManagePeople, canManageManagers, canEditSchedule, canUpdateOwnProgress,
            canDelete, canChooseFolder, canExportSchedule));
    }

    // ADR-0043: the Administrator and the manager update the progress of any task; the tech lead, the contributor and
    // the developer, only of their own; the viewer, never.
    [Theory(DisplayName = "Quem atualiza o progresso de cada tarefa")]
    [InlineData(true, null, false, true)]
    [InlineData(false, ProjectRole.Manager, false, true)]
    [InlineData(false, ProjectRole.TechLead, true, true)]
    [InlineData(false, ProjectRole.TechLead, false, false)]
    [InlineData(false, ProjectRole.Contributor, true, true)]
    [InlineData(false, ProjectRole.Contributor, false, false)]
    [InlineData(false, ProjectRole.Developer, true, true)]
    [InlineData(false, ProjectRole.Developer, false, false)]
    [InlineData(false, ProjectRole.Viewer, true, false)]
    [InlineData(false, null, true, false)]
    public void WhoUpdatesTheProgress(bool administrator, ProjectRole? role, bool isOwner, bool allowed)
    {
        var permissions = ProjectPermissions.Of(new ProjectViewer(Guid.CreateVersion7(), administrator), role);

        permissions.MayUpdateProgress(isOwner).Should().Be(allowed);
    }

    // ADR-0042, ADR-0043: the tech lead manages the people, but not the managers, and makes no one a manager.
    [Theory(DisplayName = "Quem gerencia cada pessoa")]
    [InlineData(ProjectRole.Manager, null, ProjectRole.Developer, true)]
    [InlineData(ProjectRole.Manager, null, ProjectRole.Manager, true)]
    [InlineData(ProjectRole.Manager, ProjectRole.Manager, null, true)]
    [InlineData(ProjectRole.TechLead, null, ProjectRole.Developer, true)]
    [InlineData(ProjectRole.TechLead, ProjectRole.Developer, ProjectRole.TechLead, true)]
    [InlineData(ProjectRole.TechLead, ProjectRole.TechLead, null, true)]
    [InlineData(ProjectRole.TechLead, null, ProjectRole.Manager, false)]
    [InlineData(ProjectRole.TechLead, ProjectRole.Developer, ProjectRole.Manager, false)]
    [InlineData(ProjectRole.TechLead, ProjectRole.Manager, ProjectRole.TechLead, false)]
    [InlineData(ProjectRole.TechLead, ProjectRole.Manager, null, false)]
    [InlineData(ProjectRole.Contributor, null, ProjectRole.Developer, false)]
    [InlineData(ProjectRole.Developer, ProjectRole.Viewer, null, false)]
    [InlineData(ProjectRole.Viewer, ProjectRole.Developer, ProjectRole.Contributor, false)]
    public void WhoManagesEachPerson(ProjectRole viewerRole, ProjectRole? current, ProjectRole? next, bool allowed)
    {
        var permissions = ProjectPermissions.Of(new ProjectViewer(Guid.CreateVersion7(), false), viewerRole);

        permissions.MayManage(current, next).Should().Be(allowed);
    }

    // Spec: membros-do-projeto, requirements "Um papel por pessoa", "Último gerente" and "Pessoas de um projeto encerrado".
    [Fact(DisplayName = "Incluir, trocar o papel e remover no domínio")]
    public void Members_AreIncluded_Changed_AndRemoved()
    {
        var (project, manager) = NewProject();
        var diego = Guid.CreateVersion7();

        project.AddMember(diego, ProjectRole.Developer, Now.AddDays(1)).AddedAt.Should().Be(Now.AddDays(1));
        project.RoleOf(diego).Should().Be(ProjectRole.Developer);
        project.ChangeMemberRole(diego, ProjectRole.Contributor).Should().BeTrue();
        project.RoleOf(diego).Should().Be(ProjectRole.Contributor);
        project.RemoveMember(diego).Should().BeTrue();

        project.RoleOf(diego).Should().BeNull();
        project.Members.Select(member => member.UserId).Should().Equal(manager);
    }

    [Fact(DisplayName = "Pessoa que já está no projeto")]
    public void PersonAlreadyInTheProject_IsRefused()
    {
        var (project, manager) = NewProject();

        var include = () => project.AddMember(manager, ProjectRole.Developer, Now);

        include.Should().Throw<ProjectMemberExistsException>();
        project.RoleOf(manager).Should().Be(ProjectRole.Manager);
    }

    [Fact(DisplayName = "Pessoa fora do projeto")]
    public void PersonNotInTheProject_IsNotChangedNorRemoved()
    {
        var (project, _) = NewProject();

        project.ChangeMemberRole(Guid.CreateVersion7(), ProjectRole.Viewer).Should().BeFalse();
        project.RemoveMember(Guid.CreateVersion7()).Should().BeFalse();
    }

    [Fact(DisplayName = "Remover e rebaixar o último gerente")]
    public void LastManager_IsNeitherRemovedNorDemoted()
    {
        var (project, manager) = NewProject();

        var remove = () => project.RemoveMember(manager);
        var demote = () => project.ChangeMemberRole(manager, ProjectRole.TechLead);

        remove.Should().Throw<LastManagerException>();
        demote.Should().Throw<LastManagerException>();
        project.RoleOf(manager).Should().Be(ProjectRole.Manager);
    }

    [Fact(DisplayName = "Outro gerente no projeto")]
    public void WithAnotherManager_AManagerLeaves()
    {
        var (project, manager) = NewProject();
        var beatriz = Guid.CreateVersion7();
        project.AddMember(beatriz, ProjectRole.Manager, Now);

        project.ChangeMemberRole(manager, ProjectRole.TechLead).Should().BeTrue();
        var removeTheLast = () => project.RemoveMember(beatriz);

        removeTheLast.Should().Throw<LastManagerException>();
        project.ChangeMemberRole(manager, ProjectRole.Manager).Should().BeTrue();
        project.RemoveMember(beatriz).Should().BeTrue();
    }

    [Fact(DisplayName = "Pessoas de um projeto encerrado")]
    public void ClosedProject_KeepsItsPeople()
    {
        var (project, manager) = NewProject();
        var diego = Guid.CreateVersion7();
        project.AddMember(diego, ProjectRole.Developer, Now);
        project.ChangeStatus(ProjectStatus.Completed);

        var include = () => project.AddMember(Guid.CreateVersion7(), ProjectRole.Viewer, Now);
        var change = () => project.ChangeMemberRole(diego, ProjectRole.Viewer);
        var remove = () => project.RemoveMember(diego);

        include.Should().Throw<ProjectClosedException>();
        change.Should().Throw<ProjectClosedException>();
        remove.Should().Throw<ProjectClosedException>();
        project.Members.Should().HaveCount(2);
        project.RoleOf(manager).Should().Be(ProjectRole.Manager);
    }

    private static (Project Project, Guid Manager) NewProject()
    {
        var manager = Guid.CreateVersion7();
        return (Project.Create("ERP-FIN", "ERP", null, null, manager, null, Now), manager);
    }
}
