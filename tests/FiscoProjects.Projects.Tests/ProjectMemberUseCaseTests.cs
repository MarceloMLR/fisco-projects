using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Projects.Tests;

// Spec: membros-do-projeto (module side): "Quem gerencia as pessoas do projeto", "Pessoas do projeto", "Um papel por
// pessoa", "Último gerente", "Gerente com o papel no sistema", "Incluir pessoa pelo diretório", "Quem sai perde o
// acesso" and "Pessoas de um projeto encerrado"; projetos: "Gerente e líder técnico na criação".
public sealed class ProjectMemberUseCaseTests(SqlServerFixture sqlServer) : IAsyncLifetime
{
    private ProjectsDatabase _database = null!;
    private Guid _paulo;
    private Guid _marina;
    private Guid _beatriz;
    private Guid _rafael;
    private Guid _diego;
    private Guid _lucas;
    private Guid _livia;
    private Guid _project;

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private ProjectViewer Paulo => new(_paulo, IsAdministrator: true);

    private ProjectViewer Marina => new(_marina, IsAdministrator: false);

    private ProjectViewer Rafael => new(_rafael, IsAdministrator: false);

    public async ValueTask InitializeAsync()
    {
        _database = await ProjectsDatabase.CreateAsync(sqlServer);
        _paulo = _database.People.Add("Paulo Nogueira", SystemRole.Administrator);
        _marina = _database.People.Add("Marina Costa", SystemRole.ProjectManager);
        _beatriz = _database.People.Add("Beatriz Lima", SystemRole.ProjectManager);
        _rafael = _database.People.Add("Rafael Souza", SystemRole.TechLead);
        _diego = _database.People.Add("Diego Torres", SystemRole.User);
        _lucas = _database.People.Add("Lucas Prado", SystemRole.User);
        _livia = _database.People.Add("Lívia Prado", SystemRole.User, isActive: false);
        var created = await _database.RunAsync<CreateProject, ProjectSaveResult>(create => create.ExecuteAsync(
            new NewProject("ERP-FIN", "Implantação do ERP financeiro", null, null, null, _rafael), _marina, Cancellation));
        _project = created.Id!.Value;
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact(DisplayName = "Gerente inclui, troca o papel e remove")]
    public async Task Manager_Includes_ChangesTheRole_AndRemoves()
    {
        (await AddAsync(Marina, _diego, ProjectRole.Developer)).Should().Be(new MemberAdded(MemberOutcome.Done, _diego));
        (await ChangeRoleAsync(Marina, _diego, ProjectRole.Contributor)).Should().Be(MemberOutcome.Done);
        (await RolesAsync())[_diego].Should().Be(ProjectRole.Contributor);

        (await RemoveAsync(Marina, _diego)).Should().Be(MemberOutcome.Done);

        (await RolesAsync()).Should().NotContainKey(_diego);
    }

    [Fact(DisplayName = "Pessoas do projeto")]
    public async Task Members_ComeByName_WithTheirData()
    {
        _database.Clock.Advance(TimeSpan.FromDays(1));
        await AddAsync(Marina, _diego, ProjectRole.Developer);

        var members = await ListAsync(Marina);

        members!.Select(member => (member.Name, member.Role, member.CanBeManager)).Should().Equal(
            ("Diego Torres", ProjectRole.Developer, false),
            ("Marina Costa", ProjectRole.Manager, true),
            ("Rafael Souza", ProjectRole.TechLead, false));
        var diego = members![0];
        (diego.Email, diego.AddedAt, diego.LastSignInAt).Should().Be((
            "diego.torres@empresa.example",
            _database.Clock.GetUtcNow().UtcDateTime,
            (DateTime?)new DateTime(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc)));
        (await ListAsync(new ProjectViewer(_lucas, false))).Should().BeNull();
        (await ListAsync(Paulo)).Should().HaveCount(3);
    }

