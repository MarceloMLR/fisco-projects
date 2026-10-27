using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace FiscoProjects.Notifications.Tests;

public sealed class LocalEmailTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "fiscoprojects-emails-" + Guid.NewGuid().ToString("N"));

    [Fact(DisplayName = "E-mail gravado sem envio")]
    public async Task WritesTheEmailToTheFolder()
    {
        var sender = new LocalEmailSender(_folder, "projetos@empresa.example");

        await sender.SendAsync(
            new EmailMessage("marina.costa@empresa.example", "Marina Costa", "Teste de envio", "<p>Olá, Marina.</p>", "Olá, Marina."),
            TestContext.Current.CancellationToken);

        var file = Directory.GetFiles(_folder, "*.eml").Should().ContainSingle().Subject;
        var content = await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken);
        content.Should().Contain("marina.costa@empresa.example").And.Contain("projetos@empresa.example");
        content.Should().Contain("Subject: Teste de envio");
        content.Should().Contain("text/plain").And.Contain("text/html");
    }

    [Fact]
    public void TheLocalSenderIsRefusedInProduction()
    {
        var validation = new EmailOptionsValidation(new Environment(Environments.Production));

        var result = validation.Validate(null, new EmailOptions { Provider = EmailOptions.LocalProvider });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("not allowed in production");
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("Local", true)]
    [InlineData("Graph", false)]
    [InlineData("Smtp", false)]
    public void TheProviderIsOneTheApplicationKnows(string provider, bool valid)
    {
        var validation = new EmailOptionsValidation(new Environment(Environments.Development));

        var result = validation.Validate(null, new EmailOptions { Provider = provider });

        result.Succeeded.Should().Be(valid);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("projetos", false)]
    [InlineData("FiscoProjects <projetos@empresa.example>", false)]
    [InlineData("projetos@empresa.example", true)]
    public void TheGraphNeedsTheAddressOfTheMailbox(string? mailbox, bool valid)
    {
        var validation = new EmailOptionsValidation(new Environment(Environments.Production));

        var result = validation.Validate(null, new EmailOptions { Provider = EmailOptions.GraphProvider, Mailbox = mailbox });

        result.Succeeded.Should().Be(valid);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("marina@empresa.example", false)]
    [InlineData("empresa.example", true)]
    public void TheLocalDomainsAreDomains(string domain, bool valid)
    {
        var validation = new EmailOptionsValidation(new Environment(Environments.Development));
        var options = new EmailOptions { Provider = EmailOptions.GraphProvider, Mailbox = "projetos@empresa.example" };
        options.Graph.LocalDomains.Add(domain);

        validation.Validate(null, options).Succeeded.Should().Be(valid);
    }

    [Theory]
    [InlineData("marina.costa@empresa.example", true)]
    [InlineData("Marina.Costa@EMPRESA.EXAMPLE", true)]
    [InlineData("marina.costa@sub.empresa.example", false)]
    [InlineData("empresa.example@contoso.example", false)]
    [InlineData("sem-arroba", false)]
    public void TheLocalDomainsAreTheWholeDomainOfTheAddress(string address, bool local)
    {
        EmailRouting.IsLocal(address, ["empresa.example"]).Should().Be(local);
    }

    [Theory]
    [InlineData("ftp://empresa.example")]
    [InlineData("localhost:5173")]
    public void TheSiteAddressIsHttp(string siteUrl)
    {
        var validation = new EmailOptionsValidation(new Environment(Environments.Development));

        var result = validation.Validate(null, new EmailOptions { SiteUrl = siteUrl });

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void TheDaysKeptAreAtLeastOne()
    {
        var validation = new EmailOptionsValidation(new Environment(Environments.Development));

        validation.Validate(null, new EmailOptions { KeepDays = 0 }).Failed.Should().BeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private sealed class Environment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "FiscoProjects";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
