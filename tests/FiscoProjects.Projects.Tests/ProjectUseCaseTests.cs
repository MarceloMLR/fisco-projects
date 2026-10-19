using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Projects.Tests;

// Spec: projetos (module side): "Quem cria e vê projetos", "Quem altera o projeto", "Dados do projeto", "Código do
// projeto", "Gerente e líder técnico na criação", "Status do projeto" and "Lista de projetos"; clientes: "Situação e
// exclusão do cliente".
public sealed class ProjectUseCaseTests(SqlServerFixture sqlServer) : IAsyncLifetime
{
    private ProjectsDatabase _database = null!;
    private Guid _paulo;
    private Guid _marina;
    private Guid _beatriz;
    private Guid _rafael;
    private Guid _diego;

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private ProjectViewer Paulo => new(_paulo, IsAdministrator: true);

    private ProjectViewer Marina => new(_marina, IsAdministrator: false);

    public async ValueTask InitializeAsync()
    {
        _database = await ProjectsDatabase.CreateAsync(sqlServer);
        _paulo = _database.People.Add("Paulo Nogueira", SystemRole.Administrator);
        _marina = _database.People.Add("Marina Costa", SystemRole.ProjectManager);
        _beatriz = _database.People.Add("Beatriz Lima", SystemRole.ProjectManager);
        _rafael = _database.People.Add("Rafael Souza", SystemRole.User);
        _diego = _database.People.Add("Diego Torres", SystemRole.User, isActive: false);
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact(DisplayName = "Criar projeto")]
    public async Task Project_IsCreated_InPlanning_WithTheCodeInCapitals()
    {
        var alfa = await AddClientAsync("Alfa Distribuidora");

        var created = await CreateAsync(_marina, new NewProject(
            "erp-fin", "Implantação do ERP financeiro", " Com o financeiro da matriz ", alfa, null, null));

        created.Outcome.Should().Be(ProjectSaveOutcome.Saved);
        var details = await GetAsync(created.Id!.Value, Marina);
        details!.Project.Should().BeEquivalentTo(new
        {
            Code = "ERP-FIN",
            Name = "Implantação do ERP financeiro",
            Description = "Com o financeiro da matriz",
            ClientId = (Guid?)alfa,
            Status = ProjectStatus.Planning,
        });
        details.Client!.Name.Should().Be("Alfa Distribuidora");
    }

    [Fact(DisplayName = "Quem cria é o gerente")]
    public async Task Creator_IsTheManager_ByDefault()
    {
        var created = await CreateAsync(_marina, new NewProject("ERP-FIN", "ERP", null, null, null, null));

        var details = await GetAsync(created.Id!.Value, Marina);

        details!.Members.Should().Equal(new ProjectPerson(_marina, "Marina Costa", ProjectRole.Manager));
        details.Permissions.Should().Be(new ProjectPermissions(true, true, true, true, true, true));
    }

    [Fact(DisplayName = "Projeto para outro gerente")]
    public async Task ProjectForAnotherManager_LeavesTheCreatorOut()
    {
        var created = await CreateAsync(_paulo, new NewProject("ERP-FIN", "ERP", null, null, _marina, null));

        var details = await GetAsync(created.Id!.Value, Paulo);

        details!.Members.Should().Equal(new ProjectPerson(_marina, "Marina Costa", ProjectRole.Manager));
        (await ListAsync(Paulo, mine: true)).Total.Should().Be(0);
        (await ListAsync(Paulo)).Total.Should().Be(1);
    }

    [Fact(DisplayName = "Gerente sem o papel no sistema")]
    public async Task ManagerWithoutTheSystemRole_IsRefused()
    {
        foreach (var manager in new[] { _rafael, _diego, Guid.CreateVersion7() })
        {
            var created = await CreateAsync(_paulo, new NewProject("ERP-FIN", "ERP", null, null, manager, null));

            created.Invalid.Should().Equal(ProjectField.Manager);
        }

        (await ListAsync(Paulo)).Total.Should().Be(0);
    }

