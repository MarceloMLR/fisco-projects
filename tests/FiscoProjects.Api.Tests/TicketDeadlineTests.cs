using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: chamados (API side): "Prazos da instalação", "Prazos do chamado", "Prazo pausado" and "Mudar a prioridade"
// (ADR-0025, ADR-0044, ADR-0046). The clock starts on Monday, 19/10/2026, 06:00 in São Paulo.
public sealed class TicketDeadlineTests(SqlServerFixture sqlServer)
{
    private const string Deadlines = "/api/admin/ticket-deadlines";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>ERP-FIN, managed by Marina, with Rafael as tech lead, Carla as contributor and Diego as developer.</summary>
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
        var rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.User);
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
        return new Scene(app, admin, marina, rafael, carla, diego, erp);
    }

    private static async Task<string> OpenAsync(Scene scene, string priority)
    {
        using var carla = await scene.App.SignInAsync(scene.Carla);
        var opened = await SendAsync(carla, HttpMethod.Post, "/api/tickets", new
        {
            projectId = scene.Erp,
            team = "Development",
            priority,
            title = $"Rejeição 539 ({priority})",
            description = "A SEFAZ retorna a rejeição 539.",
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        var number = (await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
        return $"/api/tickets/{number}";
    }

    private static async Task AnswerAsync(HttpClient client, string path, string text)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(Doc(text), Encoding.UTF8), "body");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{path}/messages") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        var sent = await client.SendAsync(request, Cancellation);
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
    }

    private static string Doc(string text) =>
        JsonSerializer.Serialize(new { type = "doc", content = new[] { new { type = "paragraph", content = new[] { new { type = "text", text } } } } });

    private static Task<JsonElement> TicketAsync(HttpClient client, string path) =>
        client.GetFromJsonAsync<JsonElement>(path, Cancellation);

    private static JsonElement ResolutionOf(JsonElement ticket) => ticket.GetProperty("deadlines").GetProperty("resolution");

    private static object DeadlinesWith(int mediumResolution) => new
    {
        deadlines = new[]
        {
            new { priority = "Low", firstResponseDays = 1, resolutionDays = 10 },
            new { priority = "Medium", firstResponseDays = 1, resolutionDays = mediumResolution },
            new { priority = "High", firstResponseDays = 1, resolutionDays = 2 },
            new { priority = "Critical", firstResponseDays = 1, resolutionDays = 1 },
        },
    };

    [Fact(DisplayName = "Prazos padrão")]
    public async Task Deadlines_StartAsTheOnesOfTheMvp()
    {
        await using var scene = await StartAsync();

        var deadlines = await scene.Admin.GetFromJsonAsync<JsonElement>(Deadlines, Cancellation);

        deadlines.EnumerateArray()
            .Select(item => (item.GetProperty("priority").GetString(), item.GetProperty("firstResponseDays").GetInt32(), item.GetProperty("resolutionDays").GetInt32()))
            .Should().Equal(("Low", 1, 10), ("Medium", 1, 5), ("High", 1, 2), ("Critical", 1, 1));
    }

    [Fact(DisplayName = "Mudar os prazos")]
    public async Task NewDeadlines_CountForTheTicketsOpenedAfterwards()
    {
        await using var scene = await StartAsync();
        var before = await OpenAsync(scene, "Medium");

        var changed = await SendAsync(scene.Admin, HttpMethod.Put, Deadlines, DeadlinesWith(3));

        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var after = await OpenAsync(scene, "Medium");
        ResolutionOf(await TicketAsync(scene.Admin, after)).GetProperty("dueOn").GetString().Should().Be("2026-10-22");
        ResolutionOf(await TicketAsync(scene.Admin, before)).GetProperty("dueOn").GetString().Should().Be("2026-10-26");
        var audit = await scene.Admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=TicketDeadlineSetting", Cancellation);
        audit.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Fact(DisplayName = "Prazo fora do limite")]
    public async Task ZeroDays_IsRefused()
    {
        await using var scene = await StartAsync();

        var changed = await SendAsync(scene.Admin, HttpMethod.Put, Deadlines, DeadlinesWith(0));

        changed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await changed.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").TryGetProperty("Medium", out _)
            .Should().BeTrue();
        var deadlines = await scene.Admin.GetFromJsonAsync<JsonElement>(Deadlines, Cancellation);
        deadlines[1].GetProperty("resolutionDays").GetInt32().Should().Be(5);
    }

    [Fact(DisplayName = "Prazos só do administrador")]
    public async Task OnlyTheAdministrator_SeesTheDeadlines()
    {
        await using var scene = await StartAsync();
        using var marina = await scene.App.SignInAsync(scene.Marina);

        (await marina.GetAsync(Deadlines, Cancellation)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(marina, HttpMethod.Put, Deadlines, DeadlinesWith(3))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Prazo em dias úteis")]
    public async Task HighTicket_OpenedOnFriday_IsDueOnTuesday()
    {
        await using var scene = await StartAsync();
        scene.App.Clock.Advance(TimeSpan.FromDays(4));

        var path = await OpenAsync(scene, "High");

        using var carla = await scene.App.SignInAsync(scene.Carla);
        var deadlines = (await TicketAsync(carla, path)).GetProperty("deadlines");
        deadlines.GetProperty("resolution").GetProperty("dueOn").GetString().Should().Be("2026-10-27");
        deadlines.GetProperty("firstResponse").GetProperty("dueOn").GetString().Should().Be("2026-10-26");
        // Due on the next business day: Monday, after the weekend.
        deadlines.GetProperty("firstResponse").GetProperty("state").GetString().Should().Be("Soon");
        deadlines.GetProperty("resolution").GetProperty("state").GetString().Should().Be("OnTrack");
        (deadlines.GetProperty("usedDays").GetInt32(), deadlines.GetProperty("totalDays").GetInt32()).Should().Be((0, 2));
    }

    [Fact(DisplayName = "Primeira resposta cumprida")]
    public async Task FirstAnswer_IsTheOneOfTheDeveloper()
    {
        await using var scene = await StartAsync();
        var path = await OpenAsync(scene, "Medium");
        using var carla = await scene.App.SignInAsync(scene.Carla);
        using var diego = await scene.App.SignInAsync(scene.Diego);
        await AnswerAsync(carla, path, "Segue o XML.");
        (await TicketAsync(carla, path)).GetProperty("deadlines").GetProperty("firstRespondedAt").ValueKind.Should().Be(JsonValueKind.Null);
        scene.App.Clock.Advance(TimeSpan.FromMinutes(16));

        await AnswerAsync(diego, path, "Vou olhar.");

        var deadlines = (await TicketAsync(carla, path)).GetProperty("deadlines");
        deadlines.GetProperty("firstRespondedAt").GetDateTime().Should().Be(scene.App.Clock.GetUtcNow().UtcDateTime);
        deadlines.GetProperty("firstResponse").GetProperty("state").GetString().Should().Be("Met");
    }

    [Fact(DisplayName = "Pausa estende o prazo")]
    public async Task WaitingForTheRequester_PushesTheResolution()
    {
        await using var scene = await StartAsync();
        var path = await OpenAsync(scene, "High");
        using (var diego = await scene.App.SignInAsync(scene.Diego))
        {
            await AnswerAsync(diego, path, "Vou olhar.");
            var asked = await SendAsync(diego, HttpMethod.Post, $"{path}/transitions", new { action = "AskForInformation", body = Doc("Qual a série?") });
            asked.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var waiting = (await TicketAsync(diego, path)).GetProperty("deadlines");
            waiting.GetProperty("resolution").GetProperty("state").GetString().Should().Be("Paused");
        }

        // Wednesday: Tuesday and Wednesday waited for the requester.
        scene.App.Clock.Advance(TimeSpan.FromDays(2));
        using var carla = await scene.App.SignInAsync(scene.Carla);
        (await TicketAsync(carla, path)).GetProperty("deadlines").GetProperty("pausedDays").GetInt32().Should().Be(2);

        await AnswerAsync(carla, path, "Série 2.");

        var deadlines = (await TicketAsync(carla, path)).GetProperty("deadlines");
        deadlines.GetProperty("resolution").GetProperty("dueOn").GetString().Should().Be("2026-10-23");
        deadlines.GetProperty("pausedDays").GetInt32().Should().Be(2);
        deadlines.GetProperty("usedDays").GetInt32().Should().Be(0);
    }

    [Fact(DisplayName = "Prazo vencido na API")]
    public async Task CriticalTicket_IsLateOnThursday()
    {
        await using var scene = await StartAsync();
        var path = await OpenAsync(scene, "Critical");

        // Thursday: it was due on Tuesday.
        scene.App.Clock.Advance(TimeSpan.FromDays(3));
        using var carla = await scene.App.SignInAsync(scene.Carla);

        var listed = await carla.GetFromJsonAsync<JsonElement>("/api/tickets", Cancellation);
        var resolution = listed.GetProperty("items").EnumerateArray().Single().GetProperty("resolution");
        (resolution.GetProperty("state").GetString(), resolution.GetProperty("dueOn").GetString(), resolution.GetProperty("daysLate").GetInt32())
            .Should().Be(("Overdue", "2026-10-20", 2));
        var deadlines = (await TicketAsync(carla, path)).GetProperty("deadlines");
        deadlines.GetProperty("firstResponse").GetProperty("state").GetString().Should().Be("Overdue");
        deadlines.GetProperty("usedDays").GetInt32().Should().Be(1);
    }

    [Fact(DisplayName = "Mensagem da ação conta como resposta")]
    public async Task MessageOfAnAction_IsAnAnswer()
    {
        await using var scene = await StartAsync();
        var path = await OpenAsync(scene, "Medium");
        using var marina = await scene.App.SignInAsync(scene.Marina);
        using var diego = await scene.App.SignInAsync(scene.Diego);
        (await SendAsync(marina, HttpMethod.Post, $"{path}/transitions", new { action = "StartTriage" })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SendAsync(marina, HttpMethod.Post, $"{path}/transitions", new { action = "StartWork" })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var asked = await SendAsync(diego, HttpMethod.Post, $"{path}/transitions", new { action = "AskForInformation", body = Doc("Qual a série?") });

        asked.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var ticket = await TicketAsync(diego, path);
        ticket.GetProperty("assignee").GetProperty("id").GetGuid().Should().Be(scene.Diego);
        ticket.GetProperty("following").GetBoolean().Should().BeTrue();
        ticket.GetProperty("deadlines").GetProperty("firstResponse").GetProperty("state").GetString().Should().Be("Met");
    }

    [Fact(DisplayName = "Resolução com atraso")]
    public async Task ResolvedLate_StaysLate_AfterClosing()
    {
        await using var scene = await StartAsync();
        var path = await OpenAsync(scene, "Critical");
        using (var diego = await scene.App.SignInAsync(scene.Diego))
        {
            await AnswerAsync(diego, path, "Vou olhar.");
        }

        // Resolved on Thursday: it was due on Tuesday.
        scene.App.Clock.Advance(TimeSpan.FromDays(3));
        using (var diego = await scene.App.SignInAsync(scene.Diego))
        {
            (await SendAsync(diego, HttpMethod.Post, $"{path}/transitions", new { action = "Resolve" })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var carla = await scene.App.SignInAsync(scene.Carla);
        (await SendAsync(carla, HttpMethod.Post, $"{path}/transitions", new { action = "ConfirmAndClose" })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deadlines = (await TicketAsync(carla, path)).GetProperty("deadlines");
        var resolution = deadlines.GetProperty("resolution");
        (resolution.GetProperty("state").GetString(), resolution.GetProperty("daysLate").GetInt32()).Should().Be(("Overdue", 2));
        (deadlines.GetProperty("usedDays").GetInt32(), deadlines.GetProperty("totalDays").GetInt32()).Should().Be((1, 1));
    }

    [Fact(DisplayName = "Chamado cancelado sem resolução")]
    public async Task CancelledTicket_HasNoResolution()
    {
        await using var scene = await StartAsync();
        var path = await OpenAsync(scene, "Medium");
        using var carla = await scene.App.SignInAsync(scene.Carla);

        (await SendAsync(carla, HttpMethod.Post, $"{path}/transitions", new { action = "Cancel", body = Doc("Abri no projeto errado.") }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deadlines = (await TicketAsync(carla, path)).GetProperty("deadlines");
        deadlines.GetProperty("resolution").ValueKind.Should().Be(JsonValueKind.Null);
        deadlines.GetProperty("firstResponse").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "Subir a prioridade")]
    public async Task Manager_RaisesThePriority_AndTheDeadlinesFollow()
    {
        await using var scene = await StartAsync();
        var path = await OpenAsync(scene, "Medium");
        using var marina = await scene.App.SignInAsync(scene.Marina);

        var changed = await SendAsync(marina, HttpMethod.Put, $"{path}/priority", new { priority = "High" });

        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var ticket = await TicketAsync(marina, path);
        ticket.GetProperty("priority").GetString().Should().Be("High");
        ResolutionOf(ticket).GetProperty("dueOn").GetString().Should().Be("2026-10-21");
        var change = ticket.GetProperty("events").EnumerateArray().Single();
        (change.GetProperty("kind").GetString(), change.GetProperty("fromPriority").GetString(), change.GetProperty("toPriority").GetString())
            .Should().Be(("PriorityChanged", "Medium", "High"));
        ticket.GetProperty("permissions").GetProperty("canChangePriority").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "Solicitante não muda a prioridade")]
    public async Task Requester_MayNotChangeThePriority()
    {
        await using var scene = await StartAsync();
        var path = await OpenAsync(scene, "Medium");
        using var carla = await scene.App.SignInAsync(scene.Carla);

        var changed = await SendAsync(carla, HttpMethod.Put, $"{path}/priority", new { priority = "Critical" });

        changed.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var ticket = await TicketAsync(carla, path);
        ticket.GetProperty("priority").GetString().Should().Be("Medium");
        ticket.GetProperty("permissions").GetProperty("canChangePriority").GetBoolean().Should().BeFalse();
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
