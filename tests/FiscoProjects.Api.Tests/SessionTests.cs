using System.Net;
using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: entrada-e-sessao, requirement "Sessão protegida".
public sealed class SessionTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Cookie da sessão")]
    public async Task OpeningASession_SetsOnlyAProtectedCookie()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var userId = await app.CreateUserAsync("Marina Costa", "marina.costa@empresa.example");
        using var client = app.NewClient();

        var response = await client.PostAsync($"/test/sign-in/{userId}", null, Cancellation);

        var cookie = response.Headers.GetValues("Set-Cookie").Should().ContainSingle().Subject;
        cookie.Should().StartWith($"{SessionApp.SessionCookie}=");
        var attributes = cookie.Split(';', StringSplitOptions.TrimEntries).Skip(1).Select(a => a.ToLowerInvariant());
        attributes.Should().Contain(["httponly", "secure", "samesite=lax", "path=/"])
            .And.NotContain(attribute => attribute.StartsWith("domain=", StringComparison.Ordinal));
        (await response.Content.ReadAsStringAsync(Cancellation)).Should().BeEmpty();
    }

    [Fact(DisplayName = "API sem sessão")]
    public async Task ApiWithoutSession_ReturnsUnauthorizedProblemDetails()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        using var client = app.NewClient();

        var response = await client.GetAsync(SessionApp.ProtectedRoute, Cancellation);

        await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Sessão aberta usa a API")]
    public async Task ApiWithSession_Answers()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (client, _) = await app.SignedInClientAsync();
        using var _client = client;

        var response = await client.GetAsync(SessionApp.ProtectedRoute, Cancellation);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Sessão parada por 8 horas")]
    public async Task SessionIdleForEightHours_IsRejected()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (client, _) = await app.SignedInClientAsync();
        using var _client = client;

        app.Clock.Advance(TimeSpan.FromHours(8) + TimeSpan.FromMinutes(1));
        var response = await client.GetAsync(SessionApp.ProtectedRoute, Cancellation);

        await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Uso renova a sessão")]
    public async Task UsingTheSession_RenewsItForAnotherEightHours()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (client, _) = await app.SignedInClientAsync();
        using var _client = client;

        // Early in the session, before half of the timeout, when the default would not renew it yet.
        app.Clock.Advance(TimeSpan.FromHours(1));
        var used = await client.GetAsync(SessionApp.ProtectedRoute, Cancellation);
        app.Clock.Advance(TimeSpan.FromHours(7) + TimeSpan.FromMinutes(30));
        var beforeEightHoursFromTheUse = await client.GetAsync(SessionApp.ProtectedRoute, Cancellation);

        used.StatusCode.Should().Be(HttpStatusCode.OK);
        used.Headers.GetValues("Set-Cookie").Should().ContainSingle(cookie => cookie.StartsWith(SessionApp.SessionCookie));
        beforeEightHoursFromTheUse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Tempo sem uso configurável")]
    public async Task IdleTimeout_ComesFromConfiguration()
    {
        await using var app = await SessionApp.StartAsync(sqlServer,
            builder => builder.Configuration["Session:IdleTimeout"] = "00:30:00");
        var (client, _) = await app.SignedInClientAsync();
        using var _client = client;

        app.Clock.Advance(TimeSpan.FromMinutes(31));
        var response = await client.GetAsync(SessionApp.ProtectedRoute, Cancellation);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Usuário desativado durante a sessão")]
    public async Task DeactivatedUser_LosesTheSession()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (client, userId) = await app.SignedInClientAsync();
        using var _client = client;
        (await client.GetAsync(SessionApp.ProtectedRoute, Cancellation)).StatusCode.Should().Be(HttpStatusCode.OK);

        await app.DeactivateAsync(userId);
        var response = await client.GetAsync(SessionApp.ProtectedRoute, Cancellation);

        await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Sessão de usuário inexistente")]
    public async Task SessionOfUnknownUser_IsRejected()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        using var client = app.NewClient();
        await client.PostAsync($"/test/sign-in/{Guid.CreateVersion7()}", null, Cancellation);

        var response = await client.GetAsync(SessionApp.ProtectedRoute, Cancellation);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory(DisplayName = "Alteração sem proteção contra requisição forjada")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task ChangeWithoutCsrfHeader_IsForbiddenAndChangesNothing(string method)
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (client, _) = await app.SignedInClientAsync();
        using var _client = client;

        var withoutHeader = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), SessionApp.ProtectedRoute), Cancellation);
        var changesWithoutHeader = app.Changes;
        var withHeader = new HttpRequestMessage(new HttpMethod(method), SessionApp.ProtectedRoute);
        withHeader.Headers.Add("X-CSRF", "1");
        var protectedResponse = await client.SendAsync(withHeader, Cancellation);

        await ShouldBeProblemAsync(withoutHeader, HttpStatusCode.Forbidden);
        changesWithoutHeader.Should().Be(0);
        protectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        app.Changes.Should().Be(1);
    }

    private static async Task ShouldBeProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        response.StatusCode.Should().Be(status);
        response.ShouldHaveMediaType(ResponseAssertions.ProblemJson);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation));
        problem.RootElement.GetProperty("status").GetInt32().Should().Be((int)status);
    }
}
