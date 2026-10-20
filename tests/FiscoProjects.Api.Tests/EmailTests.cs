using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Domain;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Api.Tests;

public sealed class EmailTests(SqlServerFixture sqlServer) : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "fiscoprojects-emails-" + Guid.NewGuid().ToString("N"));

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "E-mail na fila com o envio fora do ar")]
    public async Task TheTestWaitsInTheQueueWhileTheSendingIsOut()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        app.Emails.Failing = true;
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);

        using var response = await SendTestAsync(admin);
        app.Clock.Advance(TimeSpan.FromSeconds(30));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("to").GetString()
            .Should().Be("paulo.nogueira@empresa.example");
        var email = await QueuedAsync(app);
        email.Status.Should().Be(OutboxEmailStatus.Pending);
        email.Attempts.Should().Be(1);
        var status = await admin.GetFromJsonAsync<JsonElement>("/api/admin/email", Cancellation);
        status.GetProperty("lastTest").GetProperty("status").GetString().Should().Be("Pending");
        status.GetProperty("lastTest").GetProperty("to").GetString().Should().Be("paulo.nogueira@empresa.example");
    }

    [Fact]
    public async Task TheTestLeavesInTheBackground()
    {
        await using var app = await SessionApp.StartAsync(sqlServer, builder =>
        {
            builder.Configuration["Email:Mailbox"] = "projetos@empresa.example";
            builder.Configuration["Email:SiteUrl"] = "https://projetos.empresa.example";
        });
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);

        (await SendTestAsync(admin)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        app.Clock.Advance(TimeSpan.FromSeconds(30));

        var status = await StatusOnceSentAsync(admin);
        status.GetProperty("mailbox").GetString().Should().Be("projetos@empresa.example");
        status.GetProperty("lastTest").GetProperty("status").GetString().Should().Be("Sent");
        var sent = app.Emails.Sent.Should().ContainSingle().Subject;
        sent.ToAddress.Should().Be("paulo.nogueira@empresa.example");
        sent.Subject.Should().Be("Teste de envio do FiscoProjects");
        sent.TextBody.Should().Contain("https://projetos.empresa.example/admin/settings");
    }

    [Fact]
    public async Task Development_WritesTheEmailsToTheConfiguredFolder()
    {
        await using var app = await TestApp.ComposedAsync(TestApp.Development, _ => { },
            builder => builder.Configuration["Email:Local:Folder"] = _folder);
        var sender = app.Services.GetRequiredService<IEmailSender>();

        await sender.SendAsync(
            new EmailMessage("marina.costa@empresa.example", "Marina Costa", "Teste", "<p>Olá</p>", "Olá"), Cancellation);

        Directory.GetFiles(_folder, "*.eml").Should().ContainSingle();
    }

    [Fact(DisplayName = "Teste só do administrador")]
    public async Task OnlyTheAdministratorTests()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (manager, _) = await app.SignedInClientAsync("Marina Costa", SystemRole.ProjectManager);

        using var test = await SendTestAsync(manager);
        using var status = await manager.GetAsync("/api/admin/email", Cancellation);

        test.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        status.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WithoutATestTheStatusSaysSo()
    {
        await using var app = await SessionApp.StartAsync(sqlServer, builder => builder.Configuration["Email:Mailbox"] = "");
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);

        var status = await admin.GetFromJsonAsync<JsonElement>("/api/admin/email", Cancellation);

        status.GetProperty("mailbox").ValueKind.Should().Be(JsonValueKind.Null);
        status.GetProperty("lastTest").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "Implementação local em produção")]
    public async Task TheLocalSendingStopsTheApplicationInProduction()
    {
        var start = () => TestApp.ComposedAsync(TestApp.Production, _ => { },
            builder => builder.Configuration["Email:Provider"] = "Local");

        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain("not allowed in production");
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static async Task<HttpResponseMessage> SendTestAsync(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/email/test");
        request.Headers.Add("X-CSRF", "1");
        return await client.SendAsync(request, Cancellation);
    }

    private static async Task<OutboxEmail> QueuedAsync(SessionApp app)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
            var email = await store.LastOfKindAsync(EmailKinds.Test, Cancellation);
            if (email is { Attempts: > 0 } || DateTime.UtcNow > deadline)
            {
                return email!;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), Cancellation);
        }
    }

    /// <summary>The status once the sending in the background, woken by the clock, has sent the test.</summary>
    private static async Task<JsonElement> StatusOnceSentAsync(HttpClient client)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            var status = await client.GetFromJsonAsync<JsonElement>("/api/admin/email", Cancellation);
            if (status.GetProperty("lastTest").GetProperty("status").GetString() != "Pending" || DateTime.UtcNow > deadline)
            {
                return status;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), Cancellation);
        }
    }
}
