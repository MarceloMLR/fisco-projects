using System.Text;
using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Domain;
using FiscoProjects.Documents.Infrastructure.Signatures;
using FiscoProjects.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Documents.Tests;

/// <summary>
/// The Autentique against the contract of the providers and in what is its own, with its API simulated inside the
/// process (FakeAutentique): the term and the signers it gets, the sandbox, the failures without the key nor the
/// e-mails, the key only at its addresses and the notices (ADR-0130).
/// </summary>
// Spec: termos-de-aceite, requirements "Assinatura pela Autentique", "Sandbox da Autentique" and "Aviso do provedor".
public sealed class AutentiqueSignatureProviderTests : SignatureProviderContract, IDisposable
{
    private const string Secret = "segredo-do-aviso-dos-testes";

    private readonly FakeAutentique _autentique = new();
    private readonly SignatureOptions _options = new()
    {
        Provider = SignatureOptions.AutentiqueProvider,
        Autentique = { ApiKey = FakeAutentique.Key, WebhookSecret = Secret },
    };

    private readonly AutentiqueSignatureProvider _provider;

    public AutentiqueSignatureProviderTests()
    {
        _provider = new AutentiqueSignatureProvider(
            new Clients(_autentique), Options.Create(_options), TimeProvider.System, NullLogger<AutentiqueSignatureProvider>.Instance);
    }

    protected override ISignatureProvider Provider => _provider;

    public void Dispose() => _autentique.Dispose();

