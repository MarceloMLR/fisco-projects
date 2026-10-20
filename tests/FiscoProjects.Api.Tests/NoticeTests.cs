using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Notifications.Application;
using FiscoProjects.Testing;
using FiscoProjects.Tickets.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace FiscoProjects.Api.Tests;

// Spec: avisos (API side), requirements "Quem recebe os avisos", "Avisos dos chamados na hora", "Alertas de prazo dos
// chamados", "Resumo dos chamados", "Resumo dos projetos" and "Resumo diário" (ADR-0037, ADR-0038).
public sealed class NoticeTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>
    /// ERP-FIN, created by Paulo (the Administrator) and managed by Marina, with Rafael as tech lead, Carla and Beatriz as
    /// contributors and Diego as developer; Carla opens #1 for Desenvolvimento, with priority High (the first answer due
    /// on Tuesday, 20/10, and the resolution on Wednesday, 21/10). Monday, 19/10/2026, 06:00 in Brasília.
    /// </summary>
    private sealed record Scene(
        SessionApp App, HttpClient Admin, Guid Marina, Guid Rafael, Guid Carla, Guid Beatriz, Guid Diego, Guid Erp, int Number)
        : IAsyncDisposable
    {
        public string Path => $"/api/tickets/{Number}";

        public ValueTask DisposeAsync() => App.DisposeAsync();
    }

    private const string MarinaEmail = "marina.costa@empresa.example";
    private const string RafaelEmail = "rafael.souza@empresa.example";
    private const string CarlaEmail = "carla.mendes@empresa.example";
    private const string BeatrizEmail = "beatriz.lima@empresa.example";
    private const string DiegoEmail = "diego.torres@empresa.example";

    private async Task<Scene> StartAsync(bool openTicket = true)
    {
        var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        var rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.User);
        var carla = await app.CreateUserAsync("Carla Mendes", SystemRole.User);
        var beatriz = await app.CreateUserAsync("Beatriz Lima", SystemRole.User);
        var diego = await app.CreateUserAsync("Diego Torres", SystemRole.User);
        var created = await SendAsync(admin, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-FIN",
            name = "Implantação do ERP financeiro",
            managerId = marina,
            techLeadId = rafael,
        });
        var erp = (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        await app.AddProjectMemberAsync(erp, carla, "Contributor");
        await app.AddProjectMemberAsync(erp, beatriz, "Contributor");
        await app.AddProjectMemberAsync(erp, diego, "Developer");
        var number = 0;
        if (openTicket)
        {
            using var asCarla = await app.SignInAsync(carla);
            number = await OpenTicketAsync(asCarla, erp, "Rejeição 539 ao emitir nota de devolução");
        }

        return new Scene(app, admin, marina, rafael, carla, beatriz, diego, erp, number);
    }

    [Fact(DisplayName = "Chamado aberto para o time")]
    public async Task OpenedTicket_TellsWhoServesTheTeam()
    {
        await using var scene = await StartAsync();

        var sent = await scene.App.EmailsAsync();

        var opened = sent.Where(email => email.Subject == "Chamado novo #1: Rejeição 539 ao emitir nota de devolução").ToList();
        opened.Select(email => email.ToAddress).Should().BeEquivalentTo([RafaelEmail, DiegoEmail]);
        opened[0].TextBody.Should().Contain("Carla Mendes abriu o chamado #1").And.Contain("ERP-FIN").And.Contain("Desenvolvimento");
    }

    [Fact(DisplayName = "Link para o item")]
    public async Task TheEmail_LeadsToTheTicket()
    {
        await using var scene = await StartAsync();

        var sent = await scene.App.EmailsToAsync(DiegoEmail);

        sent.Should().ContainSingle().Which.HtmlBody.Should().Contain("href=\"http://localhost:5173/tickets/1\"");
    }

    [Fact(DisplayName = "Pessoa desativada não recebe")]
    public async Task InactivePerson_HearsNothing()
    {
        await using var scene = await StartAsync();
        await scene.App.DeactivateAsync(scene.Diego);
        using var carla = await scene.App.SignInAsync(scene.Carla);

        await OpenTicketAsync(carla, scene.Erp, "Erro no cálculo do DIFAL");

        var second = (await scene.App.EmailsAsync()).Where(email => email.Subject.StartsWith("Chamado novo #2", StringComparison.Ordinal));
        second.Select(email => email.ToAddress).Should().Equal(RafaelEmail);
    }

    [Fact(DisplayName = "Resposta no chamado")]
    public async Task Answer_TellsTheRequester()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);

        await AnswerAsync(diego, scene.Path, Doc(Paragraph(Text("Vou olhar o XML."))));

        var carla = await scene.App.EmailsToAsync(CarlaEmail);
        carla.Should().ContainSingle().Which.Subject.Should().Be("Nova mensagem no chamado #1: Rejeição 539 ao emitir nota de devolução");
        (await scene.App.EmailsToAsync(DiegoEmail)).Should().ContainSingle("Diego got only the e-mail of the opening");
    }

    [Fact(DisplayName = "Quem fez a ação não recebe")]
    public async Task WhoActs_HearsNothingOfIt()
    {
        await using var scene = await StartAsync();
        using (var diego = await scene.App.SignInAsync(scene.Diego))
        {
            await AnswerAsync(diego, scene.Path, Doc(Paragraph(Text("Vou olhar o XML."))));
        }

        using var carla = await scene.App.SignInAsync(scene.Carla);
        await AnswerAsync(carla, scene.Path, Doc(Paragraph(Text("Segue o XML."))));

        (await scene.App.EmailsToAsync(DiegoEmail)).Select(email => email.Subject).Should().BeEquivalentTo([
            "Chamado novo #1: Rejeição 539 ao emitir nota de devolução",
            "Nova mensagem no chamado #1: Rejeição 539 ao emitir nota de devolução"]);
        (await scene.App.EmailsToAsync(CarlaEmail)).Should().ContainSingle("Carla got only the answer of Diego");
    }

    [Fact(DisplayName = "Menção")]
    public async Task Mention_IsTheOnlyEmailOfTheMessage()
    {
        await using var scene = await StartAsync();
        using (var diego = await scene.App.SignInAsync(scene.Diego))
        {
            await AnswerAsync(diego, scene.Path, Doc(Paragraph(Text("Vou olhar o XML."))));
        }

        using var carla = await scene.App.SignInAsync(scene.Carla);
        await AnswerAsync(carla, scene.Path, Doc(Paragraph(Mention(scene.Marina, "Marina Costa"), Text(" pode priorizar?"))));

        (await scene.App.EmailsToAsync(MarinaEmail)).Should().ContainSingle()
            .Which.Subject.Should().Be("Carla Mendes mencionou você no chamado #1");
        (await scene.App.EmailsToAsync(DiegoEmail)).Should().Contain(email => email.Subject.StartsWith("Nova mensagem no chamado #1", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Pedido de informação")]
    public async Task AskingForInformation_TellsTheRequester_NotAsAMessage()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);
        await AnswerAsync(diego, scene.Path, Doc(Paragraph(Text("Vou olhar o XML."))));

        await TakenAsync(diego, scene.Path, "AskForInformation", "Pode mandar o XML da nota?");

        (await scene.App.EmailsToAsync(CarlaEmail)).Select(email => email.Subject).Should().BeEquivalentTo([
            "Nova mensagem no chamado #1: Rejeição 539 ao emitir nota de devolução",
            "Informações pedidas no chamado #1: Rejeição 539 ao emitir nota de devolução"]);
    }

    [Fact(DisplayName = "Chamado resolvido")]
    public async Task Resolution_AsksTheRequesterToConfirm()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);
        await AnswerAsync(diego, scene.Path, Doc(Paragraph(Text("Ajustei o cadastro."))));

        await TakenAsync(diego, scene.Path, "Resolve");

        var resolved = (await scene.App.EmailsToAsync(CarlaEmail))
            .Single(email => email.Subject == "Chamado #1 resolvido: Rejeição 539 ao emitir nota de devolução");
        resolved.TextBody.Should().Contain("Diego Torres resolveu o chamado #1").And.Contain("confirme");
    }

    [Fact(DisplayName = "Chamado atribuído")]
    public async Task Assignment_TellsTheNewAssignee()
    {
        await using var scene = await StartAsync();
        using var marina = await scene.App.SignInAsync(scene.Marina);

        (await SendAsync(marina, HttpMethod.Put, "/api/tickets/assignee", new { numbers = new[] { 1 }, assigneeId = scene.Diego }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await scene.App.EmailsToAsync(DiegoEmail)).Select(email => email.Subject)
            .Should().Contain("Chamado #1 atribuído a você: Rejeição 539 ao emitir nota de devolução");
    }

    [Fact(DisplayName = "Novo solicitante")]
    public async Task NewRequester_HearsAboutIt()
    {
        await using var scene = await StartAsync();
        using var marina = await scene.App.SignInAsync(scene.Marina);

        (await SendAsync(marina, HttpMethod.Put, "/api/tickets/requester", new { numbers = new[] { 1 }, requesterId = scene.Beatriz }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await scene.App.EmailsToAsync(BeatrizEmail)).Should().ContainSingle()
            .Which.Subject.Should().Be("Você é o solicitante do chamado #1: Rejeição 539 ao emitir nota de devolução");
    }

    [Fact(DisplayName = "Chamado transferido")]
    public async Task Transfer_TellsWhoServesTheNewTeam()
    {
        await using var scene = await StartAsync();
        using var marina = await scene.App.SignInAsync(scene.Marina);

        (await SendAsync(marina, HttpMethod.Put, $"{scene.Path}/team", new { team = "Functional" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var transferred = (await scene.App.EmailsAsync()).Where(email => email.Subject.StartsWith("Chamado #1 transferido", StringComparison.Ordinal));
        transferred.Select(email => email.ToAddress).Should().BeEquivalentTo([CarlaEmail, BeatrizEmail]);
    }

    [Fact(DisplayName = "Quem saiu do projeto não recebe")]
    public async Task WhoLeftTheProject_HearsNothing()
    {
        await using var scene = await StartAsync();
        using (var diego = await scene.App.SignInAsync(scene.Diego))
        {
            // Diego answers, so he is the assignee and follows the ticket.
            await AnswerAsync(diego, scene.Path, Doc(Paragraph(Text("Vou olhar o XML."))));
        }

        (await SendAsync(scene.Admin, HttpMethod.Delete, $"/api/projects/{scene.Erp}/members/{scene.Diego}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var carla = await scene.App.SignInAsync(scene.Carla);
        await AnswerAsync(carla, scene.Path, Doc(Paragraph(Text("Alguém pode olhar?"))));

        (await scene.App.EmailsToAsync(DiegoEmail)).Should().ContainSingle("Diego got only the e-mail of the opening");
    }

    [Fact(DisplayName = "Prazo vencendo")]
    public async Task ResolutionDueTomorrow_TellsTheAssigneeOnce()
    {
        await using var scene = await StartAsync();
        using (var diego = await scene.App.SignInAsync(scene.Diego))
        {
            await AnswerAsync(diego, scene.Path, Doc(Paragraph(Text("Vou olhar o XML."))));
        }

        // Tuesday, 20/10: the resolution is due on Wednesday, the next business day.
        scene.App.Clock.Advance(TimeSpan.FromDays(1));
        await scene.App.EmailsAsync(emails => emails.Any(IsDueSoon));
        scene.App.Clock.Advance(TimeSpan.FromHours(2));
        var again = await AlertsAsync(scene.App);

        again.Should().Be(0);
        var dueSoon = (await scene.App.EmailsAsync()).Where(IsDueSoon).ToList();
        dueSoon.Should().ContainSingle().Which.ToAddress.Should().Be(DiegoEmail);
        dueSoon[0].TextBody.Should().Contain("O prazo de resolução do chamado #1").And.Contain("vence em 21/10/2026");

        static bool IsDueSoon(EmailMessage email) => email.Subject.StartsWith("Prazo vencendo no chamado #1", StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Prazo vencido sem responsável")]
    public async Task FirstAnswerOverdue_TellsWhoServesTheTeam()
    {
        await using var scene = await StartAsync();

        // Wednesday, 21/10: nobody answered, and the first answer was due on Tuesday.
        scene.App.Clock.Advance(TimeSpan.FromDays(2));
        var sent = await scene.App.EmailsAsync(emails => emails.Count(IsOverdue) >= 2);

        sent.Where(IsOverdue).Select(email => email.ToAddress).Should().BeEquivalentTo([RafaelEmail, DiegoEmail]);
        sent.First(IsOverdue).TextBody.Should().Contain("O prazo de primeira resposta do chamado #1").And.Contain("venceu em 20/10/2026");

        static bool IsOverdue(EmailMessage email) => email.Subject.StartsWith("Prazo vencido no chamado #1", StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Prazo pausado não avisa")]
    public async Task WaitingForTheRequester_TellsNoDeadline()
    {
        await using var scene = await StartAsync();
        using (var diego = await scene.App.SignInAsync(scene.Diego))
        {
            await AnswerAsync(diego, scene.Path, Doc(Paragraph(Text("Vou olhar o XML."))));
            await TakenAsync(diego, scene.Path, "AskForInformation", "Pode mandar o XML da nota?");
        }

        using (var carla = await scene.App.SignInAsync(scene.Carla))
        {
            // A second ticket nobody answers: its alert shows the round of the alerts is over.
            await OpenTicketAsync(carla, scene.Erp, "Erro no cálculo do DIFAL");
        }

        scene.App.Clock.Advance(TimeSpan.FromDays(3));
        await scene.App.EmailsAsync(emails => emails.Any(email => email.Subject.StartsWith("Prazo vencido no chamado #2", StringComparison.Ordinal)));

        (await scene.App.EmailsAsync()).Should().NotContain(email => email.Subject.Contains("chamado #1:", StringComparison.Ordinal)
            && email.Subject.StartsWith("Prazo", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Mudança de status no resumo")]
    public async Task StatusChange_GoesToTheSummaryOfWhoFollows()
    {
        await using var scene = await StartAsync();
        using (var marina = await scene.App.SignInAsync(scene.Marina))
        {
            (await SendAsync(marina, HttpMethod.Put, $"{scene.Path}/following")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var rafael = await scene.App.SignInAsync(scene.Rafael))
        {
            await TakenAsync(rafael, scene.Path, "StartTriage");
        }

        (await scene.App.EmailsToAsync(MarinaEmail)).Should().NotContain(email => email.Subject.Contains("#1", StringComparison.Ordinal));
        await scene.App.DailySummaryAsync();

        var summary = (await scene.App.EmailsToAsync(MarinaEmail)).Should().ContainSingle(email => IsSummary(email)).Subject;
        summary.TextBody.Should().Contain("Chamados que você acompanha")
            .And.Contain("#1 Rejeição 539 ao emitir nota de devolução: Aberto → Em triagem, por Rafael Souza")
            .And.Contain("http://localhost:5173/tickets/1");
    }

    [Fact(DisplayName = "Fornecedor no resumo")]
    public async Task VendorUpdate_GoesToTheSummaryOfWhoFollows()
    {
        await using var scene = await StartAsync();
        var added = await SendAsync(scene.Admin, HttpMethod.Post, "/api/admin/vendors", new { name = "Emissor Exemplo", portalUrl = (string?)null });
        var vendor = (await added.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        using (var diego = await scene.App.SignInAsync(scene.Diego))
        {
            (await SendAsync(diego, HttpMethod.Post, $"{scene.Path}/vendor-tickets", new
            {
                vendorId = vendor,
                code = "EX-4471",
                openedOn = "2026-10-19",
                status = "InAnalysis",
                link = (string?)null,
                note = (string?)null,
            })).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        await scene.App.DailySummaryAsync();

        var summary = (await scene.App.EmailsToAsync(CarlaEmail)).Should().ContainSingle(email => IsSummary(email)).Subject;
        summary.TextBody.Should().Contain("Diego Torres registrou que o chamado no Emissor Exemplo está em análise");
    }

    [Fact(DisplayName = "Inclusão no projeto")]
    public async Task Inclusion_GoesToTheSummaryOfThePerson()
    {
        await using var scene = await StartAsync(openTicket: false);
        var lucas = await scene.App.CreateUserAsync("Lucas Prado", SystemRole.User);
        using (var marina = await scene.App.SignInAsync(scene.Marina))
        {
            (await SendAsync(marina, HttpMethod.Post, $"/api/projects/{scene.Erp}/members", new { userId = lucas, role = "Viewer" }))
                .StatusCode.Should().Be(HttpStatusCode.Created);
        }

        await scene.App.DailySummaryAsync();

        var summary = (await scene.App.EmailsToAsync("lucas.prado@empresa.example")).Should().ContainSingle().Subject;
        IsSummary(summary).Should().BeTrue();
        summary.TextBody.Should().Contain("Marina Costa incluiu você no projeto ERP-FIN como Leitor");
        // The manager and the tech lead chosen at the creation hear about it too.
        (await scene.App.EmailsToAsync(RafaelEmail)).Single(email => IsSummary(email)).TextBody
            .Should().Contain("Paulo Nogueira incluiu você no projeto ERP-FIN como Líder técnico");
    }

    [Fact(DisplayName = "Status do projeto")]
    public async Task ProjectStatus_GoesToTheSummaryOfTheOthers()
    {
        await using var scene = await StartAsync(openTicket: false);
        using (var marina = await scene.App.SignInAsync(scene.Marina))
        {
            (await SendAsync(marina, HttpMethod.Put, $"/api/projects/{scene.Erp}/status", new { status = "Paused" }))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        var sent = await scene.App.DailySummaryAsync();

        foreach (var address in new[] { CarlaEmail, BeatrizEmail, DiegoEmail, RafaelEmail })
        {
            sent.Single(email => email.ToAddress == address && IsSummary(email)).TextBody
                .Should().Contain("Marina Costa mudou o status do projeto ERP-FIN para Pausado");
        }

        sent.Where(email => email.ToAddress == MarinaEmail)
            .Should().NotContain(email => email.TextBody.Contains("mudou o status", StringComparison.Ordinal), "Marina paused it");
    }

    [Fact(DisplayName = "Datas empurradas em cadeia")]
    public async Task PushedDates_ShowOncePerTask_WithTheNewDates()
    {
        await using var scene = await StartAsync(openTicket: false);
        using var marina = await scene.App.SignInAsync(scene.Marina);
        var path = $"/api/projects/{scene.Erp}/schedule";
        var phase = await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"{path}/phases", new { name = "Configuração" }));
        var first = await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"{path}/items",
            Item(phase, "Parametrizar regras de ICMS", "2026-10-26", 3, scene.Carla)));
        await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"{path}/items",
            Item(phase, "Testar a emissão", "2026-10-29", 2, scene.Diego, after: [first])));

        // Two delays of the first task push the second one twice.
        (await SendAsync(marina, HttpMethod.Put, $"{path}/items/{first}/dates", new { start = "2026-10-27", end = (string?)null }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendAsync(marina, HttpMethod.Put, $"{path}/items/{first}/dates", new { start = "2026-10-28", end = (string?)null }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        await scene.App.DailySummaryAsync();

        var summary = (await scene.App.EmailsToAsync(DiegoEmail)).Single(email => IsSummary(email)).TextBody;
        summary.Should().Contain("Datas alteradas no cronograma");
        summary.Split("Testar a emissão").Should().HaveCount(2, "the task shows once");
        summary.Should().Contain("Testar a emissão, do projeto ERP-FIN: agora de 03/11/2026 a 04/11/2026");
        (await scene.App.EmailsToAsync(CarlaEmail)).Single(email => IsSummary(email)).TextBody
            .Should().Contain("Parametrizar regras de ICMS, do projeto ERP-FIN: agora de 28/10/2026 a 30/10/2026");
    }

    [Fact(DisplayName = "Resumo no horário")]
    public async Task Summary_ComesAtTheTimeOfTheInstallation()
    {
        await using var scene = await StartAsync();
        using (var rafael = await scene.App.SignInAsync(scene.Rafael))
        {
            await TakenAsync(rafael, scene.Path, "StartTriage");
        }

        // 06:00 in Brasília: not the time yet.
        await RunSummaryAsync(scene.App);
        (await scene.App.EmailsAsync()).Should().NotContain(email => IsSummary(email));

        var sent = await scene.App.DailySummaryAsync();

        var summary = sent.Single(email => email.ToAddress == CarlaEmail && IsSummary(email));
        summary.Subject.Should().Be("Seu resumo do dia no FiscoProjects");
        summary.HtmlBody.Should().Contain("href=\"http://localhost:5173/tickets/1\"");
        summary.TextBody.Should().Contain("Para não receber mais o resumo, desligue em Meu perfil");
    }

    [Fact(DisplayName = "Tarefas que vencem")]
    public async Task Summary_ListsTheTasksEndingSoon()
    {
        await using var scene = await StartAsync(openTicket: false);
        using var marina = await scene.App.SignInAsync(scene.Marina);
        var path = $"/api/projects/{scene.Erp}/schedule";
        var phase = await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"{path}/phases", new { name = "Configuração" }));
        // Ends on Tuesday, 20/10, the day after the summary; the other one only on Monday, 26/10.
        await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"{path}/items",
            Item(phase, "Parametrizar regras de ICMS", "2026-10-19", 2, scene.Carla)));
        await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"{path}/items",
            Item(phase, "Testar a emissão", "2026-10-26", 1, scene.Carla)));

        var sent = await scene.App.DailySummaryAsync();

        var summary = sent.Single(email => email.ToAddress == CarlaEmail && IsSummary(email)).TextBody;
        summary.Should().Contain("Tarefas que vencem nos próximos dias")
            .And.Contain("Parametrizar regras de ICMS, do projeto ERP-FIN, termina em 20/10/2026")
            .And.NotContain("Testar a emissão");
    }

    [Fact(DisplayName = "Sem novidades")]
    public async Task WithoutNews_NoSummary()
    {
        await using var scene = await StartAsync(openTicket: false);

        var sent = await scene.App.DailySummaryAsync();

        sent.Where(email => email.ToAddress == BeatrizEmail).Should().BeEmpty();
    }

    [Fact(DisplayName = "Resumo desligado")]
    public async Task SummaryOff_NoSummary_AndNothingPilesUp()
    {
        await using var scene = await StartAsync(openTicket: false);
        using var carla = await scene.App.SignInAsync(scene.Carla);
        (await SendAsync(carla, HttpMethod.Put, "/api/profile/daily-summary", new { receives = false }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        using (var marina = await scene.App.SignInAsync(scene.Marina))
        {
            await SendAsync(marina, HttpMethod.Put, $"/api/projects/{scene.Erp}/status", new { status = "Paused" });
        }

        var first = await scene.App.DailySummaryAsync();
        (await SendAsync(carla, HttpMethod.Put, "/api/profile/daily-summary", new { receives = true }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        var second = await scene.App.DailySummaryAsync();

        first.Should().NotContain(email => email.ToAddress == CarlaEmail);
        first.Should().Contain(email => email.ToAddress == DiegoEmail && IsSummary(email));
        second.Should().NotContain(email => email.ToAddress == CarlaEmail);
        // A day later the session of Carla has expired.
        using var again = await scene.App.SignInAsync(scene.Carla);
        (await again.GetFromJsonAsync<JsonElement>("/api/profile", Cancellation)).GetProperty("receivesDailySummary").GetBoolean()
            .Should().BeTrue();
    }

    [Fact(DisplayName = "Um resumo por dia")]
    public async Task OneSummaryADay()
    {
        await using var scene = await StartAsync(openTicket: false);
        using (var marina = await scene.App.SignInAsync(scene.Marina))
        {
            await SendAsync(marina, HttpMethod.Put, $"/api/projects/{scene.Erp}/status", new { status = "Paused" });
        }

        await scene.App.DailySummaryAsync();
        var again = await RunSummaryAsync(scene.App);

        again.Should().BeNull();
        (await scene.App.EmailsToAsync(DiegoEmail)).Count(email => IsSummary(email)).Should().Be(1);
    }

    private static async Task<int> AlertsAsync(SessionApp app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AlertTicketDeadlines>().ExecuteAsync(Cancellation);
    }

    private static bool IsSummary(EmailMessage email) => email.Subject.StartsWith("Seu resumo do dia", StringComparison.Ordinal);

    private static async Task<int?> RunSummaryAsync(SessionApp app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SendDailySummaries>().ExecuteAsync(Cancellation);
    }

    private static async Task<int> OpenTicketAsync(HttpClient client, Guid project, string title)
    {
        var opened = await SendAsync(client, HttpMethod.Post, "/api/tickets", new
        {
            projectId = project,
            team = "Development",
            priority = "High",
            title,
            description = "A SEFAZ retorna a rejeição 539.",
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        return (await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
    }

    private static object Text(string text) => new { type = "text", text };

    private static object Mention(Guid id, string label) => new { type = "mention", attrs = new { id, label } };

    private static object Paragraph(params object[] content) => new { type = "paragraph", content };

    private static string Doc(params object[] content) => JsonSerializer.Serialize(new { type = "doc", content });

    private static async Task AnswerAsync(HttpClient client, string path, string body)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(body, Encoding.UTF8), "body");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{path}/messages") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        var sent = await client.SendAsync(request, Cancellation);
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
    }

    private static async Task TakenAsync(HttpClient client, string path, string action, string? text = null)
    {
        var taken = await SendAsync(client, HttpMethod.Post, $"{path}/transitions", new
        {
            action,
            body = text is null ? null : Doc(Paragraph(Text(text))),
        });
        taken.StatusCode.Should().Be(HttpStatusCode.NoContent, await taken.Content.ReadAsStringAsync(Cancellation));
    }

    private static object Item(Guid phase, string name, string start, int duration, Guid owner, Guid[]? after = null) =>
        new { kind = "Task", phaseId = phase, name, start, duration, ownerId = owner, progress = 0, predecessorIds = after ?? [] };

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF", "1");
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request, Cancellation);
    }
}
