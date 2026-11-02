using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: documentos, "Quem vê as pastas padrão", "Quem vê as pastas do projeto", "Pastas do projeto" and "Permissões nos
// documentos" (ADR-0148): each folder is seen by the roles chosen, besides the Administrator and the manager.
public sealed class FolderVisibilityTests(SqlServerFixture sqlServer)
{
    private const string DefaultFolders = "/api/admin/document-folders";
    private const string Management = "01 Gestão do projeto";
    private const string Terms = "06 Termos de aceite";
    private static readonly string[] AllRoles = ["TechLead", "Contributor", "Developer", "Viewer"];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static string Path(Guid project) => $"/api/projects/{project}/documents";

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    private static async Task<JsonElement> FoldersAsync(HttpClient client, Guid project) =>
        await client.GetFromJsonAsync<JsonElement>($"{Path(project)}/folders", Cancellation);

    private static async Task<JsonElement?> FolderAsync(HttpClient client, Guid project, string name) =>
        (await FoldersAsync(client, project)).GetProperty("folders").EnumerateArray()
            .Cast<JsonElement?>()
            .SingleOrDefault(folder => folder!.Value.GetProperty("name").GetString() == name);

    private static async Task<Guid> FolderIdAsync(ProjectTeam team, Guid project, string name) =>
        (await FolderAsync(team.Admin, project, name))!.Value.GetProperty("id").GetGuid();

    private static string[] RolesOf(JsonElement folder) =>
        [.. folder.GetProperty("visibleTo").EnumerateArray().Select(role => role.GetString()!)];

    private static Task<HttpResponseMessage> VisibilityAsync(HttpClient client, Guid project, Guid folder, params string[] roles) =>
        SendAsync(client, HttpMethod.Put, $"{Path(project)}/folders/{folder}/visibility", new { visibleTo = roles });

