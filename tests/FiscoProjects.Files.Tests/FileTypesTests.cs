using System.Text;
using FiscoProjects.Files.Contracts;

namespace FiscoProjects.Files.Tests;

// The rules every use of files shares (ADR-0026, ADR-0031, ADR-0079).
public sealed class FileTypesTests
{
    [Theory(DisplayName = "Arquivos recusados")]
    [InlineData("nota.xml", 10, "<nfe/>", FileRefusal.None)]
    [InlineData("nota.xml", 0, "", FileRefusal.Empty)]
    [InlineData("nota.xml", 2048, "<nfe/>", FileRefusal.TooLarge)]
    [InlineData("setup.EXE", 10, "abc", FileRefusal.Dangerous)]
    [InlineData("instalar.sh", 10, "abc", FileRefusal.Dangerous)]
    [InlineData("foto.png", 10, "MZ", FileRefusal.Dangerous)]
    [InlineData("roteiro.txt", 10, "#!/bin/sh", FileRefusal.Dangerous)]
    public void Files_AreCheckedByNameSizeAndStart(string name, long size, string start, FileRefusal refusal) =>
        FileTypes.Check(name, size, Encoding.ASCII.GetBytes(start), maxBytes: 1024).Should().Be(refusal);

    [Fact(DisplayName = "Conferir o começo do arquivo e voltar")]
    public async Task CheckAsync_ReadsTheStartAndGoesBack()
    {
        await using var content = new MemoryStream(Encoding.ASCII.GetBytes("MZ e o resto do programa"));

        var refusal = await FileTypes.CheckAsync("planilha.xlsx", content.Length, content, 1024, TestContext.Current.CancellationToken);

        refusal.Should().Be(FileRefusal.Dangerous);
        content.Position.Should().Be(0);
    }

    [Theory(DisplayName = "Tipo do arquivo para o navegador")]
    [InlineData("Especificação.PDF", "application/pdf", true, false)]
    [InlineData("fluxo.png", "image/png", true, false)]
    [InlineData("regras.csv", "text/csv", false, true)]
    [InlineData("leia-me.txt", "text/plain", false, true)]
    [InlineData("mapa.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", false, false)]
    [InlineData("dados.bin", "application/octet-stream", false, false)]
    public void ContentType_FollowsTheExtension(string name, string type, bool opens, bool text)
    {
        var contentType = FileTypes.ContentTypeOf(name);

        contentType.Should().Be(type);
        FileTypes.OpensInBrowser(contentType).Should().Be(opens);
        FileTypes.IsText(contentType).Should().Be(text);
    }
}
