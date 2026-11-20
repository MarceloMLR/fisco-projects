using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using GerenWare.Projects.Contracts;
using GerenWare.Testing;
using Microsoft.Extensions.DependencyInjection;
using static GerenWare.Api.Tests.ProjectTeam;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace GerenWare.Api.Tests;

// Spec: relatorio-de-status (API side), requirements "Quem gera o relatório", "Rascunho do relatório", "Textos e riscos
// do relatório", "Riscos no relatório", "Horas no relatório" and "Relatório guardado nos documentos"; horas-do-projeto,
// "Horas do projeto no relatório de status" (ADR-0165, ADR-0166).
public sealed class StatusReportTests(SqlServerFixture sqlServer)
{
    private const string Reports = "Relatórios de status";
    private const string FileName = "ERP-FIN - Relatório de status - 2026-10-19.pptx";

    /// <summary>
    /// The team of ERP-FIN (<see cref="ProjectTeam"/>) with the phase Testes: the task Executar testes, then the milestone
    /// Homologação; and the ticket Carla opened, linked by Marina to the task, so it threatens the milestone.
    /// </summary>
    private sealed record Scene(ProjectTeam Team, HttpClient Marina, Guid Task, int Ticket) : IAsyncDisposable
    {
        public string Path => $"/api/projects/{Team.Erp}/status-report";

        public ValueTask DisposeAsync() => Team.DisposeAsync();
    }

