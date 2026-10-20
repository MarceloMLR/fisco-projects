using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: projetos, "Conclusão com chamados", "Cancelamento com chamados" and "Resumo do encerramento"; chamados,
// "Chamados de um projeto cancelado" (ADR-0043).
public sealed class ProjectClosingTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>ERP-FIN, managed by Marina, with Rafael as tech lead, Carla as contributor and Diego as developer.</summary>
    private sealed record Scene(SessionApp App, HttpClient Admin, Guid Marina, Guid Carla, Guid Diego, Guid Erp)
        : IAsyncDisposable
    {
        public string Path => $"/api/projects/{Erp}";

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
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync(Cancellation));
        var erp = (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        await app.AddProjectMemberAsync(erp, carla, "Contributor");
        await app.AddProjectMemberAsync(erp, diego, "Developer");
        return new Scene(app, admin, marina, carla, diego, erp);
    }

    private static async Task<int> OpenAsync(Scene scene, string title)
    {
        using var carla = await scene.App.SignInAsync(scene.Carla);
        var opened = await SendAsync(carla, HttpMethod.Post, "/api/tickets", new
        {
            projectId = scene.Erp,
            team = "Development",
            priority = "High",
            title,
            description = "A SEFAZ retorna a rejeição 539.",
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        return (await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
    }

    private static string Body(string text) =>
        JsonSerializer.Serialize(new { type = "doc", content = new[] { new { type = "paragraph", content = new[] { new { type = "text", text } } } } });

    /// <summary>Diego answers first, which makes him the assignee, and takes the action of the flow.</summary>
    private static async Task AnswerAndTakeAsync(Scene scene, int number, string action, string? text = null)
    {
        using var diego = await scene.App.SignInAsync(scene.Diego);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(Body("Consegui reproduzir."), Encoding.UTF8), "body");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/tickets/{number}/messages") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        (await diego.SendAsync(request, Cancellation)).StatusCode.Should().Be(HttpStatusCode.Created);
        scene.App.Clock.Advance(TimeSpan.FromMinutes(5));
        var taken = await SendAsync(diego, HttpMethod.Post, $"/api/tickets/{number}/transitions", new
        {
            action,
            body = text is null ? null : Body(text),
        });
        taken.StatusCode.Should().Be(HttpStatusCode.NoContent, await taken.Content.ReadAsStringAsync(Cancellation));
    }

    private static Task<HttpResponseMessage> ChangeStatusAsync(HttpClient client, Scene scene, string status) =>
        SendAsync(client, HttpMethod.Put, $"{scene.Path}/status", new { status });

    private static async Task<string?> StatusOfAsync(Scene scene) =>
        (await scene.Admin.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation)).GetProperty("status").GetString();

    private static Task<JsonElement> TicketAsync(HttpClient client, int number) =>
        client.GetFromJsonAsync<JsonElement>($"/api/tickets/{number}", Cancellation);

    [Fact(DisplayName = "Concluir com chamado não fechado")]
    public async Task ResolvedTicket_KeepsTheProjectFromBeingCompleted()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene, "Rejeição 539");
        await AnswerAndTakeAsync(scene, number, "Resolve");
        using var marina = await scene.App.SignInAsync(scene.Marina);

        var completed = await ChangeStatusAsync(marina, scene, "Completed");

        completed.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await completed.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        (problem.GetProperty("type").GetString(), problem.GetProperty("openTickets").GetInt32())
            .Should().Be(("urn:fiscoprojects:problem:project-has-open-tickets", 1));
        (await StatusOfAsync(scene)).Should().Be("Planning");
        (await TicketAsync(marina, number)).GetProperty("status").GetString().Should().Be("Resolved");
    }

    [Fact(DisplayName = "Concluir com os chamados fechados")]
    public async Task ClosedTickets_LetTheProjectBeCompleted()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene, "Aberto por engano");
        using (var carla = await scene.App.SignInAsync(scene.Carla))
        {
            (await SendAsync(carla, HttpMethod.Post, $"/api/tickets/{number}/transitions", new { action = "Cancel", body = Body("Aberto por engano.") }))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var marina = await scene.App.SignInAsync(scene.Marina);
        var completed = await ChangeStatusAsync(marina, scene, "Completed");

        completed.StatusCode.Should().Be(HttpStatusCode.NoContent, await completed.Content.ReadAsStringAsync(Cancellation));
        (await StatusOfAsync(scene)).Should().Be("Completed");
    }

    [Fact(DisplayName = "Cancelar com chamados abertos")]
    public async Task Cancelling_ClosesTheOpenTickets()
    {
        await using var scene = await StartAsync();
        var first = await OpenAsync(scene, "Rejeição 539");
        var second = await OpenAsync(scene, "Tabela de NCM");
        await AnswerAndTakeAsync(scene, second, "Resolve");
        scene.App.Clock.Advance(TimeSpan.FromMinutes(5));
        using var marina = await scene.App.SignInAsync(scene.Marina);

        var cancelled = await ChangeStatusAsync(marina, scene, "Cancelled");

        cancelled.StatusCode.Should().Be(HttpStatusCode.NoContent, await cancelled.Content.ReadAsStringAsync(Cancellation));
        (await StatusOfAsync(scene)).Should().Be("Cancelled");
        foreach (var (number, from) in new[] { (first, "Open"), (second, "Resolved") })
        {
            var ticket = await TicketAsync(marina, number);
            ticket.GetProperty("status").GetString().Should().Be("Closed");
            var closing = ticket.GetProperty("events").EnumerateArray().Last();
            (closing.GetProperty("kind").GetString(),
                    closing.GetProperty("actorId").GetGuid(),
                    closing.GetProperty("from").GetString(),
                    closing.GetProperty("to").GetString())
                .Should().Be(("ClosedWithProject", scene.Marina, from, "Closed"));
        }

        // Cancelling again closes nothing more: the tickets keep the one event.
        (await ChangeStatusAsync(marina, scene, "Cancelled")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await TicketAsync(marina, first)).GetProperty("events").EnumerateArray()
            .Count(item => item.GetProperty("kind").GetString() == "ClosedWithProject").Should().Be(1);
    }

    [Fact(DisplayName = "Fechado pelo cancelamento do projeto")]
    public async Task WaitingTicket_ClosesWithoutResolutionDeadline()
    {
        await using var scene = await StartAsync();
        var number = await OpenAsync(scene, "Rejeição 539");
        await AnswerAndTakeAsync(scene, number, "AskForInformation", "Qual é a nota?");
        scene.App.Clock.Advance(TimeSpan.FromDays(2));
        using var marina = await scene.App.SignInAsync(scene.Marina);

        (await ChangeStatusAsync(marina, scene, "Cancelled")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var ticket = await TicketAsync(marina, number);
        ticket.GetProperty("status").GetString().Should().Be("Closed");
        var closing = ticket.GetProperty("events").EnumerateArray().Last();
        (closing.GetProperty("kind").GetString(), closing.GetProperty("from").GetString())
            .Should().Be(("ClosedWithProject", "WaitingForRequester"));
        var deadlines = ticket.GetProperty("deadlines");
        deadlines.GetProperty("resolution").ValueKind.Should().Be(JsonValueKind.Null);
        // The waiting stopped with the closing: the paused days do not grow afterwards.
        var paused = deadlines.GetProperty("pausedDays").GetInt32();
        scene.App.Clock.Advance(TimeSpan.FromDays(7));
        using var later = await scene.App.SignInAsync(scene.Marina);
        (await TicketAsync(later, number)).GetProperty("deadlines").GetProperty("pausedDays").GetInt32().Should().Be(paused);
    }

    [Fact(DisplayName = "Resumo do encerramento")]
    public async Task Summary_TellsTheOpenTicketsAndTheIncompleteTasks()
    {
        await using var scene = await StartAsync();
        await OpenAsync(scene, "Rejeição 539");
        var schedule = $"{scene.Path}/schedule";
        var phase = await IdOfAsync(await SendAsync(scene.Admin, HttpMethod.Post, $"{schedule}/phases", new { name = "Configuração" }));
        await IdOfAsync(await SendAsync(scene.Admin, HttpMethod.Post, $"{schedule}/items", new
        {
            kind = "Task",
            phaseId = phase,
            name = "Parametrizar regras de ICMS",
            start = "2026-10-19",
            duration = 3,
            progress = 40,
            predecessorIds = Array.Empty<Guid>(),
        }));
        await IdOfAsync(await SendAsync(scene.Admin, HttpMethod.Post, $"{schedule}/items", new
        {
            kind = "Task",
            phaseId = phase,
            name = "Cadastrar CFOP",
            start = "2026-10-19",
            duration = 1,
            progress = 100,
            predecessorIds = Array.Empty<Guid>(),
        }));
        using var marina = await scene.App.SignInAsync(scene.Marina);

        var closing = await marina.GetFromJsonAsync<JsonElement>($"{scene.Path}/closing", Cancellation);

        (closing.GetProperty("openTickets").GetInt32(), closing.GetProperty("incompleteTasks").GetInt32()).Should().Be((1, 1));
    }

    [Fact(DisplayName = "Resumo só de quem muda o status")]
    public async Task Contributor_MayNotSeeTheSummary()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);

        (await carla.GetAsync($"{scene.Path}/closing", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ChangeStatusAsync(carla, scene, "Cancelled")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
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
