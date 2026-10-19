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

    // The matrix of ADR-0043 for the project: viewing, changing the data and changing the status.
    [Theory(DisplayName = "Permissões no projeto")]
    [InlineData(false, ProjectRole.Manager, true, true, true)]
    [InlineData(false, ProjectRole.TechLead, true, true, false)]
    [InlineData(false, ProjectRole.Contributor, true, false, false)]
    [InlineData(false, ProjectRole.Developer, true, false, false)]
    [InlineData(false, ProjectRole.Viewer, true, false, false)]
    [InlineData(false, null, false, false, false)]
    [InlineData(true, null, true, true, true)]
    [InlineData(true, ProjectRole.Viewer, true, true, true)]
    public void Permissions_FollowTheMatrix(
        bool administrator, ProjectRole? role, bool canView, bool canEditData, bool canChangeStatus)
    {
        var permissions = ProjectPermissions.Of(new ProjectViewer(Guid.CreateVersion7(), administrator), role);

        permissions.Should().Be(new ProjectPermissions(canView, canEditData, canChangeStatus));
    }
}