    protected override Task SignAsync(string documentId, int index)
    {
        _autentique.Sign(documentId, index, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    protected override Task RefuseAsync(string documentId, int index, string reason)
    {
        _autentique.Refuse(documentId, index, reason, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    [Fact(DisplayName = "Termo e signatários na Autentique")]
    public async Task Send_GivesTheTerm_AndEachSignerByEmail()
    {
        var sent = await SendAsync();

        var document = _autentique.DocumentOf(sent.DocumentId);
        (document.Name, document.Message, document.Refusable, document.Sandbox).Should().Be(
            ("Aceite da homologação", "Por favor, assinem até sexta.", true, false));
        Encoding.ASCII.GetString(document.File).Should().Be(Pdf);
        document.FileName.Should().Be("termo-homologacao.pdf");
        document.Signers.Select(signer => (signer.Name, signer.Email, signer.Action)).Should().Equal(
            ("Pessoa 1", "pessoa1@cliente.example", "SIGN"), ("Pessoa 2", "pessoa2@cliente.example", "SIGN"));
        // The account that created the document is one of its signatures, but not a signer of the request.
        sent.SignerIds.Should().Equal(document.Signers.Select(signer => signer.PublicId));
        _autentique.Requests.Should().OnlyContain(request => request.HadKey);
    }

    [Fact(DisplayName = "Documento de teste no sandbox")]
    public async Task Sandbox_MakesTestDocuments()
    {
        _options.Autentique.Sandbox = true;

        var sent = await SendAsync(signers: 1);

        _autentique.DocumentOf(sent.DocumentId).Sandbox.Should().BeTrue();
    }

    [Fact(DisplayName = "Data e motivo de cada signatário")]
    public async Task Status_HasTheMoments_AndTheReason()
    {
        var sent = await SendAsync();
        var signedAt = new DateTime(2026, 10, 30, 14, 20, 0, DateTimeKind.Utc);
        _autentique.Sign(sent.DocumentId, 0, signedAt);
        _autentique.Refuse(sent.DocumentId, 1, "Falta o anexo", signedAt.AddHours(1));

        var states = await Provider.StatusAsync(sent.DocumentId, Cancellation);

        states.Should().Equal(
            new SignerState(sent.SignerIds[0], AcceptanceSignerStatus.Signed, signedAt),
            new SignerState(sent.SignerIds[1], AcceptanceSignerStatus.Refused, signedAt.AddHours(1), "Falta o anexo"));
        states[0].At!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact(DisplayName = "Cancelar bloqueia na Autentique")]
    public async Task Cancel_BlocksTheDocument_EvenWithASignature()
    {
        var sent = await SendAsync();
        _autentique.Sign(sent.DocumentId, 0, DateTime.UtcNow);

        await Provider.CancelAsync(sent.DocumentId, Cancellation);

        _autentique.DocumentOf(sent.DocumentId).DeadlineAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        var sign = () => _autentique.Sign(sent.DocumentId, 1, DateTime.UtcNow);
        sign.Should().Throw<InvalidOperationException>();
    }

    [Fact(DisplayName = "Documento assinado por todos fica como está")]
    public async Task Cancel_KeepsADocumentSignedByEverybody()
    {
        var sent = await SendAsync(signers: 1);
        _autentique.Sign(sent.DocumentId, 0, DateTime.UtcNow);

        await Provider.CancelAsync(sent.DocumentId, Cancellation);

        _autentique.DocumentOf(sent.DocumentId).DeadlineAt.Should().BeNull();
    }

    [Fact(DisplayName = "Lembrete só a quem não assinou")]
    public async Task Remind_SendsAgain_OnlyToThePendingSigners()
    {
        var sent = await SendAsync();
        _autentique.Sign(sent.DocumentId, 0, DateTime.UtcNow);

        await Provider.RemindAsync(sent.DocumentId, Cancellation);

        _autentique.DocumentOf(sent.DocumentId).Resent.Should().Equal(sent.SignerIds[1]);
    }

    [Fact(DisplayName = "Falhas da Autentique sem a chave e os e-mails")]
    public async Task Failures_TellTheOperation_WithoutTheKeyNorTheEmails()
    {
        _autentique.Limited = true;
        var limited = await FailureOfSendAsync();
        _autentique.Limited = false;
        _autentique.Down = true;
        var down = await FailureOfSendAsync();
        _autentique.Down = false;
        _options.Autentique.ApiKey = "outra-chave-que-nao-vale";
        var refused = await FailureOfSendAsync();
        _options.Autentique.ApiKey = FakeAutentique.Key;
        var unknown = () => Provider.StatusAsync(new string('a', 50), Cancellation);

        limited.Should().Contain("send").And.Contain("HTTP 429");
        down.Should().Contain("HTTP 503");
        refused.Should().Contain("HTTP 401").And.NotContain("outra-chave");
        (await unknown.Should().ThrowAsync<SignatureProviderException>()).Which.Message.Should().Contain("document_not_found");
        foreach (var message in new[] { limited, down, refused })
        {
            message.Should().NotContain(FakeAutentique.Key).And.NotContain("@cliente.example").And.NotContain("homologação");
        }
    }

    [Fact(DisplayName = "Chave só nos endereços da Autentique")]
    public async Task SignedFile_TakesTheKey_OnlyToTheAutentique()
    {
        var atAutentique = await SendAsync(signers: 1);
        _autentique.Sign(atAutentique.DocumentId, 0, DateTime.UtcNow);
        await using ((await Provider.SignedFileAsync(atAutentique.DocumentId, Cancellation)).Content)
        {
        }

        _autentique.SignedFilesAt = "https://arquivos.armazenamento.example/documentos/";
        var elsewhere = await SendAsync(signers: 1);
        _autentique.Sign(elsewhere.DocumentId, 0, DateTime.UtcNow);
        var signed = await Provider.SignedFileAsync(elsewhere.DocumentId, Cancellation);
        await using (signed.Content)
        {
            signed.Name.Should().Be("Aceite da homologação.pdf");
        }

        _autentique.Requests.Where(request => request.Operation == "download").Select(request => (request.Address.Host, request.HadKey))
            .Should().Equal(("api.autentique.com.br", true), ("arquivos.armazenamento.example", false));
    }

    [Fact(DisplayName = "Aviso da Autentique conferido")]
    public void Notice_IsChecked_ByTheSignatureOfTheBody()
    {
        var finished = FakeAutentique.NoticeOf("document.finished", "da0454a42b8859527151f6cd08430a40850cc873e35b52714");
        var accepted = FakeAutentique.NoticeOf("signature.accepted", "f48a8b465d02dd87559e08f06c41e3b6d548c4d7ad835eb0f");
        // The shape of some examples of the Autentique: the resource at event.data.object.
        const string Nested = """{"event":{"type":"signature.rejected","data":{"object":{"object":"signature","document":"89c7d2ab31f9f5a13b3d20ecf53319af387e54d240ae7be993"}}}}""";

        Read(finished, FakeAutentique.SignatureOf(finished, Secret))
            .Should().Be(new SignatureNotice(true, "da0454a42b8859527151f6cd08430a40850cc873e35b52714"));
        Read(accepted, FakeAutentique.SignatureOf(accepted, Secret).ToUpperInvariant())
            .Should().Be(new SignatureNotice(true, "f48a8b465d02dd87559e08f06c41e3b6d548c4d7ad835eb0f"));
        Read(Nested, FakeAutentique.SignatureOf(Nested, Secret))
            .Should().Be(new SignatureNotice(true, "89c7d2ab31f9f5a13b3d20ecf53319af387e54d240ae7be993"));
        Read("não é JSON", FakeAutentique.SignatureOf("não é JSON", Secret)).Should().Be(new SignatureNotice(true, null));
    }

    [Fact(DisplayName = "Aviso sem a assinatura certa")]
    public void Notice_WithoutTheRightSignature_IsForged()
    {
        var notice = FakeAutentique.NoticeOf("document.finished", "da0454a42b8859527151f6cd08430a40850cc873e35b52714");

        Read(notice, FakeAutentique.SignatureOf(notice, "outro-segredo")).Should().Be(SignatureNotice.Forged);
        Read(notice, FakeAutentique.SignatureOf(notice + " ", Secret)).Should().Be(SignatureNotice.Forged);
        Read(notice, null).Should().Be(SignatureNotice.Forged);
        Read(notice, "zz").Should().Be(SignatureNotice.Forged);
        _options.Autentique.WebhookSecret = null;
        Read(notice, FakeAutentique.SignatureOf(notice, Secret)).Should().Be(SignatureNotice.Forged);
    }

    private SignatureNotice Read(string body, string? signature) =>
        _provider.Read(Encoding.UTF8.GetBytes(body), name => name.Equals("x-autentique-signature", StringComparison.OrdinalIgnoreCase) ? signature : null);

    private async Task<string> FailureOfSendAsync()
    {
        var send = () => SendAsync();
        return (await send.Should().ThrowAsync<SignatureProviderException>()).Which.Message;
    }

    /// <summary>Every client of the provider through the simulated Autentique.</summary>
    private sealed class Clients(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