    [Fact(DisplayName = "Líder técnico inclui um desenvolvedor")]
    public async Task TechLead_ManagesWhoIsNotAManager()
    {
        var sofia = _database.People.Add("Sofia Martins", SystemRole.TechLead);

        (await AddAsync(Rafael, sofia, ProjectRole.Developer)).Outcome.Should().Be(MemberOutcome.Done);
        (await ChangeRoleAsync(Rafael, sofia, ProjectRole.TechLead)).Should().Be(MemberOutcome.Done);
        (await RemoveAsync(Rafael, sofia)).Should().Be(MemberOutcome.Done);
    }

    [Fact(DisplayName = "Líder técnico e os gerentes")]
    public async Task TechLead_DoesNotTouchTheManagers()
    {
        await AddAsync(Marina, _diego, ProjectRole.Developer);

        (await AddAsync(Rafael, _beatriz, ProjectRole.Manager)).Outcome.Should().Be(MemberOutcome.Forbidden);
        (await ChangeRoleAsync(Rafael, _diego, ProjectRole.Manager)).Should().Be(MemberOutcome.Forbidden);
        (await ChangeRoleAsync(Rafael, _marina, ProjectRole.TechLead)).Should().Be(MemberOutcome.Forbidden);
        (await RemoveAsync(Rafael, _marina)).Should().Be(MemberOutcome.Forbidden);

        (await RolesAsync()).Should().BeEquivalentTo(new Dictionary<Guid, ProjectRole>
        {
            [_marina] = ProjectRole.Manager,
            [_rafael] = ProjectRole.TechLead,
            [_diego] = ProjectRole.Developer,
        });
    }

    [Theory(DisplayName = "Colaborador, desenvolvedor e leitor não mexem nas pessoas")]
    [InlineData(ProjectRole.Contributor)]
    [InlineData(ProjectRole.Developer)]
    [InlineData(ProjectRole.Viewer)]
    public async Task OtherRoles_DoNotManageThePeople(ProjectRole role)
    {
        await AddAsync(Marina, _diego, role);
        var diego = new ProjectViewer(_diego, false);

        (await AddAsync(diego, _lucas, ProjectRole.Viewer)).Outcome.Should().Be(MemberOutcome.Forbidden);
        (await ChangeRoleAsync(diego, _rafael, ProjectRole.Developer)).Should().Be(MemberOutcome.Forbidden);
        (await RemoveAsync(diego, _rafael)).Should().Be(MemberOutcome.Forbidden);
        (await ListAsync(diego)).Should().HaveCount(3);
    }

    [Fact(DisplayName = "Administrador fora do projeto")]
    public async Task Administrator_ManagesWithoutBeingInIt()
    {
        (await AddAsync(Paulo, _diego, ProjectRole.Viewer)).Outcome.Should().Be(MemberOutcome.Done);
        (await ChangeRoleAsync(Paulo, _rafael, ProjectRole.Manager)).Should().Be(MemberOutcome.InvalidRole);
        (await RemoveAsync(Paulo, _diego)).Should().Be(MemberOutcome.Done);
    }

    [Fact(DisplayName = "Pessoas de um projeto de fora")]
    public async Task Outsider_FindsNoProject()
    {
        var lucas = new ProjectViewer(_lucas, false);

        (await AddAsync(lucas, _diego, ProjectRole.Viewer)).Outcome.Should().Be(MemberOutcome.NotFound);
        (await ChangeRoleAsync(lucas, _rafael, ProjectRole.Developer)).Should().Be(MemberOutcome.NotFound);
        (await RemoveAsync(lucas, _rafael)).Should().Be(MemberOutcome.NotFound);
        (await ChangeRoleAsync(Marina, _lucas, ProjectRole.Developer)).Should().Be(MemberOutcome.NotFound);
        (await RemoveAsync(Marina, _lucas)).Should().Be(MemberOutcome.NotFound);
    }

    [Fact(DisplayName = "Pessoa que já está no projeto")]
    public async Task PersonAlreadyInTheProject_IsRefused()
    {
        (await AddAsync(Marina, _rafael, ProjectRole.Developer)).Outcome.Should().Be(MemberOutcome.MemberExists);

        (await RolesAsync())[_rafael].Should().Be(ProjectRole.TechLead);
    }

