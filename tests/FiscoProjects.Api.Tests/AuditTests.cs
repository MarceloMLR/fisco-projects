using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: auditoria, requirements "Registro automático das alterações", "Eventos de segurança",
// "Auditoria somente leitura", "Auditoria só do administrador" and "Tela de Auditoria" (API side).
public sealed class AuditTests(SqlServerFixture sqlServer)
{
    private const string MarinaEmail = "marina.costa@empresa.example";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>One action of the permission matrix (ADR-0046) and the answer when it is allowed.</summary>
    private sealed record PermissionRow(string Action, string Path, HttpStatusCode Allowed, SystemRole[] AllowedTo);

    // The permission matrix of the spec, one row per action (ADR-0056).
    private static readonly PermissionRow[] Matrix =
    [
        new("List the audit records", "/api/audit", HttpStatusCode.OK, [SystemRole.Administrator]),
    ];

    [Fact(DisplayName = "Administrador na auditoria")]
    public Task Administrator_CanReadTheAudit() => AssertMatrixAsync(SystemRole.Administrator);

    [Fact(DisplayName = "Gerente de projeto na auditoria")]
    public Task ProjectManager_CannotReadTheAudit() => AssertMatrixAsync(SystemRole.ProjectManager);

    [Fact(DisplayName = "Usuário na auditoria")]
    public Task User_CannotReadTheAudit() => AssertMatrixAsync(SystemRole.User);

    [Fact(DisplayName = "Alteração sem sessão")]
    public async Task UserCreatedOnTheFirstSignIn_IsRecordedBySystem()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        using var marina = app.NewClient();

        (await SignInLocallyAsync(marina, MarinaEmail)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var created = (await EntriesAsync(admin, "itemType=User"))
            .Should().ContainSingle(entry => entry.GetProperty("itemLabel").GetString() == "Marina Costa").Subject;
        created.GetProperty("action").GetString().Should().Be("Created");
        created.GetProperty("actor").ValueKind.Should().Be(JsonValueKind.Null);
        Fields(created).Should().BeEquivalentTo(
        [
            ("Email", (string?)null, (string?)MarinaEmail),
            ("IsActive", null, "true"),
            ("Name", null, "Marina Costa"),
            ("Role", null, "User"),
        ]);
    }

    [Fact(DisplayName = "Entrada seguinte sem alteração do usuário")]
    public async Task LaterSignIn_RecordsOnlyTheSignIn()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var adminId = await app.CreateUserAsync("Paulo Nogueira", SystemRole.Administrator);
        using var marina = app.NewClient();
        await SignInLocallyAsync(marina, MarinaEmail);
        app.Clock.Advance(TimeSpan.FromDays(1));

        await SignInLocallyAsync(marina, MarinaEmail);

