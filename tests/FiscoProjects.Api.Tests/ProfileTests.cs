using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: perfil, requirements "Tema no perfil" and "Tela Meu perfil" (API side).
public sealed class ProfileTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Tema padrão automático")]
    public async Task NewPerson_FollowsTheOperatingSystem()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (client, _) = await app.SignedInClientAsync();
        using var _client = client;

        var session = await client.GetFromJsonAsync<JsonElement>("/api/session", Cancellation);

        session.GetProperty("theme").GetString().Should().Be("Automatic");
    }

    [Fact(DisplayName = "Escolher o tema")]
    public async Task ChosenTheme_IsKeptInTheProfile()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (client, userId) = await app.SignedInClientAsync();
        using var _client = client;

        var choose = await SendAsync(client, HttpMethod.Put, "/api/profile/theme", new { theme = "Dark" });

        choose.StatusCode.Should().Be(HttpStatusCode.NoContent);
        // Another browser of the same person: a new session gets the theme from the profile.
        using var other = await app.SignInAsync(userId);
        var session = await other.GetFromJsonAsync<JsonElement>("/api/session", Cancellation);
        session.GetProperty("theme").GetString().Should().Be("Dark");
    }

    [Fact(DisplayName = "Dados da conta")]
    public async Task Profile_HasWhoThePersonIsAndHowTheySignIn()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (client, userId) = await app.SignedInClientAsync("Marina Costa", SystemRole.ProjectManager);
        using var _client = client;
        await SendAsync(client, HttpMethod.Put, "/api/profile/theme", new { theme = "Light" });

        var profile = await client.GetFromJsonAsync<JsonElement>("/api/profile", Cancellation);

        profile.GetProperty("id").GetString().Should().Be(userId.ToString());
        profile.GetProperty("name").GetString().Should().Be("Marina Costa");
        profile.GetProperty("email").GetString().Should().Be("marina.costa@empresa.example");
        profile.GetProperty("role").GetString().Should().Be("ProjectManager");
        profile.GetProperty("theme").GetString().Should().Be("Light");
        profile.GetProperty("lastSignInAt").GetDateTime().Should().Be(new DateTime(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc));
        profile.GetProperty("signInMethods").EnumerateArray().Select(method => method.GetString()).Should().Equal("local");
    }

    [Fact(DisplayName = "Tema fora da auditoria")]
    public async Task ChoosingTheTheme_IsNoAuditRecord()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, adminId) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        var choose = await SendAsync(admin, HttpMethod.Put, "/api/profile/theme", new { theme = "Dark" });

        choose.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var page = await admin.GetFromJsonAsync<JsonElement>($"/api/audit?itemType=User&pageSize=100", Cancellation);
        page.GetProperty("items").EnumerateArray()
            .Where(entry => entry.GetProperty("itemId").GetString() == adminId.ToString()
                && entry.GetProperty("action").GetString() == "Updated")
            .Should().BeEmpty();
    }

    [Fact(DisplayName = "Troca de tema sem sessão")]
    public async Task WithoutASession_NothingChanges()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        using var anonymous = app.NewClient();

        var choose = await SendAsync(anonymous, HttpMethod.Put, "/api/profile/theme", new { theme = "Dark" });
        var profile = await anonymous.GetAsync("/api/profile", Cancellation);

        choose.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        profile.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Tema desconhecido recusado pela API")]
    public async Task UnknownTheme_IsABadRequest()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (client, _) = await app.SignedInClientAsync();
        using var _client = client;

        var byName = await SendAsync(client, HttpMethod.Put, "/api/profile/theme", new { theme = "Sepia" });
        var byNumber = await SendAsync(client, HttpMethod.Put, "/api/profile/theme", new { theme = 7 });
        var session = await client.GetFromJsonAsync<JsonElement>("/api/session", Cancellation);

        byName.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        byNumber.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        session.GetProperty("theme").GetString().Should().Be("Automatic");
    }

    [Fact(DisplayName = "Troca de tema sem o cabeçalho anti-CSRF")]
    public async Task WithoutTheCsrfHeader_TheThemeStays()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (client, _) = await app.SignedInClientAsync();
        using var _client = client;

        var choose = await client.PutAsJsonAsync("/api/profile/theme", new { theme = "Dark" }, Cancellation);
        var session = await client.GetFromJsonAsync<JsonElement>("/api/session", Cancellation);

        choose.IsSuccessStatusCode.Should().BeFalse();
        session.GetProperty("theme").GetString().Should().Be("Automatic");
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