    [Fact(DisplayName = "Remover e rebaixar o último gerente")]
    public async Task LastManager_StaysAManager()
    {
        (await RemoveAsync(Paulo, _marina)).Should().Be(MemberOutcome.LastManager);
        (await ChangeRoleAsync(Marina, _marina, ProjectRole.Developer)).Should().Be(MemberOutcome.LastManager);

        (await RolesAsync())[_marina].Should().Be(ProjectRole.Manager);
    }

    [Fact(DisplayName = "Outro gerente no projeto")]
    public async Task WithAnotherManager_OneLeaves()
    {
        (await AddAsync(Marina, _beatriz, ProjectRole.Manager)).Outcome.Should().Be(MemberOutcome.Done);

        (await RemoveAsync(Beatriz, _marina)).Should().Be(MemberOutcome.Done);

        (await RolesAsync())[_beatriz].Should().Be(ProjectRole.Manager);
    }

    [Fact(DisplayName = "Dois gerentes removidos ao mesmo tempo")]
    public async Task TwoRemovalsAtOnce_LeaveAManager()
    {
        await AddAsync(Marina, _beatriz, ProjectRole.Manager);

        var outcomes = await Task.WhenAll(RemoveAsync(Paulo, _beatriz), RemoveAsync(Paulo, _marina));

        outcomes.Should().BeEquivalentTo([MemberOutcome.Done, MemberOutcome.LastManager]);
        (await RolesAsync()).Values.Should().ContainSingle(role => role == ProjectRole.Manager);
    }

    [Fact(DisplayName = "Promover a gerente")]
    public async Task ProjectManager_BecomesAManager()
    {
        await AddAsync(Marina, _beatriz, ProjectRole.Developer);

        (await ChangeRoleAsync(Marina, _beatriz, ProjectRole.Manager)).Should().Be(MemberOutcome.Done);

        (await RolesAsync()).Values.Count(role => role == ProjectRole.Manager).Should().Be(2);
    }

    [Fact(DisplayName = "Gerente sem o papel no sistema")]
    public async Task User_OrPersonOfTheDirectory_IsNoManager()
    {
        var carla = _database.People.AddToDirectory("Carla Mendes");

        (await AddAsync(Marina, _diego, ProjectRole.Manager)).Outcome.Should().Be(MemberOutcome.InvalidRole);
        (await AddDirectoryAsync(Marina, carla, ProjectRole.Manager)).Outcome.Should().Be(MemberOutcome.InvalidRole);
        (await ChangeRoleAsync(Marina, _rafael, ProjectRole.Manager)).Should().Be(MemberOutcome.InvalidRole);

        _database.People.UserOf(carla).Should().BeNull();
        (await RolesAsync()).Should().HaveCount(2);
    }

    [Fact(DisplayName = "Gerente que perde o papel no sistema")]
    public async Task Manager_WhoLosesTheSystemRole_StaysTheManager()
    {
        _database.People.ChangeRole(_marina, SystemRole.User);

        (await RolesAsync())[_marina].Should().Be(ProjectRole.Manager);
        (await AddAsync(Marina, _diego, ProjectRole.Developer)).Outcome.Should().Be(MemberOutcome.Done);
    }

    // Spec: membros-do-projeto, requirement "Líder técnico com o papel no sistema" (ADR-0097).
    [Fact(DisplayName = "Promover a líder técnico")]
    public async Task TechLeadOfTheSystem_BecomesATechLead()
    {
        var sofia = _database.People.Add("Sofia Martins", SystemRole.TechLead);
        await AddAsync(Marina, sofia, ProjectRole.Developer);

        (await ChangeRoleAsync(Marina, sofia, ProjectRole.TechLead)).Should().Be(MemberOutcome.Done);
        (await AddAsync(Marina, _paulo, ProjectRole.TechLead)).Outcome.Should().Be(MemberOutcome.Done);

        (await RolesAsync()).Values.Count(role => role == ProjectRole.TechLead).Should().Be(3);
        (await ListAsync(Marina))!.Single(person => person.Id == sofia).CanBeTechLead.Should().BeTrue();
    }

