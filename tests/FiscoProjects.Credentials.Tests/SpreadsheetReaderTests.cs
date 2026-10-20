using System.Text;
using FiscoProjects.Credentials.Application;
using FiscoProjects.Credentials.Infrastructure;
using FiscoProjects.Testing;

namespace FiscoProjects.Credentials.Tests;

// Spec: acessos, requirement "Leitura da planilha de acessos" (ADR-0094).
public sealed class SpreadsheetReaderTests
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Task<SpreadsheetResult> ReadAsync(string name, byte[] content, int? sheet = null) =>
        new SpreadsheetReader().ReadAsync(name, new MemoryStream(content), sheet, Cancellation);

    [Fact(DisplayName = "Ler planilha xlsx")]
    public async Task Workbook_IsReadAsText()
    {
        var file = SpreadsheetFiles.Workbook(("Acessos", [
            [],
            ["Cliente", "Portal", "Usuário", "Senha", null, "Código"],
            [],
            ["Alfa", "ERP", "consultoria.fiscal", " Kx7#pa2V!q ", null, 1234d],
            ["Beta", "SEFAZ", null, 123456d, "extra", 0.5d],
        ]));

        var read = await ReadAsync("acessos.xlsx", file);

        read.Refusal.Should().Be(SpreadsheetRefusal.None);
        var spreadsheet = read.Spreadsheet!;
        spreadsheet.Sheets.Should().Equal("Acessos");
        spreadsheet.Columns.Should().Equal("Cliente", "Portal", "Usuário", "Senha", "Coluna E", "Código");
        spreadsheet.Rows.Select(row => row.Number).Should().Equal(4, 5);
        spreadsheet.Rows[0].Cells.Should().Equal("Alfa", "ERP", "consultoria.fiscal", "Kx7#pa2V!q", string.Empty, "1234");
        spreadsheet.Rows[1].Cells.Should().Equal("Beta", "SEFAZ", string.Empty, "123456", "extra", "0.5");
    }

    [Fact(DisplayName = "Cabeçalho na primeira linha com valor")]
    public async Task Header_IsTheFirstLineWithAValue()
    {
        var file = SpreadsheetFiles.Workbook(("Acessos", [
            ["Acessos dos clientes"],
            [null, "Portal", "Usuário"],
            [null, "ERP", "consultoria.fiscal"],
        ]));

        var spreadsheet = (await ReadAsync("acessos.xlsx", file)).Spreadsheet!;

        spreadsheet.Columns.Should().Equal("Acessos dos clientes", "Coluna B", "Coluna C");
        spreadsheet.Rows.Select(row => row.Number).Should().Equal(2, 3);
    }

    [Fact(DisplayName = "Escolher a aba")]
    public async Task Sheet_IsChosen()
    {
        var file = SpreadsheetFiles.Workbook(
            ("Alfa", [["Portal", "Usuário"], ["ERP", "alfa"]]),
            ("Beta", [["Portal", "Usuário"], ["SEFAZ", "beta"], ["Banco", "beta.fiscal"]]));

        var first = (await ReadAsync("acessos.xlsx", file)).Spreadsheet!;
        var second = (await ReadAsync("acessos.xlsx", file, sheet: 1)).Spreadsheet!;

        first.Sheets.Should().Equal("Alfa", "Beta");
        (first.Sheet, first.Rows.Count).Should().Be((0, 1));
        (second.Sheet, second.Rows.Select(row => row.Cells[0])).Should().BeEquivalentTo((1, new[] { "SEFAZ", "Banco" }));
    }

    [Fact(DisplayName = "Ler planilha csv do Excel")]
    public async Task Csv_OfTheExcelInPortuguese_IsRead()
    {
        var text = "Cliente;Portal;Usuário;Observação\r\nAlfa;\"Prefeitura; NFS-e\";alfa-dist;\"Notas da matriz\r\nsó consulta\"\r\nBeta;ERP;\"o \"\"usuário\"\"\";\r\n";
        var file = CodePagesEncodingProvider.Instance.GetEncoding(1252)!.GetBytes(text);

        var spreadsheet = (await ReadAsync("acessos.csv", file)).Spreadsheet!;

        spreadsheet.Sheets.Should().Equal("CSV");
        spreadsheet.Columns.Should().Equal("Cliente", "Portal", "Usuário", "Observação");
        spreadsheet.Rows.Select(row => row.Number).Should().Equal(2, 4);
        spreadsheet.Rows[0].Cells.Should().Equal("Alfa", "Prefeitura; NFS-e", "alfa-dist", "Notas da matriz\r\nsó consulta");
        spreadsheet.Rows[1].Cells.Should().Equal("Beta", "ERP", "o \"usuário\"", string.Empty);
    }

    [Theory(DisplayName = "Separador e codificação do CSV")]
    [InlineData("Portal,Usuário\nERP,alfa\n", "utf-8-bom")]
    [InlineData("Portal\tUsuário\nERP\talfa\n", "utf-8")]
    [InlineData("Portal;Usuário\nERP;alfa", "utf-16")]
    public async Task Csv_TakesTheSeparatorAndTheEncoding(string text, string encoding)
    {
        var file = encoding switch
        {
            "utf-8-bom" => [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text)],
            "utf-16" => [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(text)],
            _ => Encoding.UTF8.GetBytes(text),
        };

        var spreadsheet = (await ReadAsync("acessos.CSV", file)).Spreadsheet!;

        spreadsheet.Columns.Should().Equal("Portal", "Usuário");
        spreadsheet.Rows.Should().ContainSingle().Which.Cells.Should().Equal("ERP", "alfa");
    }

    [Theory(DisplayName = "Arquivo que não é planilha")]
    [InlineData("acessos.pdf", "%PDF-1.4", SpreadsheetRefusal.Type)]
    [InlineData("acessos.xls", "planilha antiga", SpreadsheetRefusal.Type)]
    [InlineData("acessos.xlsx", "%PDF-1.4", SpreadsheetRefusal.Unreadable)]
    [InlineData("acessos.xlsx", "PK\u0003\u0004 corrompido", SpreadsheetRefusal.Unreadable)]
    [InlineData("acessos.csv", "Portal\u0000Usuário", SpreadsheetRefusal.Unreadable)]
    [InlineData("acessos.csv", "Portal;Usuário\n\n;\n", SpreadsheetRefusal.Empty)]
    public async Task OtherFiles_AreRefused(string name, string content, SpreadsheetRefusal refusal) =>
        (await ReadAsync(name, Encoding.Latin1.GetBytes(content))).Refusal.Should().Be(refusal);

    [Fact(DisplayName = "Planilha protegida por senha")]
    public async Task ProtectedWorkbook_IsUnreadable()
    {
        // A .xlsx protected by a password is an OLE file, as the old .xls.
        byte[] ole = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0, 0, 0, 0];

        (await ReadAsync("acessos.xlsx", ole)).Refusal.Should().Be(SpreadsheetRefusal.Unreadable);
    }

    [Fact(DisplayName = "Limites da planilha")]
    public async Task Limits_AreKept()
    {
        var many = new StringBuilder("Portal;Usuário\n");
        for (var line = 0; line <= CredentialImportLimits.MaxRows; line++)
        {
            many.Append("ERP;alfa\n");
        }

        var wide = string.Join(';', Enumerable.Range(1, CredentialImportLimits.MaxColumns + 1).Select(column => $"C{column}"));

        (await ReadAsync("acessos.csv", Encoding.UTF8.GetBytes(many.ToString()))).Refusal.Should().Be(SpreadsheetRefusal.TooManyRows);
        (await ReadAsync("acessos.csv", Encoding.UTF8.GetBytes($"{wide}\n{wide}\n"))).Refusal.Should().Be(SpreadsheetRefusal.TooManyRows);
        (await ReadAsync("acessos.csv", new byte[CredentialImportLimits.MaxBytes + 1])).Refusal.Should().Be(SpreadsheetRefusal.TooLarge);
    }

    [Theory(DisplayName = "Letras das colunas")]
    [InlineData("C12", 3, "C")]
    [InlineData("AB7", 28, "AB")]
    public void Columns_AndLetters(string reference, int column, string letter)
    {
        SpreadsheetReader.ColumnOf(reference).Should().Be(column);
        SpreadsheetReader.LetterOf(column).Should().Be(letter);
    }
}