    [Fact(DisplayName = "Líder técnico na criação")]
    public async Task TechLead_JoinsTheProject()
    {
        var created = await CreateAsync(_marina, new NewProject("ERP-FIN", "ERP", null, null, null, _rafael));

        var details = await GetAsync(created.Id!.Value, new ProjectViewer(_rafael, false));

        details!.Members.Should().Equal(
            new ProjectPerson(_marina, "Marina Costa", ProjectRole.Manager),
            new ProjectPerson(_rafael, "Rafael Souza", ProjectRole.TechLead));
        details.Permissions.Should().Be(new ProjectPermissions(true, true, false, true, false, false));
    }

    [Fact(DisplayName = "Líder técnico inválido")]
    public async Task TechLead_IsAnotherActivePerson()
    {
        var himself = await CreateAsync(_marina, new NewProject("ERP-FIN", "ERP", null, null, null, _marina));
        var inactive = await CreateAsync(_marina, new NewProject("ERP-FIN", "ERP", null, null, null, _diego));

        himself.Invalid.Should().Equal(ProjectField.TechLead);
        inactive.Invalid.Should().Equal(ProjectField.TechLead);
    }

    [Fact(DisplayName = "Projeto inválido")]
    public async Task InvalidProject_IsRefused_FieldByField()
    {
        var inactive = await AddClientAsync("Épsilon Logística");
        await _database.RunAsync<DeactivateClient, bool>(use => use.ExecuteAsync(inactive, Cancellation));

        var refused = await CreateAsync(_marina, new NewProject("1-ERP", " ", new string('a', 4001), inactive, null, null));
        var missingClient = await CreateAsync(_marina, new NewProject("ERP", "ERP", null, Guid.CreateVersion7(), null, null));

        refused.Invalid.Should().Equal(ProjectField.Code, ProjectField.Name, ProjectField.Description, ProjectField.Client);
        missingClient.Invalid.Should().Equal(ProjectField.Client);
    }

    [Fact(DisplayName = "Código repetido")]
    public async Task RepeatedCode_IsRefused_EvenOfAClosedProject()
    {
        var erp = await CreateAsync(_marina, new NewProject("ERP-FIN", "Implantação do ERP", null, null, null, null));
        await ChangeStatusAsync(erp.Id!.Value, ProjectStatus.Cancelled, Marina);
        var sped = await CreateAsync(_marina, new NewProject("SPED-27", "SPED", null, null, null, null));

        var created = await CreateAsync(_marina, new NewProject("erp-fin", "Outro", null, null, null, null));
        var changed = await ChangeAsync(sped.Id!.Value, new ProjectChanges("Erp-Fin", "SPED", null, null), Marina);

        created.Should().BeEquivalentTo(ProjectSaveResult.CodeInUse("Implantação do ERP"));
        changed.Should().BeEquivalentTo(ProjectSaveResult.CodeInUse("Implantação do ERP"));
    }

    [Fact(DisplayName = "Projeto de outras pessoas")]
    public async Task ProjectOfOthers_DoesNotExist_ForTheirViewers()
    {
        var created = await CreateAsync(_beatriz, new NewProject("FISC-SP", "Revisão fiscal", null, null, null, null));

        (await GetAsync(created.Id!.Value, Marina)).Should().BeNull();
        (await ListAsync(Marina)).Total.Should().Be(0);
        (await ChangeAsync(created.Id!.Value, new ProjectChanges("FISC-SP", "Outro", null, null), Marina))
            .Outcome.Should().Be(ProjectSaveOutcome.NotFound);
        (await ChangeStatusAsync(created.Id!.Value, ProjectStatus.Paused, Marina)).Should().Be(ProjectStatusOutcome.NotFound);
    }

