using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: projetos, requirements "Escolher a pasta do projeto", "Lista de pastas para escolher", "Trocar a pasta do
// projeto", "Pasta anterior e auditoria da troca", "Pasta renomeada com o projeto" and "Arquivos do projeto excluído"
// (ADR-0122).
public sealed class ProjectFolderTests(SqlServerFixture sqlServer)
{
    private const string ErpFolder = "ERP-FIN - Implantação do ERP financeiro";
    private const string Management = "01 Gestão do projeto";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    [Fact(DisplayName = "Listar as pastas")]
    public async Task TheRoot_ShowsItsFolders_AndWhichMayBeChosen()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await AreaAsync(team, "Projetos");
        await team.CreateProjectAsync("SPED-27", "Entrega do SPED");
        Directory.CreateDirectory(Path.Combine(team.Files, "Clientes", "Alfa", "ERP"));
        Directory.CreateDirectory(Path.Combine(team.Files, "FiscoProjects"));
        var marina = await team.ClientOfAsync(team.Marina);

        var root = await ListAsync(marina, null);

        root.GetProperty("current").ValueKind.Should().Be(JsonValueKind.Null);
        Choices(root).Should().Equal(
            ("Clientes", null, null),
            (ErpFolder, "InUse", "ERP-FIN"),
            ("FiscoProjects", "SystemFolder", null),
            ("Projetos", "ProjectsFolder", null));
    }

    [Fact(DisplayName = "Pastas de um projeto na lista")]
    public async Task TheFolderOfTheProjects_ShowsWhoUsesEachFolder()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await AreaAsync(team, "Projetos");
        await team.CreateProjectAsync("SPED-27", "Entrega do SPED");
        var marina = await team.ClientOfAsync(team.Marina);

        var projects = await ListAsync(marina, await FolderIdAsync(marina, "Projetos"));
        var sped = await ListAsync(marina, await FolderIdAsync(marina, "Projetos", "SPED-27 - Entrega do SPED"));

        projects.GetProperty("current").GetProperty("refusal").GetString().Should().Be("ProjectsFolder");
        Choices(projects).Should().Equal(("SPED-27 - Entrega do SPED", "InUse", "SPED-27"));
        Choices(sped).Should().Equal(("Documentos", "InsideProject", "SPED-27"));
    }

    [Fact(DisplayName = "Usuário não lista as pastas")]
    public async Task WhoDoesNotCreateProjects_DoesNotListTheFolders()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);

        (await carla.GetAsync("/api/project-folders", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Criar projeto numa pasta que já existe")]
    public async Task ANewProject_UsesTheFolderChosen()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await AreaAsync(team, "Projetos");
        Directory.CreateDirectory(Path.Combine(team.Files, "Clientes", "Alfa", "ERP"));
        var marina = await team.ClientOfAsync(team.Marina);
        var folder = await FolderIdAsync(marina, "Clientes", "Alfa", "ERP");

        var created = await CreateAsync(marina, "ALFA-ERP", "ERP da Alfa", folder);

        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync(Cancellation));
        Directory.Exists(Path.Combine(team.Files, "Clientes", "Alfa", "ERP", "Documentos", Management)).Should().BeTrue();
        Directory.Exists(Path.Combine(team.Files, "Projetos")).Should().BeFalse("the system creates no folder of its own");
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        var shown = await marina.GetFromJsonAsync<JsonElement>($"/api/projects/{id}/folder", Cancellation);
        shown.GetProperty("name").GetString().Should().Be("ERP");
        shown.GetProperty("chosen").GetBoolean().Should().BeTrue();
        shown.GetProperty("canChange").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "Pasta de outro projeto")]
    public async Task TheFolderOfAnotherProject_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);

        var used = await CreateAsync(marina, "ALFA-ERP", "ERP da Alfa", await FolderIdAsync(marina, ErpFolder));
        var inside = await CreateAsync(marina, "ALFA-ERP", "ERP da Alfa", await FolderIdAsync(marina, ErpFolder, "Documentos"));

        await ShouldRefuseFolderAsync(used);
        await ShouldRefuseFolderAsync(inside);
        (await team.App.CountAsync("SELECT COUNT(*) FROM [projects].[projects]")).Should().Be(1);
    }

    [Fact(DisplayName = "Pasta que não pode ser escolhida")]
    public async Task TheFoldersOfTheSystem_AreRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await AreaAsync(team, "Clientes/Projetos");
        await team.CreateProjectAsync("SPED-27", "Entrega do SPED");
        Directory.CreateDirectory(Path.Combine(team.Files, "FiscoProjects"));
        var marina = await team.ClientOfAsync(team.Marina);

        foreach (var path in new[] { new[] { "Clientes", "Projetos" }, ["Clientes"], ["FiscoProjects"] })
        {
            await ShouldRefuseFolderAsync(await CreateAsync(marina, "ALFA-ERP", "ERP da Alfa", await FolderIdAsync(marina, path)));
        }

        await ShouldRefuseFolderAsync(await CreateAsync(marina, "ALFA-ERP", "ERP da Alfa", "Pasta/que/não/existe"));
    }

    [Fact(DisplayName = "Trocar a pasta antes dos arquivos")]
    public async Task AProjectWithoutFiles_ChangesItsFolder()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        Directory.CreateDirectory(Path.Combine(team.Files, "Clientes", "Alfa", "ERP"));
        var marina = await team.ClientOfAsync(team.Marina);

        var changed = await ChangeFolderAsync(marina, team.Erp, await FolderIdAsync(marina, "Clientes", "Alfa", "ERP"));

        changed.StatusCode.Should().Be(HttpStatusCode.NoContent, await changed.Content.ReadAsStringAsync(Cancellation));
        Directory.Exists(Path.Combine(team.Files, "Clientes", "Alfa", "ERP", "Documentos", Management)).Should().BeTrue();
        Directory.Exists(Path.Combine(team.Files, ErpFolder)).Should().BeFalse("the folder the system created was left empty");
        await UploadAsync(marina, team.Erp, Management, "Cronograma.pdf");
        File.Exists(Path.Combine(team.Files, "Clientes", "Alfa", "ERP", "Documentos", Management, "Cronograma.pdf")).Should().BeTrue();
    }

    [Fact(DisplayName = "Pasta com arquivos não muda")]
    public async Task AProjectWithFiles_KeepsItsFolder()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        Directory.CreateDirectory(Path.Combine(team.Files, "Clientes", "Alfa", "ERP"));
        var marina = await team.ClientOfAsync(team.Marina);
        await UploadAsync(marina, team.Erp, Management, "Cronograma.pdf");

        var refused = await ChangeFolderAsync(marina, team.Erp, await FolderIdAsync(marina, "Clientes", "Alfa", "ERP"));

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(refused)).Should().Be("urn:fiscoprojects:problem:project-has-files");
        File.Exists(Path.Combine(team.Files, ErpFolder, "Documentos", Management, "Cronograma.pdf")).Should().BeTrue();
        var shown = await marina.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}/folder", Cancellation);
        shown.GetProperty("hasFiles").GetBoolean().Should().BeTrue();
        shown.GetProperty("canChange").GetBoolean().Should().BeFalse();
    }

    [Theory(DisplayName = "Permissões da pasta do projeto")]
    [InlineData("Administrator", HttpStatusCode.NoContent)]
    [InlineData("Manager", HttpStatusCode.NoContent)]
    [InlineData("TechLead", HttpStatusCode.Forbidden)]
    [InlineData("Contributor", HttpStatusCode.Forbidden)]
    [InlineData("Developer", HttpStatusCode.Forbidden)]
    [InlineData("Viewer", HttpStatusCode.Forbidden)]
    [InlineData("Outside", HttpStatusCode.NotFound)]
    public async Task OnlyTheAdministratorAndTheManager_ChangeTheFolder(string role, HttpStatusCode expected)
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        Directory.CreateDirectory(Path.Combine(team.Files, "Clientes", "Alfa", "ERP"));
        var folder = await FolderIdAsync(team.Admin, "Clientes", "Alfa", "ERP");
        var client = await team.ClientOfAsync(role);

        var answer = await ChangeFolderAsync(client, team.Erp, folder);
        var permissions = await team.Admin.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}", Cancellation);

        answer.StatusCode.Should().Be(expected);
        Directory.Exists(Path.Combine(team.Files, ErpFolder)).Should().Be(expected != HttpStatusCode.NoContent);
        permissions.GetProperty("permissions").GetProperty("canChooseFolder").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "Voltar à pasta do sistema")]
    public async Task BackToTheSystemFolder_KeepsTheFolderChosen()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        Directory.CreateDirectory(Path.Combine(team.Files, "Clientes", "Alfa", "ERP"));
        var marina = await team.ClientOfAsync(team.Marina);
        await ChangeFolderAsync(marina, team.Erp, await FolderIdAsync(marina, "Clientes", "Alfa", "ERP"));

        var back = await ChangeFolderAsync(marina, team.Erp, null);

        back.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Directory.Exists(Path.Combine(team.Files, ErpFolder, "Documentos", Management)).Should().BeTrue();
        Directory.Exists(Path.Combine(team.Files, "Clientes", "Alfa", "ERP")).Should().BeTrue();
        Directory.GetFileSystemEntries(Path.Combine(team.Files, "Clientes", "Alfa", "ERP")).Should().BeEmpty();
        (await marina.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}/folder", Cancellation))
            .GetProperty("chosen").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "Troca da pasta na auditoria")]
    public async Task AChangeOfTheFolder_IsAudited()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        Directory.CreateDirectory(Path.Combine(team.Files, "Clientes", "Alfa", "ERP"));
        var marina = await team.ClientOfAsync(team.Marina);

        await ChangeFolderAsync(marina, team.Erp, await FolderIdAsync(marina, "Clientes", "Alfa", "ERP"));

        var page = await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=Project&pageSize=100", Cancellation);
        page.GetProperty("items").EnumerateArray()
            .Where(entry => entry.GetProperty("action").GetString() == "Updated")
            .SelectMany(entry => entry.GetProperty("changes").EnumerateArray())
            .Should().ContainSingle(change => change.GetProperty("field").GetString() == "StorageFolderName"
                && change.GetProperty("before").GetString() == ErpFolder
                && change.GetProperty("after").GetString() == "ERP");
    }

    [Fact(DisplayName = "Pasta escolhida não muda de nome")]
    public async Task AFolderChosen_KeepsItsName()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        Directory.CreateDirectory(Path.Combine(team.Files, "Clientes", "Alfa", "ERP"));
        var marina = await team.ClientOfAsync(team.Marina);
        await ChangeFolderAsync(marina, team.Erp, await FolderIdAsync(marina, "Clientes", "Alfa", "ERP"));

        var renamed = await SendAsync(marina, HttpMethod.Put, $"/api/projects/{team.Erp}",
            new { code = "ERP-FIN", name = "Implantação do ERP", description = (string?)null, clientId = (Guid?)null });

        renamed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Directory.Exists(Path.Combine(team.Files, "Clientes", "Alfa", "ERP", "Documentos")).Should().BeTrue();
        Directory.GetDirectories(Path.Combine(team.Files, "Clientes", "Alfa")).Select(Path.GetFileName).Should().Equal("ERP");
    }

    [Fact(DisplayName = "Pasta escolhida fica na exclusão")]
    public async Task DeletingTheProject_KeepsTheFolderChosen()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var folder = Path.Combine(team.Files, "Clientes", "Alfa", "ERP");
        Directory.CreateDirectory(folder);
        var marina = await team.ClientOfAsync(team.Marina);
        await ChangeFolderAsync(marina, team.Erp, await FolderIdAsync(marina, "Clientes", "Alfa", "ERP"));
        await UploadAsync(marina, team.Erp, Management, "Cronograma.pdf");

        var deleted = await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{team.Erp}/deletion", new { code = "ERP-FIN" });

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Directory.Exists(folder).Should().BeTrue();
        Directory.GetFileSystemEntries(folder).Should().BeEmpty("the files and the empty folders of the system left");
    }

    private static async Task AreaAsync(ProjectTeam team, string projectsFolder)
    {
        var saved = await SendAsync(team.Admin, HttpMethod.Put, "/api/installation", new
        {
            name = "FiscoProjects",
            timeZone = "America/Sao_Paulo",
            dailySummaryAt = "07:30:00",
            notificationRetentionDays = 90,
            projectsFolder,
        });
        saved.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task<JsonElement> ListAsync(HttpClient client, string? parentId)
    {
        var path = parentId is null ? "/api/project-folders" : $"/api/project-folders?parentId={Uri.EscapeDataString(parentId)}";
        return await client.GetFromJsonAsync<JsonElement>(path, Cancellation);
    }

    private static List<(string Name, string? Refusal, string? Project)> Choices(JsonElement list) =>
        [.. list.GetProperty("folders").EnumerateArray().Select(folder => (
            folder.GetProperty("name").GetString()!,
            folder.GetProperty("refusal").GetString(),
            folder.GetProperty("projectCode").GetString()))];

    /// <summary>The id of a folder, found by its names from the root, as the dialog does.</summary>
    private static async Task<string> FolderIdAsync(HttpClient client, params string[] names)
    {
        string? parent = null;
        foreach (var name in names)
        {
            var list = await ListAsync(client, parent);
            parent = list.GetProperty("folders").EnumerateArray()
                .Single(folder => folder.GetProperty("name").GetString() == name).GetProperty("id").GetString();
        }

        return parent!;
    }

    private static Task<HttpResponseMessage> CreateAsync(HttpClient client, string code, string name, string folderId) =>
        SendAsync(client, HttpMethod.Post, "/api/projects", new { code, name, folderId });

    private static Task<HttpResponseMessage> ChangeFolderAsync(HttpClient client, Guid project, string? folderId) =>
        SendAsync(client, HttpMethod.Put, $"/api/projects/{project}/folder", new { folderId });

    private static async Task ShouldRefuseFolderAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync(Cancellation));
        (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").TryGetProperty("folderId", out _)
            .Should().BeTrue();
    }

    private static async Task UploadAsync(HttpClient client, Guid project, string folderName, string name)
    {
        var folders = await client.GetFromJsonAsync<JsonElement>($"/api/projects/{project}/documents/folders", Cancellation);
        var folder = folders.GetProperty("folders").EnumerateArray()
            .Single(item => item.GetProperty("name").GetString() == folderName).GetProperty("id").GetGuid();
        using var form = new MultipartFormDataContent
        {
            { new StringContent(folder.ToString()), "folderId" },
            { new ByteArrayContent(Encoding.UTF8.GetBytes("conteúdo")), "file", name },
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/projects/{project}/documents") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        var sent = await client.SendAsync(request, Cancellation);
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
    }
}
