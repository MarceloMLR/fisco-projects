namespace FiscoProjects.SharedKernel.Tests;

// The rules of text shared by the modules: the address of the web a person types and the search in a list in memory.
public sealed class TextRulesTests
{
    [Theory(DisplayName = "Endereço da web")]
    [InlineData(" https://erp.cliente.example/qa ", 100, true, "https://erp.cliente.example/qa")]
    [InlineData("http://erp.cliente.example", 100, true, "http://erp.cliente.example")]
    [InlineData("   ", 100, true, null)]
    [InlineData("ftp://arquivos.example", 100, false, "ftp://arquivos.example")]
    [InlineData("javascript:alert(1)", 100, false, "javascript:alert(1)")]
    [InlineData("erp.cliente.example", 100, false, "erp.cliente.example")]
    [InlineData("https://erp.cliente.example/qa", 20, false, "https://erp.cliente.example/qa")]
    public void WebAddress_IsHttpOrHttpsUpToTheLength(string text, int maxLength, bool valid, string? url)
    {
        WebAddress.TryOf(text, maxLength, out var found).Should().Be(valid);
        found.Should().Be(url);
    }

    [Theory(DisplayName = "Busca sem caixa e sem acento")]
    [InlineData("ERP · Homologação", "homologacao", true)]
    [InlineData("Especificação", "ESPECIFICA", true)]
    [InlineData("Painéis", "paineis", true)]
    [InlineData("Produção", "homolog", false)]
    [InlineData("Qualquer texto", "", true)]
    public void Search_IgnoresCaseAndAccents(string text, string search, bool finds) =>
        SearchText.Finds(text, SearchText.Simplify(search)).Should().Be(finds);
}
