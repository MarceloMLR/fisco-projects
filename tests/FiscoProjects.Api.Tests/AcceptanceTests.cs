using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Documents.Application;
using FiscoProjects.Notifications.Application;
using FiscoProjects.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using static FiscoProjects.Api.Tests.AcceptanceRequests;

namespace FiscoProjects.Api.Tests;

// Spec: termos-de-aceite (API side): "Provedor de assinatura", "Provedor local", "Situação das assinaturas", "Dados dos
// signatários", "Pedidos anteriores", "Termos só da gestão", "Pedido de aceite", "Termo assinado", "Termo anexado pelo
// gestor" and "Lembrar e cancelar"; documentos, "Permissões nos documentos" (the folder of the terms); avisos, "Avisos
// dos termos de aceite" (ADR-0037, ADR-0093, ADR-0095, ADR-0129).
public sealed class AcceptanceTests(SqlServerFixture sqlServer)
{
    private const string Title = "Aceite da homologação";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

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

    /// <summary>The team with a provider in memory in place of the local one.</summary>
    private static async Task<(ProjectTeam Team, FakeSignatureProvider Provider)> WithFakeProviderAsync(SqlServerFixture sqlServer)
    {
        var provider = new FakeSignatureProvider();
        var team = await ProjectTeam.StartAsync(sqlServer, builder => UseProvider(builder, provider));
        return (team, provider);
    }

    private static void UseProvider(WebApplicationBuilder builder, ISignatureProvider? provider)
    {
        builder.Configuration["Signature:Provider"] = string.Empty;
        if (provider is not null)
        {
            builder.Services.AddSingleton(provider);
        }
    }

    /// <summary>One round of the check in the background: each open request, in a scope of its own.</summary>
    private static async Task<int> CheckAsync(ProjectTeam team)
    {
        IReadOnlyList<Guid> open;
        await using (var scope = team.App.Services.CreateAsyncScope())
        {
            open = await scope.ServiceProvider.GetRequiredService<ListOpenSignatureRequests>().ExecuteAsync(Cancellation);
        }

        var changed = 0;
        foreach (var term in open)
        {
            await using var scope = team.App.Services.CreateAsyncScope();
            changed += await scope.ServiceProvider.GetRequiredService<ApplySignatureStatus>().ExecuteAsync(term, Cancellation) ? 1 : 0;
        }

        return changed;
    }

    private static async Task<JsonElement> ListAsync(HttpClient client, Guid project, string query = "") =>
        await client.GetFromJsonAsync<JsonElement>($"{PathOf(project)}{query}", Cancellation);

    private static async Task<List<JsonElement>> ItemsAsync(HttpClient client, Guid project, string query = "") =>
        [.. (await ListAsync(client, project, query)).GetProperty("items").EnumerateArray()];

    private static async Task<string> StatusOfAsync(HttpClient client, Guid project, Guid term) =>
        (await DetailsAsync(client, project, term)).GetProperty("term").GetProperty("status").GetString()!;

    private static List<string?> KindsOf(JsonElement details) =>
        [.. details.GetProperty("events").EnumerateArray().Select(item => item.GetProperty("kind").GetString())];

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

    private static string RequestsFolder(ProjectTeam team) =>
        System.IO.Path.Combine(team.Files, "ERP-FIN - Implantação do ERP financeiro", "Pedidos de aceite");

