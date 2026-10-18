using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: usuarios-e-papeis, requirements "Administração só do administrador", "Papel no sistema", "Desativar e reativar", "Último administrador ativo" and "Tela Usuários" (API side).
public sealed class UserAdministrationTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>One action of the permission matrix (ADR-0046) and the answer when it is allowed.</summary>
    private sealed record PermissionRow(
        string Action, Func<HttpClient, Guid, Task<HttpResponseMessage>> Call, HttpStatusCode Allowed, SystemRole[] AllowedTo);

    // The permission matrix of the spec, one row per action (ADR-0056).
    private static readonly PermissionRow[] Matrix =
    [
        new("List the users", (client, _) => client.GetAsync("/api/users", Cancellation),
            HttpStatusCode.OK, [SystemRole.Administrator]),
        new("Change the role of a user", (client, target) => SendAsync(client, HttpMethod.Put, $"/api/users/{target}/role", new { role = "ProjectManager" }),
            HttpStatusCode.NoContent, [SystemRole.Administrator]),
        new("Deactivate a user", (client, target) => SendAsync(client, HttpMethod.Post, $"/api/users/{target}/deactivate"),
            HttpStatusCode.NoContent, [SystemRole.Administrator]),
        new("Reactivate a user", (client, target) => SendAsync(client, HttpMethod.Post, $"/api/users/{target}/reactivate"),
            HttpStatusCode.NoContent, [SystemRole.Administrator]),
    ];

    [Fact(DisplayName = "Administrador na administração de usuários")]
    public Task Administrator_CanAdministerUsers() => AssertMatrixAsync(SystemRole.Administrator);

    [Fact(DisplayName = "Gerente de projeto na administração de usuários")]
    public Task ProjectManager_CannotAdministerUsers() => AssertMatrixAsync(SystemRole.ProjectManager);

    [Fact(DisplayName = "Usuário na administração de usuários")]
    public Task User_CannotAdministerUsers() => AssertMatrixAsync(SystemRole.User);

    [Fact(DisplayName = "Troca de papel na sessão aberta")]
    public async Task RoleChange_AppliesOnTheNextRequest()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var (marina, marinaId) = await app.SignedInClientAsync("Marina Costa", SystemRole.User);
        using var _admin = admin;
        using var _marina = marina;
        var before = await marina.GetAsync("/api/users", Cancellation);

        var change = await SendAsync(admin, HttpMethod.Put, $"/api/users/{marinaId}/role", new { role = "Administrator" });
        var after = await marina.GetAsync("/api/users", Cancellation);
        var session = await marina.GetFromJsonAsync<JsonElement>("/api/session", Cancellation);

        before.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        change.StatusCode.Should().Be(HttpStatusCode.NoContent);
        after.StatusCode.Should().Be(HttpStatusCode.OK);
        session.GetProperty("role").GetString().Should().Be("Administrator");
    }

    [Fact(DisplayName = "Desativar acesso")]
    public async Task Deactivating_BlocksTheOpenSessionAndTheNextSignIn()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var (marina, marinaId) = await app.SignedInClientAsync("Marina Costa", SystemRole.User);
        using var _admin = admin;
        using var _marina = marina;

        var deactivate = await SendAsync(admin, HttpMethod.Post, $"/api/users/{marinaId}/deactivate");
        var openSession = await marina.GetAsync("/api/session", Cancellation);
        using var newClient = app.NewClient();
        var newSignIn = await SendAsync(newClient, HttpMethod.Post, "/api/session/local", new { email = "marina.costa@empresa.example" });

        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);
        openSession.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        newSignIn.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ProblemTypeAsync(newSignIn)).Should().Be("urn:fiscoprojects:problem:user-inactive");
    }

    [Fact(DisplayName = "Reativar acesso")]
    public async Task Reactivating_LetsThePersonSignInAgainWithTheSameRole()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        var marinaId = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        await SendAsync(admin, HttpMethod.Post, $"/api/users/{marinaId}/deactivate");

        var reactivate = await SendAsync(admin, HttpMethod.Post, $"/api/users/{marinaId}/reactivate");
        using var marina = app.NewClient();
        var signIn = await SendAsync(marina, HttpMethod.Post, "/api/session/local", new { email = "marina.costa@empresa.example" });
        var session = await marina.GetFromJsonAsync<JsonElement>("/api/session", Cancellation);

        reactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);
        signIn.StatusCode.Should().Be(HttpStatusCode.NoContent);
        session.GetProperty("role").GetString().Should().Be("ProjectManager");
    }

    [Fact(DisplayName = "Último administrador pela API")]
    public async Task LastAdministrator_IsRefusedWithConflict()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, adminId) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        var demote = await SendAsync(admin, HttpMethod.Put, $"/api/users/{adminId}/role", new { role = "User" });
        var deactivate = await SendAsync(admin, HttpMethod.Post, $"/api/users/{adminId}/deactivate");

        foreach (var response in new[] { demote, deactivate })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            response.ShouldHaveMediaType(ResponseAssertions.ProblemJson);
            (await ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:last-administrator");
        }

        (await app.StateOfAsync(adminId)).Should().Be((SystemRole.Administrator, true));
    }

    [Fact(DisplayName = "Usuário inexistente pela API")]
    public async Task UnknownUser_IsNotFound()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        var unknown = Guid.CreateVersion7();

        var responses = new[]
        {
            await SendAsync(admin, HttpMethod.Put, $"/api/users/{unknown}/role", new { role = "User" }),
            await SendAsync(admin, HttpMethod.Post, $"/api/users/{unknown}/deactivate"),
            await SendAsync(admin, HttpMethod.Post, $"/api/users/{unknown}/reactivate"),
        };

        responses.Select(response => response.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Papel desconhecido recusado pela API")]
    public async Task UnknownRole_IsABadRequest()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        var marinaId = await app.CreateUserAsync("Marina Costa", SystemRole.User);

        var byName = await SendAsync(admin, HttpMethod.Put, $"/api/users/{marinaId}/role", new { role = "Owner" });
        var byNumber = await SendAsync(admin, HttpMethod.Put, $"/api/users/{marinaId}/role", new { role = 3 });

        byName.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        byNumber.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await app.StateOfAsync(marinaId)).Should().Be((SystemRole.User, true));
    }

    [Fact(DisplayName = "Lista de usuários pela API")]
    public async Task List_IsAlphabeticalWithTotalAndPages()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        await CreatePeopleAsync(app);

        var all = await admin.GetFromJsonAsync<JsonElement>("/api/users", Cancellation);
        var secondPage = await admin.GetFromJsonAsync<JsonElement>("/api/users?page=2&pageSize=2", Cancellation);
        var tooBig = await admin.GetAsync("/api/users?pageSize=101", Cancellation);

        Names(all).Should().Equal("Beatriz Lima", "Lívia Prado", "Marina Costa", "Paulo Nogueira", "Rafael Souza");
        all.GetProperty("total").GetInt32().Should().Be(5);
        var marina = all.GetProperty("items")[2];
        marina.GetProperty("email").GetString().Should().Be("marina.costa@empresa.example");
        marina.GetProperty("role").GetString().Should().Be("ProjectManager");
        marina.GetProperty("status").GetString().Should().Be("Active");
        marina.GetProperty("lastSignInAt").GetDateTime().Should().Be(app.Clock.GetUtcNow().UtcDateTime);
        Names(secondPage).Should().Equal("Marina Costa", "Paulo Nogueira");
        secondPage.GetProperty("total").GetInt32().Should().Be(5);
        tooBig.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Busca e filtros pela API")]
    public async Task SearchAndFilters_NarrowTheList()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        await CreatePeopleAsync(app);

        var withoutAccent = await admin.GetFromJsonAsync<JsonElement>("/api/users?search=LIVIA", Cancellation);
        var byEmail = await admin.GetFromJsonAsync<JsonElement>("/api/users?search=rafael.sou", Cancellation);
        var managers = await admin.GetFromJsonAsync<JsonElement>("/api/users?role=ProjectManager", Cancellation);
        var inactive = await admin.GetFromJsonAsync<JsonElement>("/api/users?status=Inactive", Cancellation);
        var activeUsers = await admin.GetFromJsonAsync<JsonElement>("/api/users?role=User&status=Active", Cancellation);

        Names(withoutAccent).Should().Equal("Lívia Prado");
        Names(byEmail).Should().Equal("Rafael Souza");
        Names(managers).Should().Equal("Beatriz Lima", "Marina Costa");
        Names(inactive).Should().Equal("Lívia Prado");
        Names(activeUsers).Should().Equal("Rafael Souza");
    }

    private static async Task CreatePeopleAsync(SessionApp app)
    {
        await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        await app.CreateUserAsync("Beatriz Lima", SystemRole.ProjectManager);
        await app.CreateUserAsync("Rafael Souza", SystemRole.User);
        await app.DeactivateAsync(await app.CreateUserAsync("Lívia Prado", SystemRole.User));
    }

    private async Task AssertMatrixAsync(SystemRole role)
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        await app.CreateUserAsync("Paulo Nogueira", SystemRole.Administrator);
        var (client, _) = await app.SignedInClientAsync("Marina Costa", role);
        using var _client = client;
        var target = await app.CreateUserAsync("Rafael Souza", SystemRole.User);

        foreach (var row in Matrix)
        {
            var allowed = row.AllowedTo.Contains(role);
            var response = await row.Call(client, target);

            response.StatusCode.Should().Be(allowed ? row.Allowed : HttpStatusCode.Forbidden, $"{role}: {row.Action}");
            if (!allowed)
            {
                response.ShouldHaveMediaType(ResponseAssertions.ProblemJson);
                (await app.StateOfAsync(target)).Should().Be((SystemRole.User, true), $"{role}: {row.Action} changes nothing");
            }
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

    private static async Task<string?> ProblemTypeAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation));
        return problem.RootElement.GetProperty("type").GetString();
    }

    private static string?[] Names(JsonElement page) =>
        [.. page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString())];
}
