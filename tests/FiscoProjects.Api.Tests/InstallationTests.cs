using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: configuracao-da-instalacao, requirements "Configurações só do administrador", "Nome da instalação",
// "Logo da instalação", "Marca visível sem sessão" and "Mudança das configurações na auditoria" (API side).
public sealed class InstallationTests(SqlServerFixture sqlServer) : IDisposable
{
    private const string NewName = "Consultoria Fictícia";

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

    private readonly string _files = Path.Combine(Path.GetTempPath(), $"fiscoprojects-installation-{Guid.NewGuid():N}");

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>One action of the permission matrix (ADR-0046) and the answer when it is allowed.</summary>
    private sealed record PermissionRow(
        string Action, Func<HttpClient, Task<HttpResponseMessage>> Send, HttpStatusCode Allowed, SystemRole[] AllowedTo);

    // The permission matrix of the spec, one row per action (ADR-0056). Reading the brand is open to everyone.
    private static readonly PermissionRow[] Matrix =
    [
        new("Read the brand", client => client.GetAsync("/api/installation", Cancellation), HttpStatusCode.OK,
            [SystemRole.Administrator, SystemRole.ProjectManager, SystemRole.User]),
        new("Change the name", client => SendAsync(client, HttpMethod.Put, "/api/installation", new { name = NewName }),
            HttpStatusCode.NoContent, [SystemRole.Administrator]),
        new("Send the logo", client => SendLogoAsync(client, Png, "marca.png"), HttpStatusCode.NoContent,
            [SystemRole.Administrator]),
        new("Remove the logo", client => SendAsync(client, HttpMethod.Delete, "/api/installation/logo"),
            HttpStatusCode.NoContent, [SystemRole.Administrator]),
    ];

    [Fact(DisplayName = "Administrador altera as configurações")]
    public Task Administrator_ChangesTheSettings() => AssertMatrixAsync(SystemRole.Administrator);

    [Fact(DisplayName = "Gerente de projeto não altera as configurações")]
    public Task ProjectManager_DoesNotChangeTheSettings() => AssertMatrixAsync(SystemRole.ProjectManager);

    [Fact(DisplayName = "Usuário não altera as configurações")]
    public Task User_DoesNotChangeTheSettings() => AssertMatrixAsync(SystemRole.User);