    [Fact(DisplayName = "Líder técnico sem o papel no sistema")]
    public async Task User_OrPersonOfTheDirectory_IsNoTechLead()
    {
        var carla = _database.People.AddToDirectory("Carla Mendes");
        await AddAsync(Marina, _lucas, ProjectRole.Developer);

        (await AddAsync(Marina, _diego, ProjectRole.TechLead)).Outcome.Should().Be(MemberOutcome.InvalidRole);
        (await AddAsync(Marina, _beatriz, ProjectRole.TechLead)).Outcome.Should().Be(MemberOutcome.InvalidRole);
        (await AddDirectoryAsync(Marina, carla, ProjectRole.TechLead)).Outcome.Should().Be(MemberOutcome.InvalidRole);
        (await ChangeRoleAsync(Marina, _lucas, ProjectRole.TechLead)).Should().Be(MemberOutcome.InvalidRole);

        _database.People.UserOf(carla).Should().BeNull();
        (await RolesAsync())[_lucas].Should().Be(ProjectRole.Developer);
        (await ListAsync(Marina))!.Single(person => person.Id == _lucas).CanBeTechLead.Should().BeFalse();
    }

    [Fact(DisplayName = "Líder técnico que perde o papel no sistema")]
    public async Task TechLead_WhoLosesTheSystemRole_StaysTheTechLead()
    {
        _database.People.ChangeRole(_rafael, SystemRole.User);

        (await RolesAsync())[_rafael].Should().Be(ProjectRole.TechLead);
        (await ChangeRoleAsync(Marina, _rafael, ProjectRole.TechLead)).Should().Be(MemberOutcome.Done);
        (await ChangeRoleAsync(Marina, _rafael, ProjectRole.Developer)).Should().Be(MemberOutcome.Done);
        (await ChangeRoleAsync(Marina, _rafael, ProjectRole.TechLead)).Should().Be(MemberOutcome.InvalidRole);
    }

    [Fact(DisplayName = "Incluir quem nunca entrou")]
    public async Task PersonOfTheDirectory_BecomesAUser_AndJoins()
    {
        var carla = _database.People.AddToDirectory("Carla Mendes");

        var added = await AddDirectoryAsync(Marina, carla, ProjectRole.Contributor);

        var user = _database.People.UserOf(carla);
        added.Should().Be(new MemberAdded(MemberOutcome.Done, user));
        var member = (await ListAsync(Marina))!.Single(person => person.Id == user);
        (member.Name, member.Role, member.LastSignInAt).Should().Be(("Carla Mendes", ProjectRole.Contributor, (DateTime?)null));
    }

    [Fact(DisplayName = "Pessoa fora do diretório ou inativa")]
    public async Task UnknownOrInactivePerson_IsRefused()
    {
        (await AddDirectoryAsync(Marina, "local:joao.silva@empresa.example", ProjectRole.Viewer))
            .Outcome.Should().Be(MemberOutcome.InvalidPerson);
        (await AddAsync(Marina, _livia, ProjectRole.Viewer)).Outcome.Should().Be(MemberOutcome.InvalidPerson);
        (await AddAsync(Marina, Guid.CreateVersion7(), ProjectRole.Viewer)).Outcome.Should().Be(MemberOutcome.InvalidPerson);
        (await AddMemberAsync(Marina, new NewProjectMember(null, null, ProjectRole.Viewer)))
            .Outcome.Should().Be(MemberOutcome.InvalidPerson);
        (await AddMemberAsync(Marina, new NewProjectMember(_diego, "local:diego.torres@empresa.example", ProjectRole.Viewer)))
            .Outcome.Should().Be(MemberOutcome.InvalidPerson);
        (await AddAsync(Marina, _diego, (ProjectRole)99)).Outcome.Should().Be(MemberOutcome.InvalidRole);
    }

    [Fact(DisplayName = "Pessoa removida")]
    public async Task RemovedPerson_StopsSeeingTheProject()
    {
        await AddAsync(Marina, _diego, ProjectRole.Developer);
        var diego = new ProjectViewer(_diego, false);
        (await ListAsync(diego)).Should().NotBeNull();

        await RemoveAsync(Marina, _diego);

        (await ListAsync(diego)).Should().BeNull();
        (await _database.RunAsync<GetProject, ProjectDetails?>(get => get.ExecuteAsync(_project, diego, Cancellation)))
            .Should().BeNull();
    }

