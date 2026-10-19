using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: membros-do-projeto (API side): "Quem gerencia as pessoas do projeto", "Pessoas do projeto", "Um papel por
// pessoa", "Último gerente", "Gerente com o papel no sistema", "Incluir pessoa pelo diretório", "Quem sai perde o
// acesso", "Pessoas de um projeto encerrado" and "Mudanças das pessoas na auditoria"; usuarios-e-papeis: "Usuário
// criado pelo diretório"; projetos: "Gerente e líder técnico na criação".
public sealed class ProjectMemberTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>
    /// The project of the matrix: Marina is the manager, Rafael the tech lead, Lucas a developer and Carla in it with the
    /// role of the row; Diego (a user) and Beatriz (a Project manager) are outside it.
    /// </summary>
    private sealed record Scene(
        SessionApp App, HttpClient Admin, Guid ProjectId, Guid Marina, Guid Rafael, Guid Lucas, Guid Carla, Guid Diego, Guid Beatriz);

    /// <summary>The expected status of each action on the people (ADR-0042, ADR-0043), for one person.</summary>
    private sealed record Row(HttpStatusCode List, HttpStatusCode Add, HttpStatusCode ChangeRole, HttpStatusCode Remove);

    private static readonly Row Allowed = new(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.NoContent);

    [Fact(DisplayName = "Gerente inclui, troca o papel e remove")]
    public Task Manager_ManagesThePeople() => AssertRowAsync(scene => SignInAsync(scene, scene.Marina), Allowed, (true, true));

    [Fact(DisplayName = "Líder técnico inclui um desenvolvedor")]
    public Task TechLead_ManagesThePeople() => AssertRowAsync(scene => SignInAsync(scene, scene.Rafael), Allowed, (true, false));

    [Fact(DisplayName = "Administrador fora do projeto")]
    public Task Administrator_ManagesThePeople() => AssertRowAsync(scene => Task.FromResult(scene.Admin), Allowed, (true, true));

    [Theory(DisplayName = "Colaborador, desenvolvedor e leitor não mexem nas pessoas")]
    [InlineData("Contributor")]
    [InlineData("Developer")]
    [InlineData("Viewer")]
    public Task OtherRoles_SeeThePeople_ButDoNotManageThem(string role) => AssertRowAsync(
        scene => SignInAsync(scene, scene.Carla),
        new Row(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden),
        (false, false),
        role);

    [Fact(DisplayName = "Pessoas de um projeto de fora")]
    public Task Outsider_FindsNoPeople() => AssertRowAsync(
        scene => SignInAsync(scene, scene.Beatriz),
        new Row(HttpStatusCode.NotFound, HttpStatusCode.NotFound, HttpStatusCode.NotFound, HttpStatusCode.NotFound),
        permissions: null);

    [Fact(DisplayName = "Pessoas do projeto sem sessão")]
    public async Task WithoutASession_NothingIsReadNorChanged()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var anonymous = app.NewClient();

        var row = await ActAsync(anonymous, scene);

        row.Should().Be(new Row(
            HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized));
        (await RolesAsync(scene)).Should().HaveCount(4);
    }

    [Fact(DisplayName = "Pessoas do projeto pela API")]
    public async Task Members_ComeWithTheirData()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var carla = await SignInAsync(scene, scene.Carla);

        var members = await carla.GetFromJsonAsync<JsonElement>($"/api/projects/{scene.ProjectId}/members", Cancellation);

        members.EnumerateArray().Select(member => (member.GetProperty("name").GetString(), member.GetProperty("role").GetString()))
            .Should().Equal(("Carla Mendes", "Viewer"), ("Lucas Prado", "Developer"), ("Marina Costa", "Manager"), ("Rafael Souza", "TechLead"));
        var marina = members[2];
        marina.GetProperty("email").GetString().Should().Be("marina.costa@empresa.example");
        marina.GetProperty("addedAt").GetDateTime().Should().Be(app.Clock.GetUtcNow().UtcDateTime);
        marina.GetProperty("lastSignInAt").GetDateTime().Should().Be(app.Clock.GetUtcNow().UtcDateTime);
        marina.GetProperty("canBeManager").GetBoolean().Should().BeTrue();
        members[1].GetProperty("canBeManager").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "Líder técnico e os gerentes")]
    public async Task TechLead_DoesNotTouchTheManagers()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var rafael = await SignInAsync(scene, scene.Rafael);

        (await AddAsync(rafael, scene, new { userId = scene.Beatriz, directoryId = (string?)null, role = "Manager" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ChangeRoleAsync(rafael, scene, scene.Lucas, "Manager")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ChangeRoleAsync(rafael, scene, scene.Marina, "TechLead")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await RemoveAsync(rafael, scene, scene.Marina)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await RolesAsync(scene))[scene.Marina].Should().Be("Manager");
        (await RolesAsync(scene))[scene.Lucas].Should().Be("Developer");
    }

    [Fact(DisplayName = "Gerente sem o papel no sistema")]
    public async Task User_IsNoManager()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await SignInAsync(scene, scene.Marina);

        var include = await AddAsync(marina, scene, new { userId = scene.Diego, directoryId = (string?)null, role = "Manager" });
        var fromDirectory = await AddAsync(
            marina, scene, new { userId = (Guid?)null, directoryId = "local:diego.torres@empresa.example", role = "Manager" });
        var promote = await ChangeRoleAsync(marina, scene, scene.Lucas, "Manager");

        foreach (var response in new[] { include, fromDirectory, promote })
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ErrorFieldsAsync(response)).Should().Equal("role");
        }

        (await ChangeRoleAsync(marina, scene, scene.Rafael, "Developer")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await AddAsync(marina, scene, new { userId = scene.Beatriz, directoryId = (string?)null, role = "Manager" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        (await RolesAsync(scene))[scene.Beatriz].Should().Be("Manager");
    }

    [Fact(DisplayName = "Pessoa que já está no projeto")]
    public async Task PersonAlreadyInTheProject_IsAConflict()
    {
        var scene = await StartAsync();
        await using var app = scene.App;

        var response = await AddAsync(scene.Admin, scene, new { userId = scene.Lucas, directoryId = (string?)null, role = "Viewer" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:project-member-exists");
    }

    [Fact(DisplayName = "Remover e rebaixar o último gerente")]
    public async Task LastManager_IsAConflict()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await SignInAsync(scene, scene.Marina);

        var remove = await RemoveAsync(scene.Admin, scene, scene.Marina);
        var demote = await ChangeRoleAsync(marina, scene, scene.Marina, "TechLead");

        foreach (var response in new[] { remove, demote })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:project-last-manager");
        }

        (await RolesAsync(scene))[scene.Marina].Should().Be("Manager");
    }

    [Fact(DisplayName = "Pessoa fora do diretório ou inativa")]
    public async Task UnknownOrInactivePerson_IsInvalid()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        await app.DeactivateAsync(scene.Diego);

        var inactive = await AddAsync(scene.Admin, scene, new { userId = scene.Diego, directoryId = (string?)null, role = "Viewer" });
        var unknown = await AddAsync(
            scene.Admin, scene, new { userId = (Guid?)null, directoryId = "local:joao.silva@empresa.example", role = "Viewer" });

        foreach (var response in new[] { inactive, unknown })
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ErrorFieldsAsync(response)).Should().Equal("person");
        }
    }

    [Fact(DisplayName = "Incluir quem nunca entrou")]
    public async Task PersonOfTheDirectory_BecomesAUser_AndJoins()
    {
        var scene = await StartAsync(withDiego: false);
        await using var app = scene.App;
        using var marina = await SignInAsync(scene, scene.Marina);
        var found = await marina.GetFromJsonAsync<JsonElement>("/api/people?search=diego", Cancellation);
        var directoryId = found[0].GetProperty("directoryId").GetString();

        var response = await AddAsync(marina, scene, new { userId = (Guid?)null, directoryId, role = "Developer" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var diego = (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        (await RolesAsync(scene))[diego].Should().Be("Developer");
        (await app.StateOfAsync(diego)).Should().Be((SystemRole.User, true));
        var users = await scene.Admin.GetFromJsonAsync<JsonElement>("/api/users?search=diego", Cancellation);
        users.GetProperty("items")[0].GetProperty("lastSignInAt").ValueKind.Should().Be(JsonValueKind.Null);
        var again = await marina.GetFromJsonAsync<JsonElement>("/api/people?search=diego", Cancellation);
        again[0].GetProperty("id").GetGuid().Should().Be(diego);
    }

    [Fact(DisplayName = "Pessoa removida")]
    public async Task RemovedPerson_StopsSeeingTheProject()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var lucas = await SignInAsync(scene, scene.Lucas);
        (await lucas.GetAsync($"/api/projects/{scene.ProjectId}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await RemoveAsync(scene.Admin, scene, scene.Lucas)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await lucas.GetAsync($"/api/projects/{scene.ProjectId}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await lucas.GetAsync($"/api/projects/{scene.ProjectId}/members", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Incluir pessoa num projeto concluído")]
    public async Task ClosedProject_IsAConflict()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await SignInAsync(scene, scene.Marina);
        await SendAsync(marina, HttpMethod.Put, $"/api/projects/{scene.ProjectId}/status", new { status = "Completed" });

        var add = await AddAsync(marina, scene, new { userId = scene.Diego, directoryId = (string?)null, role = "Viewer" });
        var change = await ChangeRoleAsync(marina, scene, scene.Lucas, "Viewer");
        var remove = await RemoveAsync(marina, scene, scene.Lucas);
        var project = await marina.GetFromJsonAsync<JsonElement>($"/api/projects/{scene.ProjectId}", Cancellation);

        foreach (var response in new[] { add, change, remove })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:project-closed");
        }

        project.GetProperty("permissions").GetProperty("canManagePeople").GetBoolean().Should().BeFalse();
        (await RolesAsync(scene)).Should().HaveCount(4);
    }

    [Fact(DisplayName = "Papel trocado e pessoa removida na auditoria")]
    public async Task RoleChange_AndRemoval_AreAudited_WithTheProject()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await SignInAsync(scene, scene.Marina);
        app.Clock.Advance(TimeSpan.FromMinutes(1));
        await ChangeRoleAsync(marina, scene, scene.Lucas, "Contributor");
        app.Clock.Advance(TimeSpan.FromMinutes(1));
        await RemoveAsync(marina, scene, scene.Lucas);

        var entries = (await scene.Admin.GetFromJsonAsync<JsonElement>(
                "/api/audit?itemType=ProjectMember&pageSize=100", Cancellation))
            .GetProperty("items").EnumerateArray()
            .Where(entry => entry.GetProperty("itemLabel").GetString() == "Lucas Prado")
            .ToList();

        // Lucas came into the scene straight in the database, so his inclusion has no record.
        entries.Select(entry => entry.GetProperty("action").GetString()).Should().Equal("Deleted", "Updated");
        var changed = entries[1];
        changed.GetProperty("actor").GetProperty("name").GetString().Should().Be("Marina Costa");
        Values(changed, "Role").Should().Be(("Developer", "Contributor"));
        Values(changed, "ProjectId").Should().Be(("Implantação do ERP financeiro", "Implantação do ERP financeiro"));
        Values(entries[0], "ProjectId").Should().Be(("Implantação do ERP financeiro", null));
        Values(entries[0], "Role").Should().Be(("Contributor", null));
    }

    [Fact(DisplayName = "Usuário criado pelo diretório na auditoria")]
    public async Task UserFromTheDirectory_IsAudited_ByWhoIncluded()
    {
        var scene = await StartAsync(withDiego: false);
        await using var app = scene.App;
        using var marina = await SignInAsync(scene, scene.Marina);

        await AddAsync(marina, scene, new { userId = (Guid?)null, directoryId = "local:diego.torres@empresa.example", role = "Viewer" });

        var entries = (await scene.Admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation))
            .GetProperty("items").EnumerateArray()
            .Where(entry => entry.GetProperty("itemLabel").GetString() == "Diego Torres")
            .Select(entry => (
                entry.GetProperty("itemType").GetString(),
                entry.GetProperty("action").GetString(),
                entry.GetProperty("actor").GetProperty("name").GetString()))
            .ToList();

        // Both come in the same save time; their order in the list is not part of the rule.
        entries.Should().BeEquivalentTo([("ProjectMember", "Created", "Marina Costa"), ("User", "Created", "Marina Costa")]);
    }

    [Fact(DisplayName = "Líder técnico do diretório na criação")]
    public async Task TechLeadOfTheDirectory_OnCreation()
    {
        var scene = await StartAsync(withDiego: false);
        await using var app = scene.App;
        using var marina = await SignInAsync(scene, scene.Marina);

        var created = await SendAsync(marina, HttpMethod.Post, "/api/projects", new
        {
            code = "TRIB-RS",
            name = "Revisão tributária da filial RS",
            description = (string?)null,
            clientId = (Guid?)null,
            managerId = (Guid?)null,
            techLeadId = (Guid?)null,
            techLeadDirectoryId = "local:diego.torres@empresa.example",
        });

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        var project = await marina.GetFromJsonAsync<JsonElement>($"/api/projects/{id}", Cancellation);
        project.GetProperty("members").EnumerateArray()
            .Select(member => (member.GetProperty("name").GetString(), member.GetProperty("role").GetString()))
            .Should().Equal(("Marina Costa", "Manager"), ("Diego Torres", "TechLead"));
    }

    private async Task AssertRowAsync(
        Func<Scene, Task<HttpClient>> clientOf, Row expected, (bool People, bool Managers)? permissions, string carlasRole = "Viewer")
    {
        var scene = await StartAsync(carlasRole: carlasRole);
        await using var app = scene.App;
        var client = await clientOf(scene);

        var project = await client.GetAsync($"/api/projects/{scene.ProjectId}", Cancellation);
        var row = await ActAsync(client, scene);

        row.Should().Be(expected);
        if (permissions is { } expectedPermissions)
        {
            var granted = (await project.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("permissions");
            (granted.GetProperty("canManagePeople").GetBoolean(), granted.GetProperty("canManageManagers").GetBoolean())
                .Should().Be(expectedPermissions);
        }

        // A refused action changes nothing.
        var roles = await RolesAsync(scene);
        roles.ContainsKey(scene.Diego).Should().Be(expected.Add == HttpStatusCode.Created);
        roles.ContainsKey(scene.Lucas).Should().Be(expected.Remove != HttpStatusCode.NoContent);
        if (client != scene.Admin)
        {
            client.Dispose();
        }
    }

    /// <summary>Lists the people, includes Diego as a developer, makes Lucas a contributor and removes him.</summary>
    private static async Task<Row> ActAsync(HttpClient client, Scene scene)
    {
        var list = await client.GetAsync($"/api/projects/{scene.ProjectId}/members", Cancellation);
        var add = await AddAsync(client, scene, new { userId = scene.Diego, directoryId = (string?)null, role = "Developer" });
        var change = await ChangeRoleAsync(client, scene, scene.Lucas, "Contributor");
        var remove = await RemoveAsync(client, scene, scene.Lucas);
        return new Row(list.StatusCode, add.StatusCode, change.StatusCode, remove.StatusCode);
    }

    private async Task<Scene> StartAsync(string carlasRole = "Viewer", bool withDiego = true)
    {
        var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        var rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.User);
        var lucas = await app.CreateUserAsync("Lucas Prado", SystemRole.User);
        var carla = await app.CreateUserAsync("Carla Mendes", SystemRole.User);
        var diego = withDiego ? await app.CreateUserAsync("Diego Torres", SystemRole.User) : Guid.Empty;
        var beatriz = await app.CreateUserAsync("Beatriz Lima", SystemRole.ProjectManager);

        var created = await SendAsync(admin, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-FIN",
            name = "Implantação do ERP financeiro",
            description = (string?)null,
            clientId = (Guid?)null,
            managerId = marina,
            techLeadId = rafael,
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var projectId = (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        await app.AddProjectMemberAsync(projectId, lucas, "Developer");
        await app.AddProjectMemberAsync(projectId, carla, carlasRole);
        return new Scene(app, admin, projectId, marina, rafael, lucas, carla, diego, beatriz);
    }

    private static Task<HttpClient> SignInAsync(Scene scene, Guid userId) => scene.App.SignInAsync(userId);

    private static async Task<Dictionary<Guid, string?>> RolesAsync(Scene scene) =>
        (await scene.Admin.GetFromJsonAsync<JsonElement>($"/api/projects/{scene.ProjectId}/members", Cancellation))
            .EnumerateArray()
            .ToDictionary(member => member.GetProperty("id").GetGuid(), member => member.GetProperty("role").GetString());

    private static Task<HttpResponseMessage> AddAsync(HttpClient client, Scene scene, object body) =>
        SendAsync(client, HttpMethod.Post, $"/api/projects/{scene.ProjectId}/members", body);

    private static Task<HttpResponseMessage> ChangeRoleAsync(HttpClient client, Scene scene, Guid userId, string role) =>
        SendAsync(client, HttpMethod.Put, $"/api/projects/{scene.ProjectId}/members/{userId}/role", new { role });

    private static Task<HttpResponseMessage> RemoveAsync(HttpClient client, Scene scene, Guid userId) =>
        SendAsync(client, HttpMethod.Delete, $"/api/projects/{scene.ProjectId}/members/{userId}");

    private static async Task<string?> ProblemTypeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString();

    private static async Task<List<string>> ErrorFieldsAsync(HttpResponseMessage response) =>
        [.. (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation))
            .GetProperty("errors").EnumerateObject().Select(field => field.Name)];

    private static (string? Before, string? After) Values(JsonElement entry, string name)
    {
        var field = entry.GetProperty("changes").EnumerateArray().Single(change => change.GetProperty("field").GetString() == name);
        return (field.GetProperty("before").GetString(), field.GetProperty("after").GetString());
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF", "1");
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request, Cancellation);
    }
}
