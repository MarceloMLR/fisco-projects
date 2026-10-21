using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: chamados (API side): "Fluxo de status em tabela", "Mudar o status", "Primeira resposta", "Fechamento
// automático" and "Mensagens do chamado" in a closed ticket (ADR-0024, ADR-0044, ADR-0068).
public sealed class TicketFlowTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>
    /// ERP-FIN, managed by Marina, with Rafael as tech lead, Carla as contributor and Diego as developer; Carla opens #1
    /// for Desenvolvimento.
    /// </summary>
    private sealed record Scene(SessionApp App, HttpClient Admin, Guid Marina, Guid Rafael, Guid Carla, Guid Diego, Guid Erp, int Number)
        : IAsyncDisposable
    {
        public string Path => $"/api/tickets/{Number}";

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
        var number = await OpenTicketAsync(asCarla, erp, "Rejeição 539 ao emitir nota de devolução");
        return new Scene(app, admin, marina, rafael, carla, diego, erp, number);
    }

    private static async Task<int> OpenTicketAsync(HttpClient client, Guid project, string title)
    {
        var opened = await SendAsync(client, HttpMethod.Post, "/api/tickets", new
        {
            projectId = project,
            team = "Development",
            priority = "High",
            title,
            description = "A SEFAZ retorna a rejeição 539.",
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        return (await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
    }

    private static string Doc(string text) =>
        JsonSerializer.Serialize(new { type = "doc", content = new[] { new { type = "paragraph", content = new[] { new { type = "text", text } } } } });

    private static async Task<HttpResponseMessage> MessageAsync(HttpClient client, string path, string text)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(Doc(text), Encoding.UTF8), "body");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{path}/messages") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        var response = await client.SendAsync(request, Cancellation);
        return response;
    }

    private static async Task AnswerAsync(HttpClient client, string path, string text)
    {
        var sent = await MessageAsync(client, path, text);
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
    }

    private static Task<HttpResponseMessage> TakeAsync(HttpClient client, string path, string action, string? text = null) =>
        SendAsync(client, HttpMethod.Post, $"{path}/transitions", new { action, body = text is null ? null : Doc(text) });

    private static async Task TakenAsync(HttpClient client, string path, string action, string? text = null)
    {
        var taken = await TakeAsync(client, path, action, text);
        taken.StatusCode.Should().Be(HttpStatusCode.NoContent, await taken.Content.ReadAsStringAsync(Cancellation));
    }

    private static Task<JsonElement> TicketAsync(HttpClient client, string path) =>
        client.GetFromJsonAsync<JsonElement>(path, Cancellation);

    private static async Task<string?> StatusAsync(HttpClient client, string path) =>
        (await TicketAsync(client, path)).GetProperty("status").GetString();

    private static Guid? AssigneeOf(JsonElement ticket) =>
        ticket.GetProperty("assignee").ValueKind == JsonValueKind.Null ? null : ticket.GetProperty("assignee").GetProperty("id").GetGuid();

    [Fact(DisplayName = "Fluxo padrão")]
    public async Task Flow_IsTheOneOfTheMvp()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);

        var flow = await carla.GetFromJsonAsync<JsonElement>("/api/tickets/flow", Cancellation);

        flow.GetProperty("autoCloseBusinessDays").GetInt32().Should().Be(5);
        flow.GetProperty("statuses").EnumerateArray().Select(status => status.GetString())
            .Should().Equal("Open", "Triage", "InProgress", "WaitingForRequester", "Resolved", "Closed");
        var transitions = flow.GetProperty("transitions").EnumerateArray().ToList();
        transitions.Should().HaveCount(11);
        transitions.Should().NotContain(transition => transition.GetProperty("from").GetString() == "Closed");
        var close = transitions.Single(transition => transition.GetProperty("action").GetString() == "ConfirmAndClose");
        close.GetProperty("actors").EnumerateArray().Select(actor => actor.GetString())
            .Should().Equal("Requester", "Manager", "Administrator");
        close.GetProperty("requiresMessage").GetBoolean().Should().BeFalse();
        var ask = transitions.Single(transition => transition.GetProperty("action").GetString() == "AskForInformation");
        (ask.GetProperty("from").GetString(), ask.GetProperty("to").GetString(), ask.GetProperty("requiresMessage").GetBoolean())
            .Should().Be(("InProgress", "WaitingForRequester", true));
        transitions.Single(transition => transition.GetProperty("action").GetString() == "AutoClose")
            .GetProperty("automatic").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "Resolver o chamado")]
    public async Task Attendant_ResolvesATicketInProgress()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);
        await AnswerAsync(diego, scene.Path, "Vou olhar o XML.");
        scene.App.Clock.Advance(TimeSpan.FromMinutes(20));

        await TakenAsync(diego, scene.Path, "Resolve");

        var ticket = await TicketAsync(diego, scene.Path);
        ticket.GetProperty("status").GetString().Should().Be("Resolved");
        var change = ticket.GetProperty("events").EnumerateArray().Last();
        change.GetProperty("kind").GetString().Should().Be("StatusChanged");
        (change.GetProperty("from").GetString(), change.GetProperty("to").GetString(), change.GetProperty("action").GetString())
            .Should().Be(("InProgress", "Resolved", "Resolve"));
        change.GetProperty("actorId").GetGuid().Should().Be(scene.Diego);
        change.GetProperty("at").GetDateTime().Should().Be(scene.App.Clock.GetUtcNow().UtcDateTime);
        ticket.GetProperty("updatedAt").GetDateTime().Should().Be(scene.App.Clock.GetUtcNow().UtcDateTime);
        ticket.GetProperty("permissions").GetProperty("actions").EnumerateArray().Should().BeEmpty();
    }

    [Fact(DisplayName = "Pedir informações sem mensagem")]
    public async Task AskForInformation_WithoutAMessage_IsRefused()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);
        await AnswerAsync(diego, scene.Path, "Vou olhar o XML.");

        var taken = await TakeAsync(diego, scene.Path, "AskForInformation");

        taken.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await taken.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").TryGetProperty("body", out _)
            .Should().BeTrue();
        (await StatusAsync(diego, scene.Path)).Should().Be("InProgress");
        var permissions = (await TicketAsync(diego, scene.Path)).GetProperty("permissions").GetProperty("actions").EnumerateArray()
            .Select(action => (action.GetProperty("action").GetString(), action.GetProperty("requiresMessage").GetBoolean()));
        permissions.Should().BeEquivalentTo([("AskForInformation", true), ("Resolve", false)]);
    }

    [Fact(DisplayName = "Ação de quem não pode")]
    public async Task Requester_MayNotResolve()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);
        using var carla = await scene.App.SignInAsync(scene.Carla);
        await AnswerAsync(diego, scene.Path, "Vou olhar o XML.");

        var taken = await TakeAsync(carla, scene.Path, "Resolve");

        taken.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StatusAsync(carla, scene.Path)).Should().Be("InProgress");
    }

    [Fact(DisplayName = "Ação fora do status")]
    public async Task ConfirmAndClose_OfAnOpenTicket_IsAConflict()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);

        var taken = await TakeAsync(carla, scene.Path, "ConfirmAndClose");

        taken.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await taken.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString()
            .Should().Be("urn:fiscoprojects:problem:ticket-action-not-allowed-now");
        (await StatusAsync(carla, scene.Path)).Should().Be("Open");
    }

    [Fact(DisplayName = "Ação automática não vira botão")]
    public async Task AutomaticActions_AreNotTakenByPeople()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);

        (await TakeAsync(diego, scene.Path, "FirstResponse", "Olá")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await TakeAsync(scene.Admin, scene.Path, "AutoClose")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await TakeAsync(diego, scene.Path, "Unknown")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StatusAsync(diego, scene.Path)).Should().Be("Open");
    }

    [Fact(DisplayName = "Cancelar o chamado")]
    public async Task Requester_CancelsTheOpenTicket_WithAMessage()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);

        (await TakeAsync(carla, scene.Path, "Cancel")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await TakenAsync(carla, scene.Path, "Cancel", "Abri no projeto errado.");

        var ticket = await TicketAsync(carla, scene.Path);
        ticket.GetProperty("status").GetString().Should().Be("Closed");
        ticket.GetProperty("messages").EnumerateArray().Single().GetProperty("body").GetString().Should().Contain("projeto errado");
        ticket.GetProperty("permissions").GetProperty("canSend").GetBoolean().Should().BeFalse();
        ticket.GetProperty("permissions").GetProperty("actions").EnumerateArray().Should().BeEmpty();
        (await MessageAsync(scene.Admin, scene.Path, "Mais uma.")).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "Chamado fechado sem mensagens")]
    public async Task ClosedTicket_TakesNoMessages()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        using var marina = await scene.App.SignInAsync(scene.Marina);
        await TakenAsync(marina, scene.Path, "Cancel", "Duplicado do #2.");

        var sent = await MessageAsync(carla, scene.Path, "Ainda tenho uma dúvida.");

        sent.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await sent.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString()
            .Should().Be("urn:fiscoprojects:problem:ticket-closed");
        (await TicketAsync(carla, scene.Path)).GetProperty("messages").GetArrayLength().Should().Be(1);
    }

    [Fact(DisplayName = "Primeira resposta define o responsável")]
    public async Task FirstAnswerOfADeveloper_TakesTheTicket()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);

        await AnswerAsync(diego, scene.Path, "Vou olhar o XML.");

        var ticket = await TicketAsync(diego, scene.Path);
        AssigneeOf(ticket).Should().Be(scene.Diego);
        ticket.GetProperty("status").GetString().Should().Be("InProgress");
        ticket.GetProperty("following").GetBoolean().Should().BeTrue();
        ticket.GetProperty("events").EnumerateArray().Select(item => item.GetProperty("kind").GetString())
            .Should().Equal("AssigneeSet", "StatusChanged");
        var participant = ticket.GetProperty("participants").EnumerateArray()
            .Single(person => person.GetProperty("id").GetGuid() == scene.Diego);
        participant.GetProperty("role").GetString().Should().Be("Owner");
        var listed = await diego.GetFromJsonAsync<JsonElement>("/api/tickets?view=AssignedToMe", Cancellation);
        listed.GetProperty("items").EnumerateArray().Single().GetProperty("interactions").GetInt32().Should().Be(3);
    }

    [Fact(DisplayName = "Resposta que não define responsável")]
    public async Task AnswersOfTheManagerAndOfTheRequester_LeaveItUnassigned()
    {
        await using var scene = await StartAsync();
        using var marina = await scene.App.SignInAsync(scene.Marina);
        using var carla = await scene.App.SignInAsync(scene.Carla);

        await AnswerAsync(marina, scene.Path, "Diego, consegue olhar?");
        await AnswerAsync(carla, scene.Path, "Segue o XML.");

        var ticket = await TicketAsync(carla, scene.Path);
        AssigneeOf(ticket).Should().BeNull();
        ticket.GetProperty("status").GetString().Should().Be("Open");
        ticket.GetProperty("events").EnumerateArray().Should().BeEmpty();
    }

    [Fact(DisplayName = "Responsável não muda")]
    public async Task AnswerOfTheTechLead_KeepsTheAssignee()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);
        using var rafael = await scene.App.SignInAsync(scene.Rafael);
        await AnswerAsync(diego, scene.Path, "Vou olhar o XML.");

        await AnswerAsync(rafael, scene.Path, "Confere a tabela de CFOP.");

        AssigneeOf(await TicketAsync(rafael, scene.Path)).Should().Be(scene.Diego);
    }

    [Fact(DisplayName = "Primeira resposta do líder técnico")]
    public async Task TechLead_ServesDevelopment_AndTakesTheTicket()
    {
        await using var scene = await StartAsync();
        using var rafael = await scene.App.SignInAsync(scene.Rafael);

        await AnswerAsync(rafael, scene.Path, "Eu olho.");

        AssigneeOf(await TicketAsync(rafael, scene.Path)).Should().Be(scene.Rafael);
    }

    [Fact(DisplayName = "Resposta do solicitante retoma o chamado")]
    public async Task AnswerOfTheRequester_WhileWaiting_ResumesTheTicket()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);
        using var carla = await scene.App.SignInAsync(scene.Carla);
        await AnswerAsync(diego, scene.Path, "Vou olhar o XML.");
        scene.App.Clock.Advance(TimeSpan.FromMinutes(5));
        await TakenAsync(diego, scene.Path, "AskForInformation", "Qual a série da nota?");
        (await StatusAsync(carla, scene.Path)).Should().Be("WaitingForRequester");
        scene.App.Clock.Advance(TimeSpan.FromMinutes(5));

        await AnswerAsync(carla, scene.Path, "Série 2.");

        var ticket = await TicketAsync(carla, scene.Path);
        ticket.GetProperty("status").GetString().Should().Be("InProgress");
        var resumed = ticket.GetProperty("events").EnumerateArray().Last();
        (resumed.GetProperty("action").GetString(), resumed.GetProperty("actorId").GetGuid()).Should().Be(("Reply", scene.Carla));
        ticket.GetProperty("messages").GetArrayLength().Should().Be(3);
    }

    [Fact(DisplayName = "Reabrir e confirmar")]
    public async Task Requester_ReopensAndThenConfirms()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);
        using var carla = await scene.App.SignInAsync(scene.Carla);
        await AnswerAsync(diego, scene.Path, "Ajustei o cadastro.");
        await TakenAsync(diego, scene.Path, "Resolve");

        (await TakeAsync(carla, scene.Path, "Reopen")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await TakenAsync(carla, scene.Path, "Reopen", "Continua rejeitando.");
        (await StatusAsync(carla, scene.Path)).Should().Be("InProgress");
        await TakenAsync(diego, scene.Path, "Resolve");
        (await TakeAsync(diego, scene.Path, "ConfirmAndClose")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await TakenAsync(carla, scene.Path, "ConfirmAndClose");

        (await StatusAsync(carla, scene.Path)).Should().Be("Closed");
    }

    [Fact(DisplayName = "Fechar resolvidos parados")]
    public async Task ResolvedTickets_CloseAfterFiveBusinessDays()
    {
        await using var scene = await StartAsync();
        int later;
        using (var carlaOnMonday = await scene.App.SignInAsync(scene.Carla))
        {
            later = await OpenTicketAsync(carlaOnMonday, scene.Erp, "Erro no cálculo do DIFAL");
        }

        // Monday, 19/10/2026: the first is resolved now; the second on Thursday. The sessions end in between.
        using (var diego = await scene.App.SignInAsync(scene.Diego))
        {
            await AnswerAsync(diego, scene.Path, "Ajustei o cadastro.");
            await TakenAsync(diego, scene.Path, "Resolve");
        }

        scene.App.Clock.Advance(TimeSpan.FromDays(3));
        using (var diego = await scene.App.SignInAsync(scene.Diego))
        {
            await AnswerAsync(diego, $"/api/tickets/{later}", "Corrigi a alíquota.");
            await TakenAsync(diego, $"/api/tickets/{later}", "Resolve");
        }

        // Monday, 26/10: five business days after the first, three after the second.
        scene.App.Clock.Advance(TimeSpan.FromDays(4));
        using var carla = await scene.App.SignInAsync(scene.Carla);

        var closed = await ClosedAsync(carla, scene.Path);
        closed.GetProperty("status").GetString().Should().Be("Closed");
        var change = closed.GetProperty("events").EnumerateArray().Last();
        (change.GetProperty("action").GetString(), change.GetProperty("actorId").ValueKind).Should().Be(("AutoClose", JsonValueKind.Null));
        // The same round left the second one resolved.
        (await StatusAsync(carla, $"/api/tickets/{later}")).Should().Be("Resolved");
    }

    /// <summary>The ticket once the background closing, woken by the clock, has closed it.</summary>
    private static async Task<JsonElement> ClosedAsync(HttpClient client, string path)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            var ticket = await TicketAsync(client, path);
            if (ticket.GetProperty("status").GetString() == "Closed" || DateTime.UtcNow > deadline)
            {
                return ticket;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), Cancellation);
        }
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
