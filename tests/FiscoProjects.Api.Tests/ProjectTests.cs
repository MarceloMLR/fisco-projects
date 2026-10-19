using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: projetos, requirements "Quem cria e vê projetos", "Quem altera o projeto", "Código do projeto", "Gerente e
// líder técnico na criação", "Status do projeto" and "Mudanças dos projetos na auditoria" (API side); clientes:
// "Situação e exclusão do cliente" and "Clientes para escolher".
public sealed class ProjectTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>
    /// The project of the matrix: created by the Administrator for Marina, a Project manager, with Rafael as tech lead,
    /// and Carla in it with the role of the row; Beatriz is a Project manager outside it.
    /// </summary>
    private sealed record Scene(SessionApp App, HttpClient Admin, Guid ProjectId, Guid Marina, Guid Rafael, Guid Carla, Guid Beatriz);

    /// <summary>The expected status of each action of the matrix (ADR-0043), for one person.</summary>
    private sealed record Row(
        HttpStatusCode Create, HttpStatusCode Open, HttpStatusCode ChangeData, HttpStatusCode ChangeStatus, HttpStatusCode People);

    private static readonly Row Allowed = new(
        HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NoContent, HttpStatusCode.OK);

    [Fact(DisplayName = "Administrador cria e vê projetos")]
    public Task Administrator_CreatesAndSeesEveryProject() => AssertRowAsync(scene => Task.FromResult(scene.Admin), Allowed, listsTheProject: true);

    [Fact(DisplayName = "Gerente altera o projeto")]
    public Task Manager_ChangesTheProject() => AssertRowAsync(scene => SignInAsync(scene, scene.Marina), Allowed, listsTheProject: true);

    [Fact(DisplayName = "Líder técnico altera os dados")]
    public Task TechLead_ChangesTheData_ButNotTheStatus() => AssertRowAsync(
        scene => SignInAsync(scene, scene.Rafael),
        Allowed with { Create = HttpStatusCode.Forbidden, ChangeStatus = HttpStatusCode.Forbidden },
        listsTheProject: true);

    [Theory(DisplayName = "Colaborador, desenvolvedor e leitor não alteram o projeto")]
    [InlineData("Contributor")]
    [InlineData("Developer")]
    [InlineData("Viewer")]
    public Task OtherRoles_SeeTheProject_ButDoNotChangeIt(string role) => AssertRowAsync(
        scene => SignInAsync(scene, scene.Carla),
        new Row(HttpStatusCode.Forbidden, HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.OK),
        listsTheProject: true,
        carlasRole: role);

    [Fact(DisplayName = "Projeto de outras pessoas")]
    public Task Outsider_DoesNotSeeTheProject() => AssertRowAsync(
        scene => SignInAsync(scene, scene.Beatriz),
        new Row(HttpStatusCode.Created, HttpStatusCode.NotFound, HttpStatusCode.NotFound, HttpStatusCode.NotFound, HttpStatusCode.OK),
        listsTheProject: false);

    [Fact(DisplayName = "Projetos sem sessão")]
    public async Task WithoutASession_NothingIsReadNorChanged()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var anonymous = app.NewClient();

        (await anonymous.GetAsync("/api/projects", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostAsync(anonymous, "/api/projects", NewProject("NOVO"))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync($"/api/projects/{scene.ProjectId}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PutAsync(anonymous, $"/api/projects/{scene.ProjectId}", ProjectData("ERP-FIN", "Outro")))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PutAsync(anonymous, $"/api/projects/{scene.ProjectId}/status", new { status = "Paused" }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync("/api/people", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync("/api/clients/choices", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ProjectAsync(scene.Admin, scene.ProjectId)).GetProperty("name").GetString().Should().Be("Implantação do ERP financeiro");
    }

    [Fact(DisplayName = "Usuário não cria projetos")]
    public async Task User_DoesNotCreateProjects()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        var (rafael, _) = await app.SignedInClientAsync("Lucas Prado", SystemRole.User);
        using var _rafael = rafael;

        (await PostAsync(rafael, "/api/projects", NewProject("NOVO"))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ListAsync(scene.Admin, "?situation=All")).GetProperty("total").GetInt32().Should().Be(1);
    }

    [Fact(DisplayName = "Gerente sem o papel no sistema")]
    public async Task Creation_ChecksTheFields()
    {
        var scene = await StartAsync();
        await using var app = scene.App;

        var response = await PostAsync(scene.Admin, "/api/projects", new
        {
            code = "1-ERP",
            name = " ",
            description = new string('a', 4001),
            clientId = Guid.CreateVersion7(),
            managerId = scene.Rafael,
            techLeadId = scene.Rafael,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        problem.GetProperty("errors").EnumerateObject().Select(field => field.Name)
            .Should().BeEquivalentTo("code", "name", "description", "clientId", "managerId", "techLeadId");
    }

    [Fact(DisplayName = "Código repetido")]
    public async Task RepeatedCode_IsAConflict_WithTheNameOfTheProject()
    {
        var scene = await StartAsync();
        await using var app = scene.App;

        var response = await PostAsync(scene.Admin, "/api/projects", NewProject("erp-fin"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        problem.GetProperty("type").GetString().Should().Be("urn:fiscoprojects:problem:project-code-in-use");
        problem.GetProperty("projectName").GetString().Should().Be("Implantação do ERP financeiro");
    }

    [Fact(DisplayName = "Projeto encerrado somente leitura")]
    public async Task ClosedProject_IsAConflict_UntilReopened()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await SignInAsync(scene, scene.Marina);
        await PutAsync(marina, $"/api/projects/{scene.ProjectId}/status", new { status = "Completed" });

        var closed = await PutAsync(marina, $"/api/projects/{scene.ProjectId}", ProjectData("ERP-FIN", "Outro"));
        var project = await ProjectAsync(marina, scene.ProjectId);

        closed.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await closed.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString()
            .Should().Be("urn:fiscoprojects:problem:project-closed");
        project.GetProperty("permissions").GetProperty("canEdit").GetBoolean().Should().BeFalse();
        project.GetProperty("permissions").GetProperty("canChangeStatus").GetBoolean().Should().BeTrue();
        (await ListAsync(marina, "")).GetProperty("total").GetInt32().Should().Be(0);
        (await ListAsync(marina, "?situation=Closed")).GetProperty("total").GetInt32().Should().Be(1);

        (await PutAsync(marina, $"/api/projects/{scene.ProjectId}/status", new { status = "InProgress" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await PutAsync(marina, $"/api/projects/{scene.ProjectId}", ProjectData("ERP-FIN", "Outro")))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "Cabeçalho do projeto")]
    public async Task Project_ComesWithItsPeople_AndTheClient()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var rafael = await SignInAsync(scene, scene.Rafael);

        var project = await ProjectAsync(rafael, scene.ProjectId);

        project.GetProperty("code").GetString().Should().Be("ERP-FIN");
        project.GetProperty("status").GetString().Should().Be("Planning");
        project.GetProperty("client").GetProperty("name").GetString().Should().Be("Alfa Distribuidora");
        project.GetProperty("members").EnumerateArray()
            .Select(member => (member.GetProperty("name").GetString(), member.GetProperty("role").GetString()))
            .Should().Equal(("Marina Costa", "Manager"), ("Rafael Souza", "TechLead"), ("Carla Mendes", "Viewer"));
        var listed = (await ListAsync(rafael, "")).GetProperty("items")[0];
        listed.GetProperty("managers")[0].GetProperty("name").GetString().Should().Be("Marina Costa");
    }

    [Fact(DisplayName = "Cliente com projetos")]
    public async Task ClientWithProjects_IsAConflict()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        var clientId = (await ProjectAsync(scene.Admin, scene.ProjectId)).GetProperty("client").GetProperty("id").GetGuid();

        var response = await SendAsync(scene.Admin, HttpMethod.Delete, $"/api/clients/{clientId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString()
            .Should().Be("urn:fiscoprojects:problem:client-has-projects");
    }

    [Fact(DisplayName = "Clientes para escolher")]
    public async Task ClientChoices_OpenForAnySession_WithoutTheCnpj()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var carla = await SignInAsync(scene, scene.Carla);

        var choices = await carla.GetFromJsonAsync<JsonElement>("/api/clients/choices", Cancellation);

        var choice = choices.EnumerateArray().Should().ContainSingle().Subject;
        choice.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo("id", "name", "logoUrl", "status");
        choice.GetProperty("name").GetString().Should().Be("Alfa Distribuidora");
    }

    // Spec: usuarios-e-papeis, requirement "Pessoas da empresa para escolher". Diego Torres, Lucas Prado and Sofia
    // Martins are in the demo directory of the development settings and never signed in; Carla is in it with an
    // inactive user.
    [Fact(DisplayName = "Pessoas do sistema e do diretório")]
    public async Task People_ComeActive_WithTheDirectory_AndOnlyManagersWhenAsked()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        await app.DeactivateAsync(scene.Carla);
        using var rafael = await SignInAsync(scene, scene.Rafael);

        var everyone = await rafael.GetFromJsonAsync<JsonElement>("/api/people", Cancellation);
        var managers = await scene.Admin.GetFromJsonAsync<JsonElement>("/api/people?managers=true&search=a", Cancellation);

        Names(everyone).Should().Equal(
            "Beatriz Lima", "Diego Torres", "Lucas Prado", "Marina Costa", "Paulo Nogueira", "Rafael Souza", "Sofia Martins");
        var diego = everyone.EnumerateArray().Single(person => person.GetProperty("name").GetString() == "Diego Torres");
        diego.GetProperty("id").ValueKind.Should().Be(JsonValueKind.Null);
        diego.GetProperty("directoryId").GetString().Should().Be("local:diego.torres@empresa.example");
        diego.GetProperty("canBeManager").GetBoolean().Should().BeFalse();
        everyone.EnumerateArray().Where(person => person.GetProperty("canBeManager").GetBoolean())
            .Select(person => person.GetProperty("name").GetString())
            .Should().Equal("Beatriz Lima", "Marina Costa", "Paulo Nogueira");
        Names(managers).Should().Equal("Beatriz Lima", "Marina Costa", "Paulo Nogueira");
    }

    [Fact(DisplayName = "Projeto criado na auditoria")]
    public async Task Creation_IsAudited_WithTheManagerByName()
    {
        var scene = await StartAsync();
        await using var app = scene.App;

        var entries = (await scene.Admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation))
            .GetProperty("items").EnumerateArray().ToList();

        var project = entries.Single(entry => entry.GetProperty("itemType").GetString() == "Project");
        project.GetProperty("action").GetString().Should().Be("Created");
        project.GetProperty("itemLabel").GetString().Should().Be("Implantação do ERP financeiro");
        project.GetProperty("actor").GetProperty("name").GetString().Should().Be("Paulo Nogueira");
        Field(project, "ClientId").GetProperty("after").GetString().Should().Be("Alfa Distribuidora");
        Field(project, "ClientId").GetProperty("reference").GetString().Should().Be("Client");

        var manager = entries.Single(entry =>
            entry.GetProperty("itemType").GetString() == "ProjectMember" && entry.GetProperty("itemLabel").GetString() == "Marina Costa");
        Field(manager, "Role").GetProperty("after").GetString().Should().Be("Manager");
        Field(manager, "ProjectId").GetProperty("after").GetString().Should().Be("Implantação do ERP financeiro");
        Field(manager, "UserId").GetProperty("after").GetString().Should().Be("Marina Costa");
    }

    [Fact(DisplayName = "Status na auditoria")]
    public async Task StatusChanges_AreAudited()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await SignInAsync(scene, scene.Marina);
        app.Clock.Advance(TimeSpan.FromMinutes(1));
        await PutAsync(marina, $"/api/projects/{scene.ProjectId}/status", new { status = "Completed" });
        app.Clock.Advance(TimeSpan.FromMinutes(1));
        await PutAsync(marina, $"/api/projects/{scene.ProjectId}/status", new { status = "InProgress" });

        var entries = (await scene.Admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=Project&pageSize=100", Cancellation))
            .GetProperty("items").EnumerateArray().ToList();

        entries.Select(entry => entry.GetProperty("action").GetString()).Should().Equal("Updated", "Updated", "Created");
        (Field(entries[1], "Status").GetProperty("before").GetString(), Field(entries[1], "Status").GetProperty("after").GetString())
            .Should().Be(("Planning", "Completed"));
        (Field(entries[0], "Status").GetProperty("before").GetString(), Field(entries[0], "Status").GetProperty("after").GetString())
            .Should().Be(("Completed", "InProgress"));
    }

    private async Task AssertRowAsync(
        Func<Scene, Task<HttpClient>> clientOf, Row expected, bool listsTheProject, string carlasRole = "Viewer")
    {
        var scene = await StartAsync(carlasRole);
        await using var app = scene.App;
        var client = await clientOf(scene);

        var listed = await ListAsync(client, "");
        var open = await client.GetAsync($"/api/projects/{scene.ProjectId}", Cancellation);
        var changeData = await PutAsync(client, $"/api/projects/{scene.ProjectId}", ProjectData("ERP-FIN", "ERP financeiro"));
        var changeStatus = await PutAsync(client, $"/api/projects/{scene.ProjectId}/status", new { status = "InProgress" });
        var create = await PostAsync(client, "/api/projects", NewProject("NOVO"));
        var people = await client.GetAsync("/api/people", Cancellation);

        listed.GetProperty("items").EnumerateArray().Any(item => item.GetProperty("id").GetGuid() == scene.ProjectId)
            .Should().Be(listsTheProject);
        new Row(create.StatusCode, open.StatusCode, changeData.StatusCode, changeStatus.StatusCode, people.StatusCode)
            .Should().Be(expected);
        // A refused action changes nothing.
        var project = await ProjectAsync(scene.Admin, scene.ProjectId);
        project.GetProperty("name").GetString().Should().Be(
            expected.ChangeData == HttpStatusCode.NoContent ? "ERP financeiro" : "Implantação do ERP financeiro");
        project.GetProperty("status").GetString().Should().Be(
            expected.ChangeStatus == HttpStatusCode.NoContent ? "InProgress" : "Planning");
        if (client != scene.Admin)
        {
            client.Dispose();
        }
    }

    private async Task<Scene> StartAsync(string carlasRole = "Viewer")
    {
        var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        var rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.User);
        var carla = await app.CreateUserAsync("Carla Mendes", SystemRole.User);
        var beatriz = await app.CreateUserAsync("Beatriz Lima", SystemRole.ProjectManager);

        var client = await PostAsync(admin, "/api/clients", new { name = "Alfa Distribuidora", cnpj = (string?)null, observation = (string?)null });
        var clientId = (await client.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        var created = await PostAsync(admin, "/api/projects", new
        {
            code = "ERP-FIN",
            name = "Implantação do ERP financeiro",
            description = "Financeiro da matriz",
            clientId,
            managerId = marina,
            techLeadId = rafael,
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var projectId = (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        await app.AddProjectMemberAsync(projectId, carla, carlasRole);
        return new Scene(app, admin, projectId, marina, rafael, carla, beatriz);
    }

    private static Task<HttpClient> SignInAsync(Scene scene, Guid userId) => scene.App.SignInAsync(userId);

    private static object NewProject(string code) =>
        new { code, name = "Implantação do ERP financeiro", description = (string?)null, clientId = (Guid?)null, managerId = (Guid?)null, techLeadId = (Guid?)null };

    private static object ProjectData(string code, string name) =>
        new { code, name, description = "Financeiro da matriz", clientId = (Guid?)null };

    private static Task<JsonElement> ListAsync(HttpClient client, string query) =>
        client.GetFromJsonAsync<JsonElement>($"/api/projects{query}", Cancellation);

    private static Task<JsonElement> ProjectAsync(HttpClient client, Guid id) =>
        client.GetFromJsonAsync<JsonElement>($"/api/projects/{id}", Cancellation);

    private static List<string?> Names(JsonElement people) =>
        [.. people.EnumerateArray().Select(person => person.GetProperty("name").GetString())];

    private static JsonElement Field(JsonElement entry, string name) =>
        entry.GetProperty("changes").EnumerateArray().Single(field => field.GetProperty("field").GetString() == name);

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string path, object body) =>
        SendAsync(client, HttpMethod.Post, path, body);

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, string path, object body) =>
        SendAsync(client, HttpMethod.Put, path, body);

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
