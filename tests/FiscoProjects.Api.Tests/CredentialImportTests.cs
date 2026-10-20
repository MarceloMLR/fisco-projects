using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: acessos (API side): "Leitura da planilha de acessos" and "Importação dos acessos" (ADR-0034, ADR-0045, ADR-0094).
public sealed class CredentialImportTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static string Path(Guid project) => $"/api/projects/{project}/credentials";

    private static readonly byte[] Workbook = SpreadsheetFiles.Workbook(
        ("Alfa", [
            [],
            ["Cliente", "Portal", "Ambiente", "Usuário", "Senha"],
            ["Alfa", "ERP", "Produção", "consultoria.leitura", "Kx7#pa2V!q"],
            ["Alfa", "SEFAZ", "Produção", "12.345.678/0001-90", 123456d],
        ]),
        ("Beta", [
            ["Portal", "Usuário", "Senha"],
            ["Banco", "beta.fiscal", "Senha#Beta1"],
        ]));

    private static async Task<HttpResponseMessage> PreviewAsync(HttpClient client, Guid project, string name, byte[] content, int? sheet = null)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(content), "file", name);
        if (sheet is { } chosen)
        {
            form.Add(new StringContent(chosen.ToString(System.Globalization.CultureInfo.InvariantCulture)), "sheet");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{Path(project)}/import/preview") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        return await client.SendAsync(request, Cancellation);
    }

    private static Task<HttpResponseMessage> ImportAsync(HttpClient client, Guid project, params object[] items) =>
        ProjectTeam.SendAsync(client, HttpMethod.Post, $"{Path(project)}/import", new { items });

    private static object Item(int row, string portal, string environment, string username, string password) =>
        new { row, portal, environment, url = (string?)null, username, password, notes = (string?)null };

    private static Task<int> CredentialsInDatabaseAsync(ProjectTeam team) =>
        team.App.CountAsync("SELECT COUNT(*) FROM [credentials].[portal_credentials]");

    [Fact(DisplayName = "Ler planilha xlsx pela API")]
    public async Task Preview_ReadsTheWorkbook_AndSavesNothing()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);

        var response = await PreviewAsync(marina, team.Erp, "acessos.xlsx", Workbook);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Cancellation));
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var spreadsheet = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        spreadsheet.GetProperty("sheets").EnumerateArray().Select(sheet => sheet.GetString()).Should().Equal("Alfa", "Beta");
        spreadsheet.GetProperty("columns").EnumerateArray().Select(column => column.GetString())
            .Should().Equal("Cliente", "Portal", "Ambiente", "Usuário", "Senha");
        var rows = spreadsheet.GetProperty("rows").EnumerateArray().ToList();
        rows.Select(row => row.GetProperty("number").GetInt32()).Should().Equal(3, 4);
        rows[1].GetProperty("cells").EnumerateArray().Select(cell => cell.GetString()).Last().Should().Be("123456");
        (await CredentialsInDatabaseAsync(team)).Should().Be(0);
    }

    [Fact(DisplayName = "Escolher a aba pela API")]
    public async Task Preview_ReadsTheChosenSheet()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        var response = await PreviewAsync(team.Admin, team.Erp, "acessos.xlsx", Workbook, sheet: 1);

        var spreadsheet = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        spreadsheet.GetProperty("sheet").GetInt32().Should().Be(1);
        spreadsheet.GetProperty("rows").EnumerateArray().Single().GetProperty("cells")[0].GetString().Should().Be("Banco");
    }

    [Fact(DisplayName = "Ler planilha csv do Excel pela API")]
    public async Task Preview_ReadsTheCsvOfTheExcel()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var content = CodePagesEncodingProvider.Instance.GetEncoding(1252)!
            .GetBytes("Portal;Usuário;Observação\r\nPrefeitura · NFS-e;alfa-dist;\"Notas\r\nda matriz\"\r\n");

        var response = await PreviewAsync(team.Admin, team.Erp, "acessos.csv", content);

        var spreadsheet = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        spreadsheet.GetProperty("columns")[2].GetString().Should().Be("Observação");
        spreadsheet.GetProperty("rows")[0].GetProperty("cells").EnumerateArray().Select(cell => cell.GetString())
            .Should().Equal("Prefeitura · NFS-e", "alfa-dist", "Notas\r\nda matriz");
    }

    [Fact(DisplayName = "Arquivo que não é planilha pela API")]
    public async Task Preview_RefusesWhatIsNotASpreadsheet()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        var pdf = await PreviewAsync(team.Admin, team.Erp, "acessos.pdf", Encoding.ASCII.GetBytes("%PDF-1.4"));
        var corrupt = await PreviewAsync(team.Admin, team.Erp, "acessos.xlsx", Encoding.ASCII.GetBytes("PK\u0003\u0004 corrompido"));

        pdf.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await pdf.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("reason").GetString().Should().Be("Type");
        corrupt.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await corrupt.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        (problem.GetProperty("type").GetString(), problem.GetProperty("reason").GetString())
            .Should().Be(("urn:fiscoprojects:problem:credential-import-refused", "Unreadable"));
        (await CredentialsInDatabaseAsync(team)).Should().Be(0);
    }

    [Fact(DisplayName = "Importar acessos")]
    public async Task Import_IncludesTheMappedCredentials()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var rafael = await team.ClientOfAsync(team.Rafael);

        var response = await ImportAsync(
            rafael,
            team.Erp,
            Item(3, "ERP", "Produção", "consultoria.leitura", "Kx7#pa2V!q"),
            Item(4, "SEFAZ", "Produção", "12.345.678/0001-90", "123456"),
            Item(7, "Banco", "Homologação", "beta.fiscal", "Senha#Beta1"));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Cancellation));
        (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("imported").GetInt32().Should().Be(3);
        var list = await rafael.GetFromJsonAsync<JsonElement>(Path(team.Erp), Cancellation);
        list.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("portal").GetString())
            .Should().Equal("Banco", "ERP", "SEFAZ");
        (await team.App.CountAsync(
            "SELECT COUNT(*) FROM [credentials].[portal_credentials] WHERE ProtectedPassword LIKE '%Kx7#pa2V!q%' OR ProtectedPassword LIKE '%Senha#Beta1%'"))
            .Should().Be(0);
        var erp = list.GetProperty("items").EnumerateArray().Single(item => item.GetProperty("portal").GetString() == "ERP").GetProperty("id").GetGuid();
        (await rafael.GetFromJsonAsync<JsonElement>($"{Path(team.Erp)}/{erp}/password", Cancellation))
            .GetProperty("password").GetString().Should().Be("Kx7#pa2V!q");
        var audit = await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation);
        audit.GetProperty("items").EnumerateArray()
            .Count(entry => entry.GetProperty("itemType").GetString() == "PortalCredential" && entry.GetProperty("action").GetString() == "Created")
            .Should().Be(3);
    }

    [Fact(DisplayName = "Importação com linha inválida")]
    public async Task Import_WithALineNotValid_IncludesNothing()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        var response = await ImportAsync(
            team.Admin,
            team.Erp,
            Item(3, "ERP", "Produção", "consultoria.leitura", "Kx7#pa2V!q"),
            Item(4, "SEFAZ", "Produção", "", "123456"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        problem.GetProperty("type").GetString().Should().Be("urn:fiscoprojects:problem:credential-import-invalid");
        var row = problem.GetProperty("rows").EnumerateArray().Single();
        (row.GetProperty("row").GetInt32(), row.GetProperty("fields")[0].GetString()).Should().Be((4, "username"));
        problem.ToString().Should().NotContain("Kx7#pa2V!q");
        (await CredentialsInDatabaseAsync(team)).Should().Be(0);
    }

    [Theory(DisplayName = "Só gestores importam")]
    [InlineData("Contributor")]
    [InlineData("Developer")]
    [InlineData("Viewer")]
    public async Task OthersThanTheManagement_DoNotImport(string role)
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var client = await team.ClientOfAsync(role);

        var preview = await PreviewAsync(client, team.Erp, "acessos.xlsx", Workbook);
        var import = await ImportAsync(client, team.Erp, Item(3, "ERP", "Produção", "consultoria.leitura", "Kx7#pa2V!q"));

        (preview.StatusCode, import.StatusCode).Should().Be((HttpStatusCode.Forbidden, HttpStatusCode.Forbidden));
        (await CredentialsInDatabaseAsync(team)).Should().Be(0);
    }

    [Fact(DisplayName = "Importação em projeto encerrado")]
    public async Task ClosedProject_DoesNotImport()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        (await ProjectTeam.SendAsync(marina, HttpMethod.Put, $"/api/projects/{team.Erp}/status", new { status = "Completed" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var import = await ImportAsync(marina, team.Erp, Item(3, "ERP", "Produção", "consultoria.leitura", "Kx7#pa2V!q"));

        import.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(import)).Should().Be("urn:fiscoprojects:problem:project-closed");
        (await CredentialsInDatabaseAsync(team)).Should().Be(0);
    }
}
