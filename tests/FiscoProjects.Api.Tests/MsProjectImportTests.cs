using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;
using static FiscoProjects.Testing.MsProjectFiles;

namespace FiscoProjects.Api.Tests;

// Spec: cronograma (API side): "Importar do MS Project" and "Progresso e responsáveis do MS Project" (ADR-0135). The
// calendar of 2026 has 12/10 as a holiday, on a Monday; today is Monday 19/10/2026. Carla Mendes is in the directory of
// the demonstration and never signed in.
public sealed class MsProjectImportTests(SqlServerFixture sqlServer)
{
    private const string PreviewPath = "/api/projects/ms-project-preview";
    private const string CarlaEmail = "carla.mendes@empresa.example";
    private const string CarlaInTheDirectory = "local:carla.mendes@empresa.example";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private sealed record Scene(SessionApp App, HttpClient Admin, Guid Marina, Guid Rafael, Guid Diego);

    private async Task<Scene> StartAsync()
    {
        var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        var rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.TechLead);
        var diego = await app.CreateUserAsync("Diego Torres", SystemRole.User);
        return new Scene(app, admin, marina, rafael, diego);
    }

    private static async Task<HttpResponseMessage> PreviewAsync(
        HttpClient client, byte[] content, string name = "cronograma.xml", string? start = null, Guid? clientId = null)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(content), "file", name);
        if (start is not null)
        {
            form.Add(new StringContent(start), "start");
        }

        if (clientId is { } id)
        {
            form.Add(new StringContent(id.ToString()), "clientId");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, PreviewPath) { Content = form };
        request.Headers.Add("X-CSRF", "1");
        return await client.SendAsync(request, Cancellation);
    }

    private static async Task<JsonElement> PreviewOfAsync(HttpClient client, byte[] content, string? start = null, Guid? clientId = null)
    {
        var response = await PreviewAsync(client, content, start: start, clientId: clientId);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Cancellation));
        return await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
    }

    /// <summary>The schedule of the preview to send back, with who each resource is.</summary>
    private static JsonObject ImportedSchedule(JsonElement preview, params object?[] resources) => new()
    {
        ["phases"] = JsonNode.Parse(preview.GetProperty("plan").GetProperty("phases").GetRawText()),
        ["items"] = JsonNode.Parse(preview.GetProperty("plan").GetProperty("items").GetRawText()),
        ["resources"] = JsonSerializer.SerializeToNode(resources),
    };

    private static List<JsonElement> ItemsOf(JsonElement schedule) =>
        [.. schedule.GetProperty("phases").EnumerateArray().SelectMany(phase => phase.GetProperty("items").EnumerateArray())];

    private static List<string?> WarningNames(JsonElement preview, string kind) =>
        [.. preview.GetProperty("warnings").EnumerateArray()
            .Where(warning => warning.GetProperty("kind").GetString() == kind)
            .SelectMany(warning => warning.GetProperty("names").EnumerateArray().Select(name => name.GetString()))];

    [Fact(DisplayName = "Prévia do arquivo do MS Project")]
    public async Task Preview_HasThePhase_TheTask_TheMilestone_AndTheResource_KeepingNothing()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        var users = await app.CountAsync("SELECT COUNT(*) FROM [users].[users]");

        var response = await PreviewAsync(marina, Configuration(CarlaEmail));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Cancellation));
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var preview = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        (preview.GetProperty("title").GetString(), preview.GetProperty("firstDay").GetString(), preview.GetProperty("start").GetString())
            .Should().Be(("Implantação do ERP", "2026-10-13", "2026-10-13"));
        var phase = preview.GetProperty("phases").EnumerateArray().Single();
        phase.GetProperty("name").GetString().Should().Be("Configuração");
        phase.GetProperty("items").EnumerateArray()
            .Select(item => (item.GetProperty("name").GetString(), item.GetProperty("kind").GetString(), item.GetProperty("start").GetString(),
                item.GetProperty("end").GetString(), item.GetProperty("duration").GetInt32(), item.GetProperty("progress").GetInt32()))
            .Should().Equal(
                ("Parametrizar regras de ICMS", "Task", "2026-10-13", "2026-10-15", 3, 40),
                ("Aceite da configuração", "Milestone", "2026-10-15", "2026-10-15", 0, 0));
        var plan = preview.GetProperty("plan").GetProperty("items");
        plan[1].GetProperty("after").EnumerateArray().Select(index => index.GetInt32()).Should().Equal(0);
        var carla = preview.GetProperty("resources").EnumerateArray().Single();
        (carla.GetProperty("name").GetString(), carla.GetProperty("tasks").GetInt32(), carla.GetProperty("client").GetBoolean())
            .Should().Be(("Carla Mendes", 1, false));
        carla.GetProperty("person").GetProperty("directoryId").GetString().Should().Be(CarlaInTheDirectory);
        preview.GetProperty("warnings").GetArrayLength().Should().Be(0);
        (await app.CountAsync("SELECT COUNT(*) FROM [projects].[projects]")).Should().Be(0);
        (await app.CountAsync("SELECT COUNT(*) FROM [users].[users]")).Should().Be(users);
    }

    [Fact(DisplayName = "Responsáveis do arquivo")]
    public async Task Preview_SuggestsThePersonByTheEmail_TheClientByTheName_OrNobody()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        var client = await IdOfAsync(await SendAsync(scene.Admin, HttpMethod.Post, "/api/clients", new { name = "Cliente Fictício" }));
        var file = Project(
            "Implantação do ERP",
            [
                new Line(1, 1, "Configuração", D(10, 13), D(10, 20)),
                new Line(2, 2, "Parametrizar", D(10, 13), D(10, 14)),
                new Line(3, 2, "Treinar usuários", D(10, 15), D(10, 16)),
                new Line(4, 2, "Homologar", D(10, 19), D(10, 20)),
            ],
            [new Resource(1, "Carla Mendes", CarlaEmail), new Resource(2, "Consultor externo"), new Resource(3, "cliente fictício")],
            [(2, 1), (3, 2), (3, 1), (4, 3)]);

        var preview = await PreviewOfAsync(scene.Admin, file, clientId: client);

        preview.GetProperty("resources").EnumerateArray()
            .Select(resource => (resource.GetProperty("name").GetString(), resource.GetProperty("tasks").GetInt32(),
                resource.GetProperty("person").ValueKind == JsonValueKind.Null ? null : resource.GetProperty("person").GetProperty("directoryId").GetString(),
                resource.GetProperty("client").GetBoolean()))
            .Should().Equal(
                ("Carla Mendes", 1, CarlaInTheDirectory, false),
                ("Consultor externo", 1, null, false),
                ("cliente fictício", 1, null, true));
        WarningNames(preview, "SeveralResources").Should().Equal("Treinar usuários");
    }

    [Fact(DisplayName = "O que não cabe no FiscoProjects")]
    public async Task Preview_TellsEveryChangeOfTheConversion_WithTheNames()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        var file = Project(
            "Implantação do ERP",
            [
                new Line(1, 1, "Configuração", D(10, 13), D(10, 22)),
                new Line(2, 2, "Levantar regras", D(10, 13), D(10, 14)),
                new Line(3, 2, "Testes", D(10, 15), D(10, 16)),
                new Line(4, 3, "Roteiros", D(10, 15), D(10, 16)),
                new Line(5, 4, "Roteiro de notas", D(10, 15), D(10, 16)),
                new Line(6, 3, "Massa de testes", D(10, 15), D(10, 15), Links: [new Link(2, Type: 3)]),
                new Line(7, 2, "Ajustar parâmetros", D(10, 19), D(10, 20), Links: [new Link(2, Lag: 4800)]),
                new Line(8, 2, "Revisar", D(10, 21), D(10, 21), Links: [new Link(3)]),
                new Line(9, 1, "Comunicar a diretoria", D(10, 22), D(10, 22)),
            ]);

        var preview = await PreviewOfAsync(scene.Admin, file);

        preview.GetProperty("phases").EnumerateArray().Select(phase => phase.GetProperty("name").GetString())
            .Should().Equal("Configuração", "Implantação do ERP");
        var deep = ItemsOf(preview).Single(item => item.GetProperty("name").GetString() == "Roteiro de notas");
        deep.GetProperty("isSubtask").GetBoolean().Should().BeTrue();
        var adjust = preview.GetProperty("plan").GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("name").GetString() == "Ajustar parâmetros");
        adjust.GetProperty("after").GetArrayLength().Should().Be(1);
        WarningNames(preview, "DeepTasks").Should().Equal("Roteiro de notas");
        WarningNames(preview, "DroppedSummaries").Should().Equal("Roteiros");
        WarningNames(preview, "LooseTasks").Should().Equal("Comunicar a diretoria");
        WarningNames(preview, "OtherDependencies").Should().Equal("Levantar regras → Massa de testes");
        WarningNames(preview, "DependencyLags").Should().Equal("Levantar regras → Ajustar parâmetros");
        WarningNames(preview, "SummaryDependencies").Should().Equal("Testes → Revisar");
    }

    public static TheoryData<string, string> FilesNotOfTheMsProject => new()
    {
        { "mpp", "NotMsProjectXml" },
        { "outro programa", "NotMsProjectXml" },
        { "dtd", "NotMsProjectXml" },
        { "grande", "TooLarge" },
    };

    [Theory(DisplayName = "Arquivo que não é do MS Project")]
    [MemberData(nameof(FilesNotOfTheMsProject))]
    public async Task AFileThatIsNotAnXmlOfTheMsProject_IsRefused_WithTheReason(string file, string reason)
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        var content = file switch
        {
            "mpp" => [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0x00, 0x00],
            "outro programa" => Bytes("<?xml version=\"1.0\"?><workbook><sheet name=\"Cronograma\" /></workbook>"),
            "dtd" => Bytes("""
                <?xml version="1.0"?>
                <!DOCTYPE Project [ <!ENTITY outside SYSTEM "https://example.com/entity"> ]>
                <Project xmlns="http://schemas.microsoft.com/project"><Title>&outside;</Title></Project>
                """),
            _ => new byte[(5 * 1024 * 1024) + 1],
        };

        var response = await PreviewAsync(scene.Admin, content, file == "mpp" ? "cronograma.mpp" : "cronograma.xml");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        (problem.GetProperty("type").GetString(), problem.GetProperty("reason").GetString())
            .Should().Be(("urn:fiscoprojects:problem:ms-project-file-refused", reason));
        problem.GetProperty("detail").GetString().Should().Contain("XML");
    }

    [Fact(DisplayName = "Importar sem poder criar projetos")]
    public async Task APersonWithTheRoleUser_IsForbidden()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var diego = await app.SignInAsync(scene.Diego);

        (await PreviewAsync(diego, Configuration(CarlaEmail))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Ida e volta pelo MS Project")]
    public async Task TheExportedFile_ComesBackInThePreview_AsTheSameSchedule()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        var project = await IdOfAsync(await SendAsync(marina, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-FIN",
            name = "Implantação do ERP financeiro",
            techLeadId = scene.Rafael,
        }));
        var path = $"/api/projects/{project}/schedule";
        var phase = await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"{path}/phases", new { name = "Configuração" }));
        var survey = await AddItemAsync(marina, path, phase, "Task", "Levantar regras", "2026-10-13", 3, 50, owner: scene.Rafael);
        var tests = await AddItemAsync(marina, path, phase, "Task", "Testes integrados", "2026-10-16", 3, 0, after: [survey]);
        var script = await AddItemAsync(marina, path, phase, "Task", "Roteiro", "2026-10-16", 2, 100, after: [survey], parent: tests);
        await AddItemAsync(marina, path, phase, "Task", "Execução", "2026-10-20", 1, 0, after: [script], parent: tests);
        await AddItemAsync(marina, path, phase, "Milestone", "Aceite", "2026-10-21", 0, 0, after: [script]);
        var exported = await marina.GetByteArrayAsync($"{path}/ms-project", Cancellation);

        var preview = await PreviewOfAsync(marina, exported);

        var schedule = await marina.GetFromJsonAsync<JsonElement>(path, Cancellation);
        preview.GetProperty("phases").EnumerateArray().Select(one => one.GetProperty("name").GetString()).Should().Equal("Configuração");
        static (string?, string?, int, int) Shape(JsonElement item) =>
            (item.GetProperty("name").GetString(), item.GetProperty("kind").GetString(), item.GetProperty("duration").GetInt32(),
                item.GetProperty("progress").GetInt32());
        ItemsOf(preview).Select(Shape).Should().BeEquivalentTo(ItemsOf(schedule).Select(Shape));
        ItemsOf(preview).Where(item => item.GetProperty("isSubtask").GetBoolean()).Select(item => item.GetProperty("name").GetString())
            .Should().BeEquivalentTo(["Roteiro", "Execução"]);
        var plan = preview.GetProperty("plan").GetProperty("items").EnumerateArray().ToList();
        var names = plan.Select(item => item.GetProperty("name").GetString()).ToList();
        plan.SelectMany(item => item.GetProperty("after").EnumerateArray()
                .Select(before => $"{names[before.GetInt32()]} → {item.GetProperty("name").GetString()}"))
            .Should().BeEquivalentTo(["Levantar regras → Roteiro", "Roteiro → Execução", "Roteiro → Aceite"]);
        preview.GetProperty("resources").EnumerateArray().Select(resource => resource.GetProperty("name").GetString())
            .Should().Equal("Rafael Souza");
        preview.GetProperty("warnings").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "Projeto a partir do MS Project")]
    public async Task ANewProjectFromTheMsProject_HasTheSchedule_TheOwners_AndThePeople()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        var preview = await PreviewOfAsync(marina, Configuration(CarlaEmail), start: "2026-10-09");

        // Friday 09/10, before the holiday of Monday 12/10.
        var created = await SendAsync(marina, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-RH",
            name = "Implantação do ERP de RH",
            scheduleStart = "2026-10-09",
            importedSchedule = ImportedSchedule(preview, new { directoryId = CarlaInTheDirectory, role = "Contributor" }),
        });

        var project = await IdOfAsync(created);
        var schedule = await marina.GetFromJsonAsync<JsonElement>($"/api/projects/{project}/schedule", Cancellation);
        schedule.GetProperty("phases").EnumerateArray().Select(phase => phase.GetProperty("name").GetString()).Should().Equal("Configuração");
        var items = ItemsOf(schedule);
        items.Select(item => (item.GetProperty("name").GetString(), item.GetProperty("start").GetString(), item.GetProperty("end").GetString(),
                item.GetProperty("progress").GetInt32()))
            .Should().Equal(
                ("Parametrizar regras de ICMS", "2026-10-09", "2026-10-14", 40),
                ("Aceite da configuração", "2026-10-14", "2026-10-14", 0));
        items[0].GetProperty("owner").GetProperty("name").GetString().Should().Be("Carla Mendes");
        items[1].GetProperty("predecessorIds").EnumerateArray().Single().GetGuid().Should().Be(items[0].GetProperty("id").GetGuid());
        var members = (await marina.GetFromJsonAsync<JsonElement>($"/api/projects/{project}", Cancellation)).GetProperty("members");
        members.EnumerateArray().Select(member => (member.GetProperty("name").GetString(), member.GetProperty("role").GetString()))
            .Should().BeEquivalentTo([("Marina Costa", "Manager"), ("Carla Mendes", "Contributor")]);
    }

    [Fact(DisplayName = "Progresso do arquivo")]
    public async Task TheProgressOfTheFile_CompletesTheTaskOnItsEnd_AndReachesTheMilestoneToday()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        var file = Project(
            "Andamento",
            [
                new Line(1, 1, "Configuração", D(10, 13), D(10, 23)),
                new Line(2, 2, "Parametrizar", D(10, 13), D(10, 14), Percent: 100),
                new Line(3, 2, "Aceite", D(10, 23), D(10, 23), Percent: 100, Milestone: true),
            ]);
        var preview = await PreviewOfAsync(scene.Admin, file);

        var project = await IdOfAsync(await SendAsync(scene.Admin, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-RH",
            name = "Implantação do ERP de RH",
            managerId = scene.Marina,
            scheduleStart = "2026-10-13",
            importedSchedule = ImportedSchedule(preview),
        }));

        var items = ItemsOf(await scene.Admin.GetFromJsonAsync<JsonElement>($"/api/projects/{project}/schedule", Cancellation));
        (items[0].GetProperty("progress").GetInt32(), items[0].GetProperty("completedOn").GetString()).Should().Be((100, "2026-10-14"));
        (items[1].GetProperty("start").GetString(), items[1].GetProperty("reachedOn").GetString()).Should().Be(("2026-10-23", "2026-10-19"));
    }

    [Theory(DisplayName = "Cronograma do MS Project inválido")]
    [InlineData("com modelo")]
    [InlineData("papel de gerente")]
    [InlineData("cliente sem cliente")]
    [InlineData("dependência adiante")]
    [InlineData("sem início")]
    public async Task AScheduleThatDoesNotHoldTogether_IsRefused_CreatingNobody(string invalid)
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        var preview = await PreviewOfAsync(marina, Configuration(CarlaEmail));
        var imported = ImportedSchedule(preview, new { directoryId = CarlaInTheDirectory, role = invalid == "papel de gerente" ? "Manager" : "Developer" });
        if (invalid == "cliente sem cliente")
        {
            imported["resources"]![0]!["directoryId"] = null;
            imported["resources"]![0]!["client"] = true;
        }

        if (invalid == "dependência adiante")
        {
            imported["items"]![0]!["after"] = new JsonArray(1);
            imported["items"]![1]!["after"] = new JsonArray();
        }

        var templateId = invalid == "com modelo" ? Guid.CreateVersion7() : (Guid?)null;
        var users = await app.CountAsync("SELECT COUNT(*) FROM [users].[users]");

        var response = await SendAsync(marina, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-RH",
            name = "Implantação do ERP de RH",
            scheduleStart = invalid == "sem início" ? null : "2026-10-13",
            templateId,
            importedSchedule = imported,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var fields = (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").EnumerateObject()
            .Select(field => field.Name).ToList();
        fields.Should().Contain(invalid == "sem início" ? "scheduleStart" : "importedSchedule");
        (await app.CountAsync("SELECT COUNT(*) FROM [projects].[projects]")).Should().Be(0);
        (await app.CountAsync("SELECT COUNT(*) FROM [users].[users]")).Should().Be(users);
    }

    private static async Task<Guid> AddItemAsync(
        HttpClient client, string path, Guid phase, string kind, string name, string start, int duration, int progress,
        Guid? owner = null, Guid[]? after = null, Guid? parent = null) =>
        await IdOfAsync(await SendAsync(client, HttpMethod.Post, $"{path}/items", new
        {
            kind,
            phaseId = phase,
            name,
            start,
            duration,
            ownerId = owner,
            progress,
            predecessorIds = after ?? [],
            parentId = parent,
        }));

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