        // A session of its own after the day went by, as the other one expired.
        using var admin = await app.SignInAsync(adminId);
        var marinaEntries = (await EntriesAsync(admin)).Where(entry => entry.GetProperty("itemLabel").GetString() == "Marina Costa"
            || (entry.GetProperty("actor").ValueKind == JsonValueKind.Object
                && entry.GetProperty("actor").GetProperty("name").GetString() == "Marina Costa"));
        marinaEntries.Select(entry => entry.GetProperty("action").GetString())
            .Should().BeEquivalentTo(["SignedIn", "SignedIn", "Created"]);
    }

    [Fact(DisplayName = "Entrada registrada")]
    public async Task SignIn_IsRecordedWithThePersonAndTheProvider()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        using var marina = app.NewClient();

        await SignInLocallyAsync(marina, MarinaEmail);

        var marinaId = (await marina.GetFromJsonAsync<JsonElement>("/api/session", Cancellation)).GetProperty("id").GetString();
        var signIn = (await EntriesAsync(admin, $"itemType=SignIn&actorId={marinaId}")).Should().ContainSingle().Subject;
        signIn.GetProperty("action").GetString().Should().Be("SignedIn");
        signIn.GetProperty("actor").GetProperty("name").GetString().Should().Be("Marina Costa");
        signIn.GetProperty("detail").GetString().Should().Be("local");
    }

    [Fact(DisplayName = "Entrada recusada registrada")]
    public async Task RefusedSignIns_AreRecordedBySystemWithTheIdentityAndTheReason()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        var marinaId = await app.CreateUserAsync("Marina Costa", MarinaEmail);
        await app.DeactivateAsync(marinaId);
        using var someone = app.NewClient();

        (await SignInLocallyAsync(someone, "joao.silva@parceiro.example")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SignInLocallyAsync(someone, MarinaEmail)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var refusals = (await EntriesAsync(admin, "itemType=SignIn"))
            .Where(entry => entry.GetProperty("action").GetString() == "SignInRefused").ToList();
        refusals.Should().AllSatisfy(entry => entry.GetProperty("actor").ValueKind.Should().Be(JsonValueKind.Null));
        refusals.Select(entry => (entry.GetProperty("itemLabel").GetString(), entry.GetProperty("detail").GetString()))
            .Should().BeEquivalentTo([("joao.silva@parceiro.example", "NotInDirectory"), (MarinaEmail, "Inactive")]);
    }

    [Fact(DisplayName = "Troca de papel registrada")]
    public async Task RoleChange_IsRecordedWithTheAdministratorAndTheRoles()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, adminId) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        var marinaId = await app.CreateUserAsync("Marina Costa", SystemRole.User);

        await SendAsync(admin, HttpMethod.Put, $"/api/users/{marinaId}/role", new { role = "ProjectManager" });

        var change = (await EntriesAsync(admin, $"itemType=User&actorId={adminId}")).Should().ContainSingle().Subject;
        change.GetProperty("action").GetString().Should().Be("Updated");
        change.GetProperty("itemId").GetString().Should().Be(marinaId.ToString());
        change.GetProperty("itemLabel").GetString().Should().Be("Marina Costa");
        change.GetProperty("actor").GetProperty("name").GetString().Should().Be("Paulo Nogueira");
        Fields(change).Should().Equal(("Role", "User", "ProjectManager"));
    }

    [Fact(DisplayName = "Desativação e reativação registradas")]
    public async Task DeactivationAndReactivation_AreRecordedWithTheAdministrator()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, adminId) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        var marinaId = await app.CreateUserAsync("Marina Costa", SystemRole.User);

        await SendAsync(admin, HttpMethod.Post, $"/api/users/{marinaId}/deactivate");
        app.Clock.Advance(TimeSpan.FromMinutes(5));
        await SendAsync(admin, HttpMethod.Post, $"/api/users/{marinaId}/reactivate");

        var changes = await EntriesAsync(admin, $"itemType=User&actorId={adminId}");
        changes.Should().HaveCount(2).And.OnlyContain(entry => entry.GetProperty("itemLabel").GetString() == "Marina Costa");
        changes.Select(entry => Fields(entry).Single()).Should().Equal(
            ("IsActive", "false", "true"),
            ("IsActive", "true", "false"));
    }

    [Fact(DisplayName = "Sem rota para alterar ou apagar")]
    public async Task AuditRecords_CannotBeChangedOrRemovedThroughTheApi()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        var before = await EntriesAsync(admin);
        var id = before.First().GetProperty("id").GetString();

        foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete })
        {
            foreach (var path in new[] { "/api/audit", $"/api/audit/{id}" })
            {
                var response = await SendAsync(admin, method, path, new { detail = "changed" });
                response.StatusCode.Should().BeOneOf([HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed], $"{method} {path}");
            }
        }

        (await EntriesAsync(admin)).Select(entry => entry.GetRawText())
            .Should().Equal(before.Select(entry => entry.GetRawText()));
    }

    [Fact(DisplayName = "Filtros e período pela API")]
    public async Task Filters_AndThePeriod_WorkThroughTheApi()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var start = app.Clock.GetUtcNow();
        var adminId = await app.CreateUserAsync("Paulo Nogueira", SystemRole.Administrator);
        app.Clock.Advance(TimeSpan.FromDays(1));
        using var admin = await app.SignInAsync(adminId);
        var marinaId = await app.CreateUserAsync("Marina Costa", SystemRole.User);
        await SendAsync(admin, HttpMethod.Post, $"/api/users/{marinaId}/deactivate");

        var firstDay = await EntriesAsync(admin, Period(start, start.AddDays(1)));
        var nextDay = await EntriesAsync(admin, Period(start.AddDays(1), start.AddDays(2)));
        var byAdmin = await EntriesAsync(admin, $"actorId={adminId}");

        firstDay.Select(Action).Should().BeEquivalentTo(["Created", "SignedIn"], "Paulo was created and signed in that day");
        nextDay.Select(Action).Should().BeEquivalentTo(["Created", "SignedIn", "Updated"], "Marina came and was deactivated the next day");
        byAdmin.Select(Action).Should().BeEquivalentTo(["SignedIn", "Updated"]);
        (await admin.GetAsync("/api/audit?page=0", Cancellation)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.GetAsync($"/api/audit?{Period(start.AddDays(1), start)}", Cancellation)).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task AssertMatrixAsync(SystemRole role)
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        await app.CreateUserAsync("Paulo Nogueira", SystemRole.Administrator);
        var (client, _) = await app.SignedInClientAsync("Marina Costa", role);
        using var _client = client;

        foreach (var row in Matrix)
        {
            var allowed = row.AllowedTo.Contains(role);
            var response = await client.GetAsync(row.Path, Cancellation);

            response.StatusCode.Should().Be(allowed ? row.Allowed : HttpStatusCode.Forbidden, $"{role}: {row.Action}");
        }
    }

    private static string Period(DateTimeOffset from, DateTimeOffset to) =>
        $"from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";

    private static string? Action(JsonElement entry) => entry.GetProperty("action").GetString();

    private static List<(string Field, string? Before, string? After)> Fields(JsonElement entry) =>
        [.. entry.GetProperty("changes").EnumerateArray().Select(field => (
            field.GetProperty("field").GetString()!,
            field.GetProperty("before").GetString(),
            field.GetProperty("after").GetString()))];

    private static async Task<List<JsonElement>> EntriesAsync(HttpClient admin, string query = "")
    {
        var page = await admin.GetFromJsonAsync<JsonElement>($"/api/audit?pageSize=100&{query}", Cancellation);
        return [.. page.GetProperty("items").EnumerateArray()];
    }

    private static Task<HttpResponseMessage> SignInLocallyAsync(HttpClient client, string email) =>
        SendAsync(client, HttpMethod.Post, "/api/session/local", new { email });

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
