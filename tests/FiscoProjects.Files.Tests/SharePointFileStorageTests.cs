using System.Text;
using FiscoProjects.Files.Contracts;
using FiscoProjects.Files.Infrastructure;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Files.Tests;

/// <summary>
/// The SharePoint implementation (ADR-0120), against the contract and the simulated site of Microsoft Graph: the
/// library "Documentos" and the root folder "FiscoProjects" in it.
/// </summary>
// Spec: armazenamento-de-arquivos, requirement "Arquivos no SharePoint".
public sealed class SharePointFileStorageTests : FileStorageContract
{
    private readonly FakeSharePoint _site = new();

    public SharePointFileStorageTests()
    {
        Storage = StorageOf(new SharePointStorageOptions { SiteUrl = FakeSharePoint.SiteUrl });
    }

    protected override IFileStorage Storage { get; }

    [Fact(DisplayName = "Pasta raiz criada no SharePoint")]
    public async Task TheRootFolder_IsCreatedInTheLibraryOnFirstUse()
    {
        await Storage.EnsureFolderAsync(null, "ERP-FIN - Implantação/Chamados/#101", Cancellation);
        await Storage.ListFoldersAsync(null, Cancellation);
        _site.Requests.Should().ContainSingle(request => request.StartsWith("GET sites/empresa.sharepoint.example:/sites/Projetos", StringComparison.Ordinal),
            "the site is found once");
        var otherLibrary = StorageOf(new SharePointStorageOptions
        {
            SiteUrl = FakeSharePoint.SiteUrl,
            Library = "arquivo morto",
            RootFolder = "Sistemas/FiscoProjects",
        });
        await otherLibrary.EnsureFolderAsync(null, "Logos", Cancellation);

        _site.NamesIn("Documentos", string.Empty).Should().Equal("FiscoProjects");
        _site.NamesIn("Documentos", "FiscoProjects").Should().Equal("ERP-FIN - Implantação");
        _site.NamesIn("Documentos", "FiscoProjects/ERP-FIN - Implantação/Chamados").Should().Equal("#101");
        _site.NamesIn("Arquivo morto", "Sistemas/FiscoProjects").Should().Equal("Logos");
    }

    [Fact(DisplayName = "Site ou biblioteca que não existe")]
    public async Task UnknownSiteOrLibrary_SaysWhatIsMissing()
    {
        var unknownSite = StorageOf(new SharePointStorageOptions { SiteUrl = "https://empresa.sharepoint.example/sites/Outro" });
        var unknownLibrary = StorageOf(new SharePointStorageOptions { SiteUrl = FakeSharePoint.SiteUrl, Library = "Contratos" });

        var site = () => unknownSite.EnsureFolderAsync(null, "Projetos", Cancellation);
        var library = () => unknownLibrary.ListFoldersAsync(null, Cancellation);

        (await site.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("Sites.Selected");
        (await library.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("'Contratos'");
    }

    [Fact(DisplayName = "Item fora da pasta raiz")]
    public async Task ItemsOutsideTheRoot_AreNeverOpenedNorRemoved()
    {
        await Storage.EnsureFolderAsync(null, "Projetos", Cancellation);
        var outside = _site.AddFile("Documentos", string.Empty, "Planilha da diretoria.xlsx", Encoding.UTF8.GetBytes("de fora"));

        (await Storage.OpenAsync(outside, null, Cancellation)).Should().BeNull();
        (await Storage.DeleteFileAsync(outside, Cancellation)).Should().BeFalse();
        (await Storage.FolderOfAsync(outside, Cancellation)).Should().BeNull();
        foreach (var id in new[] { "Projetos/Contrato.pdf", "../fora.txt", string.Empty })
        {
            (await Storage.OpenAsync(id, null, Cancellation)).Should().BeNull(id);
            (await Storage.DeleteFolderIfEmptyAsync(id, Cancellation)).Should().BeFalse(id);
        }

        _site.NamesIn("Documentos", string.Empty).Should().Equal("FiscoProjects", "Planilha da diretoria.xlsx");
    }

    [Fact(DisplayName = "Graph ocupado")]
    public async Task ThrottledRequests_AreSentAgain()
    {
        var folder = await Storage.EnsureFolderAsync(null, "Projetos", Cancellation);
        _site.ThrottleNext = 2;

        var stored = await UploadAsync(folder.Id, "Contrato.pdf", "contrato", SameNameAction.NewVersion);

        stored.Name.Should().Be("Contrato.pdf");
        await using var download = await Storage.OpenAsync(stored.Id, null, Cancellation);
        (await ReadAsync(download!)).Should().Be("contrato");
    }

    [Fact(DisplayName = "Pastas em páginas")]
    public async Task ManyFolders_AreListedFromEveryPage()
    {
        var project = await Storage.EnsureFolderAsync(null, "Projetos", Cancellation);
        foreach (var number in new[] { 105, 101, 104, 103, 102 })
        {
            await Storage.EnsureFolderAsync(project.Id, $"#{number}", Cancellation);
        }

        _site.PageSize = 2;

        (await NamesAsync(project.Id)).Should().Equal("#101", "#102", "#103", "#104", "#105");
    }

    private SharePointFileStorage StorageOf(SharePointStorageOptions options) => new(new Clients(_site), new FixedTokens(), options);

    private sealed class Clients(FakeSharePoint site) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(site, disposeHandler: false);
    }

    private sealed class FixedTokens : IGraphTokens
    {
        public Task<string> GetAsync(CancellationToken cancellationToken) => Task.FromResult(FakeSharePoint.Token);
    }
}