    [Theory(DisplayName = "Colaborador, desenvolvedor e leitor não alteram o projeto")]
    [InlineData(ProjectRole.Contributor)]
    [InlineData(ProjectRole.Developer)]
    [InlineData(ProjectRole.Viewer)]
    public async Task OtherRoles_SeeTheProject_ButDoNotChangeIt(ProjectRole role)
    {
        var created = await CreateAsync(_marina, new NewProject("ERP-FIN", "ERP", null, null, null, null));
        await AddMemberAsync(created.Id!.Value, _rafael, role);
        var rafael = new ProjectViewer(_rafael, false);

        (await GetAsync(created.Id!.Value, rafael))!.Permissions.Should().Be(new ProjectPermissions(true, false, false, false, false, false));
        (await ChangeAsync(created.Id!.Value, new ProjectChanges("ERP-FIN", "Outro", null, null), rafael))
            .Outcome.Should().Be(ProjectSaveOutcome.Forbidden);
        (await ChangeStatusAsync(created.Id!.Value, ProjectStatus.Paused, rafael)).Should().Be(ProjectStatusOutcome.Forbidden);
        (await GetAsync(created.Id!.Value, Marina))!.Project.Should().BeEquivalentTo(new { Name = "ERP", Status = ProjectStatus.Planning });
    }

    [Fact(DisplayName = "Líder técnico altera os dados")]
    public async Task TechLead_ChangesTheData_ButNotTheStatus()
    {
        var created = await CreateAsync(_marina, new NewProject("ERP-FIN", "ERP", null, null, null, _rafael));
        var rafael = new ProjectViewer(_rafael, false);

        var changed = await ChangeAsync(created.Id!.Value, new ProjectChanges("erp-financeiro", " ERP financeiro ", "Fase 2", null), rafael);
        var status = await ChangeStatusAsync(created.Id!.Value, ProjectStatus.Paused, rafael);

        changed.Outcome.Should().Be(ProjectSaveOutcome.Saved);
        status.Should().Be(ProjectStatusOutcome.Forbidden);
        (await GetAsync(created.Id!.Value, rafael))!.Project.Should().BeEquivalentTo(new
        {
            Code = "ERP-FINANCEIRO",
            Name = "ERP financeiro",
            Description = "Fase 2",
            Status = ProjectStatus.Planning,
        });
    }

    [Fact(DisplayName = "Administrador fora do projeto")]
    public async Task Administrator_ChangesAProjectHeIsNotIn()
    {
        var created = await CreateAsync(_marina, new NewProject("ERP-FIN", "ERP", null, null, null, null));

        (await ChangeAsync(created.Id!.Value, new ProjectChanges("ERP-FIN", "ERP 2", null, null), Paulo))
            .Outcome.Should().Be(ProjectSaveOutcome.Saved);
        (await ChangeStatusAsync(created.Id!.Value, ProjectStatus.InProgress, Paulo)).Should().Be(ProjectStatusOutcome.Saved);
    }

    [Fact(DisplayName = "Projeto encerrado somente leitura")]
    public async Task ClosedProject_IsReadOnly_UntilReopened()
    {
        var created = await CreateAsync(_marina, new NewProject("ERP-FIN", "ERP", null, null, null, _rafael));
        await ChangeStatusAsync(created.Id!.Value, ProjectStatus.Completed, Marina);

        var closed = await ChangeAsync(created.Id!.Value, new ProjectChanges("ERP-FIN", "Outro", null, null), Marina);
        await ChangeStatusAsync(created.Id!.Value, ProjectStatus.InProgress, Marina);
        var reopened = await ChangeAsync(created.Id!.Value, new ProjectChanges("ERP-FIN", "Outro", null, null), Marina);

        closed.Outcome.Should().Be(ProjectSaveOutcome.Closed);
        reopened.Outcome.Should().Be(ProjectSaveOutcome.Saved);
        (await GetAsync(created.Id!.Value, Marina))!.Project.Status.Should().Be(ProjectStatus.InProgress);
    }

