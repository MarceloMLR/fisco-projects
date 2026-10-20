using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Notifications.Application;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: termos-de-aceite (API side): "Pedido de aceite", "Resposta ao pedido", "Termo assinado", "Termo anexado pelo
// gestor", "Lembrar e cancelar" and "Permissões nos termos"; documentos, "Permissões nos documentos" (the folder of the
// terms of who signed); avisos, "Avisos dos termos de aceite" (ADR-0035, ADR-0037, ADR-0045, ADR-0093, ADR-0095).
public sealed class AcceptanceTests(SqlServerFixture sqlServer)
{
    private const string Title = "Aceite da homologação";
    private const string Terms = "06 Termos de aceite";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static string Path(Guid project) => $"/api/projects/{project}/acceptances";

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    /// <summary>A small PDF, as a signed term would start.</summary>
    private static byte[] Pdf(string text = "termo") => Encoding.ASCII.GetBytes($"%PDF-1.7\n{text}\n%%EOF");

    /// <summary>The phase "Homologação" with the milestones "Homologação concluída" and "Escopo aprovado", made by the Administrator.</summary>
    private static async Task<(Guid Homologation, Guid Scope)> MilestonesAsync(ProjectTeam team, Guid project)
    {
        var path = $"/api/projects/{project}/schedule";
        var phase = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{path}/phases", new { name = "Homologação" }));
        var homologation = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{path}/items", Milestone(phase, "Homologação concluída", "2026-11-03")));
        var scope = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{path}/items", Milestone(phase, "Escopo aprovado", "2026-10-21")));
        return (homologation, scope);
    }

    private static object Milestone(Guid phase, string name, string start) =>
        new { kind = "Milestone", phaseId = phase, name, start, duration = 0, ownerId = (Guid?)null, progress = 0, predecessorIds = Array.Empty<Guid>() };