    private static async Task<Guid> SentAsync(HttpClient client, Guid project, Guid folder, string name)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(folder.ToString()), "folderId");
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("conteúdo")), "file", name);
        using var request = new HttpRequestMessage(HttpMethod.Post, Path(project)) { Content = form };
        request.Headers.Add("X-CSRF", "1");
        var sent = await client.SendAsync(request, Cancellation);
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
        return (await sent.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, Guid project, Guid folder, string name)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(folder.ToString()), "folderId");
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("conteúdo")), "file", name);
        using var request = new HttpRequestMessage(HttpMethod.Post, Path(project)) { Content = form };
        request.Headers.Add("X-CSRF", "1");
        return await client.SendAsync(request, Cancellation);
    }

    private static async Task<List<string>> SearchAsync(HttpClient client, Guid project, string text) =>
        [.. (await client.GetFromJsonAsync<JsonElement>($"{Path(project)}?search={Uri.EscapeDataString(text)}", Cancellation))
            .GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()!)];

    private static async Task<JsonElement> DefaultFolderAsync(ProjectTeam team, string name) =>
        (await team.Admin.GetFromJsonAsync<JsonElement>(DefaultFolders, Cancellation)).EnumerateArray()
            .Single(folder => folder.GetProperty("name").GetString() == name);

    private static async Task<List<JsonElement>> AuditAsync(ProjectTeam team, string itemType) =>
        [.. (await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation)).GetProperty("items")
            .EnumerateArray().Where(entry => entry.GetProperty("itemType").GetString() == itemType)];

    private static (string? Before, string? After) ChangeOf(JsonElement record, string field)
    {
        var change = record.GetProperty("changes").EnumerateArray().Single(item => item.GetProperty("field").GetString() == field);
        return (change.GetProperty("before").GetString(), change.GetProperty("after").GetString());
    }

    [Fact(DisplayName = "Administrador muda quem vê a pasta")]
    public async Task TheAdministrator_ChangesWhoSeesAFolder_WithTheAudit()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var management = await FolderIdAsync(team, team.Erp, Management);

        var changed = await VisibilityAsync(team.Admin, team.Erp, management, "TechLead");

        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        RolesOf((await FolderAsync(team.Admin, team.Erp, Management))!.Value).Should().Equal("TechLead");
        (await FolderAsync(await team.ClientOfAsync(team.Rafael), team.Erp, Management)).Should().NotBeNull();
        (await FolderAsync(await team.ClientOfAsync(team.Carla), team.Erp, Management)).Should().BeNull();
        var record = (await AuditAsync(team, "DocumentFolder")).Should().ContainSingle().Subject;
        (record.GetProperty("action").GetString(), record.GetProperty("itemLabel").GetString()).Should().Be(("Updated", Management));
        ChangeOf(record, "VisibleTo").Should().Be(("Everyone", "TechLead"));
        // The project, by its current name (ADR-0084).
        ChangeOf(record, "ProjectId").After.Should().Be("Implantação do ERP financeiro");
    }

    [Fact(DisplayName = "Gerente não muda quem vê a pasta")]
    public async Task TheOtherPeopleOfTheProject_DoNotChangeWhoSeesAFolder()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var management = await FolderIdAsync(team, team.Erp, Management);

        foreach (var person in new[] { team.Marina, team.Rafael, team.Carla, team.Diego, team.Lucas })
        {
            (await VisibilityAsync(await team.ClientOfAsync(person), team.Erp, management, "TechLead"))
                .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        RolesOf((await FolderAsync(team.Admin, team.Erp, Management))!.Value).Should().Equal(AllRoles);
        (await FoldersAsync(await team.ClientOfAsync(team.Marina), team.Erp)).GetProperty("canChangeVisibility").GetBoolean().Should().BeFalse();
        (await FoldersAsync(team.Admin, team.Erp)).GetProperty("canChangeVisibility").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "Quem vê de um projeto de fora")]
    public async Task WhoIsNotInTheProject_GetsNotFound()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var management = await FolderIdAsync(team, team.Erp, Management);

        (await VisibilityAsync(await team.ClientOfAsync(team.Sofia), team.Erp, management, "TechLead"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await VisibilityAsync(team.Admin, team.Erp, Guid.CreateVersion7(), "TechLead"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Pasta de termos fixa")]
    public async Task TheFolderOfTheTerms_StaysOnlyTheManagements()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var terms = await FolderIdAsync(team, team.Erp, Terms);

        var opened = await VisibilityAsync(team.Admin, team.Erp, terms, "TechLead");

        opened.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await FolderAsync(await team.ClientOfAsync(team.Rafael), team.Erp, Terms)).Should().BeNull();
        RolesOf((await FolderAsync(team.Admin, team.Erp, Terms))!.Value).Should().BeEmpty();
    }

    [Fact(DisplayName = "Quem vê num projeto encerrado")]
    public async Task AClosedProject_KeepsWhoSeesItsFolders()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var management = await FolderIdAsync(team, team.Erp, Management);
        var marina = await team.ClientOfAsync(team.Marina);
        (await SendAsync(marina, HttpMethod.Put, $"/api/projects/{team.Erp}/status", new { status = "Completed" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var changed = await VisibilityAsync(team.Admin, team.Erp, management, "TechLead");

        changed.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(changed)).Should().Be("urn:fiscoprojects:problem:project-closed");
        RolesOf((await FolderAsync(team.Admin, team.Erp, Management))!.Value).Should().Equal(AllRoles);
        (await FoldersAsync(team.Admin, team.Erp)).GetProperty("canChangeVisibility").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "Pastas que já existiam")]
    public async Task TheFolders_StartSeenByTheFourRoles_AndTheTermsOnlyByTheManagement()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        foreach (var folder in (await FoldersAsync(team.Admin, team.Erp)).GetProperty("folders").EnumerateArray())
        {
            RolesOf(folder).Should().Equal(folder.GetProperty("isAcceptance").GetBoolean() ? [] : AllRoles);
        }

        foreach (var folder in (await team.Admin.GetFromJsonAsync<JsonElement>(DefaultFolders, Cancellation)).EnumerateArray())
        {
            RolesOf(folder).Should().Equal(folder.GetProperty("isAcceptance").GetBoolean() ? [] : AllRoles);
        }
    }

    [Fact(DisplayName = "Pasta escondida de um papel")]
    public async Task AHiddenFolder_IsLeftOut_WithItsDocuments()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        var management = await FolderIdAsync(team, team.Erp, Management);
        await SentAsync(marina, team.Erp, management, "Plano de comunicação.docx");
        var totalBefore = (await FoldersAsync(await team.ClientOfAsync(team.Carla), team.Erp)).GetProperty("total").GetInt32();
        (await VisibilityAsync(team.Admin, team.Erp, management, "TechLead")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var carla = await team.ClientOfAsync(team.Carla);
        var folders = await FoldersAsync(carla, team.Erp);

        folders.GetProperty("folders").EnumerateArray().Select(folder => folder.GetProperty("name").GetString()).Should().NotContain(Management);
        folders.GetProperty("total").GetInt32().Should().Be(totalBefore - 1);
        (await SearchAsync(carla, team.Erp, "Plano de comunicação")).Should().BeEmpty();
        (await SearchAsync(await team.ClientOfAsync(team.Rafael), team.Erp, "Plano de comunicação")).Should().Equal("Plano de comunicação.docx");
    }

    [Fact(DisplayName = "Documento de uma pasta escondida")]
    public async Task ADocumentOfAHiddenFolder_IsNotFound()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        var management = await FolderIdAsync(team, team.Erp, Management);
        var document = await SentAsync(marina, team.Erp, management, "Plano de comunicação.docx");
        (await VisibilityAsync(team.Admin, team.Erp, management, "TechLead")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var carla = await team.ClientOfAsync(team.Carla);

        (await carla.GetAsync($"{Path(team.Erp)}/{document}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await carla.GetAsync($"{Path(team.Erp)}/{document}/content", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await UploadAsync(carla, team.Erp, management, "Outro plano.docx")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SearchAsync(marina, team.Erp, "Outro plano")).Should().BeEmpty();
    }

    [Fact(DisplayName = "Gestão vê todas as pastas")]
    public async Task TheManagement_SeesAFolderOfNoRole()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        var management = await FolderIdAsync(team, team.Erp, Management);
        await SentAsync(marina, team.Erp, management, "Plano de comunicação.docx");

        (await VisibilityAsync(team.Admin, team.Erp, management)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        foreach (var client in new[] { team.Admin, marina })
        {
            (await FolderAsync(client, team.Erp, Management)).Should().NotBeNull();
            (await SearchAsync(client, team.Erp, "Plano de comunicação")).Should().Equal("Plano de comunicação.docx");
        }

        (await FolderAsync(await team.ClientOfAsync(team.Rafael), team.Erp, Management)).Should().BeNull();
    }

    [Fact(DisplayName = "Projeto novo recebe quem vê as pastas")]
    public async Task ANewProject_GetsWhoSeesEachDefaultFolder()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var management = (await DefaultFolderAsync(team, Management)).GetProperty("id").GetGuid();
        (await SendAsync(team.Admin, HttpMethod.Put, $"{DefaultFolders}/{management}", new { name = Management, isActive = true, visibleTo = new[] { "TechLead" } }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var project = await team.CreateProjectAsync("SPED-27", "Adequação do SPED Fiscal 2027");

        RolesOf((await FolderAsync(team.Admin, project, Management))!.Value).Should().Equal("TechLead");
        RolesOf((await FolderAsync(team.Admin, team.Erp, Management))!.Value).Should().Equal(AllRoles);
    }

    [Fact(DisplayName = "Quem vê a pasta padrão")]
    public async Task TheAdministrator_ChangesWhoSeesADefaultFolder_WithTheAudit()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var management = (await DefaultFolderAsync(team, Management)).GetProperty("id").GetGuid();

        var changed = await SendAsync(
            team.Admin, HttpMethod.Put, $"{DefaultFolders}/{management}", new { name = Management, isActive = true, visibleTo = new[] { "TechLead" } });
        var renamed = await SendAsync(team.Admin, HttpMethod.Put, $"{DefaultFolders}/{management}", new { name = Management, isActive = true });

        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        renamed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        RolesOf(await DefaultFolderAsync(team, Management)).Should().Equal("TechLead");
        var record = (await AuditAsync(team, "DefaultFolder")).Should().ContainSingle().Subject;
        ChangeOf(record, "VisibleTo").Should().Be(("Everyone", "TechLead"));
    }

    [Fact(DisplayName = "Pasta padrão nova vista por todos")]
    public async Task ANewDefaultFolder_IsSeenByTheFourRoles()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        (await SendAsync(team.Admin, HttpMethod.Post, DefaultFolders, new { name = "07 Treinamentos" })).StatusCode.Should().Be(HttpStatusCode.Created);
        (await SendAsync(team.Admin, HttpMethod.Post, DefaultFolders, new { name = "08 Diretoria", visibleTo = Array.Empty<string>() }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        RolesOf(await DefaultFolderAsync(team, "07 Treinamentos")).Should().Equal(AllRoles);
        RolesOf(await DefaultFolderAsync(team, "08 Diretoria")).Should().BeEmpty();
    }

    [Fact(DisplayName = "Pasta de termos padrão fixa")]
    public async Task TheDefaultFolderOfTheTerms_StaysOnlyTheManagements()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var terms = (await DefaultFolderAsync(team, Terms)).GetProperty("id").GetGuid();

        var opened = await SendAsync(team.Admin, HttpMethod.Put, $"{DefaultFolders}/{terms}", new { name = Terms, isActive = true, visibleTo = new[] { "Contributor" } });
        var kept = await SendAsync(team.Admin, HttpMethod.Put, $"{DefaultFolders}/{terms}", new { name = Terms, isActive = true, visibleTo = Array.Empty<string>() });

        opened.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        kept.StatusCode.Should().Be(HttpStatusCode.NoContent);
        RolesOf(await DefaultFolderAsync(team, Terms)).Should().BeEmpty();
    }
}
