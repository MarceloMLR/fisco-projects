using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: clientes, requirements "Quem cuida dos clientes", "CNPJ do cliente", "Logo do cliente", "Situação e exclusão
// do cliente" and "Mudanças dos clientes na auditoria" (API side).
public sealed class ClientTests(SqlServerFixture sqlServer) : IDisposable
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

    private readonly string _files = Path.Combine(Path.GetTempPath(), $"fiscoprojects-clients-{Guid.NewGuid():N}");

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>One action of the permission matrix (ADR-0043, ADR-0046); the id is a client the Administrator created.</summary>
    private sealed record PermissionRow(
        string Action, Func<HttpClient, Guid, Task<HttpResponseMessage>> Send, HttpStatusCode Allowed, SystemRole[] AllowedTo);

    private static readonly SystemRole[] Creators = [SystemRole.Administrator, SystemRole.ProjectManager];
    private static readonly SystemRole[] Administrator = [SystemRole.Administrator];

    // The permission matrix of the spec, one row per action (ADR-0056).
    private static readonly PermissionRow[] Matrix =
    [
        new("List the clients", (client, _) => client.GetAsync("/api/clients", Cancellation), HttpStatusCode.OK, Creators),
        new("Include a client", (client, _) => SendAsync(client, HttpMethod.Post, "/api/clients", Form("Kappa Energia")),
            HttpStatusCode.Created, Creators),
        new("Open the logo", (client, id) => client.GetAsync($"/api/clients/{id}/logo", Cancellation), HttpStatusCode.OK,
            [SystemRole.Administrator, SystemRole.ProjectManager, SystemRole.User]),
        new("Change a client", (client, id) => SendAsync(client, HttpMethod.Put, $"/api/clients/{id}",
            Form("Alfa Distribuidora S.A.", "11.222.333/0001-81")), HttpStatusCode.NoContent, Administrator),
        new("Deactivate a client", (client, id) => SendAsync(client, HttpMethod.Post, $"/api/clients/{id}/deactivate"),
            HttpStatusCode.NoContent, Administrator),
        new("Reactivate a client", (client, id) => SendAsync(client, HttpMethod.Post, $"/api/clients/{id}/reactivate"),
            HttpStatusCode.NoContent, Administrator),
        new("Send a logo", (client, id) => SendLogoAsync(client, id, Png, "alfa.png"), HttpStatusCode.NoContent, Administrator),
        new("Remove the logo", (client, id) => SendAsync(client, HttpMethod.Delete, $"/api/clients/{id}/logo"),
            HttpStatusCode.NoContent, Administrator),
        new("Remove a client", (client, id) => SendAsync(client, HttpMethod.Delete, $"/api/clients/{id}"),
            HttpStatusCode.NoContent, Administrator),
    ];

    [Fact(DisplayName = "Administrador cuida dos clientes")]
    public Task Administrator_TakesCareOfTheClients() => AssertMatrixAsync(SystemRole.Administrator);

    [Fact(DisplayName = "Gerente de projeto inclui um cliente")]
    public Task ProjectManager_ListsAndIncludes_ButDoesNotChange() => AssertMatrixAsync(SystemRole.ProjectManager);

    [Fact(DisplayName = "Usuário não cuida dos clientes")]
    public Task User_DoesNotTakeCareOfTheClients() => AssertMatrixAsync(SystemRole.User);

    [Fact(DisplayName = "Clientes sem sessão")]
    public async Task WithoutASession_NothingIsReadNorChanged()
    {
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        var id = await AddAsync(admin, Form("Alfa Distribuidora"));
        await SendLogoAsync(admin, id, Png, "alfa.png");
        using var anonymous = app.NewClient();

        foreach (var row in Matrix)
        {
            (await row.Send(anonymous, id)).StatusCode.Should().Be(HttpStatusCode.Unauthorized, row.Action);
        }

        (await ListAsync(admin)).GetProperty("total").GetInt32().Should().Be(1);
    }

    [Fact(DisplayName = "Incluir cliente")]
    public async Task Client_IsIncluded_WithTheCnpjWithoutPunctuation()
    {
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        var id = await AddAsync(admin, Form(" Alfa Distribuidora ", "11.222.333/0001-81", "Matriz em Campinas"));

        var client = (await ListAsync(admin)).GetProperty("items").EnumerateArray().Single();
        client.GetProperty("id").GetGuid().Should().Be(id);
        client.GetProperty("name").GetString().Should().Be("Alfa Distribuidora");
        client.GetProperty("cnpj").GetString().Should().Be("11222333000181");
        client.GetProperty("observation").GetString().Should().Be("Matriz em Campinas");
        client.GetProperty("status").GetString().Should().Be("Active");
        client.GetProperty("logoUrl").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "Cliente inválido")]
    public async Task InvalidClient_IsABadRequest()
    {
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        var response = await SendAsync(admin, HttpMethod.Post, "/api/clients", Form(" ", "11.222.333/0001-80", new string('a', 501)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        problem.GetProperty("errors").EnumerateObject().Select(field => field.Name)
            .Should().BeEquivalentTo("name", "cnpj", "observation");
    }

    [Fact(DisplayName = "CNPJ repetido")]
    public async Task RepeatedCnpj_IsAConflict_WithTheNameOfTheClient()
    {
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        var alfa = await AddAsync(admin, Form("Alfa Distribuidora", "11.222.333/0001-81"));
        await SendAsync(admin, HttpMethod.Post, $"/api/clients/{alfa}/deactivate");

        var response = await SendAsync(admin, HttpMethod.Post, "/api/clients", Form("Alfa Nova", "11222333000181"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        problem.GetProperty("type").GetString().Should().Be("urn:fiscoprojects:problem:client-cnpj-in-use");
        problem.GetProperty("clientName").GetString().Should().Be("Alfa Distribuidora");
    }

    [Fact(DisplayName = "Cliente que não existe")]
    public async Task MissingClient_IsNotFound()
    {
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        var id = Guid.CreateVersion7();

        (await SendAsync(admin, HttpMethod.Put, $"/api/clients/{id}", Form("Alfa"))).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(admin, HttpMethod.Delete, $"/api/clients/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendLogoAsync(admin, id, Png, "alfa.png")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.GetAsync($"/api/clients/{id}/logo", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Logo do cliente recusado")]
    public async Task RefusedLogo_SaysWhy()
    {
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        var id = await AddAsync(admin, Form("Alfa Distribuidora"));
        var large = new byte[(1024 * 1024) + 1];
        Png.CopyTo(large, 0);

        var svg = await SendLogoAsync(admin, id, "<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8.ToArray(), "alfa.svg");
        var tooLarge = await SendLogoAsync(admin, id, large, "grande.png");

        svg.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await svg.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString()
            .Should().Be("urn:fiscoprojects:problem:client-logo-format");
        (await tooLarge.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString()
            .Should().Be("urn:fiscoprojects:problem:client-logo-too-large");
    }

    [Fact(DisplayName = "Logo do cliente sem scripts")]
    public async Task Logo_IsServedSoThatItNeverRunsScripts()
    {
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        var (marina, _) = await app.SignedInClientAsync("Marina Costa", SystemRole.User);
        using var _marina = marina;
        var id = await AddAsync(admin, Form("Alfa Distribuidora"));
        await SendLogoAsync(admin, id, Png, "alfa.png");
        var logoUrl = (await ListAsync(admin)).GetProperty("items")[0].GetProperty("logoUrl").GetString()!;

        var response = await marina.GetAsync(logoUrl, Cancellation);
        var unversioned = await marina.GetAsync($"/api/clients/{id}/logo", Cancellation);

        logoUrl.Should().StartWith($"/api/clients/{id}/logo?v=");
        (await response.Content.ReadAsByteArrayAsync(Cancellation)).Should().Equal(Png);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("sandbox");
        response.Headers.CacheControl!.ToString().Should().Contain("immutable");
        response.Headers.Contains("Set-Cookie").Should().BeFalse();
        unversioned.Headers.CacheControl!.NoCache.Should().BeTrue();
    }

    [Fact(DisplayName = "Buscar e filtrar clientes")]
    public async Task Search_AndStatus_FilterTheList()
    {
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        await AddAsync(admin, Form("Alfa Distribuidora", "11.222.333/0001-81"));
        var epsilon = await AddAsync(admin, Form("Épsilon Logística"));
        await SendAsync(admin, HttpMethod.Post, $"/api/clients/{epsilon}/deactivate");

        (await NamesAsync(admin, "?search=epsilon")).Should().Equal("Épsilon Logística");
        (await NamesAsync(admin, "?search=222.333%2F0001")).Should().Equal("Alfa Distribuidora");
        (await NamesAsync(admin, "?status=Inactive")).Should().Equal("Épsilon Logística");
        (await NamesAsync(admin, "?status=Active")).Should().Equal("Alfa Distribuidora");
    }

    [Fact(DisplayName = "Cliente incluído na auditoria")]
    public async Task ClientIncludedByAProjectManager_IsAudited()
    {
        await using var app = await StartAsync();
        var (manager, managerId) = await app.SignedInClientAsync("Marina Costa", SystemRole.ProjectManager);
        using var _manager = manager;
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        await AddAsync(manager, Form("Kappa Energia", "44.555.666/0001-81"));

        var entry = (await AuditAsync(admin)).Should().ContainSingle().Subject;
        entry.GetProperty("action").GetString().Should().Be("Created");
        entry.GetProperty("actor").GetProperty("id").GetString().Should().Be(managerId.ToString());
        entry.GetProperty("itemLabel").GetString().Should().Be("Kappa Energia");
        Fields(entry).Should().Contain(("Cnpj", null, "44555666000181"));
    }

    [Fact(DisplayName = "Cliente alterado na auditoria")]
    public async Task ChangesOfTheCnpjAndTheLogo_AreAudited()
    {
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        var id = await AddAsync(admin, Form("Alfa Distribuidora"));
        app.Clock.Advance(TimeSpan.FromMinutes(1));

        await SendAsync(admin, HttpMethod.Put, $"/api/clients/{id}", Form("Alfa Distribuidora", "12.abc.345/01de-35"));
        app.Clock.Advance(TimeSpan.FromMinutes(1));
        await SendLogoAsync(admin, id, Png, "alfa.png");
        app.Clock.Advance(TimeSpan.FromMinutes(1));
        await SendAsync(admin, HttpMethod.Post, $"/api/clients/{id}/deactivate");

        var entries = await AuditAsync(admin);
        entries.Select(entry => entry.GetProperty("action").GetString()).Should().Equal("Updated", "Updated", "Updated", "Created");
        Fields(entries[2]).Should().Equal(("Cnpj", null, "12ABC34501DE35"));
        Fields(entries[1]).Should().Equal(("LogoFileName", null, "alfa.png"));
        Fields(entries[0]).Should().Equal(("IsActive", "true", "false"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_files))
        {
            Directory.Delete(_files, recursive: true);
        }
    }

    private Task<SessionApp> StartAsync() => SessionApp.StartAsync(sqlServer, builder =>
    {
        builder.Configuration["FileStorage:Provider"] = "Local";
        builder.Configuration["FileStorage:Local:RootPath"] = _files;
    });

    private async Task AssertMatrixAsync(SystemRole role)
    {
        await using var app = await StartAsync();
        var adminId = await app.CreateUserAsync("Paulo Nogueira", SystemRole.Administrator);
        using var admin = await app.SignInAsync(adminId);
        var id = await AddAsync(admin, Form("Alfa Distribuidora"));
        await SendLogoAsync(admin, id, Png, "alfa.png");
        using var client = role == SystemRole.Administrator ? admin : (await app.SignedInClientAsync("Marina Costa", role)).Client;

        foreach (var row in Matrix)
        {
            var response = await row.Send(client, id);

            response.StatusCode.Should().Be(row.AllowedTo.Contains(role) ? row.Allowed : HttpStatusCode.Forbidden, $"{role}: {row.Action}");
        }

        (await NamesAsync(admin, string.Empty)).Should().Equal(role switch
        {
            SystemRole.Administrator => ["Kappa Energia"],
            SystemRole.ProjectManager => ["Alfa Distribuidora", "Kappa Energia"],
            _ => ["Alfa Distribuidora"],
        });
    }

    private static object Form(string name, string? cnpj = null, string? observation = null) => new { name, cnpj, observation };

    private static async Task<Guid> AddAsync(HttpClient client, object form)
    {
        var response = await SendAsync(client, HttpMethod.Post, "/api/clients", form);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static Task<JsonElement> ListAsync(HttpClient client, string query = "") =>
        client.GetFromJsonAsync<JsonElement>($"/api/clients{query}", Cancellation);

    private static async Task<List<string?>> NamesAsync(HttpClient client, string query) =>
        [.. (await ListAsync(client, query)).GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString())];

    private static async Task<List<JsonElement>> AuditAsync(HttpClient client)
    {
        var page = await client.GetFromJsonAsync<JsonElement>("/api/audit?itemType=Client&pageSize=100", Cancellation);
        return [.. page.GetProperty("items").EnumerateArray()];
    }

    private static List<(string Field, string? Before, string? After)> Fields(JsonElement entry) =>
        [.. entry.GetProperty("changes").EnumerateArray().Select(field => (
            field.GetProperty("field").GetString()!,
            field.GetProperty("before").GetString(),
            field.GetProperty("after").GetString()))];

    private static async Task<HttpResponseMessage> SendLogoAsync(HttpClient client, Guid id, byte[] content, string fileName)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/clients/{id}/logo");
        request.Headers.Add("X-CSRF", "1");
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        request.Content = new MultipartFormDataContent { { file, "file", fileName } };
        return await client.SendAsync(request, Cancellation);
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
