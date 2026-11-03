using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: chamados, requirement "Chamado para o Teams" (ADR-0044, ADR-0152).
public sealed class TeamsTicketTests(SqlServerFixture sqlServer)
{
    private const string Ticket = "/api/teams/tickets/1";

    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static string Doc(string text) => JsonSerializer.Serialize(new
    {
        type = "doc",
        content = new[] { new { type = "paragraph", content = new[] { new { type = "text", text } } } },
    });

    /// <summary>A message of the person in #1, a minute after the last thing that happened, with the files given.</summary>
    private static async Task SendAsync(TeamsScene scene, Guid author, string text, params (string Name, byte[] Content)[] files)
    {
        scene.App.Clock.Advance(TimeSpan.FromMinutes(1));
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(Doc(text), Encoding.UTF8), "body");
        foreach (var (name, content) in files)
        {
            var file = new ByteArrayContent(content);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(file, "files", name);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/tickets/1/messages") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        var sent = await (await scene.Team.ClientOfAsync(author)).SendAsync(request, Cancellation);
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
    }

    /// <summary>Marina changes the priority of #1, a minute after the last thing that happened: an event.</summary>
    private static async Task PrioritizeAsync(TeamsScene scene, string priority)
    {
        scene.App.Clock.Advance(TimeSpan.FromMinutes(1));
        var marina = await scene.Team.ClientOfAsync(scene.Team.Marina);
        (await ProjectTeam.SendAsync(marina, HttpMethod.Put, "/api/tickets/1/priority", new { priority }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task<JsonElement> ReadAsync(TeamsScene scene, Guid person)
    {
        var response = await scene.ReadAsync(person, Ticket);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Cancellation));
        return await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
    }

    [Fact(DisplayName = "Resumo do chamado no Teams")]
    public async Task TheAssignee_ReadsTheTicket_WithTheHeaderThePeopleAndTheDeadlines()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);
        var marina = await scene.Team.ClientOfAsync(scene.Team.Marina);
        (await ProjectTeam.SendAsync(marina, HttpMethod.Put, "/api/tickets/assignee", new { numbers = new[] { 1 }, assigneeId = scene.Team.Diego }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        await SendAsync(scene, scene.Team.Carla, "Seguem os arquivos.", ("nfe-3391.png", Png), ("nfe-3392.png", Png));

        var response = await scene.ReadAsync(scene.Team.Diego, Ticket);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl?.NoStore.Should().BeTrue();
        var ticket = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        ticket.GetProperty("number").GetInt32().Should().Be(1);
        ticket.GetProperty("title").GetString().Should().Be("Rejeição 539 ao emitir nota de devolução");
        ticket.GetProperty("project").GetProperty("code").GetString().Should().Be("ERP-FIN");
        ticket.GetProperty("project").GetProperty("name").GetString().Should().Be("Implantação do ERP financeiro");
        ticket.GetProperty("team").GetString().Should().Be("Development");
        ticket.GetProperty("status").GetString().Should().Be("Open");
        ticket.GetProperty("priority").GetString().Should().Be("High");
        ticket.GetProperty("openedAt").GetDateTime().Should().Be(new DateTime(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc));
        ticket.GetProperty("requester").GetProperty("name").GetString().Should().Be("Carla Mendes");
        ticket.GetProperty("assignee").GetProperty("id").GetGuid().Should().Be(scene.Team.Diego);
        ticket.GetProperty("managers").EnumerateArray().Select(person => person.GetProperty("name").GetString())
            .Should().Equal("Marina Costa");
        ticket.GetProperty("attachmentCount").GetInt32().Should().Be(2);
        ticket.GetProperty("deadlines").GetProperty("firstResponse").GetProperty("state").GetString().Should().NotBeNullOrEmpty();
        ticket.GetProperty("deadlines").GetProperty("resolution").GetProperty("dueOn").GetString().Should().NotBeNullOrEmpty();
        ticket.GetProperty("viewerId").GetGuid().Should().Be(scene.Team.Diego);
    }

    [Fact(DisplayName = "Últimas interações do chamado")]
    public async Task TheLastFiveInteractions_InTheirOrder_AndHowManyCameBefore()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);
        var carla = scene.Team.Carla;
        await SendAsync(scene, carla, "Mensagem 1", ("antigo.png", Png));
        await SendAsync(scene, carla, "Mensagem 2");
        await SendAsync(scene, carla, "Mensagem 3");
        await PrioritizeAsync(scene, "Critical");
        await SendAsync(scene, carla, "Mensagem 4");
        await SendAsync(scene, carla, "Mensagem 5");
        await PrioritizeAsync(scene, "Medium");
        await SendAsync(scene, carla, "Mensagem 6");
        await SendAsync(scene, carla, "Mensagem 7");
        await PrioritizeAsync(scene, "High");
        await SendAsync(scene, carla, "Mensagem 8", ("print.png", Png));

