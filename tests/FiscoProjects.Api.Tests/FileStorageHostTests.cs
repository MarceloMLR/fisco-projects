using FiscoProjects.Files.Contracts;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Api.Tests;

// Spec: armazenamento-de-arquivos, requirement "Implementação escolhida na configuração".
public sealed class FileStorageHostTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"fiscoprojects-host-files-{Guid.NewGuid():N}");

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Implementação local no desenvolvimento")]
    public async Task Development_StoresTheFilesInTheConfiguredFolder()
    {
        await using var app = await StartAsync(TestApp.Development, ("FileStorage:Local:RootPath", _folder));
        var storage = app.Services.GetRequiredService<IFileStorage>();

        var folder = await storage.EnsureFolderAsync(null, "Projetos/ERP-FIN", Cancellation);
        await storage.UploadAsync(folder.Id, "Contrato.pdf", new MemoryStream([1, 2, 3]), SameNameAction.NewVersion, Cancellation);

        File.ReadAllBytes(Path.Combine(_folder, "Projetos", "ERP-FIN", "Contrato.pdf")).Should().Equal(1, 2, 3);
    }

    [Fact(DisplayName = "Armazenamento não configurado")]
    public async Task WithoutAProvider_TheApplicationStarts_AndAskingForTheStorageExplains()
    {
        await using var app = await StartAsync(TestApp.Production);

        var storage = () => app.Services.GetRequiredService<IFileStorage>();

        storage.Should().Throw<InvalidOperationException>().WithMessage("*FileStorage:Provider*");
    }

    [Fact(DisplayName = "Implementação local em produção")]
    public async Task LocalStorageInProduction_StopsTheApplication()
    {
        var start = () => StartAsync(TestApp.Production, ("FileStorage:Provider", "Local"));

        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain("not allowed in production");
    }

    // Spec: armazenamento-de-arquivos, requirement "Arquivos no SharePoint" (ADR-0120).
    [Fact(DisplayName = "Implementação do SharePoint em produção")]
    public async Task SharePointInProduction_StoresTheFilesInTheSite()
    {
        var site = new FakeSharePoint();
        await using var app = await StartAsync(
            TestApp.Production,
            services =>
            {
                services.AddSingleton<IGraphTokens>(new FixedGraphToken());
                services.AddHttpClient("sharepoint").ConfigurePrimaryHttpMessageHandler(() => site);
            },
            ("FileStorage:Provider", "SharePoint"),
            ("FileStorage:SharePoint:SiteUrl", FakeSharePoint.SiteUrl));
        var storage = app.Services.GetRequiredService<IFileStorage>();

        var folder = await storage.EnsureFolderAsync(null, "ERP-FIN - Implantação/Documentos", Cancellation);
        var stored = await storage.UploadAsync(folder.Id, "Contrato.pdf", new MemoryStream([1, 2, 3]), SameNameAction.NewVersion, Cancellation);

        stored.Size.Should().Be(3);
        site.NamesIn("Documentos", "FiscoProjects/ERP-FIN - Implantação/Documentos").Should().Equal("Contrato.pdf");
    }

    [Fact(DisplayName = "SharePoint sem o endereço do site")]
    public async Task SharePointWithoutTheSite_StopsTheApplication()
    {
        var start = () => StartAsync(TestApp.Development, ("FileStorage:Provider", "SharePoint"), ("FileStorage:SharePoint:SiteUrl", "http://empresa.example/sites/Projetos"));

        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain("FileStorage:SharePoint:SiteUrl");
    }

    [Fact(DisplayName = "Implementação desconhecida")]
    public async Task UnknownProvider_StopsTheApplication()
    {
        var start = () => StartAsync(TestApp.Development, ("FileStorage:Provider", "Dropbox"));

        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain("'Dropbox'");
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static Task<WebApplication> StartAsync(string environment, params (string Key, string Value)[] settings) =>
        StartAsync(environment, _ => { }, settings);

    private static Task<WebApplication> StartAsync(
        string environment, Action<IServiceCollection> services, params (string Key, string Value)[] settings) =>
        TestApp.ComposedAsync(environment, _ => { }, builder =>
        {
            services(builder.Services);
            // No database here: this checks only how the file storage is set up.
            builder.Configuration["Database:MigrateOnStartup"] = "false";
            if (environment == TestApp.Production)
            {
                builder.Configuration["FileStorage:Provider"] = string.Empty;
            }

            foreach (var (key, value) in settings)
            {
                builder.Configuration[key] = value;
            }
        });

    private sealed class FixedGraphToken : IGraphTokens
    {
        public Task<string> GetAsync(CancellationToken cancellationToken) => Task.FromResult(FakeSharePoint.Token);
    }
}