    [Fact(DisplayName = "Cliente inativo no projeto")]
    public async Task InactiveClient_StaysWhileNotChanged()
    {
        var alfa = await AddClientAsync("Alfa Distribuidora");
        var beta = await AddClientAsync("Beta Alimentos");
        var created = await CreateAsync(_marina, new NewProject("ERP-FIN", "ERP", null, alfa, null, null));
        await _database.RunAsync<DeactivateClient, bool>(use => use.ExecuteAsync(alfa, Cancellation));
        await _database.RunAsync<DeactivateClient, bool>(use => use.ExecuteAsync(beta, Cancellation));

        var kept = await ChangeAsync(created.Id!.Value, new ProjectChanges("ERP-FIN", "ERP 2", null, alfa), Marina);
        var changed = await ChangeAsync(created.Id!.Value, new ProjectChanges("ERP-FIN", "ERP 2", null, beta), Marina);
        var removed = await ChangeAsync(created.Id!.Value, new ProjectChanges("ERP-FIN", "ERP 2", null, null), Marina);

        kept.Outcome.Should().Be(ProjectSaveOutcome.Saved);
        changed.Invalid.Should().Equal(ProjectField.Client);
        removed.Outcome.Should().Be(ProjectSaveOutcome.Saved);
    }

    [Fact(DisplayName = "Projetos na lista")]
    public async Task List_ShowsTheActiveProjects_InAlphabeticalOrder()
    {
        var alfa = await AddClientAsync("Alfa Distribuidora");
        await CreateAsync(_marina, new NewProject("TRIB-RS", "Revisão tributária da filial RS", null, null, null, null));
        await CreateAsync(_marina, new NewProject("ERP-FIN", "Implantação do ERP financeiro", null, alfa, null, null));
        var wms = await CreateAsync(_marina, new NewProject("WMS-ALFA", "Implantação do WMS", null, alfa, null, null));
        await ChangeStatusAsync(wms.Id!.Value, ProjectStatus.Completed, Marina);

        var active = await ListAsync(Marina);

        active.Total.Should().Be(2);
        active.Items.Select(item => item.Code).Should().Equal("ERP-FIN", "TRIB-RS");
        active.Items[0].Client!.Name.Should().Be("Alfa Distribuidora");
        active.Items[0].Managers.Should().Equal(new ProjectPerson(_marina, "Marina Costa", ProjectRole.Manager));
        active.Items[1].Client.Should().BeNull();
    }

    [Fact(DisplayName = "Projetos encerrados pelo filtro")]
    public async Task ClosedProjects_ComeWithTheFilter()
    {
        await CreateAsync(_marina, new NewProject("ERP-FIN", "ERP", null, null, null, null));
        var wms = await CreateAsync(_marina, new NewProject("WMS-ALFA", "WMS", null, null, null, null));
        var conc = await CreateAsync(_marina, new NewProject("CONC-BAN", "Conciliação", null, null, null, null));
        await ChangeStatusAsync(wms.Id!.Value, ProjectStatus.Completed, Marina);
        await ChangeStatusAsync(conc.Id!.Value, ProjectStatus.Cancelled, Marina);

        (await ListAsync(Marina, situation: ProjectSituation.Closed)).Items.Select(item => item.Code)
            .Should().Equal("CONC-BAN", "WMS-ALFA");
        (await ListAsync(Marina, situation: ProjectSituation.All)).Total.Should().Be(3);
    }

    [Fact(DisplayName = "Buscar e filtrar projetos")]
    public async Task Search_AndClient_FilterTheList()
    {
        var alfa = await AddClientAsync("Alfa Distribuidora");
        await CreateAsync(_marina, new NewProject("ERP-FIN", "Implantação do ERP financeiro", null, null, null, null));
        await CreateAsync(_marina, new NewProject("SPED-27", "Adequação do SPED", null, alfa, null, null));
        await CreateAsync(_marina, new NewProject("FISC-SP", "Revisão fiscal", null, null, null, null));

        (await ListAsync(Marina, search: "erp")).Items.Select(item => item.Code).Should().Equal("ERP-FIN");
        (await ListAsync(Marina, search: "adequacao")).Items.Select(item => item.Code).Should().Equal("SPED-27");
        (await ListAsync(Marina, search: "sp")).Items.Select(item => item.Code).Should().Equal("SPED-27", "FISC-SP");
        (await ListAsync(Marina, clientId: alfa)).Items.Select(item => item.Code).Should().Equal("SPED-27");
    }

