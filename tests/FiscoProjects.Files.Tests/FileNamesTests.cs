using FiscoProjects.Files.Contracts;

namespace FiscoProjects.Files.Tests;

// Spec: armazenamento-de-arquivos, requirement "Nomes e caminhos seguros".
public sealed class FileNamesTests
{
    [Fact(DisplayName = "Nome com caracteres não aceitos")]
    public void NamesWithCharactersSharePointRefuses_AreAdjusted()
    {
        FileNames.Normalize("Relatório: março?.pdf ").Should().Be("Relatório_ março_.pdf");
        FileNames.Normalize("a*b<c>d|e\"f/g\\h.txt").Should().Be("a_b_c_d_e_f_g_h.txt");
        FileNames.Normalize("  notas.. ").Should().Be("notas");
        FileNames.Normalize("linha\tcom\ncontrole.txt").Should().Be("linha_com_controle.txt");
        FileNames.Normalize("pasta_vti_x").Should().Be("pastavti_x");
    }

    [Theory(DisplayName = "Nomes reservados")]
    [InlineData("CON", "_CON")]
    [InlineData("con.txt", "_con.txt")]
    [InlineData("LPT1", "_LPT1")]
    [InlineData("desktop.ini", "_desktop.ini")]
    [InlineData(".lock", "_.lock")]
    [InlineData(".fiscoprojects", "_.fiscoprojects")]
    [InlineData("Contrato.pdf", "Contrato.pdf")]
    public void ReservedNames_GetAnUnderscore(string name, string stored) =>
        FileNames.Normalize(name).Should().Be(stored);

    [Fact(DisplayName = "Nome vazio")]
    public void EmptyName_BecomesTheFallback()
    {
        FileNames.Normalize(null).Should().Be(FileNames.Fallback);
        FileNames.Normalize("  ...  ").Should().Be(FileNames.Fallback);
    }

    [Fact(DisplayName = "Nome longo cortado com a extensão")]
    public void LongName_IsCutKeepingTheExtension()
    {
        var stored = FileNames.Normalize(new string('a', 300) + ".pdf");

        stored.Should().HaveLength(FileNames.MaxLength).And.EndWith("a.pdf");
    }

    [Fact(DisplayName = "Nome do arquivo mantido ao lado")]
    public void NumberedName_GoesBeforeTheExtension()
    {
        FileNames.Numbered("print.png", 1).Should().Be("print (1).png");
        FileNames.Numbered("LEIAME", 2).Should().Be("LEIAME (2)");
        FileNames.Numbered("arquivo.tar.gz", 3).Should().Be("arquivo.tar (3).gz");
        FileNames.Numbered(new string('a', 196) + ".png", 12).Should().HaveLength(FileNames.MaxLength).And.EndWith(" (12).png");
    }

    [Fact(DisplayName = "Caminho em pastas")]
    public void Path_IsSplitInAdjustedFolders()
    {
        FileNames.Segments("Projetos/ERP-FIN/Chamados/#101").Should().Equal("Projetos", "ERP-FIN", "Chamados", "#101");
        FileNames.Segments("/Projetos//Atas: 2026/ ").Should().Equal("Projetos", "Atas_ 2026");
    }

    [Theory(DisplayName = "Caminho recusado")]
    [InlineData("../outra-pasta")]
    [InlineData("Projetos/../../segredos")]
    [InlineData("Projetos/./x")]
    [InlineData("")]
    [InlineData(" / ")]
    public void PathGoingOutOrEmpty_IsRefused(string path)
    {
        var split = () => FileNames.Segments(path);

        split.Should().Throw<ArgumentException>();
    }
}
