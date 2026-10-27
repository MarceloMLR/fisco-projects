using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Domain;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Api.Tests;

// Spec: avisos, requirements "Envio pelo Microsoft Graph", "Configuração do envio conferida" and "E-mails da
// demonstração na pasta local" (ADR-0123). Microsoft Graph is simulated inside the process (FakeGraph).
public sealed class GraphEmailTests(SqlServerFixture sqlServer) : IDisposable
{
    private const string Mailbox = "fiscoprojects@empresa.example";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "fiscoprojects-emails-" + Guid.NewGuid().ToString("N"));
    private readonly FakeGraph _graph = new() { Mailboxes = { Mailbox } };

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "E-mail pela caixa compartilhada")]
    public async Task TheTestLeavesFromTheSharedMailbox()
    {
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);

        (await SendTestAsync(admin)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        app.Clock.Advance(TimeSpan.FromSeconds(30));

        var test = await LastTestAsync(app);
        test.Status.Should().Be(OutboxEmailStatus.Sent);
        var mail = _graph.Mail.Should().ContainSingle().Subject;
        mail.Mailbox.Should().Be(Mailbox);
        mail.ToAddress.Should().Be("paulo.nogueira@empresa.example");
        mail.ToName.Should().Be("Paulo Nogueira");
        mail.Subject.Should().Be("Teste de envio do FiscoProjects");
        mail.ContentType.Should().Be("HTML");
        mail.Content.Should().Contain("<html");
        mail.SaveToSentItems.Should().BeTrue();
        var status = await admin.GetFromJsonAsync<JsonElement>("/api/admin/email", Cancellation);
        status.GetProperty("mailbox").GetString().Should().Be(Mailbox);
        status.GetProperty("lastTest").GetProperty("status").GetString().Should().Be("Sent");
    }

    [Fact(DisplayName = "Caixa sem permissão de envio")]
    public async Task AMailboxOutsideTheScopeIsAFailedTry()
    {
        _graph.Mailboxes.Clear();
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);

        await SendTestAsync(admin);
        app.Clock.Advance(TimeSpan.FromSeconds(30));

        var test = await LastTestAsync(app);
        test.Status.Should().Be(OutboxEmailStatus.Pending);
        test.Attempts.Should().Be(1);
        test.LastError.Should().Contain("403").And.Contain("ErrorAccessDenied");
        test.LastError.Should().NotContain("paulo.nogueira").And.NotContain(Mailbox);
        _graph.Mail.Should().BeEmpty();
    }

    [Fact(DisplayName = "Graph fora do ar")]
    public async Task GraphOutOfServiceIsAFailedTry()
    {
        _graph.Down = true;
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);

        await SendTestAsync(admin);
        app.Clock.Advance(TimeSpan.FromSeconds(30));

        var test = await LastTestAsync(app);
        test.Status.Should().Be(OutboxEmailStatus.Pending);
        test.LastError.Should().Contain("503");
        test.NextAttemptAt.Should().Be(app.Clock.GetUtcNow().UtcDateTime.AddMinutes(1));
    }

    [Fact(DisplayName = "Endereço da demonstração na pasta")]
    public async Task TheAddressesOfTheDemonstrationStayInTheFolder()
    {
        await using var app = await StartAsync(builder => LocalDomains(builder, "EMPRESA.example"));
        var sender = app.Services.GetRequiredService<IEmailSender>();

        await sender.SendAsync(
            new EmailMessage("marina.costa@empresa.example", "Marina Costa", "Teste", "<p>Olá</p>", "Olá"), Cancellation);

        Directory.GetFiles(_folder, "*.eml").Should().ContainSingle();
        _graph.Mail.Should().BeEmpty();
        _graph.Requests.Should().BeEmpty();
    }

    [Fact(DisplayName = "Endereço do tenant pelo Graph")]
    public async Task TheOtherAddressesLeaveThroughGraph()
    {
        await using var app = await StartAsync(builder => LocalDomains(builder, "empresa.example"));
        var sender = app.Services.GetRequiredService<IEmailSender>();

        await sender.SendAsync(
            new EmailMessage("beatriz.lima@contoso.example", "Beatriz Lima", "Teste", "<p>Olá</p>", "Olá"), Cancellation);

        _graph.Mail.Should().ContainSingle().Which.ToAddress.Should().Be("beatriz.lima@contoso.example");
        Directory.Exists(_folder).Should().BeFalse();
    }

    [Fact(DisplayName = "Graph sem caixa de envio")]
    public async Task GraphWithoutTheMailboxDoesNotStart()
    {
        var start = () => TestApp.ComposedAsync(TestApp.Production, _ => { }, builder =>
        {
            builder.Configuration["Email:Provider"] = "Graph";
            builder.Configuration["Email:Mailbox"] = "";
        });

        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain("Email:Mailbox");
    }

    [Fact(DisplayName = "Domínios locais em produção")]
    public async Task LocalDomainsDoNotStartInProduction()
    {
        var start = () => TestApp.ComposedAsync(TestApp.Production, _ => { }, builder =>
        {
            builder.Configuration["Email:Provider"] = "Graph";
            builder.Configuration["Email:Mailbox"] = Mailbox;
            builder.Configuration["Email:Graph:LocalDomains:0"] = "empresa.example";
        });

        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain("never in production");
    }

    public void Dispose()
    {
        _graph.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private Task<SessionApp> StartAsync(Action<WebApplicationBuilder>? configure = null) =>
        SessionApp.StartAsync(sqlServer, builder =>
        {
            builder.Configuration["Email:Provider"] = "Graph";
            builder.Configuration["Email:Mailbox"] = Mailbox;
            builder.Services.AddSingleton<IGraphTokens>(new GraphToken());
            builder.Services.AddHttpClient("graph-mail").ConfigurePrimaryHttpMessageHandler(() => _graph);
            configure?.Invoke(builder);
        }, recordEmails: false);

    private void LocalDomains(WebApplicationBuilder builder, string domain)
    {
        builder.Configuration["Email:Graph:LocalDomains:0"] = domain;
        builder.Configuration["Email:Local:Folder"] = _folder;
    }

    private static async Task<HttpResponseMessage> SendTestAsync(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/email/test");
        request.Headers.Add("X-CSRF", "1");
        return await client.SendAsync(request, Cancellation);
    }

    /// <summary>The test e-mail once the sending in the background, woken by the clock, has tried it.</summary>
    private static async Task<OutboxEmail> LastTestAsync(SessionApp app)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var email = await scope.ServiceProvider.GetRequiredService<IOutboxStore>().LastOfKindAsync(EmailKinds.Test, Cancellation);
            if (email is { Attempts: > 0 } && (email.Status != OutboxEmailStatus.Pending || email.LastError is not null)
                || DateTime.UtcNow > deadline)
            {
                return email!;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), Cancellation);
        }
    }

    private sealed class GraphToken : IGraphTokens
    {
        public Task<string> GetAsync(CancellationToken cancellationToken) => Task.FromResult(FakeGraph.Token);
    }
}
