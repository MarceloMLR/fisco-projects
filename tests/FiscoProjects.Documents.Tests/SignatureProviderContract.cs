using System.Text;
using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Domain;

namespace FiscoProjects.Documents.Tests;

/// <summary>
/// What every provider of signatures does (ADR-0129): the local one now, the Autentique next. An implementation is
/// checked by a test class that inherits these tests and gives the provider and a way for its signers to answer.
/// </summary>
// Spec: termos-de-aceite, requirement "Provedor de assinatura", scenario "Mesmas regras em cada provedor".
public abstract class SignatureProviderContract
{
    protected const string Pdf = "%PDF-1.7\n1 0 obj\n<<>>\nendobj\n%%EOF";

    protected static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    protected abstract ISignatureProvider Provider { get; }

    /// <summary>The signer at <paramref name="index"/> of the document signs, as on the page of the provider.</summary>
    protected abstract Task SignAsync(string documentId, int index);

    /// <summary>The signer at <paramref name="index"/> of the document refuses with the reason.</summary>
    protected abstract Task RefuseAsync(string documentId, int index, string reason);

    protected async Task<SentSignature> SendAsync(int signers = 2)
    {
        await using var content = new MemoryStream(Encoding.ASCII.GetBytes(Pdf));
        return await Provider.SendAsync(
            new SignatureRequest(
                "Aceite da homologação",
                "Por favor, assinem até sexta.",
                "termo-homologacao.pdf",
                content,
                [.. Enumerable.Range(1, signers).Select(index => new SignatureSigner($"Pessoa {index}", $"pessoa{index}@cliente.example"))]),
            Cancellation);
    }

    [Fact(DisplayName = "Mesmas regras em cada provedor")]
    public async Task Send_GivesTheDocument_AndEachSigner()
    {
        var sent = await SendAsync();

        sent.DocumentId.Should().NotBeNullOrWhiteSpace();
        sent.SignerIds.Should().HaveCount(2).And.OnlyHaveUniqueItems();
        (await Provider.StatusAsync(sent.DocumentId, Cancellation)).Select(state => (state.SignerId, state.Status))
            .Should().Equal((sent.SignerIds[0], AcceptanceSignerStatus.Pending), (sent.SignerIds[1], AcceptanceSignerStatus.Pending));
    }

    [Fact(DisplayName = "Situação de cada signatário")]
    public async Task Status_TellsWhoSigned_AndWhen()
    {
        var sent = await SendAsync();

        await SignAsync(sent.DocumentId, 0);

        var states = await Provider.StatusAsync(sent.DocumentId, Cancellation);
        states[0].Status.Should().Be(AcceptanceSignerStatus.Signed);
        states[0].At.Should().NotBeNull();
        states[1].Status.Should().Be(AcceptanceSignerStatus.Pending);
    }

    [Fact(DisplayName = "Recusa com o motivo")]
    public async Task Status_TellsTheRefusal_WithTheReason()
    {
        var sent = await SendAsync();

        await RefuseAsync(sent.DocumentId, 1, "Falta o relatório de testes");

        var refused = (await Provider.StatusAsync(sent.DocumentId, Cancellation))[1];
        (refused.Status, refused.Reason).Should().Be((AcceptanceSignerStatus.Refused, "Falta o relatório de testes"));
    }

    [Fact(DisplayName = "PDF assinado por todos")]
    public async Task SignedFile_IsThePdf_WhenEverySignerSigned()
    {
        var sent = await SendAsync();
        await SignAsync(sent.DocumentId, 0);
        var early = () => Provider.SignedFileAsync(sent.DocumentId, Cancellation);
        await early.Should().ThrowAsync<SignatureProviderException>();

        await SignAsync(sent.DocumentId, 1);

        var signed = await Provider.SignedFileAsync(sent.DocumentId, Cancellation);
        signed.Name.Should().EndWith(".pdf");
        await using (signed.Content)
        {
            using var reader = new StreamReader(signed.Content, Encoding.ASCII);
            (await reader.ReadToEndAsync(Cancellation)).Should().StartWith("%PDF-");
        }
    }

    [Fact(DisplayName = "Lembrar quem não assinou")]
    public async Task Remind_KeepsTheStatus()
    {
        var sent = await SendAsync();
        await SignAsync(sent.DocumentId, 0);

        await Provider.RemindAsync(sent.DocumentId, Cancellation);

        (await Provider.StatusAsync(sent.DocumentId, Cancellation)).Select(state => state.Status)
            .Should().Equal(AcceptanceSignerStatus.Signed, AcceptanceSignerStatus.Pending);
    }

    [Fact(DisplayName = "Cancelar o documento")]
    public async Task Cancel_KeepsTheDocument_FromBeingSigned()
    {
        var sent = await SendAsync(signers: 1);

        await Provider.CancelAsync(sent.DocumentId, Cancellation);

        var remind = () => Provider.RemindAsync(sent.DocumentId, Cancellation);
        await remind.Should().ThrowAsync<SignatureProviderException>();
        await Provider.CancelAsync(sent.DocumentId, Cancellation);
    }

    [Fact(DisplayName = "Excluir o documento")]
    public async Task Delete_ForgetsTheDocument()
    {
        var sent = await SendAsync(signers: 1);
        await Provider.CancelAsync(sent.DocumentId, Cancellation);

        await Provider.DeleteAsync(sent.DocumentId, Cancellation);
        await Provider.DeleteAsync(sent.DocumentId, Cancellation);

        var status = () => Provider.StatusAsync(sent.DocumentId, Cancellation);
        await status.Should().ThrowAsync<SignatureProviderException>();
    }

    [Fact(DisplayName = "Documento que o provedor não tem")]
    public async Task UnknownDocument_IsAFailure()
    {
        var status = () => Provider.StatusAsync("nao-existe", Cancellation);
        var remind = () => Provider.RemindAsync("nao-existe", Cancellation);

        await status.Should().ThrowAsync<SignatureProviderException>();
        await remind.Should().ThrowAsync<SignatureProviderException>();
    }
}