    [Fact(DisplayName = "Alteração sem sessão")]
    public async Task WithoutASession_NothingChanges()
    {
        await using var app = await StartAsync();
        using var anonymous = app.NewClient();

        var rename = await SendAsync(anonymous, HttpMethod.Put, "/api/installation", new { name = NewName });
        var logo = await SendLogoAsync(anonymous, Png, "marca.png");
        var remove = await SendAsync(anonymous, HttpMethod.Delete, "/api/installation/logo");

        rename.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        logo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        remove.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await BrandAsync(anonymous)).Should().Be(("FiscoProjects", null));
    }

    [Fact(DisplayName = "Nome inválido")]
    public async Task InvalidName_IsABadRequest()
    {
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        foreach (var name in new[] { "", "   ", new string('a', 61) })
        {
            var response = await SendAsync(admin, HttpMethod.Put, "/api/installation", new { name });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
            problem.GetProperty("errors").TryGetProperty("name", out _).Should().BeTrue();
        }

        (await BrandAsync(admin)).Name.Should().Be("FiscoProjects");
    }

    [Fact(DisplayName = "Marca na página de entrada")]
    public async Task WithoutASession_TheNameAndTheLogoAreRead()
    {
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        using var anonymous = app.NewClient();
        (await BrandAsync(anonymous)).Should().Be(("FiscoProjects", null));

        await SendAsync(admin, HttpMethod.Put, "/api/installation", new { name = NewName });
        await SendLogoAsync(admin, Png, "marca.png");

        var brand = await anonymous.GetFromJsonAsync<JsonElement>("/api/installation", Cancellation);
        brand.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo("name", "logoUrl");
        brand.GetProperty("name").GetString().Should().Be(NewName);
        var logoUrl = brand.GetProperty("logoUrl").GetString();
        logoUrl.Should().StartWith("/api/installation/logo?v=");
        (await anonymous.GetByteArrayAsync(logoUrl, Cancellation)).Should().Equal(Png);
    }

    [Fact(DisplayName = "Enviar e remover o logo pela API")]
    public async Task Logo_IsSent_ThenRemoved()
    {
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        (await SendLogoAsync(admin, Png, "marca.png")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var first = (await BrandAsync(admin)).LogoUrl;
        app.Clock.Advance(TimeSpan.FromMinutes(1));
        var svg = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect width=\"4\" height=\"4\"/></svg>");
        (await SendLogoAsync(admin, svg, "marca.svg")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var second = (await BrandAsync(admin)).LogoUrl;

        second.Should().NotBe(first);
        (await admin.GetByteArrayAsync(second, Cancellation)).Should().Equal(svg);

        (await SendAsync(admin, HttpMethod.Delete, "/api/installation/logo")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await BrandAsync(admin)).LogoUrl.Should().BeNull();
        (await admin.GetAsync("/api/installation/logo", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Logo recusado")]
    public async Task RefusedLogo_SaysWhy_AndKeepsThePreviousOne()
    {
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        await SendLogoAsync(admin, Png, "marca.png");
        var before = await BrandAsync(admin);
        var large = new byte[(1024 * 1024) + 1];
        Png.CopyTo(large, 0);
        var unsafeSvg = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>");

        var refusals = new[]
        {
            (await SendLogoAsync(admin, [0xFF, 0xD8, 0xFF, 0xE0], "foto.jpg"), "urn:fiscoprojects:problem:logo-format"),
            (await SendLogoAsync(admin, large, "grande.png"), "urn:fiscoprojects:problem:logo-too-large"),
            (await SendLogoAsync(admin, unsafeSvg, "marca.svg"), "urn:fiscoprojects:problem:logo-unsafe"),
        };

        foreach (var (response, type) in refusals)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString().Should().Be(type);
        }

        (await BrandAsync(admin)).Should().Be(before);
    }

    [Fact(DisplayName = "Logo sem scripts")]
    public async Task Logo_IsServedSoThatItNeverRunsScripts()
    {
        await using var app = await StartAsync();
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        await SendLogoAsync(admin, Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"/>"), "marca.svg");
        using var anonymous = app.NewClient();
        var current = (await BrandAsync(anonymous)).LogoUrl!;

        var response = await anonymous.GetAsync(current, Cancellation);
        var unversioned = await anonymous.GetAsync("/api/installation/logo", Cancellation);

        response.Content.Headers.ContentType!.MediaType.Should().Be("image/svg+xml");
        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("sandbox").And.Contain("default-src 'none'");
        response.Headers.CacheControl!.ToString().Should().Contain("immutable");
        unversioned.Headers.CacheControl!.NoCache.Should().BeTrue();
    }

    [Fact(DisplayName = "Troca do nome na auditoria")]
    public async Task ChangesOfTheSettings_AreAudited()
    {
        await using var app = await StartAsync();
        var (admin, adminId) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        await SendAsync(admin, HttpMethod.Put, "/api/installation", new { name = NewName });
        app.Clock.Advance(TimeSpan.FromMinutes(1));
        await SendLogoAsync(admin, Png, "marca.png");

        var page = await admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=InstallationSettings&pageSize=100", Cancellation);
        var entries = page.GetProperty("items").EnumerateArray().ToList();
        entries.Should().HaveCount(2);
        entries.Should().AllSatisfy(entry =>
        {
            entry.GetProperty("action").GetString().Should().Be("Updated");
            entry.GetProperty("actor").GetProperty("id").GetString().Should().Be(adminId.ToString());
        });
        Fields(entries[1]).Should().Equal(("Name", "FiscoProjects", NewName));
        Fields(entries[0]).Should().Equal(("LogoFileName", null, "marca.png"));
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
        await app.CreateUserAsync("Paulo Nogueira", SystemRole.Administrator);
        var (client, _) = await app.SignedInClientAsync("Marina Costa", role);
        using var _client = client;

        foreach (var row in Matrix)
        {
            var allowed = row.AllowedTo.Contains(role);
            var response = await row.Send(client);

            response.StatusCode.Should().Be(allowed ? row.Allowed : HttpStatusCode.Forbidden, $"{role}: {row.Action}");
        }

        (await BrandAsync(client)).Name.Should().Be(role == SystemRole.Administrator ? NewName : "FiscoProjects");
    }

    private static async Task<(string Name, string? LogoUrl)> BrandAsync(HttpClient client)
    {
        var brand = await client.GetFromJsonAsync<JsonElement>("/api/installation", Cancellation);
        return (brand.GetProperty("name").GetString()!, brand.GetProperty("logoUrl").GetString());
    }

    private static List<(string Field, string? Before, string? After)> Fields(JsonElement entry) =>
        [.. entry.GetProperty("changes").EnumerateArray().Select(field => (
            field.GetProperty("field").GetString()!,
            field.GetProperty("before").GetString(),
            field.GetProperty("after").GetString()))];

    private static async Task<HttpResponseMessage> SendLogoAsync(HttpClient client, byte[] content, string fileName)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/installation/logo");
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
