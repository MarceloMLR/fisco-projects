using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: documentos (API side): "Pastas padrão", "Pastas do projeto", "Envio de documentos", "Nova versão do documento",
// "Status do documento", "Vínculo com o cronograma", "Busca e visualização", "Exclusão de documento" and "Permissões nos
// documentos" (ADR-0030, ADR-0031, ADR-0045, ADR-0092, ADR-0093).
public sealed class DocumentTests(SqlServerFixture sqlServer)
{
    private const string DefaultFolders = "/api/admin/document-folders";
    private const string Requirements = "03 Requisitos e especificações";
    private const string Terms = "06 Termos de aceite";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static string Path(Guid project) => $"/api/projects/{project}/documents";

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    private static async Task<JsonElement> FoldersAsync(HttpClient client, Guid project) =>
        await client.GetFromJsonAsync<JsonElement>($"{Path(project)}/folders", Cancellation);

    private static async Task<List<string>> FolderNamesAsync(HttpClient client, Guid project) =>
        [.. (await FoldersAsync(client, project)).GetProperty("folders").EnumerateArray().Select(folder => folder.GetProperty("name").GetString()!)];

    private static async Task<Guid> FolderAsync(HttpClient client, Guid project, string name) =>
        (await FoldersAsync(client, project)).GetProperty("folders").EnumerateArray()
            .Single(folder => folder.GetProperty("name").GetString() == name).GetProperty("id").GetGuid();

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client, Guid project, Guid folder, string name, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(folder.ToString()), "folderId");
        form.Add(new ByteArrayContent(content), "file", name);
        using var request = new HttpRequestMessage(HttpMethod.Post, Path(project)) { Content = form };
        request.Headers.Add("X-CSRF", "1");
        return await client.SendAsync(request, Cancellation);
    }

    private static Task<HttpResponseMessage> UploadAsync(HttpClient client, Guid project, Guid folder, string name, string text = "conteúdo") =>
        UploadAsync(client, project, folder, name, Encoding.UTF8.GetBytes(text));

    private static async Task<Guid> SentAsync(HttpClient client, Guid project, Guid folder, string name, string text = "conteúdo")
    {
        var sent = await UploadAsync(client, project, folder, name, text);
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
        return (await sent.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<List<JsonElement>> ListAsync(HttpClient client, Guid project, string query) =>
        [.. (await client.GetFromJsonAsync<JsonElement>($"{Path(project)}?{query}", Cancellation)).GetProperty("items").EnumerateArray()];

    private static async Task<JsonElement> DetailsAsync(HttpClient client, Guid project, Guid document) =>
        await client.GetFromJsonAsync<JsonElement>($"{Path(project)}/{document}", Cancellation);

    private static Task<HttpResponseMessage> StatusAsync(HttpClient client, Guid project, Guid document, string status) =>
        SendAsync(client, HttpMethod.Put, $"{Path(project)}/{document}/status", new { status });

    [Fact(DisplayName = "Incluir pasta padrão")]
    public async Task AddDefaultFolder_ShowsInTheOrderOfTheName()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        var added = await SendAsync(team.Admin, HttpMethod.Post, DefaultFolders, new { name = "05a Treinamentos" });

        added.StatusCode.Should().Be(HttpStatusCode.Created);
        var folders = await team.Admin.GetFromJsonAsync<JsonElement>(DefaultFolders, Cancellation);
        folders.EnumerateArray().Select(folder => folder.GetProperty("name").GetString()).Should().Equal(
            "01 Gestão do projeto", "02 Contratos e compras", Requirements, "04 Atas de reunião", "05 Entregas e manuais",
            "05a Treinamentos", Terms);
        folders.EnumerateArray().Should().OnlyContain(folder => folder.GetProperty("isActive").GetBoolean());
        folders.EnumerateArray().Where(folder => folder.GetProperty("isAcceptance").GetBoolean())
            .Select(folder => folder.GetProperty("name").GetString()).Should().Equal(Terms);
    }

    [Fact(DisplayName = "Nome de pasta repetido")]
    public async Task RepeatedFolderName_IsAConflict()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        var added = await SendAsync(team.Admin, HttpMethod.Post, DefaultFolders, new { name = "04 atas de reunião" });

        added.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(added)).Should().Be("urn:fiscoprojects:problem:document-folder-name-in-use");
        (await team.Admin.GetFromJsonAsync<JsonElement>(DefaultFolders, Cancellation)).GetArrayLength().Should().Be(6);
    }

    [Fact(DisplayName = "Pasta de termos sempre ativa")]
    public async Task AcceptanceFolder_IsNeverDeactivated()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var terms = (await team.Admin.GetFromJsonAsync<JsonElement>(DefaultFolders, Cancellation)).EnumerateArray()
            .Single(folder => folder.GetProperty("isAcceptance").GetBoolean()).GetProperty("id").GetGuid();

        var deactivated = await SendAsync(team.Admin, HttpMethod.Put, $"{DefaultFolders}/{terms}", new { name = Terms, isActive = false });
        var renamed = await SendAsync(team.Admin, HttpMethod.Put, $"{DefaultFolders}/{terms}", new { name = "07 Termos assinados", isActive = true });

        deactivated.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        renamed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await team.Admin.GetFromJsonAsync<JsonElement>(DefaultFolders, Cancellation)).EnumerateArray()
            .Single(folder => folder.GetProperty("id").GetGuid() == terms).GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "Pastas padrão só do administrador")]
    public async Task DefaultFolders_AreOnlyTheAdministrators()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);

        (await SendAsync(marina, HttpMethod.Post, DefaultFolders, new { name = "07 Treinamentos" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await marina.GetAsync(DefaultFolders, Cancellation)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Projeto novo recebe as pastas padrão")]
    public async Task NewProject_GetsTheDefaultFolders()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        (await FolderNamesAsync(team.Admin, team.Erp)).Should().Equal(
            "01 Gestão do projeto", "02 Contratos e compras", Requirements, "04 Atas de reunião", "05 Entregas e manuais", Terms);
        Directory.GetDirectories(System.IO.Path.Combine(team.Files, "ERP-FIN - Implantação do ERP financeiro", "Documentos"))
            .Select(System.IO.Path.GetFileName).Should().HaveCount(6, "the folders are made in the storage with the project");
    }

    [Fact]
    public async Task ProjectMadeBeforeTheModule_GetsTheFoldersWhenOpened()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await team.App.CountAsync("DELETE FROM [documents].[folders] WHERE ProjectId = @project; SELECT 0", ("@project", team.Erp));

        (await FolderNamesAsync(team.Admin, team.Erp)).Should().HaveCount(6);
        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[folders] WHERE ProjectId = @project", ("@project", team.Erp)))
            .Should().Be(6);
    }

    [Fact(DisplayName = "Pasta desativada fica fora dos projetos novos")]
    public async Task DeactivatedFolder_IsLeftOutOfNewProjects()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var minutes = (await team.Admin.GetFromJsonAsync<JsonElement>(DefaultFolders, Cancellation)).EnumerateArray()
            .Single(folder => folder.GetProperty("name").GetString() == "04 Atas de reunião").GetProperty("id").GetGuid();
        (await SendAsync(team.Admin, HttpMethod.Put, $"{DefaultFolders}/{minutes}", new { name = "04 Atas de reunião", isActive = false }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fisc = await team.CreateProjectAsync("FISC-SP", "Revisão fiscal da filial SP");

        (await FolderNamesAsync(team.Admin, fisc)).Should().NotContain("04 Atas de reunião");
        (await FolderNamesAsync(team.Admin, team.Erp)).Should().Contain("04 Atas de reunião");
    }

    [Fact(DisplayName = "Enviar documento")]
    public async Task Upload_MakesADraftInVersionOne()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);
        var folder = await FolderAsync(carla, team.Erp, Requirements);

        var id = await SentAsync(carla, team.Erp, folder, "Mapa de processos fiscais.docx");

        var item = (await ListAsync(carla, team.Erp, $"folderId={folder}")).Should().ContainSingle().Subject;
        item.GetProperty("id").GetGuid().Should().Be(id);
        item.GetProperty("name").GetString().Should().Be("Mapa de processos fiscais.docx");
        (item.GetProperty("version").GetInt32(), item.GetProperty("status").GetString()).Should().Be((1, "Draft"));
        item.GetProperty("updated").GetProperty("name").GetString().Should().Be("Carla Mendes");
        item.GetProperty("folderName").GetString().Should().Be(Requirements);
        item.GetProperty("link").ValueKind.Should().Be(JsonValueKind.Null);
        (await FoldersAsync(carla, team.Erp)).GetProperty("total").GetInt32().Should().Be(1);
    }

    [Fact(DisplayName = "Arquivo acima do limite")]
    public async Task TooLargeFile_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer, builder => builder.Configuration["Documents:MaxMegabytes"] = "1");
        var folder = await FolderAsync(team.Admin, team.Erp, Requirements);

        var sent = await UploadAsync(team.Admin, team.Erp, folder, "Base de produtos.csv", new byte[(1024 * 1024) + 1]);

        sent.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await sent.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        (problem.GetProperty("type").GetString(), problem.GetProperty("reason").GetString())
            .Should().Be(("urn:fiscoprojects:problem:document-refused", "TooLarge"));
        (await ListAsync(team.Admin, team.Erp, $"folderId={folder}")).Should().BeEmpty();
    }

    [Fact(DisplayName = "Programa recusado")]
    public async Task Program_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var folder = await FolderAsync(team.Admin, team.Erp, Requirements);

        var sent = await UploadAsync(team.Admin, team.Erp, folder, "instalador.exe", "MZ programa");

        sent.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await sent.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("reason").GetString().Should().Be("Dangerous");
        (await ListAsync(team.Admin, team.Erp, $"folderId={folder}")).Should().BeEmpty();
    }

    [Fact(DisplayName = "Pasta de termos não recebe envios")]
    public async Task AcceptanceFolder_TakesNoUploads()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        var terms = await FolderAsync(marina, team.Erp, Terms);

        var sent = await UploadAsync(marina, team.Erp, terms, "termo.pdf", "%PDF-1.7");

        sent.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(sent)).Should().Be("urn:fiscoprojects:problem:acceptance-folder-read-only");
        (await ListAsync(marina, team.Erp, $"folderId={terms}")).Should().BeEmpty();
    }

    [Fact(DisplayName = "Mesmo nome vira nova versão")]
    public async Task SameName_IsANewVersion()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);
        var diego = await team.ClientOfAsync(team.Diego);
        var folder = await FolderAsync(carla, team.Erp, Requirements);
        var id = await SentAsync(carla, team.Erp, folder, "Mapa de processos fiscais.docx", "versão 1");

        (await SentAsync(diego, team.Erp, folder, "Mapa de processos fiscais.docx", "versão 2")).Should().Be(id);

        var details = await DetailsAsync(carla, team.Erp, id);
        details.GetProperty("document").GetProperty("version").GetInt32().Should().Be(2);
        details.GetProperty("versions").EnumerateArray()
            .Select(version => (version.GetProperty("number").GetInt32(), version.GetProperty("uploaded").GetProperty("name").GetString()))
            .Should().Equal((2, "Diego Torres"), (1, "Carla Mendes"));
        (await carla.GetStringAsync($"{Path(team.Erp)}/{id}/content?version=1&download=true", Cancellation)).Should().Be("versão 1");
        (await ListAsync(carla, team.Erp, $"folderId={folder}")).Should().ContainSingle();
    }

    [Fact(DisplayName = "Nova versão de documento aprovado volta para revisão")]
    public async Task NewVersion_OfAnApprovedDocument_GoesBackToReview()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);
        var rafael = await team.ClientOfAsync(team.Rafael);
        var folder = await FolderAsync(carla, team.Erp, Requirements);
        var id = await SentAsync(carla, team.Erp, folder, "Regras de CFOP por operação.xlsx");
        (await StatusAsync(rafael, team.Erp, id, "Approved")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        await SentAsync(carla, team.Erp, folder, "Regras de CFOP por operação.xlsx", "nova versão");

        var document = (await DetailsAsync(carla, team.Erp, id)).GetProperty("document");
        document.GetProperty("status").GetString().Should().Be("InReview");
        document.GetProperty("statusChanged").GetProperty("name").GetString().Should().Be("Carla Mendes");
    }

    [Fact(DisplayName = "Nova versão mantém o status")]
    public async Task NewVersion_KeepsADraft()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var folder = await FolderAsync(team.Admin, team.Erp, Requirements);
        var id = await SentAsync(team.Admin, team.Erp, folder, "Plano de testes integrados.docx");

        await SentAsync(team.Admin, team.Erp, folder, "Plano de testes integrados.docx", "revisto");

        (await DetailsAsync(team.Admin, team.Erp, id)).GetProperty("document").GetProperty("status").GetString().Should().Be("Draft");
    }

    [Fact(DisplayName = "Aprovar documento")]
    public async Task TechLead_Approves()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var rafael = await team.ClientOfAsync(team.Rafael);
        var folder = await FolderAsync(rafael, team.Erp, Requirements);
        var id = await SentAsync(rafael, team.Erp, folder, "Especificação das regras de ICMS-ST.pdf");
        (await StatusAsync(rafael, team.Erp, id, "InReview")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        team.App.Clock.Advance(TimeSpan.FromHours(2));

        (await StatusAsync(rafael, team.Erp, id, "Approved")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var document = (await DetailsAsync(rafael, team.Erp, id)).GetProperty("document");
        document.GetProperty("status").GetString().Should().Be("Approved");
        document.GetProperty("statusChanged").GetProperty("name").GetString().Should().Be("Rafael Souza");
        document.GetProperty("statusChanged").GetProperty("at").GetDateTime().Should().Be(team.App.Clock.GetUtcNow().UtcDateTime);
    }

    [Fact(DisplayName = "Colaborador não aprova")]
    public async Task Contributor_DoesNotApprove()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);
        var folder = await FolderAsync(carla, team.Erp, Requirements);
        var id = await SentAsync(carla, team.Erp, folder, "Mapa de processos fiscais.docx");

        (await StatusAsync(carla, team.Erp, id, "Approved")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await DetailsAsync(carla, team.Erp, id)).GetProperty("document").GetProperty("status").GetString().Should().Be("Draft");
    }

    [Fact(DisplayName = "Obsoleto sai da lista padrão")]
    public async Task Obsolete_LeavesTheDefaultList()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var folder = await FolderAsync(team.Admin, team.Erp, Requirements);
        var old = await SentAsync(team.Admin, team.Erp, folder, "Especificação de relatórios fiscais v1.pdf");
        await SentAsync(team.Admin, team.Erp, folder, "Especificação de relatórios fiscais v2.pdf");

        (await StatusAsync(team.Admin, team.Erp, old, "Obsolete")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ListAsync(team.Admin, team.Erp, $"folderId={folder}")).Select(item => item.GetProperty("id").GetGuid()).Should().NotContain(old);
        (await ListAsync(team.Admin, team.Erp, $"folderId={folder}&status=All")).Select(item => item.GetProperty("id").GetGuid()).Should().Contain(old);
        var folders = await FoldersAsync(team.Admin, team.Erp);
        folders.GetProperty("total").GetInt32().Should().Be(1);
        folders.GetProperty("folders").EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == folder)
            .GetProperty("count").GetInt32().Should().Be(1);
    }

    [Fact(DisplayName = "Vincular a uma tarefa")]
    public async Task Link_ToATask_ShowsItsName()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task) = await ScheduleAsync(team, team.Erp);
        var carla = await team.ClientOfAsync(team.Carla);
        var folder = await FolderAsync(carla, team.Erp, Requirements);
        var id = await SentAsync(carla, team.Erp, folder, "Regras de CFOP por operação.xlsx");

        var linked = await SendAsync(carla, HttpMethod.Put, $"{Path(team.Erp)}/{id}/link", new { phaseId = (Guid?)null, itemId = task });

        linked.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var link = (await ListAsync(carla, team.Erp, $"folderId={folder}")).Single().GetProperty("link");
        (link.GetProperty("id").GetGuid(), link.GetProperty("type").GetString(), link.GetProperty("name").GetString())
            .Should().Be((task, "Task", "Parametrizar regras de ICMS"));
    }

    [Fact(DisplayName = "Item de outro projeto")]
    public async Task Link_ToAnotherProject_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var fisc = await team.CreateProjectAsync("FISC-SP", "Revisão fiscal da filial SP");
        var (_, otherTask) = await ScheduleAsync(team, fisc);
        var folder = await FolderAsync(team.Admin, team.Erp, Requirements);
        var id = await SentAsync(team.Admin, team.Erp, folder, "Regras de CFOP por operação.xlsx");

        var linked = await SendAsync(team.Admin, HttpMethod.Put, $"{Path(team.Erp)}/{id}/link", new { phaseId = (Guid?)null, itemId = otherTask });

        linked.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ListAsync(team.Admin, team.Erp, $"folderId={folder}")).Single().GetProperty("link").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "Item removido do cronograma")]
    public async Task RemovedItem_LeavesTheDocumentWithoutALink()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (phase, task) = await ScheduleAsync(team, team.Erp);
        var folder = await FolderAsync(team.Admin, team.Erp, Requirements);
        var byTask = await SentAsync(team.Admin, team.Erp, folder, "Regras de CFOP por operação.xlsx");
        var byPhase = await SentAsync(team.Admin, team.Erp, folder, "Plano de configuração.docx");
        await SendAsync(team.Admin, HttpMethod.Put, $"{Path(team.Erp)}/{byTask}/link", new { phaseId = (Guid?)null, itemId = task });
        await SendAsync(team.Admin, HttpMethod.Put, $"{Path(team.Erp)}/{byPhase}/link", new { phaseId = phase, itemId = (Guid?)null });

        (await SendAsync(team.Admin, HttpMethod.Delete, $"/api/projects/{team.Erp}/schedule/items/{task}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var items = await ListAsync(team.Admin, team.Erp, $"folderId={folder}");
        items.Single(item => item.GetProperty("id").GetGuid() == byTask).GetProperty("link").ValueKind.Should().Be(JsonValueKind.Null);
        items.Single(item => item.GetProperty("id").GetGuid() == byPhase).GetProperty("link").GetProperty("name").GetString().Should().Be("Configuração");
    }

    [Fact(DisplayName = "Buscar nos documentos do projeto")]
    public async Task Search_FindsInEveryFolderThePersonSees()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var requirements = await FolderAsync(team.Admin, team.Erp, Requirements);
        var management = await FolderAsync(team.Admin, team.Erp, "01 Gestão do projeto");
        await SentAsync(team.Admin, team.Erp, requirements, "Regras de CFOP por operação.xlsx");
        await SentAsync(team.Admin, team.Erp, management, "Regras de comunicação.docx");
        await SentAsync(team.Admin, team.Erp, management, "Plano do projeto.docx");

        var found = await ListAsync(team.Admin, team.Erp, "search=regras");

        found.Select(item => (item.GetProperty("name").GetString(), item.GetProperty("folderName").GetString())).Should().Equal(
            ("Regras de CFOP por operação.xlsx", Requirements), ("Regras de comunicação.docx", "01 Gestão do projeto"));
    }

    [Fact(DisplayName = "Abrir PDF no navegador")]
    public async Task Pdf_OpensInTheBrowser()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var folder = await FolderAsync(team.Admin, team.Erp, Requirements);
        var pdf = await SentAsync(team.Admin, team.Erp, folder, "Especificação das regras de ICMS-ST.pdf", "%PDF-1.7 especificação");
        var csv = await SentAsync(team.Admin, team.Erp, folder, "Regras de CFOP.csv", "CFOP;Descrição");
        var docx = await SentAsync(team.Admin, team.Erp, folder, "Mapa de processos fiscais.docx");

        using var opened = await team.Admin.GetAsync($"{Path(team.Erp)}/{pdf}/content", Cancellation);
        using var text = await team.Admin.GetAsync($"{Path(team.Erp)}/{csv}/content", Cancellation);
        using var downloaded = await team.Admin.GetAsync($"{Path(team.Erp)}/{docx}/content", Cancellation);

        opened.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        opened.Content.Headers.ContentDisposition.Should().BeNull("the browser shows it in a tab");
        opened.Headers.GetValues("Content-Security-Policy").Should().Equal("sandbox");
        opened.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        text.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        text.Content.Headers.ContentDisposition.Should().BeNull();
        downloaded.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
    }

    [Fact(DisplayName = "Baixar versão anterior")]
    public async Task PreviousVersion_IsDownloaded()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var folder = await FolderAsync(team.Admin, team.Erp, Requirements);
        var id = await SentAsync(team.Admin, team.Erp, folder, "Dicionário de dados da migração.csv", "versão 1");
        await SentAsync(team.Admin, team.Erp, folder, "Dicionário de dados da migração.csv", "versão 2");

        using var first = await team.Admin.GetAsync($"{Path(team.Erp)}/{id}/content?version=1&download=true", Cancellation);
        var current = await team.Admin.GetStringAsync($"{Path(team.Erp)}/{id}/content", Cancellation);

        (await first.Content.ReadAsStringAsync(Cancellation)).Should().Be("versão 1");
        first.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        current.Should().Be("versão 2");
        (await team.Admin.GetAsync($"{Path(team.Erp)}/{id}/content?version=3", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Gestor exclui documento")]
    public async Task Manager_RemovesTheDocumentAndItsFile()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        var folder = await FolderAsync(marina, team.Erp, Requirements);
        var id = await SentAsync(marina, team.Erp, folder, "Mapa de processos fiscais.docx");
        await SentAsync(marina, team.Erp, folder, "Mapa de processos fiscais.docx", "versão 2");

        (await SendAsync(marina, HttpMethod.Delete, $"{Path(team.Erp)}/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ListAsync(marina, team.Erp, $"folderId={folder}&status=All")).Should().BeEmpty();
        (await marina.GetAsync($"{Path(team.Erp)}/{id}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        Directory.EnumerateFiles(System.IO.Path.Combine(team.Files, "ERP-FIN - Implantação do ERP financeiro", "Documentos", Requirements))
            .Should().BeEmpty();
        var audit = await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation);
        audit.GetProperty("items").EnumerateArray()
            .Should().Contain(entry => entry.GetProperty("itemLabel").GetString() == "Mapa de processos fiscais.docx"
                && entry.GetProperty("action").GetString() == "Deleted");
    }

    [Fact(DisplayName = "Colaborador não exclui")]
    public async Task Contributor_DoesNotRemove()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);
        var folder = await FolderAsync(carla, team.Erp, Requirements);
        var id = await SentAsync(carla, team.Erp, folder, "Mapa de processos fiscais.docx");

        (await SendAsync(carla, HttpMethod.Delete, $"{Path(team.Erp)}/{id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await ListAsync(carla, team.Erp, $"folderId={folder}")).Should().ContainSingle();
    }

    [Theory(DisplayName = "Permissões nos documentos por papel")]
    [InlineData("Administrator", true, true, true)]
    [InlineData("Manager", true, true, true)]
    [InlineData("TechLead", true, true, true)]
    [InlineData("Contributor", true, false, false)]
    [InlineData("Developer", true, false, false)]
    [InlineData("Viewer", false, false, false)]
    public async Task Permissions_FollowTheMatrix(string role, bool sends, bool approves, bool removes)
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, task) = await ScheduleAsync(team, team.Erp);
        var client = await team.ClientOfAsync(role);
        var folder = await FolderAsync(team.Admin, team.Erp, Requirements);
        var mine = await SentAsync(team.Admin, team.Erp, folder, "Matriz.docx");
        var permissions = (await FoldersAsync(client, team.Erp)).GetProperty("permissions");

        var sent = await UploadAsync(client, team.Erp, folder, $"Envio de {role}.docx");
        var linked = await SendAsync(client, HttpMethod.Put, $"{Path(team.Erp)}/{mine}/link", new { phaseId = (Guid?)null, itemId = task });
        var reviewed = await StatusAsync(client, team.Erp, mine, "InReview");
        var approved = await StatusAsync(client, team.Erp, mine, "Approved");
        var removed = await SendAsync(client, HttpMethod.Delete, $"{Path(team.Erp)}/{mine}");

        (sent.StatusCode, linked.StatusCode, reviewed.StatusCode).Should().Be(sends
            ? (HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.NoContent)
            : (HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden));
        approved.StatusCode.Should().Be(approves ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden);
        removed.StatusCode.Should().Be(removes ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden);
        (permissions.GetProperty("canUpload").GetBoolean(), permissions.GetProperty("canApprove").GetBoolean(), permissions.GetProperty("canDelete").GetBoolean())
            .Should().Be((sends, approves, removes));
    }

    [Fact(DisplayName = "Documentos de um projeto de fora")]
    public async Task ProjectOfOthers_IsNotFound()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var folder = await FolderAsync(team.Admin, team.Erp, Requirements);
        var id = await SentAsync(team.Admin, team.Erp, folder, "Mapa de processos fiscais.docx");
        var sofia = await team.ClientOfAsync(team.Sofia);

        (await sofia.GetAsync($"{Path(team.Erp)}/folders", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await sofia.GetAsync($"{Path(team.Erp)}?folderId={folder}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await sofia.GetAsync($"{Path(team.Erp)}/{id}/content", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await UploadAsync(sofia, team.Erp, folder, "Intruso.docx")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Leitor vê e não envia")]
    public async Task Viewer_SeesAndDoesNotSend()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var folder = await FolderAsync(team.Admin, team.Erp, Requirements);
        var id = await SentAsync(team.Admin, team.Erp, folder, "Mapa de processos fiscais.docx", "mapa");
        var lucas = await team.ClientOfAsync(team.Lucas);

        (await FolderNamesAsync(lucas, team.Erp)).Should().HaveCount(5);
        (await ListAsync(lucas, team.Erp, $"folderId={folder}")).Should().ContainSingle();
        (await lucas.GetStringAsync($"{Path(team.Erp)}/{id}/content?download=true", Cancellation)).Should().Be("mapa");
        (await UploadAsync(lucas, team.Erp, folder, "Leitor.docx")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Pasta de termos só da gestão")]
    public async Task AcceptanceFolder_IsOnlyForTheManagement()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var terms = await FolderAsync(team.Admin, team.Erp, Terms);

        foreach (var person in new[] { team.Rafael, team.Carla, team.Diego, team.Lucas })
        {
            var client = await team.ClientOfAsync(person);
            (await FolderNamesAsync(client, team.Erp)).Should().NotContain(Terms);
            (await ListAsync(client, team.Erp, $"folderId={terms}")).Should().BeEmpty();
        }

        (await FolderNamesAsync(await team.ClientOfAsync(team.Marina), team.Erp)).Should().Contain(Terms);
    }

    [Fact(DisplayName = "Projeto encerrado só leitura")]
    public async Task ClosedProject_IsReadOnly()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        var folder = await FolderAsync(marina, team.Erp, Requirements);
        var id = await SentAsync(marina, team.Erp, folder, "Mapa de processos fiscais.docx");
        (await SendAsync(marina, HttpMethod.Put, $"/api/projects/{team.Erp}/status", new { status = "Completed" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var sent = await UploadAsync(marina, team.Erp, folder, "Ata final.docx");
        var approved = await StatusAsync(marina, team.Erp, id, "Approved");
        var removed = await SendAsync(marina, HttpMethod.Delete, $"{Path(team.Erp)}/{id}");

        new[] { sent.StatusCode, approved.StatusCode, removed.StatusCode }.Should().AllBeEquivalentTo(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(sent)).Should().Be("urn:fiscoprojects:problem:project-closed");
        var permissions = (await FoldersAsync(marina, team.Erp)).GetProperty("permissions");
        permissions.GetProperty("canUpload").GetBoolean().Should().BeFalse();
        (await ListAsync(marina, team.Erp, $"folderId={folder}")).Should().ContainSingle();
    }

    /// <summary>The phase "Configuração" with the task "Parametrizar regras de ICMS", made by the Administrator.</summary>
    private static async Task<(Guid Phase, Guid Task)> ScheduleAsync(ProjectTeam team, Guid project)
    {
        var path = $"/api/projects/{project}/schedule";
        var phase = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{path}/phases", new { name = "Configuração" }));
        var task = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{path}/items", new
        {
            kind = "Task",
            phaseId = phase,
            name = "Parametrizar regras de ICMS",
            start = "2026-10-19",
            duration = 3,
            ownerId = (Guid?)null,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
        }));
        return (phase, task);
    }

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }
}