    private static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string path, IReadOnlyDictionary<string, string?> fields, string? fileName, byte[]? content)
    {
        using var form = new MultipartFormDataContent();
        foreach (var (name, value) in fields)
        {
            if (value is not null)
            {
                form.Add(new StringContent(value, Encoding.UTF8), name);
            }
        }

        if (fileName is not null && content is not null)
        {
            form.Add(new ByteArrayContent(content), "file", fileName);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = form };
        request.Headers.Add("X-CSRF", "1");
        return await client.SendAsync(request, Cancellation);
    }

    private static Task<HttpResponseMessage> RequestAsync(
        HttpClient client, Guid project, Guid milestone, Guid recipient, string title = Title, string fileName = "termo-homologacao.pdf", byte[]? content = null) =>
        PostFormAsync(client, Path(project), new Dictionary<string, string?>
        {
            ["milestoneId"] = milestone.ToString(),
            ["recipientId"] = recipient.ToString(),
            ["title"] = title,
            ["message"] = "Carla, os testes de homologação terminaram sem pendências. Assine o termo e envie por aqui.",
        }, fileName, content ?? Pdf());

    private static async Task<Guid> RequestedAsync(HttpClient client, Guid project, Guid milestone, Guid recipient, string title = Title) =>
        await IdOfAsync(await RequestAsync(client, project, milestone, recipient, title));

    private static Task<HttpResponseMessage> SignAsync(
        HttpClient client, Guid project, Guid term, string fileName = "termo-homologacao-assinado.pdf", byte[]? content = null, bool onBehalf = false) =>
        PostFormAsync(client, $"{Path(project)}/{term}/{(onBehalf ? "attached" : "signed")}", new Dictionary<string, string?>(), fileName, content ?? Pdf("assinado"));

    private static Task<HttpResponseMessage> AttachAsync(HttpClient client, Guid project, Guid milestone, Guid recipient, string title = "Aceite do escopo") =>
        PostFormAsync(client, $"{Path(project)}/attached", new Dictionary<string, string?>
        {
            ["milestoneId"] = milestone.ToString(),
            ["recipientId"] = recipient.ToString(),
            ["title"] = title,
        }, "termo-escopo-assinado.pdf", Pdf("escopo"));

    private static Task<HttpResponseMessage> RefuseAsync(HttpClient client, Guid project, Guid term, string? reason) =>
        SendAsync(client, HttpMethod.Post, $"{Path(project)}/{term}/refusal", new { reason });

    private static async Task<JsonElement> ListAsync(HttpClient client, Guid project, string query = "") =>
        await client.GetFromJsonAsync<JsonElement>($"{Path(project)}{query}", Cancellation);

    private static async Task<List<JsonElement>> ItemsAsync(HttpClient client, Guid project, string query = "") =>
        [.. (await ListAsync(client, project, query)).GetProperty("items").EnumerateArray()];

    private static async Task<JsonElement> DetailsAsync(HttpClient client, Guid project, Guid term) =>
        await client.GetFromJsonAsync<JsonElement>($"{Path(project)}/{term}", Cancellation);

    private static async Task<string> StatusOfAsync(HttpClient client, Guid project, Guid term) =>
        (await DetailsAsync(client, project, term)).GetProperty("term").GetProperty("status").GetString()!;

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<List<string>> ErrorFieldsAsync(HttpResponseMessage response) =>
        [.. (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").EnumerateObject().Select(field => field.Name)];

    private static async Task<JsonElement> MilestoneOfAsync(HttpClient client, Guid project, Guid milestone) =>
        (await client.GetFromJsonAsync<JsonElement>($"/api/projects/{project}/schedule", Cancellation))
            .GetProperty("phases").EnumerateArray().SelectMany(phase => phase.GetProperty("items").EnumerateArray())
            .Single(item => item.GetProperty("id").GetGuid() == milestone);

    private static async Task<JsonElement> FoldersAsync(HttpClient client, Guid project) =>
        await client.GetFromJsonAsync<JsonElement>($"/api/projects/{project}/documents/folders", Cancellation);

    private static async Task<Guid?> TermsFolderAsync(HttpClient client, Guid project) =>
        (await FoldersAsync(client, project)).GetProperty("folders").EnumerateArray()
            .Where(folder => folder.GetProperty("isAcceptance").GetBoolean())
            .Select(folder => (Guid?)folder.GetProperty("id").GetGuid())
            .SingleOrDefault();

    private static async Task<List<JsonElement>> DocumentsAsync(HttpClient client, Guid project, Guid folder) =>
        [.. (await client.GetFromJsonAsync<JsonElement>($"/api/projects/{project}/documents?folderId={folder}", Cancellation))
            .GetProperty("items").EnumerateArray()];

    /// <summary>The notification of the kind in the bell of the person.</summary>
    private static async Task<JsonElement> NotificationOfAsync(HttpClient client, string kind) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/notifications", Cancellation)).GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("kind").GetString() == kind);

    /// <summary>The daily summary of the person among the e-mails sent.</summary>
    private static EmailMessage SummaryOf(IReadOnlyList<EmailMessage> emails, string address) =>
        emails.Single(email => email.ToAddress == address && email.Subject.StartsWith("Seu resumo do dia", StringComparison.Ordinal));

    [Fact(DisplayName = "Pedir aceite")]
    public async Task Request_IsPending_ForThePerson()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var carla = await team.ClientOfAsync(team.Carla);

        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);

        var term = (await ItemsAsync(marina, team.Erp)).Single();
        (term.GetProperty("id").GetGuid(), term.GetProperty("title").GetString(), term.GetProperty("status").GetString())
            .Should().Be((id, Title, "Pending"));
        (term.GetProperty("milestone").GetProperty("name").GetString(), term.GetProperty("milestone").GetProperty("day").GetString())
            .Should().Be(("Homologação concluída", "2026-11-03"));
        (term.GetProperty("recipient").GetProperty("name").GetString(), term.GetProperty("requested").GetProperty("name").GetString())
            .Should().Be(("Carla Mendes", "Marina Costa"));
        term.GetProperty("isRequest").GetBoolean().Should().BeTrue();
        (await ItemsAsync(carla, team.Erp)).Select(item => item.GetProperty("id").GetGuid()).Should().Equal(id);
        var details = await DetailsAsync(carla, team.Erp, id);
        details.GetProperty("toSign").GetProperty("name").GetString().Should().Be("termo-homologacao.pdf");
        details.GetProperty("message").GetString().Should().StartWith("Carla, os testes");
        details.GetProperty("events").EnumerateArray().Select(item => item.GetProperty("kind").GetString()).Should().Equal("Requested");
        details.GetProperty("actions").GetProperty("canAnswer").GetBoolean().Should().BeTrue();
        var file = await carla.GetAsync($"{Path(team.Erp)}/{id}/file", Cancellation);
        file.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        (await file.Content.ReadAsByteArrayAsync(Cancellation)).Should().Equal(Pdf());
        File.Exists(System.IO.Path.Combine(team.Files, "ERP-FIN - Implantação do ERP financeiro", "Pedidos de aceite", "termo-homologacao.pdf"))
            .Should().BeTrue();
    }

    [Fact(DisplayName = "Pedido sem marco do projeto")]
    public async Task Request_OfATaskOrOfAnotherProject_IsABadRequest()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await MilestonesAsync(team, team.Erp);
        var other = await team.CreateProjectAsync("FISC-SP", "Revisão fiscal da filial SP");
        var (elsewhere, _) = await MilestonesAsync(team, other);
        var path = $"/api/projects/{team.Erp}/schedule";
        var phase = (await team.Admin.GetFromJsonAsync<JsonElement>(path, Cancellation)).GetProperty("phases")[0].GetProperty("id").GetGuid();
        var task = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{path}/items", new
        {
            kind = "Task",
            phaseId = phase,
            name = "Testes de homologação",
            start = "2026-10-19",
            duration = 3,
            ownerId = (Guid?)null,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
        }));
        var marina = await team.ClientOfAsync(team.Marina);

        var ofATask = await RequestAsync(marina, team.Erp, task, team.Carla);
        var ofAnotherProject = await RequestAsync(marina, team.Erp, elsewhere, team.Carla);

        ofATask.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorFieldsAsync(ofATask)).Should().Equal("milestoneId");
        ofAnotherProject.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[acceptance_terms]")).Should().Be(0);
    }

    [Fact(DisplayName = "Termo que não é PDF")]
    public async Task Request_OfAFileThatIsNotAPdf_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);

        var word = await RequestAsync(marina, team.Erp, homologation, team.Carla, fileName: "termo.docx", content: Encoding.ASCII.GetBytes("PK\u0003\u0004"));
        var named = await RequestAsync(marina, team.Erp, homologation, team.Carla, fileName: "termo.pdf", content: Encoding.ASCII.GetBytes("PK\u0003\u0004"));
        var empty = await RequestAsync(marina, team.Erp, homologation, team.Carla, content: []);

        foreach (var (response, reason) in new[] { (word, "NotPdf"), (named, "NotPdf"), (empty, "Empty") })
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
            (problem.GetProperty("type").GetString(), problem.GetProperty("reason").GetString())
                .Should().Be(("urn:fiscoprojects:problem:acceptance-file-refused", reason));
        }

        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[acceptance_terms]")).Should().Be(0);
        Directory.Exists(System.IO.Path.Combine(team.Files, "ERP-FIN - Implantação do ERP financeiro", "Pedidos de aceite")).Should().BeFalse();
    }

    [Fact(DisplayName = "Pedido para quem não está no projeto")]
    public async Task Request_OfSomeoneOutside_IsABadRequest()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);

        var outside = await RequestAsync(marina, team.Erp, homologation, team.Sofia);
        var nobody = await PostFormAsync(marina, Path(team.Erp), new Dictionary<string, string?>
        {
            ["milestoneId"] = homologation.ToString(),
            ["title"] = " ",
        }, null, null);

        outside.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorFieldsAsync(outside)).Should().Equal("recipientId");
        (await ErrorFieldsAsync(nobody)).Should().BeEquivalentTo("recipientId", "title", "file");
        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[acceptance_terms]")).Should().Be(0);
    }

    [Fact(DisplayName = "Enviar o termo assinado")]
    public async Task SignedTerm_FromThePerson_SignsTheRequest()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var carla = await team.ClientOfAsync(team.Carla);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);
        team.App.Clock.Advance(TimeSpan.FromHours(2));

        var signed = await SignAsync(carla, team.Erp, id);

        signed.StatusCode.Should().Be(HttpStatusCode.NoContent, await signed.Content.ReadAsStringAsync(Cancellation));
        var details = await DetailsAsync(marina, team.Erp, id);
        details.GetProperty("term").GetProperty("status").GetString().Should().Be("Signed");
        var answered = details.GetProperty("term").GetProperty("answered");
        (answered.GetProperty("name").GetString(), answered.GetProperty("at").GetDateTime())
            .Should().Be(("Carla Mendes", team.App.Clock.GetUtcNow().UtcDateTime));
        details.GetProperty("events").EnumerateArray().Select(item => (item.GetProperty("kind").GetString(), item.GetProperty("actor").GetProperty("name").GetString()))
            .Should().Equal(("Requested", "Marina Costa"), ("Signed", "Carla Mendes"));
        details.GetProperty("signed").GetProperty("name").GetString().Should().Be("termo-homologacao-assinado.pdf");
        details.GetProperty("actions").GetProperty("canCancel").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "Termo assinado só em PDF")]
    public async Task SignedTerm_ThatIsNotAPdf_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var carla = await team.ClientOfAsync(team.Carla);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);

        var image = await SignAsync(carla, team.Erp, id, "termo-assinado.png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        image.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ProjectTeam.ProblemTypeAsync(image)).Should().Be("urn:fiscoprojects:problem:acceptance-file-refused");
        (await StatusOfAsync(carla, team.Erp, id)).Should().Be("Pending");
        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[documents]")).Should().Be(0);
    }

    [Fact(DisplayName = "Recusar com motivo")]
    public async Task Refusal_KeepsTheReason_AndTheMilestoneOpen()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var carla = await team.ClientOfAsync(team.Carla);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);

        var refused = await RefuseAsync(carla, team.Erp, id, "  Falta o relatório de testes  ");

        refused.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var details = await DetailsAsync(marina, team.Erp, id);
        (details.GetProperty("term").GetProperty("status").GetString(), details.GetProperty("refusalReason").GetString())
            .Should().Be(("Refused", "Falta o relatório de testes"));
        details.GetProperty("events").EnumerateArray().Select(item => item.GetProperty("kind").GetString()).Should().Equal("Requested", "Refused");
        (await MilestoneOfAsync(marina, team.Erp, homologation)).GetProperty("reachedOn").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "Recusa sem motivo")]
    public async Task Refusal_WithoutAReason_IsABadRequest()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var carla = await team.ClientOfAsync(team.Carla);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);

        var refused = await RefuseAsync(carla, team.Erp, id, "   ");

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorFieldsAsync(refused)).Should().Equal("reason");
        (await StatusOfAsync(carla, team.Erp, id)).Should().Be("Pending");
    }

    [Fact(DisplayName = "Só a pessoa do pedido responde")]
    public async Task OnlyThePerson_Answers()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);

        var byTheManager = await SignAsync(marina, team.Erp, id);
        var refusedByTheManager = await RefuseAsync(marina, team.Erp, id, "Não");
        var byTheAdministrator = await SignAsync(team.Admin, team.Erp, id);
        var byDiego = await SignAsync(await team.ClientOfAsync(team.Diego), team.Erp, id);
        var bySofia = await RefuseAsync(await team.ClientOfAsync(team.Sofia), team.Erp, id, "Não");

        new[] { byTheManager.StatusCode, refusedByTheManager.StatusCode, byTheAdministrator.StatusCode, byDiego.StatusCode }
            .Should().AllBeEquivalentTo(HttpStatusCode.Forbidden);
        bySofia.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await StatusOfAsync(marina, team.Erp, id)).Should().Be("Pending");
    }

    [Fact(DisplayName = "Pedido já respondido")]
    public async Task AnsweredRequest_IsAConflict()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var carla = await team.ClientOfAsync(team.Carla);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);
        (await RefuseAsync(carla, team.Erp, id, "Falta o relatório de testes")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var signed = await SignAsync(carla, team.Erp, id);
        var refused = await RefuseAsync(carla, team.Erp, id, "De novo");
        var attached = await SignAsync(marina, team.Erp, id, onBehalf: true);
        var reminded = await SendAsync(marina, HttpMethod.Post, $"{Path(team.Erp)}/{id}/reminder");

        foreach (var response in new[] { signed, refused, attached, reminded })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ProjectTeam.ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:acceptance-answered");
        }

        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[documents]")).Should().Be(0);
    }

    [Fact(DisplayName = "Termo assinado na pasta de termos")]
    public async Task SignedTerm_IsAnApprovedDocument_ThatStays()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var carla = await team.ClientOfAsync(team.Carla);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);

        (await SignAsync(carla, team.Erp, id)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var folder = (await TermsFolderAsync(marina, team.Erp))!.Value;
        var document = (await DocumentsAsync(marina, team.Erp, folder)).Single();
        (document.GetProperty("name").GetString(), document.GetProperty("status").GetString())
            .Should().Be(("termo-homologacao-assinado.pdf", "Approved"));
        (document.GetProperty("link").GetProperty("id").GetGuid(), document.GetProperty("link").GetProperty("name").GetString())
            .Should().Be((homologation, "Homologação concluída"));
        document.GetProperty("updated").GetProperty("name").GetString().Should().Be("Carla Mendes");
        var documentId = document.GetProperty("id").GetGuid();
        (await DetailsAsync(marina, team.Erp, id)).GetProperty("signed").GetProperty("documentId").GetGuid().Should().Be(documentId);
        var removed = await SendAsync(marina, HttpMethod.Delete, $"/api/projects/{team.Erp}/documents/{documentId}");
        var obsolete = await SendAsync(marina, HttpMethod.Put, $"/api/projects/{team.Erp}/documents/{documentId}/status", new { status = "Obsolete" });
        new[] { removed.StatusCode, obsolete.StatusCode }.Should().AllBeEquivalentTo(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(removed)).Should().Be("urn:fiscoprojects:problem:acceptance-folder-read-only");
        var content = await marina.GetAsync($"/api/projects/{team.Erp}/documents/{documentId}/content", Cancellation);
        (await content.Content.ReadAsByteArrayAsync(Cancellation)).Should().Equal(Pdf("assinado"));
    }

    [Fact(DisplayName = "Marco atingido pelo termo")]
    public async Task SignedTerm_ReachesTheMilestone()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var carla = await team.ClientOfAsync(team.Carla);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);

        (await SignAsync(carla, team.Erp, id)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var milestone = await MilestoneOfAsync(await team.ClientOfAsync(team.Rafael), team.Erp, homologation);
        milestone.GetProperty("reachedOn").GetString().Should().Be("2026-10-19");
        // The milestone reached by the term is of no person: it does not enter the undo of the manager (ADR-0091).
        (await marina.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}/schedule", Cancellation))
            .GetProperty("undo").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "Anexar em nome da pessoa do pedido")]
    public async Task Manager_AttachesTheSignedTerm_InTheNameOfThePerson()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);

        var attached = await SignAsync(marina, team.Erp, id, "termo-recebido-por-email.pdf", onBehalf: true);

        attached.StatusCode.Should().Be(HttpStatusCode.NoContent, await attached.Content.ReadAsStringAsync(Cancellation));
        var details = await DetailsAsync(await team.ClientOfAsync(team.Carla), team.Erp, id);
        var term = details.GetProperty("term");
        (term.GetProperty("status").GetString(), term.GetProperty("recipient").GetProperty("name").GetString(), term.GetProperty("answered").GetProperty("name").GetString())
            .Should().Be(("Signed", "Carla Mendes", "Marina Costa"));
        details.GetProperty("events").EnumerateArray().Select(item => (item.GetProperty("kind").GetString(), item.GetProperty("actor").GetProperty("name").GetString()))
            .Should().Equal(("Requested", "Marina Costa"), ("Attached", "Marina Costa"));
        (await MilestoneOfAsync(marina, team.Erp, homologation)).GetProperty("reachedOn").GetString().Should().Be("2026-10-19");
    }

    [Fact(DisplayName = "Anexar termo sem pedido")]
    public async Task Manager_AttachesASignedTerm_WithoutARequest()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, scope) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var carla = await team.ClientOfAsync(team.Carla);

        var id = await IdOfAsync(await AttachAsync(marina, team.Erp, scope, team.Carla));

        var term = (await ItemsAsync(carla, team.Erp)).Single();
        (term.GetProperty("id").GetGuid(), term.GetProperty("title").GetString(), term.GetProperty("status").GetString(), term.GetProperty("isRequest").GetBoolean())
            .Should().Be((id, "Aceite do escopo", "Signed", false));
        (term.GetProperty("requested").GetProperty("name").GetString(), term.GetProperty("answered").GetProperty("name").GetString())
            .Should().Be(("Marina Costa", "Marina Costa"));
        var details = await DetailsAsync(carla, team.Erp, id);
        details.GetProperty("toSign").ValueKind.Should().Be(JsonValueKind.Null);
        details.GetProperty("events").EnumerateArray().Select(item => item.GetProperty("kind").GetString()).Should().Equal("Attached");
        (await carla.GetAsync($"{Path(team.Erp)}/{id}/file", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await MilestoneOfAsync(marina, team.Erp, scope)).GetProperty("reachedOn").GetString().Should().Be("2026-10-19");
        (await DocumentsAsync(marina, team.Erp, (await TermsFolderAsync(marina, team.Erp))!.Value))
            .Select(document => document.GetProperty("name").GetString()).Should().Equal("termo-escopo-assinado.pdf");
    }

    [Fact(DisplayName = "Lembrar a pessoa")]
    public async Task Reminder_SendsTheRequestAgain_AndEntersTheHistory()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);
        team.App.Clock.Advance(TimeSpan.FromMinutes(30));

        var reminded = await SendAsync(marina, HttpMethod.Post, $"{Path(team.Erp)}/{id}/reminder");

        reminded.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await DetailsAsync(marina, team.Erp, id)).GetProperty("events").EnumerateArray().Select(item => item.GetProperty("kind").GetString())
            .Should().Equal("Requested", "Reminded");
        (await team.App.EmailsToAsync("carla.mendes@empresa.example")).Select(email => email.Subject)
            .Should().BeEquivalentTo($"Pedido de aceite: {Title}", $"Lembrete do pedido de aceite: {Title}");
    }

    [Fact(DisplayName = "Cancelar pedido")]
    public async Task Cancellation_RemovesTheRequest_AndStaysInTheAudit()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);
        var file = System.IO.Path.Combine(team.Files, "ERP-FIN - Implantação do ERP financeiro", "Pedidos de aceite", "termo-homologacao.pdf");
        File.Exists(file).Should().BeTrue();

        var cancelled = await SendAsync(marina, HttpMethod.Delete, $"{Path(team.Erp)}/{id}");

        cancelled.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ItemsAsync(marina, team.Erp)).Should().BeEmpty();
        (await marina.GetAsync($"{Path(team.Erp)}/{id}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        File.Exists(file).Should().BeFalse();
        var audit = await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation);
        var entries = audit.GetProperty("items").EnumerateArray().Where(entry => entry.GetProperty("itemType").GetString() == "AcceptanceTerm").ToList();
        entries.Select(entry => entry.GetProperty("action").GetString()).Should().Contain(["Created", "Deleted"]);
        entries.Should().OnlyContain(entry => entry.GetProperty("itemLabel").GetString() == Title);
    }

    [Fact(DisplayName = "Cancelar pedido respondido")]
    public async Task Cancelling_AnAnsweredRequest_IsAConflict()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);
        (await SignAsync(await team.ClientOfAsync(team.Carla), team.Erp, id)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var cancelled = await SendAsync(marina, HttpMethod.Delete, $"{Path(team.Erp)}/{id}");

        cancelled.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(cancelled)).Should().Be("urn:fiscoprojects:problem:acceptance-answered");
        (await StatusOfAsync(marina, team.Erp, id)).Should().Be("Signed");
    }

    [Fact(DisplayName = "Permissões nos termos por papel")]
    public async Task Permissions_FollowTheMatrix()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, scope) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var ofCarla = await RequestedAsync(marina, team.Erp, homologation, team.Carla);
        // The terms the Administrator and the manager make go to Marina, so the others keep seeing only their own.
        var seenByManagers = 1;

        foreach (var role in new[] { "Administrator", "Manager", "TechLead", "Contributor", "Developer", "Viewer" })
        {
            var client = await team.ClientOfAsync(role);
            var manages = role is "Administrator" or "Manager";
            var own = role == "Contributor";
            var list = await ListAsync(client, team.Erp);
            list.GetProperty("items").GetArrayLength().Should().Be(manages ? seenByManagers : own ? 1 : 0, role);
            (list.GetProperty("canManage").GetBoolean(), list.GetProperty("canRequest").GetBoolean()).Should().Be((manages, manages), role);
            (await client.GetAsync($"{Path(team.Erp)}/{ofCarla}", Cancellation)).StatusCode
                .Should().Be(manages || own ? HttpStatusCode.OK : HttpStatusCode.NotFound, role);
            (await client.GetAsync($"{Path(team.Erp)}/milestones", Cancellation)).StatusCode
                .Should().Be(manages ? HttpStatusCode.OK : HttpStatusCode.Forbidden, role);

            var requested = await RequestAsync(client, team.Erp, homologation, team.Marina, $"Aceite de {role}");
            var attached = await AttachAsync(client, team.Erp, scope, team.Marina, $"Escopo de {role}");
            requested.StatusCode.Should().Be(manages ? HttpStatusCode.Created : HttpStatusCode.Forbidden, role);
            attached.StatusCode.Should().Be(manages ? HttpStatusCode.Created : HttpStatusCode.Forbidden, role);
            var reminded = await SendAsync(client, HttpMethod.Post, $"{Path(team.Erp)}/{ofCarla}/reminder");
            reminded.StatusCode.Should().Be(manages ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden, role);
            if (manages)
            {
                var created = (await requested.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
                (await SendAsync(client, HttpMethod.Delete, $"{Path(team.Erp)}/{created}")).StatusCode.Should().Be(HttpStatusCode.NoContent, role);
                seenByManagers++;
            }
            else
            {
                (await SendAsync(client, HttpMethod.Delete, $"{Path(team.Erp)}/{ofCarla}")).StatusCode.Should().Be(HttpStatusCode.Forbidden, role);
                (await SignAsync(client, team.Erp, ofCarla, onBehalf: true)).StatusCode.Should().Be(HttpStatusCode.Forbidden, role);
            }
        }

        (await StatusOfAsync(marina, team.Erp, ofCarla)).Should().Be("Pending");
    }

    [Fact(DisplayName = "Leitor responde o próprio pedido")]
    public async Task Viewer_AnswersTheOwnRequest()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var lucas = await team.ClientOfAsync(team.Lucas);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Lucas);

        var signed = await SignAsync(lucas, team.Erp, id);

        signed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await StatusOfAsync(lucas, team.Erp, id)).Should().Be("Signed");
    }

    [Fact(DisplayName = "Termos de um projeto de fora")]
    public async Task Outside_GetsNotFound()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var id = await RequestedAsync(await team.ClientOfAsync(team.Marina), team.Erp, homologation, team.Carla);
        var sofia = await team.ClientOfAsync(team.Sofia);

        (await sofia.GetAsync(Path(team.Erp), Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await sofia.GetAsync($"{Path(team.Erp)}/{id}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await sofia.GetAsync($"{Path(team.Erp)}/{id}/file", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await RequestAsync(sofia, team.Erp, homologation, team.Carla)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SignAsync(sofia, team.Erp, id)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Termos de projeto encerrado")]
    public async Task ClosedProject_IsReadOnly()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, scope) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var carla = await team.ClientOfAsync(team.Carla);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);
        (await SendAsync(marina, HttpMethod.Put, $"/api/projects/{team.Erp}/status", new { status = "Completed" })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        var signed = await SignAsync(carla, team.Erp, id);
        var refused = await RefuseAsync(carla, team.Erp, id, "Falta o relatório");
        var requested = await RequestAsync(marina, team.Erp, homologation, team.Carla);
        var attached = await AttachAsync(marina, team.Erp, scope, team.Carla);

        foreach (var response in new[] { signed, refused, requested, attached })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ProjectTeam.ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:project-closed");
        }

        (await StatusOfAsync(carla, team.Erp, id)).Should().Be("Pending");
        (await ListAsync(marina, team.Erp)).GetProperty("canRequest").GetBoolean().Should().BeFalse();
        (await DetailsAsync(carla, team.Erp, id)).GetProperty("actions").GetProperty("canAnswer").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "Filtro por situação")]
    public async Task List_FiltersByStatus_WithTheCounts()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, scope) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var carla = await team.ClientOfAsync(team.Carla);
        var refused = await RequestedAsync(marina, team.Erp, homologation, team.Carla, "Aceite da especificação");
        (await RefuseAsync(carla, team.Erp, refused, "Falta o relatório")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        team.App.Clock.Advance(TimeSpan.FromMinutes(5));
        var pending = await RequestedAsync(marina, team.Erp, homologation, team.Carla);
        team.App.Clock.Advance(TimeSpan.FromMinutes(5));
        var attached = await IdOfAsync(await AttachAsync(marina, team.Erp, scope, team.Diego));

        var all = await ListAsync(marina, team.Erp);
        var counts = all.GetProperty("counts");

        all.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Equal(attached, pending, refused);
        (counts.GetProperty("all").GetInt32(), counts.GetProperty("pending").GetInt32(), counts.GetProperty("signed").GetInt32(), counts.GetProperty("refused").GetInt32())
            .Should().Be((3, 1, 1, 1));
        (await ItemsAsync(marina, team.Erp, "?status=Pending")).Select(item => item.GetProperty("id").GetGuid()).Should().Equal(pending);
        (await ItemsAsync(carla, team.Erp, "?status=Signed")).Should().BeEmpty();
        (await ListAsync(carla, team.Erp)).GetProperty("counts").GetProperty("all").GetInt32().Should().Be(2);
    }

    // Spec: documentos, requirement "Permissões nos documentos".
    [Fact(DisplayName = "Pasta de termos de quem assinou")]
    public async Task TermsFolder_OfWhoSigned_HasOnlyTheOwnTerms()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, scope) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var carla = await team.ClientOfAsync(team.Carla);
        var ofCarla = await RequestedAsync(marina, team.Erp, homologation, team.Carla);
        (await SignAsync(carla, team.Erp, ofCarla)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await IdOfAsync(await AttachAsync(marina, team.Erp, scope, team.Diego));

        var folder = await TermsFolderAsync(carla, team.Erp);

        folder.Should().NotBeNull();
        var terms = (await FoldersAsync(carla, team.Erp)).GetProperty("folders").EnumerateArray()
            .Single(item => item.GetProperty("isAcceptance").GetBoolean());
        terms.GetProperty("count").GetInt32().Should().Be(1);
        var own = await DocumentsAsync(carla, team.Erp, folder!.Value);
        own.Select(document => document.GetProperty("name").GetString()).Should().Equal("termo-homologacao-assinado.pdf");
        var ofDiego = (await DocumentsAsync(marina, team.Erp, folder.Value))
            .Single(document => document.GetProperty("name").GetString() == "termo-escopo-assinado.pdf").GetProperty("id").GetGuid();
        (await carla.GetAsync($"/api/projects/{team.Erp}/documents/{ofDiego}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await carla.GetAsync($"/api/projects/{team.Erp}/documents/{ofDiego}/content", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var search = (await carla.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}/documents?search=termo", Cancellation)).GetProperty("items");
        search.EnumerateArray().Select(document => document.GetProperty("name").GetString()).Should().Equal("termo-homologacao-assinado.pdf");
        (await TermsFolderAsync(await team.ClientOfAsync(team.Rafael), team.Erp)).Should().BeNull();
    }

    // Spec: avisos, requirement "Avisos dos termos de aceite".
    [Fact(DisplayName = "Pedido de aceite por e-mail")]
    public async Task Request_GoesByEmail_Now()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);

        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);

        var email = (await team.App.EmailsToAsync("carla.mendes@empresa.example")).Single();
        email.Subject.Should().Be($"Pedido de aceite: {Title}");
        email.TextBody.Should().Contain("Marina Costa pediu o seu aceite do marco Homologação concluída, no projeto ERP-FIN")
            .And.Contain($"/projects/{team.Erp}/acceptances?term={id}")
            .And.NotContain("Carla, os testes");
    }

    // Spec: avisos, requirement "Avisos dos termos de aceite".
    [Fact(DisplayName = "Lembrete do pedido")]
    public async Task Reminder_GoesByEmail_Now()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);

        (await SendAsync(marina, HttpMethod.Post, $"{Path(team.Erp)}/{id}/reminder")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var reminder = (await team.App.EmailsToAsync("carla.mendes@empresa.example"))
            .Single(email => email.Subject == $"Lembrete do pedido de aceite: {Title}");
        reminder.TextBody.Should().Contain("Marina Costa lembrou o pedido de aceite");
    }

    // Spec: avisos, requirement "Avisos dos termos de aceite".
    [Fact(DisplayName = "Termo assinado avisa a gestão")]
    public async Task SignedTerm_TellsWhoAsked_AndTheManager()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var id = await RequestedAsync(team.Admin, team.Erp, homologation, team.Carla);

        (await SignAsync(await team.ClientOfAsync(team.Carla), team.Erp, id)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        foreach (var person in new[] { team.Admin, await team.ClientOfAsync(team.Marina) })
        {
            var notification = await NotificationOfAsync(person, "AcceptanceSigned");
            (notification.GetProperty("actor").GetString(), notification.GetProperty("path").GetString())
                .Should().Be(("Carla Mendes", $"/projects/{team.Erp}/acceptances?term={id}"));
            notification.GetProperty("values").GetProperty("title").GetString().Should().Be(Title);
        }

        (await team.App.EmailsToAsync("marina.costa@empresa.example")).Should().BeEmpty("the signed term waits for the daily summary");
        var summary = SummaryOf(await team.App.DailySummaryAsync(), "marina.costa@empresa.example");
        summary.TextBody.Should().Contain("Termos de aceite").And.Contain($"{Title}, do projeto ERP-FIN: termo assinado enviado por Carla Mendes");
    }

    // Spec: avisos, requirement "Avisos dos termos de aceite".
    [Fact(DisplayName = "Recusa avisa a gestão")]
    public async Task Refusal_TellsWhoAsked_AndTheManager()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);

        (await RefuseAsync(await team.ClientOfAsync(team.Carla), team.Erp, id, "Falta o relatório de testes")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await NotificationOfAsync(marina, "AcceptanceRefused")).GetProperty("actor").GetString().Should().Be("Carla Mendes");
        var summary = SummaryOf(await team.App.DailySummaryAsync(), "marina.costa@empresa.example");
        summary.TextBody.Should().Contain($"{Title}, do projeto ERP-FIN: recusado por Carla Mendes").And.NotContain("Falta o relatório");
    }

    // Spec: avisos, requirement "Avisos dos termos de aceite".
    [Fact(DisplayName = "Aceites pendentes no resumo")]
    public async Task PendingRequests_AreInTheSummary_OfThePerson()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, team.Carla);
        var other = await team.CreateProjectAsync("FISC-SP", "Revisão fiscal da filial SP");
        await team.App.AddProjectMemberAsync(other, team.Carla, "Contributor");
        var (closedMilestone, _) = await MilestonesAsync(team, other);
        await RequestedAsync(marina, other, closedMilestone, team.Carla, "Aceite do plano de testes");
        (await SendAsync(marina, HttpMethod.Put, $"/api/projects/{other}/status", new { status = "Cancelled" })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var summary = SummaryOf(await team.App.DailySummaryAsync(), "carla.mendes@empresa.example");

        summary.TextBody.Should().Contain("Termos de aceite esperando a sua resposta")
            .And.Contain($"{Title}, do projeto ERP-FIN, pedido em 19/10/2026")
            .And.Contain($"/projects/{team.Erp}/acceptances?term={id}")
            .And.NotContain("Aceite do plano de testes", "a cancelled project leaves the summary (ADR-0095)");
    }
}
