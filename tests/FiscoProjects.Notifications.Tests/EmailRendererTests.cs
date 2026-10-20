using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Infrastructure;

namespace FiscoProjects.Notifications.Tests;

public sealed class EmailRendererTests
{
    private static readonly EmailContent Content = new(
        "test.subject",
        ["test.greeting", "test.body"],
        new Dictionary<string, string> { ["name"] = "Marina <Costa>" },
        "test.action",
        "/admin/settings");

    [Fact]
    public void PutsTheTextsInTheLayout()
    {
        var renderer = new EmailRenderer("https://projetos.empresa.example/");

        var email = renderer.Render(Content, "Projetos da Empresa");

        email.Subject.Should().Be("Teste de envio do Projetos da Empresa");
        email.TextBody.Should().Contain("Olá, Marina <Costa>.")
            .And.Contain("Abrir as Configurações gerais: https://projetos.empresa.example/admin/settings");
        email.HtmlBody.Should().Contain("Olá, Marina &lt;Costa&gt;.")
            .And.Contain("href=\"https://projetos.empresa.example/admin/settings\"")
            .And.Contain("IBM Plex Sans")
            .And.NotContain("{{");
    }

    [Fact]
    public void TheColorsComeFromTheDesignSystem()
    {
        var email = new EmailRenderer("https://projetos.empresa.example").Render(Content, "Projetos da Empresa");

        // The button has the action color of the light theme, as the tokens of the design system say.
        email.HtmlBody.Should().MatchRegex("background:#[0-9A-Fa-f]{6};\"><a href");
    }

    [Fact]
    public void WithoutTheSiteAddressThereIsNoButton()
    {
        var email = new EmailRenderer(null).Render(Content, "Projetos da Empresa");

        email.HtmlBody.Should().NotContain("<a href");
        email.TextBody.Should().NotContain("Abrir as Configurações gerais");
    }

    [Fact]
    public void AnUnknownTextIsAnError()
    {
        var render = () => new EmailRenderer(null).Render(Content with { SubjectKey = "test.missing" }, "Projetos");

        render.Should().Throw<KeyNotFoundException>();
    }
}
