using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: chamados (API side): "Mensagens do chamado", "Menções no chamado", "Acompanhar o chamado", "Anexos do chamado"
// and "Visões e filtros da lista" (ADR-0026, ADR-0030, ADR-0044, ADR-0068).
public sealed class TicketConversationTests(SqlServerFixture sqlServer) : IDisposable
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

    private readonly string _files = Path.Combine(Path.GetTempPath(), $"fiscoprojects-tickets-{Guid.NewGuid():N}");

    /// <summary>
    /// ERP-FIN, managed by Marina, with Rafael as tech lead, Carla as contributor, Diego as developer and Lucas as viewer;
    /// Carla opens #1 for Desenvolvimento. Sofia is in no project.
    /// </summary>
    private sealed record Scene(
        SessionApp App, HttpClient Admin, Guid Marina, Guid Rafael, Guid Carla, Guid Diego, Guid Lucas, Guid Sofia, Guid Erp, int Number)
        : IAsyncDisposable
    {
        public string Path => $"/api/tickets/{Number}";

        public ValueTask DisposeAsync() => App.DisposeAsync();
    }

    public void Dispose()
    {
        if (Directory.Exists(_files))
        {
            Directory.Delete(_files, recursive: true);
        }
    }

    private async Task<Scene> StartAsync(int maxMegabytes = 15)
    {
        var app = await SessionApp.StartAsync(sqlServer, builder =>
        {
            builder.Configuration["FileStorage:Provider"] = "Local";
            builder.Configuration["FileStorage:Local:RootPath"] = _files;
            builder.Configuration["Tickets:Attachments:MaxMegabytes"] = maxMegabytes.ToString(System.Globalization.CultureInfo.InvariantCulture);
        });
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        var rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.TechLead);
        var carla = await app.CreateUserAsync("Carla Mendes", SystemRole.User);
        var diego = await app.CreateUserAsync("Diego Torres", SystemRole.User);
        var lucas = await app.CreateUserAsync("Lucas Prado", SystemRole.User);
        var sofia = await app.CreateUserAsync("Sofia Martins", SystemRole.User);
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
        await app.AddProjectMemberAsync(erp, lucas, "Viewer");
        using var asCarla = await app.SignInAsync(carla);
        var opened = await SendAsync(asCarla, HttpMethod.Post, "/api/tickets", new
        {
            projectId = erp,
            team = "Development",
            priority = "High",
            title = "Rejeição 539 ao emitir nota de devolução",
            description = "ignored",
            descriptionBody = Doc(Paragraph(Text("A SEFAZ retorna a "), Text("rejeição 539", bold: true))),
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        var number = (await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
        return new Scene(app, admin, marina, rafael, carla, diego, lucas, sofia, erp, number);
    }

    private static object Text(string text, bool bold = false) =>
        bold ? new { type = "text", text, marks = new[] { new { type = "bold" } } } : new { type = "text", text };

    private static object Mention(Guid id, string label) => new { type = "mention", attrs = new { id, label } };

    private static object Paragraph(params object[] content) => new { type = "paragraph", content };

    private static string Doc(params object[] content) => JsonSerializer.Serialize(new { type = "doc", content });

    private static Task<HttpResponseMessage> PostMessageAsync(
        HttpClient client, Scene scene, string body, params (string Name, byte[] Content)[] files) =>
        PostFormAsync(client, $"{scene.Path}/messages", body, files);

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string path, string? body, params (string Name, byte[] Content)[] files)
    {
        using var form = new MultipartFormDataContent();
        if (body is not null)
        {
            form.Add(new StringContent(body, Encoding.UTF8), "body");
        }

        foreach (var (name, content) in files)
        {
            var file = new ByteArrayContent(content);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(file, "files", name);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = form };
        request.Headers.Add("X-CSRF", "1");
        return await client.SendAsync(request, Cancellation);
    }

    private static Task<JsonElement> TicketAsync(HttpClient client, Scene scene) =>
        client.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation);

    [Fact(DisplayName = "Mandar mensagem")]
    public async Task Requester_SendsAFormattedMessage_SeenByEveryone()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        using var lucas = await scene.App.SignInAsync(scene.Lucas);
        scene.App.Clock.Advance(TimeSpan.FromMinutes(30));

        var sent = await PostMessageAsync(carla, scene, Doc(Paragraph(Text("Segue o "), Text("XML", bold: true))));

        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
        var ticket = await TicketAsync(lucas, scene);
        var message = ticket.GetProperty("messages").EnumerateArray().Single();
        message.GetProperty("authorId").GetGuid().Should().Be(scene.Carla);
        message.GetProperty("body").GetString().Should().Contain("\"bold\"").And.Contain("XML");
        ticket.GetProperty("updatedAt").GetDateTime().Should().Be(scene.App.Clock.GetUtcNow().UtcDateTime);
        ticket.GetProperty("descriptionBody").GetString().Should().Contain("rejeição 539");
        ticket.GetProperty("description").GetString().Should().Be("A SEFAZ retorna a rejeição 539");
        // The viewer only follows: no answer box.
        ticket.GetProperty("permissions").GetProperty("canSend").GetBoolean().Should().BeFalse();
        (await TicketAsync(carla, scene)).GetProperty("permissions").GetProperty("canSend").GetBoolean().Should().BeTrue();
    }

    public static TheoryData<string, HttpStatusCode> WhoSends => new()
    {
        { "Administrator", HttpStatusCode.Created },
        { "Manager", HttpStatusCode.Created },
        { "TechLead", HttpStatusCode.Created },
        { "Requester", HttpStatusCode.Created },
        { "Developer", HttpStatusCode.Created },
        { "Contributor", HttpStatusCode.Forbidden },
        { "Viewer", HttpStatusCode.Forbidden },
        { "Outside", HttpStatusCode.NotFound },
    };

    // The matrix of ADR-0044 for messages: the Administrator, the managers, who serves the team and the requester.
    [Theory(DisplayName = "Quem manda mensagens")]
    [MemberData(nameof(WhoSends))]
    public async Task Messages_FollowTheRelationWithTheTicket(string who, HttpStatusCode expected)
    {
        await using var scene = await StartAsync();
        var person = who switch
        {
            "Manager" => scene.Marina,
            "TechLead" => scene.Rafael,
            "Requester" => scene.Carla,
            "Developer" => scene.Diego,
            "Contributor" => await ContributorAsync(scene),
            "Viewer" => scene.Lucas,
            _ => scene.Sofia,
        };
        using var client = who == "Administrator" ? null : await scene.App.SignInAsync(person);

        (await PostMessageAsync(client ?? scene.Admin, scene, Doc(Paragraph(Text("Olá"))))).StatusCode.Should().Be(expected);
    }

    /// <summary>Another contributor, who serves Funcional and not the Desenvolvimento of the ticket.</summary>
    private static async Task<Guid> ContributorAsync(Scene scene)
    {
        var beatriz = await scene.App.CreateUserAsync("Beatriz Lima", SystemRole.User);
        await scene.App.AddProjectMemberAsync(scene.Erp, beatriz, "Contributor");
        return beatriz;
    }

    [Fact(DisplayName = "Leitor não manda mensagem")]
    public async Task Viewer_DoesNotSendMessages()
    {
        await using var scene = await StartAsync();
        using var lucas = await scene.App.SignInAsync(scene.Lucas);

        (await PostMessageAsync(lucas, scene, Doc(Paragraph(Text("Olá"))))).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await TicketAsync(lucas, scene)).GetProperty("messages").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "Texto formatado inválido")]
    public async Task InvalidRichText_IsRefused()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);

        string[] bodies =
        [
            Doc(Paragraph()),
            "não é JSON",
            JsonSerializer.Serialize(new { type = "doc", content = new[] { new { type = "heading", content = new[] { Text("Título") } } } }),
            JsonSerializer.Serialize(new
            {
                type = "doc",
                content = new[]
                {
                    new
                    {
                        type = "paragraph",
                        content = new[]
                        {
                            new { type = "text", text = "clique", marks = new[] { new { type = "link", attrs = new { href = "javascript:alert(1)" } } } },
                        },
                    },
                },
            }),
        ];
        foreach (var body in bodies)
        {
            var response = await PostMessageAsync(carla, scene, body);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        }

        (await TicketAsync(carla, scene)).GetProperty("messages").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "Mencionar quem atende")]
    public async Task Requester_MentionsADeveloper_WhoThenFollows()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        using var diego = await scene.App.SignInAsync(scene.Diego);

        var sent = await PostMessageAsync(carla, scene, Doc(Paragraph(Mention(scene.Diego, "Diego Torres"), Text(" pode olhar?"))));

        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
        var ticket = await TicketAsync(diego, scene);
        ticket.GetProperty("followers").EnumerateArray().Select(id => id.GetGuid()).Should().BeEquivalentTo([scene.Carla, scene.Diego]);
        ticket.GetProperty("following").GetBoolean().Should().BeTrue();
        // Who may be mentioned: the managers, the requester and who serves Desenvolvimento.
        ticket.GetProperty("mentionable").EnumerateArray().Select(id => id.GetGuid())
            .Should().BeEquivalentTo([scene.Marina, scene.Rafael, scene.Carla, scene.Diego]);
        var page = await diego.GetFromJsonAsync<JsonElement>("/api/tickets?view=Following", Cancellation);
        page.GetProperty("items").EnumerateArray().Single().GetProperty("number").GetInt32().Should().Be(scene.Number);
        (await diego.GetFromJsonAsync<JsonElement>("/api/tickets/counts", Cancellation)).GetProperty("following").GetInt32().Should().Be(1);
        // The parts of the people, for the colors of the conversation (ADR-0069).
        var roles = ticket.GetProperty("participants").EnumerateArray()
            .ToDictionary(person => person.GetProperty("id").GetGuid(), person => person.GetProperty("role").GetString());
        (roles[scene.Carla], roles[scene.Diego], roles[scene.Marina], roles[scene.Rafael])
            .Should().Be(("Requester", "Development", "Manager", "TechLead"));
    }

    [Fact(DisplayName = "Menção fora dos envolvidos")]
    public async Task MentionOfSomeoneNotInvolved_IsRefused()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);
        var beatriz = await ContributorAsync(scene);

        var response = await PostMessageAsync(diego, scene, Doc(Paragraph(Mention(beatriz, "Beatriz Lima"))));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString()
            .Should().Be("urn:fiscoprojects:problem:ticket-mention-refused");
        (await TicketAsync(diego, scene)).GetProperty("messages").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "Acompanhar um chamado")]
    public async Task Viewer_FollowsATicket()
    {
        await using var scene = await StartAsync();
        using var lucas = await scene.App.SignInAsync(scene.Lucas);

        (await SendAsync(lucas, HttpMethod.Put, $"{scene.Path}/following")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SendAsync(lucas, HttpMethod.Put, $"{scene.Path}/following")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var ticket = await TicketAsync(lucas, scene);
        ticket.GetProperty("following").GetBoolean().Should().BeTrue();
        ticket.GetProperty("followers").EnumerateArray().Select(id => id.GetGuid()).Should().BeEquivalentTo([scene.Carla, scene.Lucas]);
        (await lucas.GetFromJsonAsync<JsonElement>("/api/tickets?view=Following", Cancellation))
            .GetProperty("total").GetInt32().Should().Be(1);
        using var sofia = await scene.App.SignInAsync(scene.Sofia);
        (await SendAsync(sofia, HttpMethod.Put, $"{scene.Path}/following")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Deixar de acompanhar")]
    public async Task Requester_StopsFollowing()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        (await carla.GetFromJsonAsync<JsonElement>("/api/tickets?view=Following", Cancellation))
            .GetProperty("total").GetInt32().Should().Be(1);

        (await SendAsync(carla, HttpMethod.Delete, $"{scene.Path}/following")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await carla.GetFromJsonAsync<JsonElement>("/api/tickets?view=Following", Cancellation))
            .GetProperty("total").GetInt32().Should().Be(0);
        (await TicketAsync(carla, scene)).GetProperty("following").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "Anexar arquivos a uma mensagem")]
    public async Task Message_TakesFiles_AndTheImageOpensInTheBrowser()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);
        using var lucas = await scene.App.SignInAsync(scene.Lucas);

        var sent = await PostMessageAsync(
            diego, scene, Doc(Paragraph(Text("Seguem os arquivos"))), ("erro-539.png", Png), ("nota-3381.xml", "<nfe/>"u8.ToArray()));

        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
        var ticket = await TicketAsync(lucas, scene);
        var message = ticket.GetProperty("messages").EnumerateArray().Single().GetProperty("id").GetGuid();
        var attachments = ticket.GetProperty("attachments").EnumerateArray().ToList();
        attachments.Select(attachment => attachment.GetProperty("name").GetString()).Should().Equal("erro-539.png", "nota-3381.xml");
        attachments.Should().OnlyContain(attachment => attachment.GetProperty("messageId").GetGuid() == message);

        var image = await lucas.GetAsync($"{scene.Path}/attachments/{attachments[0].GetProperty("id").GetGuid()}", Cancellation);
        image.StatusCode.Should().Be(HttpStatusCode.OK);
        image.Content.Headers.ContentType?.MediaType.Should().Be("image/png");
        image.Content.Headers.ContentDisposition.Should().BeNull();
        (await image.Content.ReadAsByteArrayAsync(Cancellation)).Should().Equal(Png);
        var xml = await lucas.GetAsync($"{scene.Path}/attachments/{attachments[1].GetProperty("id").GetGuid()}", Cancellation);
        xml.Content.Headers.ContentDisposition?.DispositionType.Should().Be("attachment");
        xml.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
    }

    [Fact(DisplayName = "Anexo recusado")]
    public async Task DangerousOrLargeFiles_AreRefused()
    {
        await using var scene = await StartAsync(maxMegabytes: 1);
        using var diego = await scene.App.SignInAsync(scene.Diego);
        var body = Doc(Paragraph(Text("Arquivo")));

        async Task<string?> ReasonAsync(HttpResponseMessage response)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("reason").GetString();
        }

        (await ReasonAsync(await PostMessageAsync(diego, scene, body, ("ferramenta.exe", [1, 2, 3])))).Should().Be("Dangerous");
        // A program with another name is told by its start.
        (await ReasonAsync(await PostMessageAsync(diego, scene, body, ("leia.txt", "MZ\u0090\0"u8.ToArray())))).Should().Be("Dangerous");
        (await ReasonAsync(await PostMessageAsync(diego, scene, body, ("grande.xml", new byte[(1024 * 1024) + 1]))))
            .Should().Be("TooLarge");

        var ticket = await TicketAsync(diego, scene);
        ticket.GetProperty("messages").GetArrayLength().Should().Be(0);
        ticket.GetProperty("attachments").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "Anexo fora do acesso")]
    public async Task Outsider_DoesNotOpenAttachments()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        using var sofia = await scene.App.SignInAsync(scene.Sofia);
        // The requester attaches to the description right after opening it, while nobody answered.
        (await PostFormAsync(carla, $"{scene.Path}/attachments", null, ("erro-539.png", Png))).StatusCode.Should().Be(HttpStatusCode.Created);
        var attachment = (await TicketAsync(carla, scene)).GetProperty("attachments").EnumerateArray().Single();
        attachment.GetProperty("messageId").ValueKind.Should().Be(JsonValueKind.Null);

        (await sofia.GetAsync($"{scene.Path}/attachments/{attachment.GetProperty("id").GetGuid()}", Cancellation))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var diego = await scene.App.SignInAsync(scene.Diego);
        (await PostFormAsync(diego, $"{scene.Path}/attachments", null, ("outro.png", Png))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Baixar um anexo")]
    public async Task Attachment_DownloadsWithItsName()
    {
        await using var scene = await StartAsync();
        using var diego = await scene.App.SignInAsync(scene.Diego);
        (await PostMessageAsync(diego, scene, Doc(Paragraph(Text("Print"))), ("erro-539.png", Png))).StatusCode.Should().Be(HttpStatusCode.Created);
        using var lucas = await scene.App.SignInAsync(scene.Lucas);
        var attachment = (await TicketAsync(lucas, scene)).GetProperty("attachments").EnumerateArray().Single();

        var image = await lucas.GetAsync($"{scene.Path}/attachments/{attachment.GetProperty("id").GetGuid()}?download=true", Cancellation);

        image.StatusCode.Should().Be(HttpStatusCode.OK);
        image.Content.Headers.ContentDisposition?.DispositionType.Should().Be("attachment");
        image.Content.Headers.ContentDisposition?.FileNameStar.Should().Be("erro-539.png");
        (await image.Content.ReadAsByteArrayAsync(Cancellation)).Should().Equal(Png);
    }

    [Fact(DisplayName = "Baixar todos os anexos")]
    public async Task Archive_HasAllTheAttachments_InTheOrderOfTheList()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        var pdf = "%PDF-1.4\n"u8.ToArray();
        (await PostFormAsync(carla, $"{scene.Path}/attachments", null, ("erro.png", Png))).StatusCode.Should().Be(HttpStatusCode.Created);
        using var diego = await scene.App.SignInAsync(scene.Diego);
        (await PostMessageAsync(diego, scene, Doc(Paragraph(Text("Outro print e a nota"))), ("erro.png", [.. Png, 0]), ("nota.pdf", pdf)))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        using var lucas = await scene.App.SignInAsync(scene.Lucas);
        var attachments = (await TicketAsync(lucas, scene)).GetProperty("attachments").EnumerateArray()
            .Select(attachment => (Name: attachment.GetProperty("name").GetString(), Size: attachment.GetProperty("size").GetInt64()))
            .ToList();

        var response = await lucas.GetAsync($"{scene.Path}/attachments/archive", Cancellation);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/zip");
        response.Content.Headers.ContentDisposition?.FileNameStar.Should().Be($"chamado-{scene.Number}-anexos.zip");
        response.Headers.CacheControl?.NoStore.Should().BeTrue();
        await using var zip = await ZipArchive.CreateAsync(
            await response.Content.ReadAsStreamAsync(Cancellation), ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null, Cancellation);
        // The two "erro.png" already have different names in the ticket, as the folder of the ticket keeps them.
        attachments.Select(attachment => attachment.Name).Distinct().Should().HaveCount(3);
        zip.Entries.Select(entry => entry.FullName).Should().Equal(attachments.Select(attachment => attachment.Name));
        var second = zip.GetEntry(attachments.Single(attachment => attachment.Size == Png.Length + 1).Name!);
        await using var content = await second!.OpenAsync(Cancellation);
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, Cancellation);
        copy.ToArray().Should().Equal([.. Png, 0]);
    }

    [Fact(DisplayName = "Baixar todos sem anexos ou sem acesso")]
    public async Task Archive_IsNotFound_WithoutAttachmentsOrAccess()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        (await carla.GetAsync($"{scene.Path}/attachments/archive", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await PostFormAsync(carla, $"{scene.Path}/attachments", null, ("erro.png", Png))).StatusCode.Should().Be(HttpStatusCode.Created);
        (await carla.GetAsync($"{scene.Path}/attachments/archive", Cancellation)).StatusCode.Should().Be(HttpStatusCode.OK);
        using var sofia = await scene.App.SignInAsync(scene.Sofia);
        (await sofia.GetAsync($"{scene.Path}/attachments/archive", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Interações na lista")]
    public async Task List_CountsTheMessages()
    {
        await using var scene = await StartAsync();
        using var carla = await scene.App.SignInAsync(scene.Carla);
        foreach (var text in new[] { "Um", "Dois", "Três" })
        {
            (await PostMessageAsync(carla, scene, Doc(Paragraph(Text(text))))).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var item = (await carla.GetFromJsonAsync<JsonElement>("/api/tickets", Cancellation)).GetProperty("items").EnumerateArray().Single();

        item.GetProperty("interactions").GetInt32().Should().Be(3);
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