        var conversation = (await ReadAsync(scene, scene.Team.Lucas)).GetProperty("conversation");

        conversation.GetProperty("earlier").GetInt32().Should().Be(7);
        conversation.GetProperty("description").ValueKind.Should().Be(JsonValueKind.Null);
        conversation.GetProperty("messages").EnumerateArray()
            .Select(message => message.GetProperty("body").GetString()!)
            .Should().Equal(Doc("Mensagem 6"), Doc("Mensagem 7"), Doc("Mensagem 8"));
        conversation.GetProperty("messages").EnumerateArray()
            .Should().AllSatisfy(message => message.GetProperty("authorId").GetGuid().Should().Be(carla));
        conversation.GetProperty("events").EnumerateArray()
            .Select(item => item.GetProperty("toPriority").GetString())
            .Should().Equal("Medium", "High");
        var file = conversation.GetProperty("attachments").EnumerateArray().Should().ContainSingle().Subject;
        file.GetProperty("name").GetString().Should().Be("print.png");
        file.GetProperty("size").GetInt64().Should().Be(Png.Length);
        file.TryGetProperty("id", out _).Should().BeFalse();
        conversation.GetProperty("participants").EnumerateArray()
            .Single(person => person.GetProperty("id").GetGuid() == carla)
            .GetProperty("role").GetString().Should().Be("Requester");
    }

    [Fact(DisplayName = "Descrição como a primeira interação")]
    public async Task TheDescription_IsTheFirstInteraction()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);
        await SendAsync(scene, scene.Team.Carla, "Mensagem 1");
        await SendAsync(scene, scene.Team.Carla, "Mensagem 2");

        var conversation = (await ReadAsync(scene, scene.Team.Diego)).GetProperty("conversation");

        conversation.GetProperty("earlier").GetInt32().Should().Be(0);
        conversation.GetProperty("description").GetProperty("body").GetString().Should().Contain("A SEFAZ retorna a rejeição 539.");
        conversation.GetProperty("messages").GetArrayLength().Should().Be(2);
    }

    [Fact(DisplayName = "Resumo do chamado sem e-mails")]
    public async Task TheTicketInTeams_HasNoEmail()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);
        await SendAsync(scene, scene.Team.Marina, "Vou olhar.");

        var response = await scene.ReadAsync(scene.Team.Diego, Ticket);

        var text = await response.Content.ReadAsStringAsync(Cancellation);
        text.Should().Contain("Marina Costa").And.NotContain("@empresa.example").And.NotContain("\"email\"");
    }

    [Fact(DisplayName = "Leitor lê o chamado no Teams")]
    public async Task TheViewer_ReadsTheTicket()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);

        (await ReadAsync(scene, scene.Team.Lucas)).GetProperty("number").GetInt32().Should().Be(1);
    }

    [Fact(DisplayName = "Administrador lê o chamado no Teams")]
    public async Task TheAdministrator_OutsideTheProject_ReadsTheTicket()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);

        (await ReadAsync(scene, scene.Team.Paulo)).GetProperty("number").GetInt32().Should().Be(1);
    }

    [Fact(DisplayName = "Fora do projeto não lê o chamado no Teams")]
    public async Task SomeoneOutsideTheProject_GetsNotFound_WithNothingOfTheTicket()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);

        var response = await scene.ReadAsync(scene.Team.Sofia, Ticket);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(Cancellation)).Should().NotContain("Rejeição");
    }

    [Fact(DisplayName = "Chamado que não existe no Teams")]
    public async Task ANumberWithoutTicket_GetsNotFound()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);

        (await scene.ReadAsync(scene.Team.Diego, "/api/teams/tickets/99")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