    private async Task<Scene> StartAsync()
    {
        var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        var schedule = $"/api/projects/{team.Erp}/schedule";
        var phase = await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"{schedule}/phases", new { name = "Testes" }));
        async Task<Guid> AddAsync(string kind, string name, int duration, Guid[] after) =>
            await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"{schedule}/items", new
            {
                kind,
                phaseId = phase,
                name,
                start = "2026-10-26",
                duration,
                ownerId = (Guid?)null,
                progress = 0,
                predecessorIds = after,
            }));
        var task = await AddAsync("Task", "Executar testes", 5, []);
        await AddAsync("Milestone", "Homologação", 0, [task]);
        var ticket = await OpenAsync(team, "Rejeição 539 ao emitir nota de devolução");
        var linked = await SendAsync(marina, HttpMethod.Put, $"/api/tickets/{ticket}/impacts", new { itemIds = new[] { task } });
        linked.StatusCode.Should().Be(HttpStatusCode.NoContent, await linked.Content.ReadAsStringAsync(Cancellation));
        return new Scene(team, marina, task, ticket);
    }

    private static async Task<int> OpenAsync(ProjectTeam team, string title)
    {
        var carla = await team.ClientOfAsync(team.Carla);
        var opened = await SendAsync(carla, HttpMethod.Post, "/api/tickets", new
        {
            projectId = team.Erp,
            team = "Development",
            priority = "High",
            title,
            description = "A SEFAZ retorna a rejeição.",
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        return (await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
    }

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static object Body(
        string summary = "Cadastros validados", object[]? risks = null, bool showHours = false) =>
        new
        {
            summary,
            focus = "Integração fiscal",
            nextSteps = "Homologar a emissão",
            clientNeeds = "Aprovar o roteiro de testes",
            risks = risks ?? [],
            showHours,
        };

    private static object RiskOf(int number, string level = "High", string attention = "Depende do retorno do fornecedor") =>
        new { number, level, attention };

    private static async Task<JsonElement> GeneratedAsync(HttpClient client, Scene scene, object body)
    {
        var generated = await SendAsync(client, HttpMethod.Post, scene.Path, body);
        generated.StatusCode.Should().Be(HttpStatusCode.OK, await generated.Content.ReadAsStringAsync(Cancellation));
        return await generated.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
    }

    private static Task<JsonElement> DraftAsync(HttpClient client, Scene scene) =>
        client.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation);

    /// <summary>The text of each slide of the report kept, as the PowerPoint has it.</summary>
    private static async Task<List<string>> SlidesOfAsync(HttpClient client, Scene scene, JsonElement report)
    {
        var content = await client.GetByteArrayAsync(
            $"/api/projects/{scene.Team.Erp}/documents/{report.GetProperty("documentId").GetGuid()}/content?download=true", Cancellation);
        using var document = PresentationDocument.Open(new MemoryStream(content), false);
        var presentation = document.PresentationPart!;
        return [.. presentation.Presentation!.SlideIdList!.Elements<P.SlideId>()
            .Select(id => ((SlidePart)presentation.GetPartById(id.RelationshipId!)).Slide!)
            .Select(slide => string.Join("\n", slide.Descendants<A.Paragraph>().Select(paragraph => paragraph.InnerText)))];
    }

    [Theory(DisplayName = "Quem gera o relatório por papel")]
    [InlineData("Administrator", HttpStatusCode.OK)]
    [InlineData("Manager", HttpStatusCode.OK)]
    [InlineData("TechLead", HttpStatusCode.Forbidden)]
    [InlineData("Contributor", HttpStatusCode.Forbidden)]
    [InlineData("Developer", HttpStatusCode.Forbidden)]
    [InlineData("Viewer", HttpStatusCode.Forbidden)]
    [InlineData("Outside", HttpStatusCode.NotFound)]
    public async Task OnlyTheManagers_GenerateTheReport(string role, HttpStatusCode expected)
    {
        await using var scene = await StartAsync();
        var client = await scene.Team.ClientOfAsync(role);

        var draft = await client.GetAsync(scene.Path, Cancellation);
        var generated = await SendAsync(client, HttpMethod.Post, scene.Path, Body());

        (draft.StatusCode, generated.StatusCode).Should().Be((expected, expected));
        var project = await client.GetAsync($"/api/projects/{scene.Team.Erp}", Cancellation);
        if (project.IsSuccessStatusCode)
        {
            (await project.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("permissions")
                .GetProperty("canGenerateStatusReport").GetBoolean().Should().Be(expected == HttpStatusCode.OK);
        }

        (await scene.Team.App.CountAsync("SELECT COUNT(*) FROM [documents].[documents] WHERE ProjectId = @project", ("@project", scene.Team.Erp)))
            .Should().Be(expected == HttpStatusCode.OK ? 1 : 0);
    }

    [Fact(DisplayName = "Relatório de projeto encerrado")]
    public async Task ClosedProject_StillGetsItsReport()
    {
        await using var scene = await StartAsync();
        var cancelled = await SendAsync(scene.Marina, HttpMethod.Put, $"/api/projects/{scene.Team.Erp}/status", new { status = "Cancelled" });
        cancelled.StatusCode.Should().Be(HttpStatusCode.NoContent, await cancelled.Content.ReadAsStringAsync(Cancellation));

        var report = await GeneratedAsync(scene.Marina, scene, Body());

        report.GetProperty("name").GetString().Should().Be(FileName);
        (await SlidesOfAsync(scene.Marina, scene, report))[0].Should().Contain("Cancelado");
    }

    [Fact(DisplayName = "Rascunho sem relatório anterior")]
    public async Task FirstDraft_IsEmpty_AndSuggestsTheTicketThatThreatensAMilestone()
    {
        await using var scene = await StartAsync();
        var other = await OpenAsync(scene.Team, "Dúvida sobre o relatório fiscal");

        var draft = await DraftAsync(scene.Marina, scene);

        draft.GetProperty("last").ValueKind.Should().Be(JsonValueKind.Null);
        new[] { "summary", "focus", "nextSteps", "clientNeeds" }.Select(name => draft.GetProperty(name).GetString())
            .Should().OnlyContain(text => text == string.Empty);
        draft.GetProperty("tickets").EnumerateArray()
            .Select(ticket => (ticket.GetProperty("number").GetInt32(), ticket.GetProperty("threatens").GetBoolean()))
            .Should().Equal((scene.Ticket, true), (other, false));
        var risk = draft.GetProperty("risks").EnumerateArray().Should().ContainSingle().Subject;
        (risk.GetProperty("number").GetInt32(), risk.GetProperty("level").GetString(), risk.GetProperty("attention").GetString())
            .Should().Be((scene.Ticket, "Medium", string.Empty));
    }

    [Fact(DisplayName = "Textos do último relatório")]
    public async Task NextDraft_HasTheTextsAndTheRisksOfTheLastReport()
    {
        await using var scene = await StartAsync();
        await GeneratedAsync(scene.Marina, scene, Body(risks: [RiskOf(scene.Ticket)]));

        var draft = await DraftAsync(scene.Marina, scene);

        draft.GetProperty("summary").GetString().Should().Be("Cadastros validados");
        draft.GetProperty("clientNeeds").GetString().Should().Be("Aprovar o roteiro de testes");
        draft.GetProperty("last").GetProperty("generatedBy").GetProperty("name").GetString().Should().Be("Marina Costa");
        var risk = draft.GetProperty("risks").EnumerateArray().Should().ContainSingle().Subject;
        (risk.GetProperty("level").GetString(), risk.GetProperty("attention").GetString())
            .Should().Be(("High", "Depende do retorno do fornecedor"));
    }

    [Fact(DisplayName = "Risco do último relatório já resolvido")]
    public async Task ResolvedTicket_LeavesTheDraft()
    {
        await using var scene = await StartAsync();
        await GeneratedAsync(scene.Marina, scene, Body(risks: [RiskOf(scene.Ticket)]));
        var diego = await scene.Team.ClientOfAsync(scene.Team.Diego);
        foreach (var action in new[] { "StartTriage", "StartWork", "Resolve" })
        {
            var taken = await SendAsync(diego, HttpMethod.Post, $"/api/tickets/{scene.Ticket}/transitions", new { action });
            taken.StatusCode.Should().Be(HttpStatusCode.NoContent, await taken.Content.ReadAsStringAsync(Cancellation));
        }

        var draft = await DraftAsync(scene.Marina, scene);

        draft.GetProperty("risks").GetArrayLength().Should().Be(0);
        draft.GetProperty("tickets").GetArrayLength().Should().Be(0);
        var refused = await SendAsync(scene.Marina, HttpMethod.Post, scene.Path, Body(risks: [RiskOf(scene.Ticket)]));
        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Chamado fechado como risco")]
    public async Task RiskThatIsNoOpenTicket_IsRefusedInTheRisks()
    {
        await using var scene = await StartAsync();

        var refused = await SendAsync(scene.Marina, HttpMethod.Post, scene.Path, Body(risks: [RiskOf(scene.Ticket + 100)]));

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await refused.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").TryGetProperty("risks", out _).Should().BeTrue();
        (await scene.Team.App.CountAsync("SELECT COUNT(*) FROM [documents].[documents] WHERE ProjectId = @project", ("@project", scene.Team.Erp)))
            .Should().Be(0);
    }

    [Fact(DisplayName = "Riscos demais")]
    public async Task ElevenRisks_AreRefused()
    {
        await using var scene = await StartAsync();

        var refused = await SendAsync(
            scene.Marina, HttpMethod.Post, scene.Path, Body(risks: [.. Enumerable.Range(0, 11).Select(index => RiskOf(scene.Ticket))]));

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await refused.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").TryGetProperty("risks", out _).Should().BeTrue();
    }

    [Fact(DisplayName = "Texto longo demais")]
    public async Task LongSummary_IsRefusedInItsField()
    {
        await using var scene = await StartAsync();

        var refused = await SendAsync(scene.Marina, HttpMethod.Post, scene.Path, Body(summary: new string('a', 1001)));

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await refused.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").TryGetProperty("summary", out _).Should().BeTrue();
    }

    [Fact(DisplayName = "Relatório guardado na pasta dos relatórios")]
    public async Task Report_IsKeptInTheFolderOfTheReports()
    {
        await using var scene = await StartAsync();

        var report = await GeneratedAsync(scene.Marina, scene, Body());

        (report.GetProperty("name").GetString(), report.GetProperty("folderName").GetString(), report.GetProperty("version").GetInt32())
            .Should().Be((FileName, Reports, 1));
        var items = (await scene.Marina.GetFromJsonAsync<JsonElement>(
            $"/api/projects/{scene.Team.Erp}/documents?folderId={report.GetProperty("folderId").GetGuid()}", Cancellation)).GetProperty("items");
        var item = items.EnumerateArray().Should().ContainSingle().Subject;
        item.GetProperty("name").GetString().Should().Be(FileName);
        item.GetProperty("updated").GetProperty("name").GetString().Should().Be("Marina Costa");
        var slides = await SlidesOfAsync(scene.Marina, scene, report);
        slides.Should().HaveCount(3);
        slides[0].Should().Contain("ERP-FIN · Implantação do ERP financeiro").And.Contain("Cadastros validados").And.Contain("Marina Costa");
        slides[1].Should().Contain("Testes").And.Contain("Homologação");
    }

    [Fact(DisplayName = "Relatório gerado de novo no mesmo dia")]
    public async Task ReportGeneratedAgainOnTheSameDay_IsANewVersion()
    {
        await using var scene = await StartAsync();

        var first = await GeneratedAsync(scene.Marina, scene, Body());
        var second = await GeneratedAsync(scene.Team.Admin, scene, Body(summary: "Testes iniciados"));

        second.GetProperty("documentId").GetGuid().Should().Be(first.GetProperty("documentId").GetGuid());
        second.GetProperty("version").GetInt32().Should().Be(2);
        (await SlidesOfAsync(scene.Marina, scene, second))[0].Should().Contain("Testes iniciados");
    }

    [Fact(DisplayName = "Risco com chamado no fornecedor")]
    public async Task RiskSlide_HasTheVendorAndTheMilestonesThatDependOnTheTicket()
    {
        await using var scene = await StartAsync();
        var vendor = await IdOfAsync(await SendAsync(scene.Team.Admin, HttpMethod.Post, "/api/admin/vendors", new { name = "Fornecedor Fictício" }));
        var registered = await SendAsync(scene.Marina, HttpMethod.Post, $"/api/tickets/{scene.Ticket}/vendor-tickets", new
        {
            vendorId = vendor,
            code = "77812",
            openedOn = "2026-10-19",
            status = "InAnalysis",
            link = "https://suporte.example/77812",
            note = (string?)null,
        });
        registered.StatusCode.Should().Be(HttpStatusCode.Created, await registered.Content.ReadAsStringAsync(Cancellation));

        var report = await GeneratedAsync(scene.Marina, scene, Body(risks: [RiskOf(scene.Ticket)]));

        var slides = await SlidesOfAsync(scene.Marina, scene, report);
        slides.Should().HaveCount(4);
        slides[3].Should().Contain($"#{scene.Ticket} · Rejeição 539 ao emitir nota de devolução").And.Contain("Aberto").And.Contain("Alta");
        slides[3].Should().Contain("Fornecedor Fictício · 77812 · Em análise").And.Contain("Homologação").And.Contain("Depende do retorno do fornecedor");
    }

    [Fact(DisplayName = "Relatório sem horas")]
    public async Task ReportWithoutHours_HasNoSlideOfTheHours()
    {
        await using var scene = await StartAsync();
        (await SendAsync(scene.Team.Admin, HttpMethod.Put, $"/api/admin/project-hours/{scene.Team.Erp}/contracted", new { hours = 200 }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var slides = await SlidesOfAsync(scene.Marina, scene, await GeneratedAsync(scene.Marina, scene, Body()));

        string.Join("\n", slides).Should().NotContain("Horas do projeto").And.NotContain("200 h");
    }

    [Fact(DisplayName = "Saldo do projeto no relatório pelo gerente")]
    public async Task Manager_SeesTheBalanceOnlyThroughTheReport()
    {
        await using var scene = await StartAsync();
        (await SendAsync(scene.Team.Admin, HttpMethod.Put, $"/api/admin/project-hours/{scene.Team.Erp}/contracted", new { hours = 200 }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        await LogAsync(scene, scene.Team.Diego, 6, scene.Task);
        await LogAsync(scene, scene.Team.Carla, 2, null);

        var slides = await SlidesOfAsync(scene.Marina, scene, await GeneratedAsync(scene.Marina, scene, Body(showHours: true)));

        var hours = slides.Last();
        hours.Should().Contain("Horas do projeto").And.Contain("200 h").And.Contain("8 h").And.Contain("192 h").And.Contain("Dentro das horas");
        hours.Should().Contain("Executar testes").And.Contain("2 h apontadas fora das tarefas do cronograma");
        hours.Should().NotContain("Diego Torres").And.NotContain("Carla Mendes");
        (await scene.Marina.GetAsync("/api/admin/project-hours", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Horas do relatório sem as pessoas")]
    public async Task HoursForTheReport_HaveOnlyTheTotals()
    {
        await using var scene = await StartAsync();
        await LogAsync(scene, scene.Team.Diego, 6, scene.Task);
        await LogAsync(scene, scene.Team.Carla, 2, scene.Task);
        await LogAsync(scene, scene.Team.Rafael, 1, null);
        await using var services = scene.Team.App.Services.CreateAsyncScope();
        var source = services.ServiceProvider.GetRequiredService<IProjectReportHours>();

        var hours = await source.ForReportAsync(scene.Team.Erp, scene.Team.Paulo, isAdministrator: true, Cancellation);
        var refused = await source.ForReportAsync(scene.Team.Erp, scene.Team.Rafael, isAdministrator: false, Cancellation);

        hours.Should().NotBeNull();
        (hours!.Contracted, hours.Logged, hours.Outside, hours.State).Should().Be(((decimal?)null, 9m, 1m, ReportHoursState.NoContract));
        hours.Tasks.Should().ContainSingle().Which.Should().Be(new ReportTaskHours(scene.Task, "Executar testes", null, 8m));
        refused.Should().BeNull();
    }

    private static async Task LogAsync(Scene scene, Guid person, decimal hours, Guid? task)
    {
        var client = await scene.Team.ClientOfAsync(person);
        var logged = await SendAsync(client, HttpMethod.Post, "/api/time-entries", new
        {
            projectId = scene.Team.Erp,
            day = "2026-10-19",
            hours,
            description = "Trabalho no projeto",
            scheduleItemId = task,
            ticketId = (Guid?)null,
        });
        logged.StatusCode.Should().Be(HttpStatusCode.Created, await logged.Content.ReadAsStringAsync(Cancellation));
    }
}