    [Fact(DisplayName = "Cliente com projetos")]
    public async Task ClientWithProjects_IsNotRemoved()
    {
        var alfa = await AddClientAsync("Alfa Distribuidora");
        var created = await CreateAsync(_marina, new NewProject("ERP-FIN", "ERP", null, alfa, null, null));
        await ChangeStatusAsync(created.Id!.Value, ProjectStatus.Cancelled, Marina);

        var removal = await _database.RunAsync<RemoveClient, ClientRemoval>(use => use.ExecuteAsync(alfa, Cancellation));

        removal.Should().Be(ClientRemoval.HasProjects);
        (await _database.RunAsync<ListClients, ClientListPage>(list =>
            list.ExecuteAsync(new ClientListQuery(null, null, 1, 25), Cancellation))).Total.Should().Be(1);
    }

    [Fact(DisplayName = "Clientes para escolher")]
    public async Task ClientChoices_ComeInAlphabeticalOrder_ActiveAndInactive()
    {
        var beta = await AddClientAsync("Beta Alimentos");
        var alfa = await AddClientAsync("Alfa Distribuidora");
        await _database.RunAsync<DeactivateClient, bool>(use => use.ExecuteAsync(beta, Cancellation));

        var choices = await _database.RunAsync<ListClientChoices, IReadOnlyList<ClientChoice>>(list => list.ExecuteAsync(Cancellation));

        choices.Should().Equal(
            new ClientChoice(alfa, "Alfa Distribuidora", true, null),
            new ClientChoice(beta, "Beta Alimentos", false, null));
    }

    private async Task<Guid> AddClientAsync(string name)
    {
        var added = await _database.RunAsync<AddClient, ClientSaveResult>(add =>
            add.ExecuteAsync(new ClientChanges(name, null, null), Cancellation));
        return added.Id!.Value;
    }

    private async Task AddMemberAsync(Guid projectId, Guid userId, ProjectRole role)
    {
        await using var db = _database.NewContext();
        db.ProjectMembers.Add(new ProjectMember(projectId, userId, role, _database.Clock.GetUtcNow().UtcDateTime));
        await db.SaveChangesAsync(Cancellation);
    }

    private Task<ProjectSaveResult> CreateAsync(Guid creator, NewProject form) =>
        _database.RunAsync<CreateProject, ProjectSaveResult>(create => create.ExecuteAsync(form, creator, Cancellation));

    private Task<ProjectDetails?> GetAsync(Guid id, ProjectViewer viewer) =>
        _database.RunAsync<GetProject, ProjectDetails?>(get => get.ExecuteAsync(id, viewer, Cancellation));

    private Task<ProjectSaveResult> ChangeAsync(Guid id, ProjectChanges changes, ProjectViewer viewer) =>
        _database.RunAsync<ChangeProject, ProjectSaveResult>(change => change.ExecuteAsync(id, changes, viewer, Cancellation));

    private Task<ProjectStatusOutcome> ChangeStatusAsync(Guid id, ProjectStatus status, ProjectViewer viewer) =>
        _database.RunAsync<ChangeProjectStatus, ProjectStatusOutcome>(change => change.ExecuteAsync(id, status, viewer, Cancellation));

    private Task<ProjectListPage> ListAsync(
        ProjectViewer viewer,
        string? search = null,
        Guid? clientId = null,
        ProjectSituation situation = ProjectSituation.Active,
        bool mine = false) =>
        _database.RunAsync<ListProjects, ProjectListPage>(list =>
            list.ExecuteAsync(new ProjectListQuery(search, clientId, situation, mine, 1, 25), viewer, Cancellation));
}
