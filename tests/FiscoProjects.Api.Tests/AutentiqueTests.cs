using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using static FiscoProjects.Api.Tests.AcceptanceRequests;

namespace FiscoProjects.Api.Tests;

// Spec: termos-de-aceite, requirements "Assinatura pela Autentique", "Sandbox da Autentique", "Aviso do provedor",
// "Signatários da demonstração no provedor local" and "Lembrar e cancelar" (ADR-0130). The Autentique is simulated inside the process
// (FakeAutentique).
public sealed class AutentiqueTests(SqlServerFixture sqlServer) : IDisposable
{
    private const string Title = "Aceite da homologação";
    private const string Secret = "segredo-do-aviso-dos-testes";

    private readonly FakeAutentique _autentique = new();

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public void Dispose() => _autentique.Dispose();

    [Fact(DisplayName = "Pedido pela Autentique")]
    public async Task Request_GoesToTheAutentique_WhichSendsTheLinks()
    {
        await using var team = await StartAsync();
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);

        var id = await RequestedAsync(marina, team.Erp, homologation, Title);

        var document = _autentique.Documents.Should().ContainSingle().Subject;
        (document.Name, document.Message, document.Refusable, document.Sandbox)
            .Should().Be((Title, "Os testes de homologação terminaram sem pendências. Assinem o termo, por favor.", true, false));
        document.Signers.Select(signer => (signer.Name, signer.Email))
            .Should().Equal(("Ana Souza", "ana.souza@cliente.example"), ("Bruno Lima", "bruno.lima@cliente.example"));
        var details = await DetailsAsync(marina, team.Erp, id);
        details.GetProperty("term").GetProperty("status").GetString().Should().Be("Pending");
        // The links go by e-mail from the Autentique: the drawer has none, and the FiscoProjects sends no e-mail.
        details.GetProperty("signers").EnumerateArray().Select(signer => signer.GetProperty("signingLink").ValueKind)
            .Should().AllBeEquivalentTo(JsonValueKind.Null);
        (await team.App.EmailsAsync()).Should().NotContain(email => email.ToAddress.EndsWith("@cliente.example", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Termo assinado pela Autentique")]
    public async Task SignedAtTheAutentique_IsTheApprovedDocument_AndReachesTheMilestone()
    {
        await using var team = await StartAsync();
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);
        var document = _autentique.Documents.Single();
        _autentique.Sign(document.Id, 0, new DateTime(2026, 10, 30, 14, 0, 0, DateTimeKind.Utc));
        _autentique.Sign(document.Id, 1, new DateTime(2026, 10, 30, 15, 0, 0, DateTimeKind.Utc));

        (await CheckAsync(team)).Should().Be(1);

        var details = await DetailsAsync(marina, team.Erp, id);
        details.GetProperty("term").GetProperty("status").GetString().Should().Be("Signed");
        details.GetProperty("signers").EnumerateArray().Select(signer => signer.GetProperty("respondedAt").GetDateTime())
            .Should().Equal(new DateTime(2026, 10, 30, 14, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 30, 15, 0, 0, DateTimeKind.Utc));
        var signed = details.GetProperty("signed");
        signed.GetProperty("name").GetString().Should().Be($"{Title}.pdf");
        var content = await marina.GetByteArrayAsync($"/api/projects/{team.Erp}/documents/{signed.GetProperty("documentId").GetGuid()}/content", Cancellation);
        Encoding.ASCII.GetString(content).Should().StartWith("%PDF-").And.Contain("assinado pela Autentique");
        var milestone = (await marina.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}/schedule", Cancellation))
            .GetProperty("phases").EnumerateArray().SelectMany(phase => phase.GetProperty("items").EnumerateArray())
            .Single(item => item.GetProperty("id").GetGuid() == homologation);
        milestone.GetProperty("reachedOn").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact(DisplayName = "Cancelar na Autentique")]
    public async Task Cancel_BlocksTheDocument_AtTheAutentique()
    {
        await using var team = await StartAsync();
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);

        var cancelled = await ProjectTeam.SendAsync(marina, HttpMethod.Delete, $"{PathOf(team.Erp)}/{id}");

        cancelled.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _autentique.Documents.Single().DeadlineAt.Should().NotBeNull();
        (await marina.GetFromJsonAsync<JsonElement>(PathOf(team.Erp), Cancellation)).GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "Lembrete logo depois do envio")]
    public async Task ARemindRefusedForBeingTooSoon_IsAConflict_OutOfTheHistory()
    {
        await using var team = await StartAsync();
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);
        _autentique.ResendBlocked = true;

        var remind = await ProjectTeam.SendAsync(marina, HttpMethod.Post, $"{PathOf(team.Erp)}/{id}/reminder");

        remind.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(remind)).Should().Be("urn:fiscoprojects:problem:signature-reminded-recently");
        (await DetailsAsync(marina, team.Erp, id)).GetProperty("events").EnumerateArray()
            .Select(item => item.GetProperty("kind").GetString()).Should().Equal("Requested");
    }

    [Fact(DisplayName = "Falha da Autentique sem dados pessoais")]
    public async Task AFailureOfTheAutentique_SavesNothing_AndTellsNoPersonalData()
    {
        await using var team = await StartAsync();
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        _autentique.Limited = true;

        var response = await RequestAsync(marina, team.Erp, homologation, Title);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        (await ProjectTeam.ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:signature-provider-failed");
        (await response.Content.ReadAsStringAsync(Cancellation)).Should().NotContain(FakeAutentique.Key).And.NotContain("@cliente.example");
        (await marina.GetFromJsonAsync<JsonElement>(PathOf(team.Erp), Cancellation)).GetProperty("items").GetArrayLength().Should().Be(0);
        var requests = Path.Combine(team.Files, "ERP-FIN - Implantação do ERP financeiro", "Pedidos de aceite");
        (Directory.Exists(requests) ? Directory.GetFiles(requests) : []).Should().BeEmpty();
    }

    [Fact(DisplayName = "Sandbox fora da produção")]
    public async Task Sandbox_MakesTheDocumentsOfTest()
    {
        await using var team = await StartAsync(builder => builder.Configuration["Signature:Autentique:Sandbox"] = "true");
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);

        await RequestedAsync(marina, team.Erp, homologation, Title);

        _autentique.Documents.Single().Sandbox.Should().BeTrue();
    }

    [Fact(DisplayName = "Aviso de assinatura")]
    public async Task ANotice_AppliesTheSignatures_RightAway()
    {
        await using var team = await StartAsync();
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);
        var document = _autentique.Documents.Single();
        _autentique.Sign(document.Id, 0, DateTime.UtcNow);
        _autentique.Sign(document.Id, 1, DateTime.UtcNow);

        var notice = await NoticeAsync(team, FakeAutentique.NoticeOf("document.finished", document.Id), Secret);

        notice.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StatusEventuallyAsync(marina, team.Erp, id, "Signed")).Should().Be("Signed");
    }

    [Fact(DisplayName = "Aviso forjado")]
    public async Task AForgedNotice_ChangesNothing()
    {
        await using var team = await StartAsync();
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);
        var document = _autentique.Documents.Single();
        _autentique.Sign(document.Id, 0, DateTime.UtcNow);
        _autentique.Sign(document.Id, 1, DateTime.UtcNow);
        var body = FakeAutentique.NoticeOf("document.finished", document.Id);

        var forged = await NoticeAsync(team, body, "outro-segredo");
        using var anonymous = team.App.NewClient();
        using var unsigned = new StringContent(body, Encoding.UTF8, "application/json");
        var withoutSignature = await anonymous.PostAsync("/signature-notices", unsigned, Cancellation);

        forged.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        withoutSignature.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await Task.Delay(TimeSpan.FromSeconds(1), Cancellation);
        (await DetailsAsync(marina, team.Erp, id)).GetProperty("term").GetProperty("status").GetString().Should().Be("Pending");
        _autentique.Requests.Should().NotContain(request => request.Operation == "document");
    }

    [Fact(DisplayName = "Aviso de documento de fora")]
    public async Task ANoticeOfAnotherDocument_ChangesNothing()
    {
        await using var team = await StartAsync();
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await RequestedAsync(marina, team.Erp, homologation, Title);

        var notice = await NoticeAsync(team, FakeAutentique.NoticeOf("signature.accepted", "f48a8b465d02dd87559e08f06c41e3b6d548c4d7ad835eb0f"), Secret);

        notice.StatusCode.Should().Be(HttpStatusCode.OK);
        await Task.Delay(TimeSpan.FromSeconds(1), Cancellation);
        (await DetailsAsync(marina, team.Erp, id)).GetProperty("term").GetProperty("status").GetString().Should().Be("Pending");
    }

    [Fact(DisplayName = "Aviso sem segredo configurado")]
    public async Task WithoutTheSecret_OrWithTheLocalProvider_ThereIsNoNotice()
    {
        var body = FakeAutentique.NoticeOf("document.finished", "da0454a42b8859527151f6cd08430a40850cc873e35b52714");
        HttpStatusCode withoutSecret;
        await using (var team = await StartAsync(builder => builder.Configuration["Signature:Autentique:WebhookSecret"] = string.Empty))
        {
            withoutSecret = (await NoticeAsync(team, body, Secret)).StatusCode;
        }

        await using var local = await ProjectTeam.StartAsync(sqlServer);
        var withLocal = await NoticeAsync(local, body, Secret);

        withoutSecret.Should().Be(HttpStatusCode.NotFound);
        withLocal.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Pedido aos signatários da demonstração")]
    public async Task TheSignersOfTheDemonstration_SignAtTheLocalProvider()
    {
        await using var team = await StartAsync(DemonstrationDomain);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);

        var id = await RequestedAsync(marina, team.Erp, homologation, Title);
        await SignAllAsync(team, marina, team.Erp, id);

        _autentique.Documents.Should().BeEmpty();
        _autentique.Requests.Should().BeEmpty();
        (await DetailsAsync(marina, team.Erp, id)).GetProperty("term").GetProperty("status").GetString().Should().Be("Signed");
    }

    [Fact(DisplayName = "Pedido com signatário de fora da demonstração")]
    public async Task ASignerOutsideTheDemonstration_TakesTheRequestToTheAutentique()
    {
        await using var team = await StartAsync(DemonstrationDomain);
        var (homologation, _) = await MilestonesAsync(team, team.Erp);
        var marina = await team.ClientOfAsync(team.Marina);

        var id = await RequestedAsync(marina, team.Erp, homologation, Title,
            [("Ana Souza", "ana.souza@cliente.example"), ("Helena Duarte", "helena.duarte@contoso.example")]);

        _autentique.Documents.Single().Signers.Select(signer => signer.Email)
            .Should().Equal("ana.souza@cliente.example", "helena.duarte@contoso.example");
        (await DetailsAsync(marina, team.Erp, id)).GetProperty("signers").EnumerateArray()
            .Select(signer => signer.GetProperty("signingLink").ValueKind).Should().AllBeEquivalentTo(JsonValueKind.Null);
    }

    private static void DemonstrationDomain(WebApplicationBuilder builder) =>
        builder.Configuration["Signature:LocalDomains:0"] = "cliente.example";

    /// <summary>The team with the Autentique, with the notices, as an installation in the Azure (ADR-0130).</summary>
    private Task<ProjectTeam> StartAsync(Action<WebApplicationBuilder>? configure = null) =>
        ProjectTeam.StartAsync(sqlServer, builder =>
        {
            builder.Configuration["Signature:Provider"] = "Autentique";
            builder.Configuration["Signature:Autentique:ApiKey"] = FakeAutentique.Key;
            builder.Configuration["Signature:Autentique:WebhookSecret"] = Secret;
            builder.Services.AddHttpClient("autentique").ConfigurePrimaryHttpMessageHandler(() => _autentique);
            configure?.Invoke(builder);
        });

    /// <summary>The Autentique posts a notice, signed with the secret, without a session nor X-CSRF.</summary>
    private static async Task<HttpResponseMessage> NoticeAsync(ProjectTeam team, string body, string secret)
    {
        using var client = team.App.NewClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/signature-notices")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Autentique-Signature", FakeAutentique.SignatureOf(body, secret));
        return await client.SendAsync(request, Cancellation);
    }

    /// <summary>The status of the term once the check in the background, woken by the notice, has run.</summary>
    private static async Task<string?> StatusEventuallyAsync(HttpClient client, Guid project, Guid term, string expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            var status = (await DetailsAsync(client, project, term)).GetProperty("term").GetProperty("status").GetString();
            if (status == expected || DateTime.UtcNow > deadline)
            {
                return status;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), Cancellation);
        }
    }
}
