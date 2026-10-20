using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: modelos-de-projeto (API side): "Salvar como modelo", "Projeto a partir de um modelo", "Modelos na
// Administração" and "Modelos na auditoria" (ADR-0017, ADR-0043, ADR-0046). The calendar of 2026 has 12/10 as a holiday.
public sealed class TemplateTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private sealed record Scene(SessionApp App, HttpClient Admin, Guid AdminId, Guid Marina, Guid Rafael, Guid ProjectId)
    {
        public string Path => $"/api/projects/{ProjectId}/schedule";
    }

    /// <summary>ERP-FIN, managed by Marina, with Rafael as tech lead: a phase with a task of 3 business days and a milestone.</summary>
    private async Task<Scene> StartAsync()
    {
        var app = await SessionApp.StartAsync(sqlServer);
        var (admin, adminId) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        var rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.User);
        var projectId = await IdOfAsync(await SendAsync(admin, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-FIN",
            name = "Implantação do ERP financeiro",
            managerId = marina,
            techLeadId = rafael,
        }));
        var path = $"/api/projects/{projectId}/schedule";
        var phase = await IdOfAsync(await SendAsync(admin, HttpMethod.Post, $"{path}/phases", new { name = "Configuração" }));
        await SendAsync(admin, HttpMethod.Post, $"{path}/phases", new { name = "Homologação" });
        var task = await IdOfAsync(await SendAsync(admin, HttpMethod.Post, $"{path}/items", new
        {
            kind = "Task",
            phaseId = phase,
            name = "Parametrizar regras de ICMS",
            start = "2026-10-13",
            duration = 3,
            ownerId = rafael,
            progress = 50,
            predecessorIds = Array.Empty<Guid>(),
        }));
        await IdOfAsync(await SendAsync(admin, HttpMethod.Post, $"{path}/items", new
        {
            kind = "Milestone",
            phaseId = phase,
            name = "Aceite da configuração",
            start = "2026-10-15",
            duration = 0,
            progress = 0,
            predecessorIds = new[] { task },
        }));
        return new Scene(app, admin, adminId, marina, rafael, projectId);
    }

    private static Task<HttpResponseMessage> SaveAsync(HttpClient client, Scene scene, string name) =>
        SendAsync(client, HttpMethod.Post, $"{scene.Path}/template", new { name, description = "Fases de uma implantação" });

    [Fact(DisplayName = "Salvar o cronograma como modelo pela API")]
    public async Task Manager_SavesTheSchedule_AsATemplate_AndOnlyTheManagersMay()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        using var rafael = await app.SignInAsync(scene.Rafael);

        (await SaveAsync(rafael, scene, "Implantação de ERP")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SaveAsync(marina, scene, "Implantação de ERP")).StatusCode.Should().Be(HttpStatusCode.Created);

        var templates = await marina.GetFromJsonAsync<JsonElement>("/api/project-templates", Cancellation);
        var template = templates.EnumerateArray().Single();
        (template.GetProperty("name").GetString(), template.GetProperty("description").GetString())
            .Should().Be(("Implantação de ERP", "Fases de uma implantação"));
        template.GetProperty("phases").EnumerateArray().Select(phase => phase.GetProperty("name").GetString())
            .Should().Equal("Configuração", "Homologação");
        template.GetProperty("phases")[0].GetProperty("items").EnumerateArray()
            .Select(item => (item.GetProperty("name").GetString(), item.GetProperty("startOffset").GetInt32(), item.GetProperty("duration").GetInt32()))
            .Should().Equal(("Parametrizar regras de ICMS", 0, 3), ("Aceite da configuração", 2, 0));
    }

    [Fact(DisplayName = "Modelo de um projeto encerrado")]
    public async Task AClosedProject_IsSavedAsATemplate()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        await SendAsync(marina, HttpMethod.Put, $"/api/projects/{scene.ProjectId}/status", new { status = "Completed" });

        (await SaveAsync(marina, scene, "Implantação de ERP")).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact(DisplayName = "Nome de modelo repetido")]
    public async Task AnotherTemplateWithTheName_IsAConflict()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        await SaveAsync(scene.Admin, scene, "Implantação de ERP");

        var response = await SaveAsync(scene.Admin, scene, " Implantação de ERP ");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString()
            .Should().Be("urn:fiscoprojects:problem:template-name-in-use");
        (await scene.Admin.GetFromJsonAsync<JsonElement>("/api/admin/project-templates", Cancellation)).GetArrayLength().Should().Be(1);
    }

    [Fact(DisplayName = "Criar projeto a partir de um modelo pela API")]
    public async Task ANewProjectFromATemplate_HasItsScheduleInBusinessDays()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        var template = await IdOfAsync(await SaveAsync(marina, scene, "Implantação de ERP"));

        // Friday 09/10, before the holiday of Monday 12/10.
        var created = await SendAsync(marina, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-RH",
            name = "Implantação do ERP de RH",
            templateId = template,
            scheduleStart = "2026-10-09",
        });

        var projectId = await IdOfAsync(created);
        var schedule = await marina.GetFromJsonAsync<JsonElement>($"/api/projects/{projectId}/schedule", Cancellation);
        var items = schedule.GetProperty("phases").EnumerateArray().SelectMany(phase => phase.GetProperty("items").EnumerateArray()).ToList();
        items.Select(item => (item.GetProperty("name").GetString(), item.GetProperty("start").GetString(), item.GetProperty("end").GetString()))
            .Should().Equal(("Parametrizar regras de ICMS", "2026-10-09", "2026-10-14"), ("Aceite da configuração", "2026-10-14", "2026-10-14"));
        items.Should().OnlyContain(item => item.GetProperty("owner").ValueKind == JsonValueKind.Null && item.GetProperty("progress").GetInt32() == 0);
        items[1].GetProperty("predecessorIds").EnumerateArray().Single().GetGuid().Should().Be(items[0].GetProperty("id").GetGuid());
    }

    [Fact(DisplayName = "Modelo inativo na criação")]
    public async Task AnInactiveTemplate_OrNoStart_IsRefused()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        var template = await IdOfAsync(await SaveAsync(scene.Admin, scene, "Implantação de ERP"));

        var withoutStart = await SendAsync(scene.Admin, HttpMethod.Post, "/api/projects", new { code = "ERP-RH", name = "RH", templateId = template });
        (await SendAsync(scene.Admin, HttpMethod.Put, $"/api/admin/project-templates/{template}",
            new { name = "Implantação de ERP", description = (string?)null, isActive = false })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var inactive = await SendAsync(scene.Admin, HttpMethod.Post, "/api/projects",
            new { code = "ERP-RH", name = "RH", templateId = template, scheduleStart = "2026-10-09" });

        withoutStart.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorFieldsAsync(withoutStart)).Should().Equal("scheduleStart");
        inactive.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorFieldsAsync(inactive)).Should().Equal("templateId");
        (await scene.Admin.GetFromJsonAsync<JsonElement>("/api/project-templates", Cancellation)).GetArrayLength().Should().Be(0);
        (await scene.Admin.GetFromJsonAsync<JsonElement>("/api/projects?search=ERP-RH", Cancellation)).GetProperty("total").GetInt32().Should().Be(0);
    }

    [Fact(DisplayName = "Projeto em branco")]
    public async Task ABlankProject_HasNoSchedule()
    {
        var scene = await StartAsync();
        await using var app = scene.App;

        var projectId = await IdOfAsync(await SendAsync(scene.Admin, HttpMethod.Post, "/api/projects", new { code = "ERP-RH", name = "RH" }));

        (await scene.Admin.GetFromJsonAsync<JsonElement>($"/api/projects/{projectId}/schedule", Cancellation))
            .GetProperty("phases").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "Excluir um modelo")]
    public async Task RemovingATemplate_KeepsTheProjectsMadeFromIt()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        var template = await IdOfAsync(await SaveAsync(scene.Admin, scene, "Implantação de ERP"));
        var made = await IdOfAsync(await SendAsync(scene.Admin, HttpMethod.Post, "/api/projects",
            new { code = "ERP-RH", name = "RH", templateId = template, scheduleStart = "2026-10-13" }));

        (await SendAsync(scene.Admin, HttpMethod.Delete, $"/api/admin/project-templates/{template}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await scene.Admin.GetFromJsonAsync<JsonElement>("/api/admin/project-templates", Cancellation)).GetArrayLength().Should().Be(0);
        (await scene.Admin.GetFromJsonAsync<JsonElement>($"/api/projects/{made}/schedule", Cancellation))
            .GetProperty("phases").GetArrayLength().Should().Be(2);
    }

    [Fact(DisplayName = "Gerente fora da Administração dos modelos")]
    public async Task OnlyTheAdministrator_KeepsTheTemplates()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        using var rafael = await app.SignInAsync(scene.Rafael);
        var template = await IdOfAsync(await SaveAsync(marina, scene, "Implantação de ERP"));

        (await marina.GetAsync("/api/admin/project-templates", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(marina, HttpMethod.Put, $"/api/admin/project-templates/{template}", new { name = "Outro", isActive = true }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(marina, HttpMethod.Delete, $"/api/admin/project-templates/{template}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        // Who creates no project sees no model.
        (await rafael.GetAsync("/api/project-templates", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Modelo na auditoria")]
    public async Task Template_IsAudited_ByItsName_WithoutItsItems()
    {
        var scene = await StartAsync();
        await using var app = scene.App;
        using var marina = await app.SignInAsync(scene.Marina);
        var template = await IdOfAsync(await SaveAsync(marina, scene, "Implantação de ERP"));
        app.Clock.Advance(TimeSpan.FromMinutes(1));
        await SendAsync(scene.Admin, HttpMethod.Put, $"/api/admin/project-templates/{template}",
            new { name = "Implantação de ERP", description = "Fases de uma implantação", isActive = false });

        var entries = (await scene.Admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=ProjectTemplate&pageSize=100", Cancellation))
            .GetProperty("items").EnumerateArray().ToList();
        var all = (await scene.Admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation)).GetProperty("items");

        entries.Select(entry => (entry.GetProperty("action").GetString(), entry.GetProperty("itemLabel").GetString()))
            .Should().Equal(("Updated", "Implantação de ERP"), ("Created", "Implantação de ERP"));
        all.EnumerateArray().Select(entry => entry.GetProperty("itemType").GetString())
            .Should().NotContain(type => type!.StartsWith("ProjectTemplate", StringComparison.Ordinal) && type != "ProjectTemplate");
    }

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<List<string>> ErrorFieldsAsync(HttpResponseMessage response) =>
        [.. (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation))
            .GetProperty("errors").EnumerateObject().Select(field => field.Name)];

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
