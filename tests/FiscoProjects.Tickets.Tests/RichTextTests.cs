using FiscoProjects.Files.Contracts;
using FiscoProjects.Tickets.Domain;

namespace FiscoProjects.Tickets.Tests;

// Spec: chamados, "Mensagens do chamado" and "Anexos do chamado" (ADR-0026).
public sealed class RichTextTests
{
    private const string Person = "0199f5a2-7c1e-7d40-9a1b-000000000006";

    [Fact(DisplayName = "Texto formatado aceito")]
    public void Parse_KeepsOnlyWhatIsAllowed_WithThePlainTextAndTheMentions()
    {
        var json = $$$"""
            {"type":"doc","content":[
              {"type":"paragraph","attrs":{"textAlign":"left"},"content":[
                {"type":"mention","attrs":{"id":"{{{Person}}}","label":"Diego Torres"}},
                {"type":"text","text":" veja o "},
                {"type":"text","text":"XML","marks":[{"type":"bold"},{"type":"link","attrs":{"href":"https://exemplo.example/nota","target":"_blank"}}]}
              ]},
              {"type":"bulletList","content":[{"type":"listItem","content":[{"type":"paragraph","content":[{"type":"text","text":"item"}]}]}]}
            ]}
            """;

        var text = RichText.Parse(json);

        text.Should().NotBeNull();
        text!.Text.Should().Be("@Diego Torres veja o XML\nitem");
        text.Mentions.Should().Equal(Guid.Parse(Person));
        // Written again: without the attributes the server does not know.
        text.Json.Should().NotContain("textAlign").And.NotContain("_blank").And.Contain("https://exemplo.example/nota");
    }

    [Theory(DisplayName = "Texto formatado recusado")]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("""{"type":"doc","content":[]}""")]
    [InlineData("""{"type":"doc","content":[{"type":"paragraph"}]}""")]
    [InlineData("""{"type":"doc","content":[{"type":"heading","content":[{"type":"text","text":"A"}]}]}""")]
    [InlineData("""{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"A","marks":[{"type":"italic"}]}]}]}""")]
    [InlineData("""{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"A","marks":[{"type":"link","attrs":{"href":"javascript:alert(1)"}}]}]}]}""")]
    [InlineData("""{"type":"doc","content":[{"type":"paragraph","content":[{"type":"image","attrs":{"src":"x"}}]}]}""")]
    [InlineData("""{"type":"doc","content":[{"type":"listItem","content":[{"type":"paragraph","content":[{"type":"text","text":"A"}]}]}]}""")]
    public void Parse_RefusesAnythingElse(string json) => RichText.Parse(json).Should().BeNull();

    [Fact(DisplayName = "Descrição em texto simples")]
    public void OfPlain_MakesAParagraphPerLine() =>
        RichText.Parse(RichText.OfPlain("Linha 1\n\nLinha 3").Json)!.Text.Should().Be("Linha 1\n\nLinha 3");

    [Fact(DisplayName = "Menção de pessoa anonimizada")]
    public void RenameMention_PutsTheIdentifierInTheMentionsOfThePerson()
    {
        var other = Guid.CreateVersion7();
        var json = $$$"""
            {"type":"doc","content":[
              {"type":"paragraph","content":[
                {"type":"mention","attrs":{"id":"{{{Person}}}","label":"Carla Mendes"}},
                {"type":"text","text":" e "},
                {"type":"mention","attrs":{"id":"{{{other}}}","label":"Diego Torres"}},
                {"type":"text","text":", Carla Mendes pediu."}
              ]},
              {"type":"bulletList","content":[{"type":"listItem","content":[{"type":"paragraph","content":[
                {"type":"mention","attrs":{"id":"{{{Person.ToUpperInvariant()}}}","label":"Carla Mendes"}}
              ]}]}]}
            ]}
            """;

        var renamed = RichText.Parse(json)!.RenameMention(Guid.Parse(Person), "Pessoa removida nº 1");

        // What was typed stays as typed (ADR-0058): only the mentions of the person change.
        renamed.Text.Should().Be("@Pessoa removida nº 1 e @Diego Torres, Carla Mendes pediu.\n@Pessoa removida nº 1");
        renamed.Mentions.Should().BeEquivalentTo([Guid.Parse(Person), other]);
        RichText.Parse(json)!.RenameMention(Guid.CreateVersion7(), "Pessoa removida nº 1").Text
            .Should().Be("@Carla Mendes e @Diego Torres, Carla Mendes pediu.\n@Carla Mendes");
    }

    [Theory(DisplayName = "Anexos recusados")]
    [InlineData("nota.xml", 10, "<nfe/>", FileRefusal.None)]
    [InlineData("nota.xml", 0, "", FileRefusal.Empty)]
    [InlineData("nota.xml", 2048, "<nfe/>", FileRefusal.TooLarge)]
    [InlineData("setup.EXE", 10, "abc", FileRefusal.Dangerous)]
    [InlineData("script.ps1", 10, "abc", FileRefusal.Dangerous)]
    [InlineData("foto.png", 10, "MZ", FileRefusal.Dangerous)]
    public void Attachments_AreCheckedByNameSizeAndStart(string name, long size, string start, FileRefusal refusal) =>
        FileTypes.Check(name, size, System.Text.Encoding.ASCII.GetBytes(start), maxBytes: 1024).Should().Be(refusal);
}
