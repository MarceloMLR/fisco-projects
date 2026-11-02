using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: avisos (API side), requirements "Notificações no sino", "Sino e painel" and "Guarda das notificações" (ADR-0038).
public sealed class BellTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>
    /// ERP-FIN, managed by Marina, with Rafael as tech lead, Carla as contributor and Diego as developer; Carla opens #1
    /// for Desenvolvimento.
    /// </summary>
    private sealed record Scene(SessionApp App, HttpClient Admin, Guid Marina, Guid Rafael, Guid Carla, Guid Diego, Guid Erp)
        : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => App.DisposeAsync();
    }

    private async Task<Scene> StartAsync()
    {
        var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        var rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.TechLead);
        var carla = await app.CreateUserAsync("Carla Mendes", SystemRole.User);
        var diego = await app.CreateUserAsync("Diego Torres", SystemRole.User);
        var created = await SendAsync(admin, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-FIN",
            name = "Implantação do ERP financeiro",
            managerId = marina,
            techLeadId = rafael,
        });
        var erp = (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        await app.AddProjectMemberAsync(erp, carla, "Contributor");
        await app.AddProjectMemberAsync(erp, diego, "Developer");
        using var asCarla = await app.SignInAsync(carla);
        var opened = await SendAsync(asCarla, HttpMethod.Post, "/api/tickets", new
        {
            projectId = erp,
            team = "Development",
            priority = "High",
            title = "Rejeição 539 ao emitir nota de devolução",
            description = "A SEFAZ retorna a rejeição 539.",
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created);
        return new Scene(app, admin, marina, rafael, carla, diego, erp);
    }

    [Fact(DisplayName = "Notificação do mesmo aviso do e-mail")]
    public async Task Message_IsAlsoInTheBell()
    {
        await using var scene = await StartAsync();
        using (var diego = await scene.App.SignInAsync(scene.Diego))
        {
            await AnswerAsync(diego, "Vou olhar o XML.");
        }

        using var carla = await scene.App.SignInAsync(scene.Carla);
        var page = await carla.GetFromJsonAsync<JsonElement>("/api/notifications", Cancellation);

        var item = page.GetProperty("items").EnumerateArray().Should().ContainSingle().Subject;
        item.GetProperty("kind").GetString().Should().Be("TicketMessage");
        item.GetProperty("actor").GetString().Should().Be("Diego Torres");
        item.GetProperty("path").GetString().Should().Be("/tickets/1");
        item.GetProperty("values").GetProperty("number").GetString().Should().Be("1");
        item.GetProperty("values").GetProperty("project").GetString().Should().Be("ERP-FIN");
        item.GetProperty("read").GetBoolean().Should().BeFalse();
        page.GetProperty("unread").GetInt32().Should().Be(1);
        page.GetProperty("hasMore").GetBoolean().Should().BeFalse();
        (await scene.App.EmailsToAsync("carla.mendes@empresa.example")).Should().ContainSingle();
    }

    [Fact(DisplayName = "Notificação do resumo na hora")]
    public async Task StatusChange_IsInTheBellNow()
    {
        await using var scene = await StartAsync();
        using (var rafael = await scene.App.SignInAsync(scene.Rafael))
        {
            var taken = await SendAsync(rafael, HttpMethod.Post, "/api/tickets/1/transitions", new { action = "StartTriage", body = (string?)null });
            taken.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var carla = await scene.App.SignInAsync(scene.Carla);
        var item = (await carla.GetFromJsonAsync<JsonElement>("/api/notifications", Cancellation))
            .GetProperty("items").EnumerateArray().Should().ContainSingle().Subject;

        item.GetProperty("kind").GetString().Should().Be("TicketStatusChanged");
        (item.GetProperty("values").GetProperty("from").GetString(), item.GetProperty("values").GetProperty("to").GetString())
            .Should().Be(("Open", "Triage"));
        (await scene.App.EmailsToAsync("carla.mendes@empresa.example")).Should().BeEmpty("the e-mail waits for the summary");
    }

    [Fact(DisplayName = "Só as próprias notificações")]
    public async Task SomeoneElsesNotification_IsNotFound()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);
        var id = (await diego.GetFromJsonAsync<JsonElement>("/api/notifications", Cancellation))
            .GetProperty("items")[0].GetProperty("id").GetGuid();
        using var marina = await scene.App.SignInAsync(scene.Marina);

        var read = await SendAsync(marina, HttpMethod.Put, $"/api/notifications/{id}/read");

        read.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await UnreadAsync(diego)).Should().Be(1);
        (await marina.GetFromJsonAsync<JsonElement>("/api/notifications", Cancellation)).GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid()).Should().NotContain(id);
    }

    [Fact(DisplayName = "Contador de não lidas")]
    public async Task UnreadCount_CountsOnlyTheUnread()
    {
        await using var scene = await StartAsync();
        using (var carla = await scene.App.SignInAsync(scene.Carla))
        {
            await SendAsync(carla, HttpMethod.Post, "/api/tickets", new
            {
                projectId = scene.Erp,
                team = "Development",
                priority = "Low",
                title = "Erro no cálculo do DIFAL",
                description = "O DIFAL sai zerado.",
            });
        }

        using var diego = await scene.App.SignInAsync(scene.Diego);

        (await UnreadAsync(diego)).Should().Be(2);
    }

    [Fact(DisplayName = "Abrir uma notificação")]
    public async Task ReadingANotification_TakesItFromTheUnread()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);
        var id = (await diego.GetFromJsonAsync<JsonElement>("/api/notifications", Cancellation))
            .GetProperty("items")[0].GetProperty("id").GetGuid();

        (await SendAsync(diego, HttpMethod.Put, $"/api/notifications/{id}/read")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await UnreadAsync(diego)).Should().Be(0);
        (await diego.GetFromJsonAsync<JsonElement>("/api/notifications?unread=true", Cancellation)).GetProperty("items").GetArrayLength()
            .Should().Be(0);
        (await diego.GetFromJsonAsync<JsonElement>("/api/notifications", Cancellation)).GetProperty("items")[0].GetProperty("read")
            .GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "Marcar todas como lidas")]
    public async Task ReadAll_LeavesNothingUnread()
    {
        await using var scene = await StartAsync();
        using var rafael = await scene.App.SignInAsync(scene.Rafael);

        (await SendAsync(rafael, HttpMethod.Put, "/api/notifications/read-all")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await UnreadAsync(rafael)).Should().Be(0);
        using var diego = await scene.App.SignInAsync(scene.Diego);
        (await UnreadAsync(diego)).Should().Be(1, "the notifications of the others stay");
    }

    [Fact(DisplayName = "Notificação antiga apagada")]
    public async Task OldNotification_LeavesTheBell()
    {
        await using var scene = await StartAsync();

        using (var before = await scene.App.SignInAsync(scene.Marina))
        {
            // Included in the project at its creation.
            (await UnreadAsync(before)).Should().Be(1);
        }

        scene.App.Clock.Advance(TimeSpan.FromDays(91));

        using var marina = await scene.App.SignInAsync(scene.Marina);
        (await UnreadEventuallyAsync(marina, 0)).Should().Be(0);
    }

    [Fact(DisplayName = "Trocar a guarda")]
    public async Task ShorterRetention_RemovesOnTheNextRound()
    {
        await using var scene = await StartAsync();
        (await SendAsync(scene.Admin, HttpMethod.Put, "/api/installation", new
        {
            name = "FiscoProjects",
            timeZone = "America/Sao_Paulo",
            dailySummaryAt = "07:30:00",
            notificationRetentionDays = 30,
        })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        scene.App.Clock.Advance(TimeSpan.FromDays(31));

        using var marina = await scene.App.SignInAsync(scene.Marina);
        (await UnreadEventuallyAsync(marina, 0)).Should().Be(0);
    }

    [Fact]
    public async Task TheBellNeedsASession()
    {
        await using var scene = await StartAsync();
        using var anonymous = scene.App.NewClient();

        (await anonymous.GetAsync("/api/notifications/unread-count", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<int> UnreadAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/notifications/unread-count", Cancellation)).GetProperty("unread").GetInt32();

    /// <summary>The unread count once the removal in the background, woken by the clock, has run.</summary>
    private static async Task<int> UnreadEventuallyAsync(HttpClient client, int expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            var unread = await UnreadAsync(client);
            if (unread == expected || DateTime.UtcNow > deadline)
            {
                return unread;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), Cancellation);
        }
    }

    private static async Task AnswerAsync(HttpClient client, string text)
    {
        var body = JsonSerializer.Serialize(new
        {
            type = "doc",
            content = new[] { new { type = "paragraph", content = new[] { new { type = "text", text } } } },
        });
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(body, Encoding.UTF8), "body");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/tickets/1/messages") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        var sent = await client.SendAsync(request, Cancellation);
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF", "1");
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request, Cancellation);
    }
}