    [Fact(DisplayName = "Pessoas de um projeto encerrado")]
    public async Task ClosedProject_KeepsItsPeople()
    {
        await AddAsync(Marina, _diego, ProjectRole.Developer);
        await _database.RunAsync<ChangeProjectStatus, ProjectStatusOutcome>(async change =>
            (await change.ExecuteAsync(_project, ProjectStatus.Completed, Marina, Cancellation)).Outcome);

        (await AddAsync(Marina, _lucas, ProjectRole.Viewer)).Outcome.Should().Be(MemberOutcome.Closed);
        (await ChangeRoleAsync(Marina, _diego, ProjectRole.Viewer)).Should().Be(MemberOutcome.Closed);
        (await RemoveAsync(Marina, _diego)).Should().Be(MemberOutcome.Closed);
    }

    [Fact(DisplayName = "Líder técnico do diretório na criação")]
    public async Task TechLeadOfTheDirectory_IsRefused_OnCreation()
    {
        var carla = _database.People.AddToDirectory("Carla Mendes");

        var created = await CreateAsync(new NewProject("TRIB-RS", "Revisão", null, null, null, null, carla));

        created.Invalid.Should().Equal(ProjectField.TechLead);
        _database.People.UserOf(carla).Should().BeNull();
    }

    [Fact(DisplayName = "Líder técnico inválido do diretório")]
    public async Task TechLeadOfTheDirectory_Refused_CreatesNobody()
    {
        var carla = _database.People.AddToDirectory("Carla Mendes");

        var both = await CreateAsync(new NewProject("TRIB-RS", "Revisão", null, null, null, _rafael, carla));
        var unknown = await CreateAsync(new NewProject("TRIB-RS", "Revisão", null, null, null, null, "local:joao@empresa.example"));
        var codeInUse = await CreateAsync(new NewProject("ERP-FIN", "Outro", null, null, null, null, carla));

        both.Invalid.Should().Equal(ProjectField.TechLead);
        unknown.Invalid.Should().Equal(ProjectField.TechLead);
        codeInUse.Invalid.Should().Equal(ProjectField.TechLead);
        _database.People.UserOf(carla).Should().BeNull();
    }

    private ProjectViewer Beatriz => new(_beatriz, IsAdministrator: false);

    private Task<ProjectSaveResult> CreateAsync(NewProject form) =>
        _database.RunAsync<CreateProject, ProjectSaveResult>(create => create.ExecuteAsync(form, _marina, Cancellation));

    private Task<MemberAdded> AddAsync(ProjectViewer viewer, Guid userId, ProjectRole role) =>
        AddMemberAsync(viewer, new NewProjectMember(userId, null, role));

    private Task<MemberAdded> AddDirectoryAsync(ProjectViewer viewer, string directoryId, ProjectRole role) =>
        AddMemberAsync(viewer, new NewProjectMember(null, directoryId, role));

    private Task<MemberAdded> AddMemberAsync(ProjectViewer viewer, NewProjectMember form) =>
        _database.RunAsync<AddProjectMember, MemberAdded>(add => add.ExecuteAsync(_project, form, viewer, Cancellation));

    private Task<MemberOutcome> ChangeRoleAsync(ProjectViewer viewer, Guid userId, ProjectRole role) =>
        _database.RunAsync<ChangeProjectMemberRole, MemberOutcome>(change =>
            change.ExecuteAsync(_project, userId, role, viewer, Cancellation));

    private Task<MemberOutcome> RemoveAsync(ProjectViewer viewer, Guid userId) =>
        _database.RunAsync<RemoveProjectMember, MemberOutcome>(remove =>
            remove.ExecuteAsync(_project, userId, viewer, Cancellation));

    private Task<IReadOnlyList<ProjectMemberItem>?> ListAsync(ProjectViewer viewer) =>
        _database.RunAsync<ListProjectMembers, IReadOnlyList<ProjectMemberItem>?>(list =>
            list.ExecuteAsync(_project, viewer, Cancellation));

    private async Task<Dictionary<Guid, ProjectRole>> RolesAsync() =>
        (await ListAsync(Paulo))!.ToDictionary(member => member.Id, member => member.Role);
}