    [Fact(DisplayName = "Pedir aceite")]
    public async Task Request_IsPending_WithTheSigners_AtTheProvider()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);

        var id = await RequestedAsync(marina, team.Erp, homologation, Title);

        var term = (await ItemsAsync(marina, team.Erp)).Single();
        (term.GetProperty("id").GetGuid(), term.GetProperty("title").GetString(), term.GetProperty("status").GetString())
            .Should().Be((id, Title, "Pending"));
        (term.GetProperty("milestone").GetProperty("name").GetString(), term.GetProperty("milestone").GetProperty("day").GetString())
            .Should().Be(("Homologação concluída", "2026-11-03"));
        term.GetProperty("signers").EnumerateArray().Select(signer => (signer.GetProperty("name").GetString(), signer.GetProperty("status").GetString()))
            .Should().Equal(("Ana Souza", "Pending"), ("Bruno Lima", "Pending"));
        (term.GetProperty("recipient").ValueKind, term.GetProperty("requested").GetProperty("name").GetString())
            .Should().Be((JsonValueKind.Null, "Marina Costa"));
        term.GetProperty("isRequest").GetBoolean().Should().BeTrue();
        var details = await DetailsAsync(marina, team.Erp, id);
        details.GetProperty("toSign").GetProperty("name").GetString().Should().Be("termo-homologacao.pdf");
        details.GetProperty("message").GetString().Should().StartWith("Os testes de homologação");
        details.GetProperty("signers").EnumerateArray().Select(signer => signer.GetProperty("email").GetString())
            .Should().Equal("ana.souza@cliente.example", "bruno.lima@cliente.example");
        KindsOf(details).Should().Equal("Requested");
        var actions = details.GetProperty("actions");
        (actions.GetProperty("canAttach").GetBoolean(), actions.GetProperty("canRemind").GetBoolean(), actions.GetProperty("canCancel").GetBoolean())
            .Should().Be((true, true, true));
        var file = await marina.GetAsync($"{PathOf(team.Erp)}/{id}/file", Cancellation);
        file.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        (await file.Content.ReadAsByteArrayAsync(Cancellation)).Should().Equal(Pdf());
        File.Exists(System.IO.Path.Combine(RequestsFolder(team), "termo-homologacao.pdf")).Should().BeTrue();
        // The local provider has the request, with the PDF and the two signers.
        Directory.GetFiles(team.Signatures, "term.pdf", SearchOption.AllDirectories).Should().ContainSingle();
    }

    [Fact(DisplayName = "Pedido sem provedor configurado")]
    public async Task Request_WithoutAProvider_IsAConflict()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer, builder => UseProvider(builder, null));
        var (homologation, scope) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);

        var requested = await RequestAsync(marina, team.Erp, homologation, Title);

        requested.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(requested)).Should().Be("urn:fiscoprojects:problem:signature-provider-missing");
        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[acceptance_terms]")).Should().Be(0);
        Directory.Exists(RequestsFolder(team)).Should().BeFalse();
        var list = await ListAsync(marina, team.Erp);
        (list.GetProperty("canRequest").GetBoolean(), list.GetProperty("canAttach").GetBoolean()).Should().Be((false, true));
        await IdOfAsync(await AttachAsync(marina, team.Erp, scope));
        using var anonymous = team.App.NewClient();
        (await anonymous.GetAsync($"/api/local-signatures/{new string('x', 43)}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
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

        var ofATask = await RequestAsync(marina, team.Erp, task, Title);
        var ofAnotherProject = await RequestAsync(marina, team.Erp, elsewhere, Title);

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

        var word = await RequestAsync(marina, team.Erp, homologation, Title, fileName: "termo.docx", content: Encoding.ASCII.GetBytes("PK\u0003\u0004"));
        var named = await RequestAsync(marina, team.Erp, homologation, Title, fileName: "termo.pdf", content: Encoding.ASCII.GetBytes("PK\u0003\u0004"));
        var empty = await RequestAsync(marina, team.Erp, homologation, Title, content: []);

        foreach (var (response, reason) in new[] { (word, "NotPdf"), (named, "NotPdf"), (empty, "Empty") })
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
            (problem.GetProperty("type").GetString(), problem.GetProperty("reason").GetString())
                .Should().Be(("urn:fiscoprojects:problem:acceptance-file-refused", reason));
        }

        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[acceptance_terms]")).Should().Be(0);
        Directory.Exists(RequestsFolder(team)).Should().BeFalse();
        Directory.Exists(team.Signatures).Should().BeFalse();
    }

    [Fact(DisplayName = "Pedido para quem não está no projeto")]
    public async Task Request_OfSomeoneOutsideTheSystem_KeepsTheSigner_WithoutAUser()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var users = await team.App.CountAsync("SELECT COUNT(*) FROM [users].[users]");

        var id = await RequestedAsync(marina, team.Erp, homologation, Title, [("Helena Rocha", "helena.rocha@cliente.example")]);

        (await DetailsAsync(marina, team.Erp, id)).GetProperty("signers").EnumerateArray()
            .Select(signer => (signer.GetProperty("name").GetString(), signer.GetProperty("email").GetString()))
            .Should().Equal(("Helena Rocha", "helena.rocha@cliente.example"));
        (await team.App.CountAsync("SELECT COUNT(*) FROM [users].[users]")).Should().Be(users);
    }

    [Fact(DisplayName = "Signatários inválidos")]
    public async Task Request_WithSignersNotValid_IsABadRequest()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var eleven = Enumerable.Range(1, 11).Select(index => ($"Pessoa {index}", $"pessoa{index}@cliente.example")).ToList();

        var responses = new[]
        {
            await RequestAsync(marina, team.Erp, homologation, Title, []),
            await RequestAsync(marina, team.Erp, homologation, Title, eleven),
            await RequestAsync(marina, team.Erp, homologation, Title, [("Ana Souza", "ana.souza")]),
            await RequestAsync(marina, team.Erp, homologation, Title, [("Ana Souza", "ana.souza@cliente.example"), ("Ana", "ANA.SOUZA@cliente.example")]),
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ErrorFieldsAsync(response)).Should().Equal("signers");
        }

        var nothing = await PostFormAsync(marina, PathOf(team.Erp), new Dictionary<string, string?> { ["title"] = " " }, null, null);
        (await ErrorFieldsAsync(nothing)).Should().BeEquivalentTo("milestoneId", "title", "file", "signers");
        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[acceptance_terms]")).Should().Be(0);
        Directory.Exists(team.Signatures).Should().BeFalse();
    }

    [Fact(DisplayName = "Provedor recusa o pedido")]
    public async Task Request_RefusedByTheProvider_SavesNothing()
    {
        var (team, provider) = await WithFakeProviderAsync(sqlServer);
        await using var _ = team;
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        provider.Failing = true;

        var requested = await RequestAsync(await team.ClientOfAsync(team.Marina), team.Erp, homologation, Title);

        requested.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        (await ProjectTeam.ProblemTypeAsync(requested)).Should().Be("urn:fiscoprojects:problem:signature-provider-failed");
        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[acceptance_terms]")).Should().Be(0);
        Directory.EnumerateFiles(RequestsFolder(team)).Should().BeEmpty("the term to sign is removed");
    }

    [Fact(DisplayName = "Assinar pela página local")]
    public async Task Signer_SignsOnTheLocalPage()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);
        var link = await LinkOfAsync(marina, team.Erp, id, 0);
        using var anonymous = team.App.NewClient();

        var page = await anonymous.GetFromJsonAsync<JsonElement>(ApiOf(link), Cancellation);
        var document = await anonymous.GetAsync($"{ApiOf(link)}/document", Cancellation);
        await SignLocallyAsync(team.App, link);

        (page.GetProperty("title").GetString(), page.GetProperty("signer").GetString(), page.GetProperty("fileName").GetString())
            .Should().Be((Title, "Ana Souza", "termo-homologacao.pdf"));
        page.ToString().Should().NotContain("@cliente.example");
        (await document.Content.ReadAsByteArrayAsync(Cancellation)).Should().Equal(Pdf());
        var ana = (await DetailsAsync(marina, team.Erp, id)).GetProperty("signers")[0];
        (ana.GetProperty("status").GetString(), ana.GetProperty("respondedAt").GetDateTime(), ana.GetProperty("signingLink").ValueKind)
            .Should().Be(("Signed", new DateTime(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc), JsonValueKind.Null));
    }

    [Fact(DisplayName = "Link local de outro termo ou já usado")]
    public async Task UsedOrUnknownLocalLinks_AreNoLongerValid()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);
        var link = await LinkOfAsync(marina, team.Erp, id, 0);
        await SignLocallyAsync(team.App, link);
        using var anonymous = team.App.NewClient();

        (await anonymous.GetAsync(ApiOf(link), Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await anonymous.GetAsync($"{ApiOf(link)}/document", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(anonymous, HttpMethod.Post, $"{ApiOf(link)}/sign")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await anonymous.GetAsync($"/api/local-signatures/{new string('x', 43)}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(anonymous, HttpMethod.Post, $"{ApiOf(await LinkOfAsync(marina, team.Erp, id, 1))}/refuse", new { reason = " " }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        KindsOf(await DetailsAsync(marina, team.Erp, id)).Should().Equal("Requested", "Signed");
    }

    [Fact(DisplayName = "Assinatura parcial")]
    public async Task OneOfTwoSignatures_KeepsTheRequestPending()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);
        team.App.Clock.Advance(TimeSpan.FromHours(2));

        await SignLocallyAsync(team.App, await LinkOfAsync(marina, team.Erp, id, 1));

        var details = await DetailsAsync(marina, team.Erp, id);
        details.GetProperty("term").GetProperty("status").GetString().Should().Be("Pending");
        details.GetProperty("signers").EnumerateArray().Select(signer => (signer.GetProperty("status").GetString(), signer.GetProperty("email").GetString()))
            .Should().Equal(("Pending", "ana.souza@cliente.example"), ("Signed", "bruno.lima@cliente.example"));
        details.GetProperty("signers")[1].GetProperty("respondedAt").GetDateTime().Should().Be(new DateTime(2026, 10, 19, 11, 0, 0, DateTimeKind.Utc));
        var signed = details.GetProperty("events").EnumerateArray().Last();
        (signed.GetProperty("kind").GetString(), signed.GetProperty("signer").GetString(), signed.GetProperty("actor").ValueKind)
            .Should().Be(("Signed", "Bruno Lima", JsonValueKind.Null));
        (await ItemsAsync(marina, team.Erp)).Single().GetProperty("signers").EnumerateArray().Select(signer => signer.GetProperty("status").GetString())
            .Should().Equal("Pending", "Signed");
        (await MilestoneOfAsync(marina, team.Erp, homologation)).GetProperty("reachedOn").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "Termo assinado na pasta de termos")]
    public async Task SignedTerm_IsTheSignedPdf_AsAnApprovedDocument_ThatStays()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);

        await SignAllAsync(team, marina, team.Erp, id);

        (await StatusOfAsync(marina, team.Erp, id)).Should().Be("Signed");
        var folder = (await TermsFolderAsync(marina, team.Erp))!.Value;
        var document = (await DocumentsAsync(marina, team.Erp, folder)).Single();
        (document.GetProperty("name").GetString(), document.GetProperty("status").GetString())
            .Should().Be(("termo-homologacao.pdf", "Approved"));
        (document.GetProperty("link").GetProperty("id").GetGuid(), document.GetProperty("link").GetProperty("name").GetString())
            .Should().Be((homologation, "Homologação concluída"));
        document.GetProperty("updated").GetProperty("name").GetString().Should().Be("Marina Costa");
        var documentId = document.GetProperty("id").GetGuid();
        var details = await DetailsAsync(marina, team.Erp, id);
        details.GetProperty("signed").GetProperty("documentId").GetGuid().Should().Be(documentId);
        details.GetProperty("term").GetProperty("closedAt").GetDateTime().Should().Be(new DateTime(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc));
        var removed = await SendAsync(marina, HttpMethod.Delete, $"/api/projects/{team.Erp}/documents/{documentId}");
        var obsolete = await SendAsync(marina, HttpMethod.Put, $"/api/projects/{team.Erp}/documents/{documentId}/status", new { status = "Obsolete" });
        new[] { removed.StatusCode, obsolete.StatusCode }.Should().AllBeEquivalentTo(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(removed)).Should().Be("urn:fiscoprojects:problem:acceptance-folder-read-only");
        var content = await marina.GetAsync($"/api/projects/{team.Erp}/documents/{documentId}/content", Cancellation);
        (await content.Content.ReadAsByteArrayAsync(Cancellation)).Should().Equal(Pdf());
    }

    [Fact(DisplayName = "E-mail apagado ao fechar o pedido")]
    public async Task Emails_AreForgotten_WhenTheLastSignerSigns()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);

        await SignAllAsync(team, marina, team.Erp, id);

        (await DetailsAsync(marina, team.Erp, id)).GetProperty("signers").EnumerateArray()
            .Select(signer => (signer.GetProperty("name").GetString(), signer.GetProperty("email").ValueKind))
            .Should().Equal(("Ana Souza", JsonValueKind.Null), ("Bruno Lima", JsonValueKind.Null));
        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[acceptance_signers] WHERE [Email] IS NOT NULL")).Should().Be(0);
        (await team.App.CountAsync("SELECT COUNT(*) FROM [audit].[entries] WHERE [Changes] LIKE '%@cliente.example%'")).Should().Be(0);
    }

    [Fact(DisplayName = "E-mail fora da auditoria")]
    public async Task SignerEmails_AreSecretsInTheAudit()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);

        await RequestedAsync(await team.ClientOfAsync(team.Marina), team.Erp, homologation, Title);

        var audit = await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation);
        var signers = audit.GetProperty("items").EnumerateArray().Where(entry => entry.GetProperty("itemType").GetString() == "AcceptanceSigner").ToList();
        signers.Select(entry => entry.GetProperty("itemLabel").GetString()).Should().BeEquivalentTo("Ana Souza", "Bruno Lima");
        foreach (var entry in signers)
        {
            var email = entry.GetProperty("changes").EnumerateArray().Single(field => field.GetProperty("field").GetString() == "Email");
            (email.GetProperty("secret").GetBoolean(), email.GetProperty("after").ValueKind).Should().Be((true, JsonValueKind.Null));
            entry.GetProperty("changes").EnumerateArray().Single(field => field.GetProperty("field").GetString() == "TermId")
                .GetProperty("after").GetString().Should().Be(Title);
        }

        audit.ToString().Should().NotContain("@cliente.example");
    }

    [Fact(DisplayName = "Conferência repetida")]
    public async Task TheSameSignature_ByTheNoticeAndTheCheck_IsKeptOnce()
    {
        var (team, provider) = await WithFakeProviderAsync(sqlServer);
        await using var _ = team;
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);
        provider.Sign(0, new DateTime(2026, 10, 19, 10, 0, 0, DateTimeKind.Utc));
        provider.Sign(1, new DateTime(2026, 10, 19, 10, 30, 0, DateTimeKind.Utc));

        // The notice of the provider and the check at the same time, and the check again.
        (await Task.WhenAll(CheckAsync(team), CheckAsync(team))).Sum().Should().Be(1);
        (await CheckAsync(team)).Should().Be(0);

        var details = await DetailsAsync(marina, team.Erp, id);
        details.GetProperty("term").GetProperty("status").GetString().Should().Be("Signed");
        KindsOf(details).Should().Equal("Requested", "Signed", "Signed");
        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[documents]")).Should().Be(1);
        (await DocumentsAsync(marina, team.Erp, (await TermsFolderAsync(marina, team.Erp))!.Value))
            .Select(document => document.GetProperty("name").GetString()).Should().Equal("termo-homologacao-assinado.pdf");
        (await team.App.CountAsync("SELECT COUNT(*) FROM [notifications].[notifications] WHERE [Kind] = 'AcceptanceSigned'")).Should().Be(1);
    }

    [Fact(DisplayName = "Provedor fora do ar")]
    public async Task ProviderOutOfTheAir_LeavesTheRequest_ForTheNextCheck()
    {
        var (team, provider) = await WithFakeProviderAsync(sqlServer);
        await using var _ = team;
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title, [("Ana Souza", "ana.souza@cliente.example")]);
        provider.Sign(0, new DateTime(2026, 10, 19, 10, 0, 0, DateTimeKind.Utc));
        provider.Failing = true;

        (await CheckAsync(team)).Should().Be(0);

        var details = await DetailsAsync(marina, team.Erp, id);
        details.GetProperty("term").GetProperty("status").GetString().Should().Be("Pending");
        details.GetProperty("signers")[0].GetProperty("status").GetString().Should().Be("Pending");
        provider.Failing = false;
        (await CheckAsync(team)).Should().Be(1);
        (await StatusOfAsync(marina, team.Erp, id)).Should().Be("Signed");
    }

    [Fact(DisplayName = "Marco atingido pelo termo")]
    public async Task SignedTerm_ReachesTheMilestone()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);

        await SignAllAsync(team, marina, team.Erp, id);

        var milestone = await MilestoneOfAsync(await team.ClientOfAsync(team.Rafael), team.Erp, homologation);
        milestone.GetProperty("reachedOn").GetString().Should().Be("2026-10-19");
        // The milestone reached by the term is of no person: it does not enter the undo of the manager (ADR-0091).
        (await marina.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}/schedule", Cancellation))
            .GetProperty("undo").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "Recusa de um signatário")]
    public async Task OneRefusal_RefusesTheTerm_AndClosesTheDocument()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);
        var other = await LinkOfAsync(marina, team.Erp, id, 0);

        await RefuseLocallyAsync(team.App, await LinkOfAsync(marina, team.Erp, id, 1), "Falta o relatório de testes");

        var details = await DetailsAsync(marina, team.Erp, id);
        (details.GetProperty("term").GetProperty("status").GetString(), details.GetProperty("refusalReason").GetString())
            .Should().Be(("Refused", "Falta o relatório de testes"));
        var bruno = details.GetProperty("signers")[1];
        (bruno.GetProperty("status").GetString(), bruno.GetProperty("refusalReason").GetString()).Should().Be(("Refused", "Falta o relatório de testes"));
        details.GetProperty("events").EnumerateArray().Last().GetProperty("signer").GetString().Should().Be("Bruno Lima");
        using var anonymous = team.App.NewClient();
        (await anonymous.GetAsync(ApiOf(other), Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound, "the document closed");
        (await MilestoneOfAsync(marina, team.Erp, homologation)).GetProperty("reachedOn").ValueKind.Should().Be(JsonValueKind.Null);
        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[acceptance_signers] WHERE [Email] IS NOT NULL")).Should().Be(0);
    }

    [Fact(DisplayName = "Anexar num pedido pendente")]
    public async Task SignedTerm_AttachedToARequest_CancelsTheDocument()
    {
        var (team, provider) = await WithFakeProviderAsync(sqlServer);
        await using var _ = team;
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);

        var attached = await AttachToAsync(marina, team.Erp, id);

        attached.StatusCode.Should().Be(HttpStatusCode.NoContent, await attached.Content.ReadAsStringAsync(Cancellation));
        var details = await DetailsAsync(marina, team.Erp, id);
        (details.GetProperty("term").GetProperty("status").GetString(), details.GetProperty("term").GetProperty("answered").GetProperty("name").GetString())
            .Should().Be(("Signed", "Marina Costa"));
        details.GetProperty("events").EnumerateArray().Last().GetProperty("actor").GetProperty("name").GetString().Should().Be("Marina Costa");
        KindsOf(details).Should().Equal("Requested", "Attached");
        provider.Cancelled.Should().ContainSingle();
        (await MilestoneOfAsync(marina, team.Erp, homologation)).GetProperty("reachedOn").GetString().Should().Be("2026-10-19");
        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[acceptance_signers] WHERE [Email] IS NOT NULL")).Should().Be(0);
    }

    [Fact(DisplayName = "Anexar em nome da pessoa do pedido")]
    public async Task SignedTerm_AttachedToARequestOfBefore_KeepsThePerson()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedBeforeAsync(team, team.Erp, homologation, team.Carla, Title);

        (await AttachToAsync(marina, team.Erp, id)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var details = await DetailsAsync(marina, team.Erp, id);
        var term = details.GetProperty("term");
        (term.GetProperty("status").GetString(), term.GetProperty("recipient").GetProperty("name").GetString(), term.GetProperty("answered").GetProperty("name").GetString())
            .Should().Be(("Signed", "Carla Mendes", "Marina Costa"));
        details.GetProperty("events").EnumerateArray().Select(item => (item.GetProperty("kind").GetString(), item.GetProperty("actor").GetProperty("name").GetString()))
            .Should().Equal(("Requested", "Marina Costa"), ("Attached", "Marina Costa"));
    }

    [Fact(DisplayName = "Pedido anterior pendente")]
    public async Task RequestOfBefore_IsOnlyAttachedOrCancelled()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedBeforeAsync(team, team.Erp, homologation, team.Carla, Title);

        var details = await DetailsAsync(marina, team.Erp, id);
        var reminded = await SendAsync(marina, HttpMethod.Post, $"{PathOf(team.Erp)}/{id}/reminder");

        details.GetProperty("term").GetProperty("recipient").GetProperty("name").GetString().Should().Be("Carla Mendes");
        details.GetProperty("signers").GetArrayLength().Should().Be(0);
        var actions = details.GetProperty("actions");
        (actions.GetProperty("canAttach").GetBoolean(), actions.GetProperty("canRemind").GetBoolean(), actions.GetProperty("canCancel").GetBoolean())
            .Should().Be((true, false, true));
        reminded.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(reminded)).Should().Be("urn:fiscoprojects:problem:signature-provider-missing");
        (await SendAsync(marina, HttpMethod.Delete, $"{PathOf(team.Erp)}/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ItemsAsync(marina, team.Erp)).Should().BeEmpty();
    }

    [Fact(DisplayName = "Anexar termo sem pedido")]
    public async Task Manager_AttachesASignedTerm_WithoutARequest()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, scope) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);

        var id = await IdOfAsync(await AttachAsync(marina, team.Erp, scope));

        var term = (await ItemsAsync(marina, team.Erp)).Single();
        (term.GetProperty("id").GetGuid(), term.GetProperty("title").GetString(), term.GetProperty("status").GetString(), term.GetProperty("isRequest").GetBoolean())
            .Should().Be((id, "Aceite do escopo", "Signed", false));
        (term.GetProperty("requested").GetProperty("name").GetString(), term.GetProperty("answered").GetProperty("name").GetString())
            .Should().Be(("Marina Costa", "Marina Costa"));
        (term.GetProperty("recipient").ValueKind, term.GetProperty("signers").GetArrayLength()).Should().Be((JsonValueKind.Null, 0));
        var details = await DetailsAsync(marina, team.Erp, id);
        details.GetProperty("toSign").ValueKind.Should().Be(JsonValueKind.Null);
        KindsOf(details).Should().Equal("Attached");
        (await marina.GetAsync($"{PathOf(team.Erp)}/{id}/file", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await MilestoneOfAsync(marina, team.Erp, scope)).GetProperty("reachedOn").GetString().Should().Be("2026-10-19");
        (await DocumentsAsync(marina, team.Erp, (await TermsFolderAsync(marina, team.Erp))!.Value))
            .Select(document => document.GetProperty("name").GetString()).Should().Equal("termo-escopo-assinado.pdf");
    }

    [Fact(DisplayName = "Lembrar a pessoa")]
    public async Task Reminder_GoesByTheProvider_AndEntersTheHistory()
    {
        var (team, provider) = await WithFakeProviderAsync(sqlServer);
        await using var _ = team;
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);
        team.App.Clock.Advance(TimeSpan.FromMinutes(30));

        var reminded = await SendAsync(marina, HttpMethod.Post, $"{PathOf(team.Erp)}/{id}/reminder");

        reminded.StatusCode.Should().Be(HttpStatusCode.NoContent);
        provider.Reminded.Should().ContainSingle();
        KindsOf(await DetailsAsync(marina, team.Erp, id)).Should().Equal("Requested", "Reminded");
        provider.Failing = true;
        var failed = await SendAsync(marina, HttpMethod.Post, $"{PathOf(team.Erp)}/{id}/reminder");
        failed.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        KindsOf(await DetailsAsync(marina, team.Erp, id)).Should().Equal("Requested", "Reminded");
    }

    [Fact(DisplayName = "Cancelar pedido")]
    public async Task Cancellation_CancelsAtTheProvider_RemovesTheRequest_AndStaysInTheAudit()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);
        var link = await LinkOfAsync(marina, team.Erp, id, 0);
        var file = System.IO.Path.Combine(RequestsFolder(team), "termo-homologacao.pdf");
        File.Exists(file).Should().BeTrue();

        var cancelled = await SendAsync(marina, HttpMethod.Delete, $"{PathOf(team.Erp)}/{id}");

        cancelled.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ItemsAsync(marina, team.Erp)).Should().BeEmpty();
        (await marina.GetAsync($"{PathOf(team.Erp)}/{id}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        File.Exists(file).Should().BeFalse();
        using var anonymous = team.App.NewClient();
        (await anonymous.GetAsync(ApiOf(link), Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound, "the document was cancelled");
        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[acceptance_signers]")).Should().Be(0);
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
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);
        await SignAllAsync(team, marina, team.Erp, id);

        var cancelled = await SendAsync(marina, HttpMethod.Delete, $"{PathOf(team.Erp)}/{id}");
        var attached = await AttachToAsync(marina, team.Erp, id);

        cancelled.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(cancelled)).Should().Be("urn:fiscoprojects:problem:acceptance-answered");
        attached.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StatusOfAsync(marina, team.Erp, id)).Should().Be("Signed");
    }

    [Fact(DisplayName = "Permissões nos termos por papel")]
    public async Task Permissions_AreOnlyOfTheManagement()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, scope) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var pending = await RequestedAsync(marina, team.Erp, homologation, Title);

        foreach (var role in new[] { "Administrator", "Manager", "TechLead", "Contributor", "Developer", "Viewer" })
        {
            var client = await team.ClientOfAsync(role);
            var manages = role is "Administrator" or "Manager";
            var allowed = manages ? HttpStatusCode.OK : HttpStatusCode.Forbidden;
            (await client.GetAsync(PathOf(team.Erp), Cancellation)).StatusCode.Should().Be(allowed, role);
            (await client.GetAsync($"{PathOf(team.Erp)}/{pending}", Cancellation)).StatusCode.Should().Be(allowed, role);
            (await client.GetAsync($"{PathOf(team.Erp)}/{pending}/file", Cancellation)).StatusCode.Should().Be(allowed, role);
            (await client.GetAsync($"{PathOf(team.Erp)}/milestones", Cancellation)).StatusCode.Should().Be(allowed, role);

            var requested = await RequestAsync(client, team.Erp, homologation, $"Aceite de {role}");
            var attached = await AttachAsync(client, team.Erp, scope, $"Escopo de {role}");
            requested.StatusCode.Should().Be(manages ? HttpStatusCode.Created : HttpStatusCode.Forbidden, role);
            attached.StatusCode.Should().Be(manages ? HttpStatusCode.Created : HttpStatusCode.Forbidden, role);
            var reminded = await SendAsync(client, HttpMethod.Post, $"{PathOf(team.Erp)}/{pending}/reminder");
            reminded.StatusCode.Should().Be(manages ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden, role);
            if (manages)
            {
                var created = (await requested.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
                (await AttachToAsync(client, team.Erp, created)).StatusCode.Should().Be(HttpStatusCode.NoContent, role);
                var another = await RequestedAsync(client, team.Erp, homologation, $"Outro aceite de {role}");
                (await SendAsync(client, HttpMethod.Delete, $"{PathOf(team.Erp)}/{another}")).StatusCode.Should().Be(HttpStatusCode.NoContent, role);
            }
            else
            {
                (await SendAsync(client, HttpMethod.Delete, $"{PathOf(team.Erp)}/{pending}")).StatusCode.Should().Be(HttpStatusCode.Forbidden, role);
                (await AttachToAsync(client, team.Erp, pending)).StatusCode.Should().Be(HttpStatusCode.Forbidden, role);
            }
        }

        (await StatusOfAsync(marina, team.Erp, pending)).Should().Be("Pending");
    }

    [Fact(DisplayName = "Termos de um projeto de fora")]
    public async Task Outside_GetsNotFound()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var id = await RequestedAsync(await team.ClientOfAsync(team.Marina), team.Erp, homologation, Title);
        var sofia = await team.ClientOfAsync(team.Sofia);

        (await sofia.GetAsync(PathOf(team.Erp), Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await sofia.GetAsync($"{PathOf(team.Erp)}/{id}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await sofia.GetAsync($"{PathOf(team.Erp)}/{id}/file", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await RequestAsync(sofia, team.Erp, homologation, Title)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await AttachToAsync(sofia, team.Erp, id)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Termos de projeto encerrado")]
    public async Task ClosedProject_IsReadOnly()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, scope) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);
        (await SendAsync(marina, HttpMethod.Put, $"/api/projects/{team.Erp}/status", new { status = "Completed" })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        var requested = await RequestAsync(marina, team.Erp, homologation, Title);
        var attached = await AttachAsync(marina, team.Erp, scope);
        var attachedToRequest = await AttachToAsync(marina, team.Erp, id);
        var reminded = await SendAsync(marina, HttpMethod.Post, $"{PathOf(team.Erp)}/{id}/reminder");

        foreach (var response in new[] { requested, attached, attachedToRequest, reminded })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ProjectTeam.ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:project-closed");
        }

        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[acceptance_terms]")).Should().Be(1);
        var list = await ListAsync(marina, team.Erp);
        (list.GetProperty("canRequest").GetBoolean(), list.GetProperty("canAttach").GetBoolean()).Should().Be((false, false));
        (await DetailsAsync(marina, team.Erp, id)).GetProperty("actions").GetProperty("canCancel").GetBoolean().Should().BeFalse();
        // The check in the background leaves the requests of a closed project as they are.
        await SignLocallyAsync(team.App, await LinkOfAsync(marina, team.Erp, id, 0));
        (await DetailsAsync(marina, team.Erp, id)).GetProperty("signers")[0].GetProperty("status").GetString().Should().Be("Pending");
    }

    [Fact(DisplayName = "Filtro por situação")]
    public async Task List_FiltersByStatus_WithTheCounts()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, scope) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var refused = await RequestedAsync(marina, team.Erp, homologation, "Aceite da especificação");
        await RefuseLocallyAsync(team.App, await LinkOfAsync(marina, team.Erp, refused, 0), "Falta o relatório");
        team.App.Clock.Advance(TimeSpan.FromMinutes(5));
        var pending = await RequestedAsync(marina, team.Erp, homologation, Title);
        team.App.Clock.Advance(TimeSpan.FromMinutes(5));
        var attached = await IdOfAsync(await AttachAsync(marina, team.Erp, scope));

        var all = await ListAsync(marina, team.Erp);
        var counts = all.GetProperty("counts");

        all.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Equal(attached, pending, refused);
        (counts.GetProperty("all").GetInt32(), counts.GetProperty("pending").GetInt32(), counts.GetProperty("signed").GetInt32(), counts.GetProperty("refused").GetInt32())
            .Should().Be((3, 1, 1, 1));
        (await ItemsAsync(marina, team.Erp, "?status=Pending")).Select(item => item.GetProperty("id").GetGuid()).Should().Equal(pending);
    }

    // Spec: documentos, requirement "Permissões nos documentos".
    [Fact(DisplayName = "Pasta de termos de quem assinou")]
    public async Task TermsFolder_OfWhoSignedBefore_IsNotShown()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (_, scope) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var carla = await team.ClientOfAsync(team.Carla);
        var term = await IdOfAsync(await AttachAsync(marina, team.Erp, scope));
        // A term signed by Carla in the flow of before keeps her as the person of the request.
        await team.App.ExecuteAsync("UPDATE [documents].[acceptance_terms] SET [RecipientId] = @carla WHERE [Id] = @term", ("@carla", team.Carla), ("@term", term));
        var document = (await DocumentsAsync(marina, team.Erp, (await TermsFolderAsync(marina, team.Erp))!.Value)).Single().GetProperty("id").GetGuid();

        (await TermsFolderAsync(carla, team.Erp)).Should().BeNull();
        (await carla.GetAsync($"/api/projects/{team.Erp}/documents/{document}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await carla.GetAsync($"/api/projects/{team.Erp}/documents/{document}/content", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await carla.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}/documents?search=termo", Cancellation))
            .GetProperty("items").GetArrayLength().Should().Be(0);
        (await carla.GetAsync(PathOf(team.Erp), Cancellation)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // Spec: avisos, requirement "Avisos dos termos de aceite".
    [Fact(DisplayName = "Pedido de aceite por e-mail")]
    public async Task Request_SendsNoEmail_OfTheSystem()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);

        await RequestedAsync(await team.ClientOfAsync(team.Marina), team.Erp, homologation, Title);

        (await team.App.EmailsAsync()).Should().BeEmpty("the provider sends the link to each signer");
        Directory.GetFiles(team.Signatures, "request.json", SearchOption.AllDirectories).Should().ContainSingle();
    }

    // Spec: avisos, requirement "Avisos dos termos de aceite".
    [Fact(DisplayName = "Lembrete do pedido")]
    public async Task Reminder_SendsNoEmail_OfTheSystem()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);

        (await SendAsync(marina, HttpMethod.Post, $"{PathOf(team.Erp)}/{id}/reminder")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await team.App.EmailsAsync()).Should().BeEmpty();
        (await team.App.CountAsync("SELECT COUNT(*) FROM [notifications].[notifications] WHERE [Kind] LIKE 'Acceptance%'")).Should().Be(0);
    }

    // Spec: avisos, requirement "Avisos dos termos de aceite".
    [Fact(DisplayName = "Termo assinado avisa a gestão")]
    public async Task SignedTerm_TellsWhoAsked_AndTheManager()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var id = await RequestedAsync(team.Admin, team.Erp, homologation, Title);

        await SignAllAsync(team, team.Admin, team.Erp, id);

        foreach (var person in new[] { team.Admin, await team.ClientOfAsync(team.Marina) })
        {
            var notification = await NotificationOfAsync(person, "AcceptanceSigned");
            (notification.GetProperty("actor").ValueKind, notification.GetProperty("path").GetString())
                .Should().Be((JsonValueKind.Null, $"/projects/{team.Erp}/acceptances?term={id}"));
            notification.GetProperty("values").GetProperty("title").GetString().Should().Be(Title);
        }

        (await team.App.EmailsToAsync("marina.costa@empresa.example")).Should().BeEmpty("the signed term waits for the daily summary");
        var summary = SummaryOf(await team.App.DailySummaryAsync(), "marina.costa@empresa.example");
        summary.TextBody.Should().Contain("Termos de aceite").And.Contain($"{Title}, do projeto ERP-FIN: assinado por todos os signatários");
    }

    // Spec: avisos, requirement "Avisos dos termos de aceite".
    [Fact(DisplayName = "Recusa avisa a gestão")]
    public async Task Refusal_TellsWhoAsked_AndTheManager()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);

        await RefuseLocallyAsync(team.App, await LinkOfAsync(marina, team.Erp, id, 0), "Falta o relatório de testes");

        (await NotificationOfAsync(marina, "AcceptanceRefused")).GetProperty("actor").ValueKind.Should().Be(JsonValueKind.Null);
        var summary = SummaryOf(await team.App.DailySummaryAsync(), "marina.costa@empresa.example");
        summary.TextBody.Should().Contain($"{Title}, do projeto ERP-FIN: recusado por um signatário")
            .And.NotContain("Falta o relatório").And.NotContain("Ana Souza").And.NotContain("@cliente.example");
    }

    // Spec: avisos, requirement "Avisos dos termos de aceite".
    [Fact(DisplayName = "Aceites pendentes no resumo")]
    public async Task PendingRequestsOfBefore_AreNotInTheSummary_OfThePerson()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        await RequestedBeforeAsync(team, team.Erp, homologation, team.Carla, Title);
        await team.ClientOfAsync(team.Carla);

        var summaries = await team.App.DailySummaryAsync();

        summaries.Where(email => email.ToAddress == "carla.mendes@empresa.example").Should().BeEmpty();
    }

    // Spec: termos-de-aceite, requirement "Termos na exclusão do projeto".
    [Fact(DisplayName = "Excluir projeto com pedido aberto")]
    public async Task ProjectDeletion_CancelsTheOpenDocument_AtTheProvider()
    {
        var (team, provider) = await WithFakeProviderAsync(sqlServer);
        await using var _ = team;
        var (homologation, scope) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        await RequestedAsync(marina, team.Erp, homologation, Title);
        var signed = await RequestedAsync(marina, team.Erp, scope, "Aceite do escopo");
        provider.Cancelled.Clear();
        await AttachToAsync(marina, team.Erp, signed);
        provider.Cancelled.Clear();

        var deleted = await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{team.Erp}/deletion", new { code = "ERP-FIN" });

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleted.Content.ReadAsStringAsync(Cancellation));
        provider.Cancelled.Should().ContainSingle("only the open request is cancelled");
        provider.Deleted.Should().HaveCount(2);
        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[acceptance_terms]")).Should().Be(0);
        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[acceptance_signers]")).Should().Be(0);
    }

    // Spec: termos-de-aceite, requirement "Termos na exclusão do projeto".
    [Fact(DisplayName = "Exclusão com o provedor fora do ar")]
    public async Task ProjectDeletion_GoesOn_WithTheProviderOutOfTheAir()
    {
        var (team, provider) = await WithFakeProviderAsync(sqlServer);
        await using var _ = team;
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        await RequestedAsync(await team.ClientOfAsync(team.Marina), team.Erp, homologation, Title);
        provider.Failing = true;

        var deleted = await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{team.Erp}/deletion", new { code = "ERP-FIN" });

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await team.App.CountAsync("SELECT COUNT(*) FROM [documents].[acceptance_terms]")).Should().Be(0);
    }
}
