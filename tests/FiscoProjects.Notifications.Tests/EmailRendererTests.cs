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

public sealed class NoticeEmailTests
{
    [Fact]
    public void CodesAndDaysAreShownInWords()
    {
        var notice = new FiscoProjects.Notifications.Contracts.Notice(
            FiscoProjects.Notifications.Contracts.NoticeKind.TicketDueSoon,
            [Guid.NewGuid()],
            null,
            Guid.NewGuid(),
            "/tickets/7",
            new Dictionary<string, string>
            {
                ["number"] = "7",
                ["title"] = "Erro no DIFAL",
                ["project"] = "ERP-FIN",
                ["team"] = "Development",
                ["deadline"] = "FirstResponse",
                ["dueOn"] = "2026-10-21",
            });

        var email = new EmailRenderer("https://projetos.empresa.example")
            .Render(NoticeEmails.ContentOf(notice, "Diego Torres", null), "FiscoProjects");

        email.Subject.Should().Be("Prazo vencendo no chamado #7: Erro no DIFAL");
        email.TextBody.Should().Contain("Olá, Diego Torres.")
            .And.Contain("O prazo de primeira resposta do chamado #7, \"Erro no DIFAL\", do projeto ERP-FIN, vence em 21/10/2026.")
            .And.Contain("Abrir o chamado: https://projetos.empresa.example/tickets/7");
    }

    [Fact]
    public void EveryKindHasItsTexts()
    {
        var renderer = new EmailRenderer("https://projetos.empresa.example");
        foreach (var kind in EmailChannel.Immediate)
        {
            var notice = new FiscoProjects.Notifications.Contracts.Notice(
                kind, [Guid.NewGuid()], Guid.NewGuid(), null, "/tickets/1", new Dictionary<string, string>());

            var render = () => renderer.Render(NoticeEmails.ContentOf(notice, "Carla Mendes", "Diego Torres"), "FiscoProjects");

            render.Should().NotThrow($"{kind} has a subject, a body and an action");
        }
    }

    [Fact]
    public void ASummaryHasItsSectionsWithLinks()
    {
        var content = new EmailContent("summary.subject", ["summary.greeting"], new Dictionary<string, string> { ["name"] = "Carla" })
        {
            Sections =
            [
                new EmailSection("summary.sections.tickets",
                [
                    new EmailLine("summary.lines.ticketStatusChanged", new Dictionary<string, string>
                    {
                        ["number"] = "1", ["title"] = "Rejeição 539", ["from"] = "Open", ["to"] = "Triage", ["actor"] = "Rafael Souza",
                    }, "/tickets/1"),
                ]),
            ],
            NoteKey = "summary.note",
        };

        var email = new EmailRenderer("https://projetos.empresa.example").Render(content, "FiscoProjects");

        email.TextBody.Should().Contain("Chamados que você acompanha\n- #1 Rejeição 539: Aberto → Em triagem, por Rafael Souza: https://projetos.empresa.example/tickets/1");
        email.HtmlBody.Should().Contain("<a href=\"https://projetos.empresa.example/tickets/1\"");
        email.TextBody.Should().Contain("desligue em Meu perfil");
    }
}
