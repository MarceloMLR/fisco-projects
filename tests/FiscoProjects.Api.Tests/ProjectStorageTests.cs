using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace FiscoProjects.Api.Tests;

// Spec: configuracao-da-instalacao, requirement "Pasta dos projetos"; projetos, requirements "Pasta de cada projeto",
// "Pasta de antes e pasta apagada", "Pasta renomeada com o projeto" and "Arquivos do projeto excluído" (ADR-0121).
public sealed class ProjectStorageTests(SqlServerFixture sqlServer)
{
    private const string ErpFolder = "ERP-FIN - Implantação do ERP financeiro";
    private const string Management = "01 Gestão do projeto";
    private const string Requirements = "03 Requisitos e especificações";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    [Fact(DisplayName = "Pasta dos projetos padrão")]
    public async Task WithoutAFolderOfTheProjects_TheProjectsAreAtTheRoot()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        (await ProjectsFolderAsync(team.Admin)).Should().BeEmpty();
        Directory.Exists(Path.Combine(team.Files, ErpFolder, "Documentos", Management)).Should().BeTrue();
    }

    [Fact(DisplayName = "Escolher a pasta dos projetos")]
    public async Task TheFolderOfTheProjects_HoldsTheFoldersOfTheNewProjects()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        (await ChangeProjectsFolderAsync(team.Admin, " /Clientes/Projetos/ ")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await team.CreateProjectAsync("SPED-27", "Entrega do SPED");

        (await ProjectsFolderAsync(team.Admin)).Should().Be("Clientes/Projetos");
        Directory.Exists(Path.Combine(team.Files, "Clientes", "Projetos", "SPED-27 - Entrega do SPED", "Documentos", Management))
            .Should().BeTrue();
        Directory.Exists(Path.Combine(team.Files, ErpFolder)).Should().BeTrue("the project that had a folder keeps it");
    }

    [Fact(DisplayName = "Pasta dos projetos inválida")]
    public async Task AFolderTheStorageWouldChange_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        foreach (var folder in new[] { "Projetos/../Outros", "Projetos/Fin:2026", "Projetos//2026", new string('a', 401) })
        {
            var refused = await ChangeProjectsFolderAsync(team.Admin, folder);

            refused.StatusCode.Should().Be(HttpStatusCode.BadRequest, folder);
            (await refused.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").TryGetProperty("projectsFolder", out _)
                .Should().BeTrue(folder);
        }

        (await ProjectsFolderAsync(team.Admin)).Should().BeEmpty();
    }

    [Fact(DisplayName = "Pasta dos projetos na auditoria")]
    public async Task ChangeOfTheFolderOfTheProjects_IsAudited()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        await ChangeProjectsFolderAsync(team.Admin, "Projetos");

        var page = await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=InstallationSettings&pageSize=100", Cancellation);
        var changes = page.GetProperty("items").EnumerateArray().Should().ContainSingle().Subject.GetProperty("changes").EnumerateArray().ToList();
        changes.Should().ContainSingle(change => change.GetProperty("field").GetString() == "ProjectsFolder"
            && string.IsNullOrEmpty(change.GetProperty("before").GetString())
            && change.GetProperty("after").GetString() == "Projetos");
    }

    [Fact(DisplayName = "Pasta criada com o projeto")]
    public async Task TheFolderOfANewProject_IsInTheFolderOfTheProjects()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await ChangeProjectsFolderAsync(team.Admin, "Projetos");
        var sped = await team.CreateProjectAsync("SPED-27", "Entrega do SPED");
        var folder = Path.Combine(team.Files, "Projetos", "SPED-27 - Entrega do SPED");
        var made = Directory.Exists(Path.Combine(folder, "Documentos", Management));

        await UploadAsync(team.Admin, sped, Management, "Cronograma.pdf", "cronograma");

        made.Should().BeTrue("the folders of the documents are made with the project");
        File.Exists(Path.Combine(folder, "Documentos", Management, "Cronograma.pdf")).Should().BeTrue();
    }

    [Fact(DisplayName = "Tudo na mesma pasta")]
    public async Task Documents_Attachments_AndTerms_AreInTheFolderOfTheProject()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        await UploadAsync(team.Admin, team.Erp, Requirements, "Escopo.pdf", "escopo");
        var number = await AttachAsync(team, team.Erp, "Rejeição 539 na emissão");
        await RequestAcceptanceAsync(team, team.Erp);

        var project = Path.Combine(team.Files, ErpFolder);
        Directory.GetDirectories(project).Select(Path.GetFileName).Should().BeEquivalentTo(["Chamados", "Documentos", "Pedidos de aceite"]);
        File.Exists(Path.Combine(project, "Documentos", Requirements, "Escopo.pdf")).Should().BeTrue();
        File.Exists(Path.Combine(project, "Chamados", $"#{number}", "nota.xml")).Should().BeTrue();
        File.Exists(Path.Combine(project, "Pedidos de aceite", "termo.pdf")).Should().BeTrue();
    }

    [Fact(DisplayName = "Projeto com a pasta de antes")]
    public async Task AProjectWithoutItsFolderKept_TakesTheFolderAtTheRoot()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        // As a project from before its folder was kept: the folder is at the root, and the project does not know it.
        await team.App.ExecuteAsync(
            "UPDATE [projects].[projects] SET StorageFolderId = NULL, StorageFolderName = NULL WHERE Id = @id", ("@id", team.Erp));
        await ChangeProjectsFolderAsync(team.Admin, "Projetos");

        var number = await AttachAsync(team, team.Erp, "Rejeição 539 na emissão");

        File.Exists(Path.Combine(team.Files, ErpFolder, "Chamados", $"#{number}", "nota.xml")).Should().BeTrue();
        Directory.Exists(Path.Combine(team.Files, "Projetos")).Should().BeFalse();
    }

    [Fact(DisplayName = "Pasta apagada por fora")]
    public async Task AFolderRemovedOutside_IsMadeAgain()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        Directory.Delete(Path.Combine(team.Files, ErpFolder), recursive: true);

        await UploadAsync(team.Admin, team.Erp, Management, "Cronograma.pdf", "cronograma");
        var number = await AttachAsync(team, team.Erp, "Rejeição 539 na emissão");

        File.Exists(Path.Combine(team.Files, ErpFolder, "Documentos", Management, "Cronograma.pdf")).Should().BeTrue();
        File.Exists(Path.Combine(team.Files, ErpFolder, "Chamados", $"#{number}", "nota.xml")).Should().BeTrue();
    }

    [Fact(DisplayName = "Renomear o projeto")]
    public async Task RenamingTheProject_RenamesItsFolder_AndTheFilesStillOpen()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var document = await UploadAsync(team.Admin, team.Erp, Requirements, "Escopo.pdf", "escopo");

        (await RenameAsync(team, team.Erp, "Implantação do ERP")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await UploadAsync(team.Admin, team.Erp, Management, "Cronograma.pdf", "cronograma");

        Directory.Exists(Path.Combine(team.Files, ErpFolder)).Should().BeFalse();
        var renamed = Path.Combine(team.Files, "ERP-FIN - Implantação do ERP");
        File.Exists(Path.Combine(renamed, "Documentos", Requirements, "Escopo.pdf")).Should().BeTrue();
        File.Exists(Path.Combine(renamed, "Documentos", Management, "Cronograma.pdf")).Should().BeTrue();
        (await team.Admin.GetStringAsync($"/api/projects/{team.Erp}/documents/{document}/content", Cancellation)).Should().Be("escopo");
    }

    [Fact(DisplayName = "Nome da pasta já usado")]
    public async Task ANameInUse_KeepsTheFolder_AndTheNewFilesGoToIt()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        Directory.CreateDirectory(Path.Combine(team.Files, "ERP-FIN - Implantação do ERP"));

        (await RenameAsync(team, team.Erp, "Implantação do ERP")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var number = await AttachAsync(team, team.Erp, "Rejeição 539 na emissão");

        (await team.Admin.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}", Cancellation)).GetProperty("name").GetString()
            .Should().Be("Implantação do ERP");
        File.Exists(Path.Combine(team.Files, ErpFolder, "Chamados", $"#{number}", "nota.xml")).Should().BeTrue();
        Directory.GetFileSystemEntries(Path.Combine(team.Files, "ERP-FIN - Implantação do ERP")).Should().BeEmpty();
    }

    [Fact(DisplayName = "Armazenamento fora do ar")]
    public async Task ARenameThatFailed_IsTriedAgainOnTheNextChange()
    {
        var site = new FakeSharePoint();
        await using var team = await ProjectTeam.StartAsync(sqlServer, builder =>
        {
            builder.Configuration["FileStorage:Provider"] = "SharePoint";
            builder.Configuration["FileStorage:SharePoint:SiteUrl"] = FakeSharePoint.SiteUrl;
            builder.Services.AddSingleton<IGraphTokens>(new SiteToken());
            builder.Services.AddHttpClient("sharepoint").ConfigurePrimaryHttpMessageHandler(() => site);
        });
        site.NamesIn("Documentos", string.Empty).Should().Equal(ErpFolder);

        site.ThrottleNext = 1000;
        var failed = await RenameAsync(team, team.Erp, "Implantação do ERP");
        site.ThrottleNext = 0;
        var kept = site.NamesIn("Documentos", string.Empty);
        var again = await RenameAsync(team, team.Erp, "Implantação do ERP", "Segunda alteração.");

        failed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        kept.Should().Equal(ErpFolder);
        again.StatusCode.Should().Be(HttpStatusCode.NoContent);
        site.NamesIn("Documentos", string.Empty).Should().Equal("ERP-FIN - Implantação do ERP");
        site.NamesIn("Documentos", "ERP-FIN - Implantação do ERP/Documentos").Should().Contain(Management);
    }

    [Fact(DisplayName = "Pasta dos projetos fica")]
    public async Task DeletingTheLastProject_KeepsTheFolderOfTheProjects()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await ChangeProjectsFolderAsync(team.Admin, "Projetos");
        var sped = await team.CreateProjectAsync("SPED-27", "Entrega do SPED");
        await UploadAsync(team.Admin, sped, Management, "Cronograma.pdf", "cronograma");

        var deleted = await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{sped}/deletion", new { code = "SPED-27" });

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Directory.Exists(Path.Combine(team.Files, "Projetos")).Should().BeTrue();
        Directory.GetFileSystemEntries(Path.Combine(team.Files, "Projetos")).Should().BeEmpty();
    }

    private static Task<HttpResponseMessage> ChangeProjectsFolderAsync(HttpClient admin, string projectsFolder) =>
        SendAsync(admin, HttpMethod.Put, "/api/installation", new
        {
            name = "FiscoProjects",
            timeZone = "America/Sao_Paulo",
            mainColor = (string?)null,
            dailySummaryAt = "07:30:00",
            notificationRetentionDays = 90,
            projectsFolder,
        });

    private static async Task<string?> ProjectsFolderAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/installation/rules", Cancellation)).GetProperty("projectsFolder").GetString();

    private static Task<HttpResponseMessage> RenameAsync(ProjectTeam team, Guid project, string name, string? description = null) =>
        SendAsync(team.Admin, HttpMethod.Put, $"/api/projects/{project}", new { code = "ERP-FIN", name, description, clientId = (Guid?)null });

    private static async Task<Guid> UploadAsync(HttpClient client, Guid project, string folderName, string name, string text)
    {
        var folders = await client.GetFromJsonAsync<JsonElement>($"/api/projects/{project}/documents/folders", Cancellation);
        var folder = folders.GetProperty("folders").EnumerateArray()
            .Single(item => item.GetProperty("name").GetString() == folderName).GetProperty("id").GetGuid();
        var sent = await PostFormAsync(
            client, $"/api/projects/{project}/documents", new Dictionary<string, string?> { ["folderId"] = folder.ToString() }, name, Encoding.UTF8.GetBytes(text));
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
        return (await sent.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    /// <summary>Carla opens a ticket and Rafael answers with the XML of the invoice; the number of the ticket.</summary>
    private static async Task<int> AttachAsync(ProjectTeam team, Guid projectId, string title)
    {
        var carla = await team.ClientOfAsync(team.Carla);
        var opened = await SendAsync(carla, HttpMethod.Post, "/api/tickets", new
        {
            projectId,
            team = "Functional",
            priority = "Medium",
            title,
            description = "Descrição do chamado.",
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        var number = (await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
        var rafael = await team.ClientOfAsync(team.Rafael);
        var sent = await PostFormAsync(rafael, $"/api/tickets/{number}/messages", new Dictionary<string, string?>
        {
            ["body"] = """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"Segue o XML."}]}]}""",
        }, "nota.xml", Encoding.UTF8.GetBytes("<nfe/>"));
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
        return number;
    }

    /// <summary>The Administrator asks Carla to accept a milestone, with the term to sign.</summary>
    private static async Task RequestAcceptanceAsync(ProjectTeam team, Guid project)
    {
        var schedule = $"/api/projects/{project}/schedule";
        var phase = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{schedule}/phases", new { name = "Homologação" }));
        var milestone = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{schedule}/items", new
        {
            kind = "Milestone",
            phaseId = phase,
            name = "Homologação concluída",
            start = "2026-11-03",
            duration = 0,
            ownerId = (Guid?)null,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
        }));
        await IdOfAsync(await PostFormAsync(team.Admin, $"/api/projects/{project}/acceptances", new Dictionary<string, string?>
        {
            ["milestoneId"] = milestone.ToString(),
            ["recipientId"] = team.Carla.ToString(),
            ["title"] = "Aceite da homologação",
            ["message"] = "Assine o termo, por favor.",
        }, "termo.pdf", Encoding.ASCII.GetBytes("%PDF-1.7\ntermo\n%%EOF")));
    }

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string path, IReadOnlyDictionary<string, string?> fields, string fileName, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        foreach (var (name, value) in fields)
        {
            if (value is not null)
            {
                form.Add(new StringContent(value, Encoding.UTF8), name);
            }
        }

        form.Add(new ByteArrayContent(content), "file", fileName);
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = form };
        request.Headers.Add("X-CSRF", "1");
        return await client.SendAsync(request, Cancellation);
    }

    private sealed class SiteToken : IGraphTokens
    {
        public Task<string> GetAsync(CancellationToken cancellationToken) => Task.FromResult(FakeSharePoint.Token);
    }
}
