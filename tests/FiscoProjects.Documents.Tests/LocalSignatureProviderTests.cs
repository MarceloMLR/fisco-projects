using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Infrastructure.Signatures;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiscoProjects.Documents.Tests;

/// <summary>The local provider, in a folder of its own for each test, against the contract and its page.</summary>
// Spec: termos-de-aceite, requirement "Provedor local".
public sealed class LocalSignatureProviderTests : SignatureProviderContract, IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"fiscoprojects-signatures-{Guid.NewGuid():N}");
    private readonly LocalSignatureProvider _local;

    public LocalSignatureProviderTests()
    {
        _local = new LocalSignatureProvider(_folder, TimeProvider.System, NullLogger<LocalSignatureProvider>.Instance);
    }

    protected override ISignatureProvider Provider => _local;

    public void Dispose()
    {
        _local.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    protected override async Task SignAsync(string documentId, int index) =>
        (await _local.SignAsync(await TokenOfAsync(documentId, index), Cancellation)).Should().Be(documentId);

    protected override async Task RefuseAsync(string documentId, int index, string reason) =>
        (await _local.RefuseAsync(await TokenOfAsync(documentId, index), reason, Cancellation)).Should().Be(documentId);

    [Fact(DisplayName = "Página local do signatário")]
    public async Task Page_ShowsTheRequest_ToThePendingSigner()
    {
        var sent = await SendAsync();
        var token = await TokenOfAsync(sent.DocumentId, 1);

        var signing = await _local.FindAsync(token, Cancellation);

        signing.Should().Be(new LocalSigning(sent.DocumentId, "Pessoa 2", "Aceite da homologação", "Por favor, assinem até sexta.", "termo-homologacao.pdf"));
        await using var content = await _local.OpenAsync(token, Cancellation);
        content.Should().NotBeNull();
    }

    [Fact(DisplayName = "Link local de outro termo ou já usado")]
    public async Task UsedOrUnknownLinks_AreNoLongerValid()
    {
        var sent = await SendAsync();
        var token = await TokenOfAsync(sent.DocumentId, 0);
        await _local.SignAsync(token, Cancellation);

        (await _local.FindAsync(token, Cancellation)).Should().BeNull();
        (await _local.OpenAsync(token, Cancellation)).Should().BeNull();
        (await _local.SignAsync(token, Cancellation)).Should().BeNull();
        (await _local.FindAsync(new string('a', 43), Cancellation)).Should().BeNull();
        (await _local.FindAsync("../../etc", Cancellation)).Should().BeNull();
        (await _local.LinkOfAsync(sent.DocumentId, sent.SignerIds[0], Cancellation)).Should().BeNull();
    }

    [Fact(DisplayName = "Links de um pedido cancelado")]
    public async Task CancelledRequest_HasNoLinks()
    {
        var sent = await SendAsync();
        var token = await TokenOfAsync(sent.DocumentId, 0);

        await _local.CancelAsync(sent.DocumentId, Cancellation);

        (await _local.FindAsync(token, Cancellation)).Should().BeNull();
        (await _local.LinkOfAsync(sent.DocumentId, sent.SignerIds[1], Cancellation)).Should().BeNull();
    }

    [Fact(DisplayName = "Recusa fecha o pedido local")]
    public async Task Refusal_ClosesTheRequest()
    {
        var sent = await SendAsync();
        var other = await TokenOfAsync(sent.DocumentId, 1);

        await RefuseAsync(sent.DocumentId, 0, "Falta o relatório de testes");

        (await _local.FindAsync(other, Cancellation)).Should().BeNull();
    }

    private async Task<string> TokenOfAsync(string documentId, int index)
    {
        var signer = (await _local.StatusAsync(documentId, Cancellation))[index].SignerId;
        var link = await _local.LinkOfAsync(documentId, signer, Cancellation);
        link.Should().StartWith("/sign/");
        return link!["/sign/".Length..];
    }
}
