using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Api.Tests;

// Spec: entrada-e-sessao, requirements "Entrada pelo Entra ID", "Recusas na entrada pelo Entra" and "Entrada local"
// (ADR-0117). Entra ID is simulated inside the process (FakeEntra).
public sealed class EntraSignInTests(SqlServerFixture sqlServer)
{
    private const string MarinaObjectId = "0b5f7c1e-2d3a-4f6b-8c9d-0e1f2a3b4c5d";
    private const string MarinaEmail = "marina.costa@empresa.example";

    private static readonly Dictionary<string, object> Marina = FakeEntra.Member(MarinaObjectId, "Marina Costa", MarinaEmail);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Entrada pelo Entra com a dica do e-mail")]
    public async Task Start_AnswersTheAddressOfMicrosoft_WithTheEmailAsTheHint()
    {
        var entra = new FakeEntra();
        await using var app = await StartAsync(entra);
        using var client = app.NewClient();

        var start = await FakeEntra.StartAsync(client, new { email = $" {MarinaEmail} ", returnUrl = "/projects" });

        start.Location.Host.Should().Be(FakeEntra.Host);
        start.Location.AbsolutePath.Should().Be($"/{FakeEntra.TenantId}/oauth2/v2.0/authorize");
        start.Query["login_hint"].Should().Be(MarinaEmail);
        start.Query["client_id"].Should().Be(FakeEntra.ClientId);
        start.Query["response_type"].Should().Be("code");
        start.Query["scope"].Split(' ').Should().BeEquivalentTo(["openid", "profile", "email"]);
        start.Query["redirect_uri"].Should().Be("https://localhost/signin-oidc");
        start.Query["code_challenge_method"].Should().Be("S256");
        start.Query.Should().ContainKey("nonce").And.ContainKey("state");
        // The e-mail goes only to Microsoft: not in the state that comes back, nor in the answer of the API.
        start.Query["state"].Should().NotContain("marina");
        start.Response.Headers.Location.Should().BeNull();
        start.Response.Headers.GetValues("Set-Cookie").Should().OnlyContain(cookie => !cookie.Contains("marina", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Primeira entrada pelo Entra")]
    public async Task FirstSignIn_CreatesTheUserWithTheEntraLink_AndOpensOnlyTheSession()
    {
        var entra = new FakeEntra();
        await using var app = await StartAsync(entra);
        using var client = app.NewClient();

        var (_, back) = await entra.SignInAsync(client, Marina);

        back.StatusCode.Should().Be(HttpStatusCode.Found);
        back.Headers.Location!.OriginalString.Should().Be("/projects");
        var person = await SessionPersonAsync(client);
        person.GetProperty("name").GetString().Should().Be("Marina Costa");
        person.GetProperty("email").GetString().Should().Be(MarinaEmail);
        (await app.CountAsync(
            "SELECT COUNT(*) FROM [users].[identity_links] WHERE Provider = 'entra' AND ExternalId = @oid", ("@oid", MarinaObjectId)))
            .Should().Be(1);

        // The session cookie holds only the user: no token of Microsoft is kept.
        var ticket = SessionTicketOf(app, back);
        ticket.Principal.Claims.Should().ContainSingle();
        ticket.Properties.GetTokens().Should().BeEmpty();

        using var again = app.NewClient();
        await entra.SignInAsync(again, Marina);
        (await SessionPersonAsync(again)).GetProperty("id").GetString().Should().Be(person.GetProperty("id").GetString());
    }

    [Fact(DisplayName = "Volta só para dentro do FiscoProjects")]
    public async Task ReturnToAnotherSite_GoesHome()
    {
        var entra = new FakeEntra();
        await using var app = await StartAsync(entra);

        foreach (var returnUrl in new[] { "https://outro.example/x", "//outro.example/x", "/\\outro.example", "projects" })
        {
            using var client = app.NewClient();
            var (_, back) = await entra.SignInAsync(client, Marina, new { email = MarinaEmail, returnUrl });
            back.Headers.Location!.OriginalString.Should().Be("/", $"\"{returnUrl}\" is not a path of this site");
        }
    }

    [Fact(DisplayName = "Outra pessoa depois da sessão expirada pelo Entra")]
    public async Task SomeoneElseAfterAnExpiredSession_GoesHome()
    {
        var entra = new FakeEntra();
        await using var app = await StartAsync(entra);
        using var client = app.NewClient();
        var diego = await app.CreateUserAsync("Diego Almeida", "diego.almeida@empresa.example");

        var (_, back) = await entra.SignInAsync(client, Marina, new { email = MarinaEmail, returnUrl = "/tickets", previousUserId = diego });
        back.Headers.Location!.OriginalString.Should().Be("/");

        // The same person again goes back to the screen.
        var marinaId = Guid.Parse((await SessionPersonAsync(client)).GetProperty("id").GetString()!);
        using var same = app.NewClient();
        var (_, sameBack) = await entra.SignInAsync(same, Marina, new { email = MarinaEmail, returnUrl = "/tickets", previousUserId = marinaId });
        sameBack.Headers.Location!.OriginalString.Should().Be("/tickets");
    }

    [Fact(DisplayName = "Credencial pela identidade gerenciada")]
    public async Task CodeExchange_UsesTheProofOfTheManagedIdentity_WithoutSecret()
    {
        var entra = new FakeEntra();
        await using var app = await StartAsync(entra);
        using var client = app.NewClient();

        await entra.SignInAsync(client, Marina);

        entra.LastTokenRequest["grant_type"].Should().Be("authorization_code");
        entra.LastTokenRequest["client_assertion_type"].Should().Be("urn:ietf:params:oauth:client-assertion-type:jwt-bearer");
        entra.LastTokenRequest["client_assertion"].Should().Be(FakeEntra.ManagedIdentityProof);
        entra.LastTokenRequest.Should().ContainKey("code_verifier").And.NotContainKey("client_secret");
    }

    [Fact(DisplayName = "Convidado recusado")]
    public async Task Guest_IsRefused_WithoutCreatingTheUser()
    {
        var entra = new FakeEntra();
        await using var app = await StartAsync(entra);
        var byStatus = new Dictionary<string, object>(Marina) { ["acct"] = 1 };
        var byProvider = new Dictionary<string, object>(Marina) { ["idp"] = "https://sts.windows.net/2f1e0d9c-8b7a-4c6d-9e5f-1a2b3c4d5e6f/" };
        byProvider.Remove("acct");

        foreach (var guest in new[] { byStatus, byProvider })
        {
            using var client = app.NewClient();
            var (_, back) = await entra.SignInAsync(client, guest);

            back.Headers.Location!.OriginalString.Should().Be("/sign-in?refused=guest");
            (await client.GetAsync("/api/session", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        (await app.CountAsync("SELECT COUNT(*) FROM [users].[users]")).Should().Be(0);
        (await app.CountAsync("SELECT COUNT(*) FROM [audit].[entries] WHERE Action = 'SignInRefused' AND Detail = 'Guest' AND ItemLabel = @email", ("@email", MarinaEmail)))
            .Should().Be(2);
    }

    [Fact(DisplayName = "Sem atribuição no Entra")]
    public async Task NotAssigned_GoesBackToTheSignInPageSayingSo()
    {
        var entra = new FakeEntra();
        await using var app = await StartAsync(entra);

        var refusals = new List<string>();
        foreach (var (error, description) in new[]
        {
            ("access_denied", "AADSTS50105: The signed in user is not assigned to a role for the application."),
            ("invalid_request", "AADSTS50105: The signed in user is not assigned to a role for the application."),
            ("access_denied", "AADSTS65004: User declined to consent."),
        })
        {
            using var client = app.NewClient();
            var start = await FakeEntra.StartAsync(client, new { email = MarinaEmail, returnUrl = "/" });
            var back = await client.PostAsync("/signin-oidc", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["error"] = error,
                ["error_description"] = description,
                ["state"] = start.Query["state"],
            }), Cancellation);
            refusals.Add(back.Headers.Location!.OriginalString);
            (await client.GetAsync("/api/session", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        refusals.Should().Equal("/sign-in?refused=notAssigned", "/sign-in?refused=notAssigned", "/sign-in?refused=failed");
    }

    [Fact(DisplayName = "Desativado na entrada pelo Entra")]
    public async Task DeactivatedUser_IsRefused()
    {
        var entra = new FakeEntra();
        await using var app = await StartAsync(entra);
        using var first = app.NewClient();
        await entra.SignInAsync(first, Marina);
        await app.DeactivateAsync(Guid.Parse((await SessionPersonAsync(first)).GetProperty("id").GetString()!));
        using var client = app.NewClient();

        var (_, back) = await entra.SignInAsync(client, Marina);

        back.Headers.Location!.OriginalString.Should().Be("/sign-in?refused=inactive");
        (await client.GetAsync("/api/session", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Falha na volta da Microsoft")]
    public async Task TamperedWayBack_GoesBackToTheSignInPage()
    {
        var entra = new FakeEntra();
        await using var app = await StartAsync(entra);
        using var client = app.NewClient();
        await FakeEntra.StartAsync(client, new { email = MarinaEmail, returnUrl = "/" });

        var back = await client.PostAsync("/signin-oidc", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = "code-of-the-sign-in",
            ["state"] = "tampered",
        }), Cancellation);

        back.Headers.Location!.OriginalString.Should().Be("/sign-in?refused=failed");
        (await client.GetAsync("/api/session", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Entradas local e pelo Entra juntas")]
    public async Task BothSignIns_EmailOutsideTheDemoGoesToMicrosoft_WithoutRefusal()
    {
        // In Development the local sign-in is on, with the demo directory of appsettings.Development.json.
        const string outsideTheDemo = "joana.lima@empresa.example";
        var entra = new FakeEntra();
        await using var app = await StartAsync(entra);
        using var client = app.NewClient();

        var options = await client.GetFromJsonAsync<JsonElement>("/api/session/sign-in-options", Cancellation);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/session/local") { Content = JsonContent.Create(new { email = outsideTheDemo }) };
        request.Headers.Add("X-CSRF", "1");
        var local = await client.SendAsync(request, Cancellation);

        options.GetProperty("localSignIn").GetBoolean().Should().BeTrue();
        options.GetProperty("entraSignIn").GetBoolean().Should().BeTrue();
        local.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await app.CountAsync("SELECT COUNT(*) FROM [audit].[entries] WHERE Action = 'SignInRefused'")).Should().Be(0);
        (await FakeEntra.StartAsync(client, new { email = outsideTheDemo, returnUrl = "/" })).Query["login_hint"].Should().Be(outsideTheDemo);
    }

    [Fact(DisplayName = "Sem a entrada pelo Entra")]
    public async Task WithoutEntra_TheStartDoesNotExist()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        using var client = app.NewClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/session/entra") { Content = JsonContent.Create(new { email = MarinaEmail }) };
        request.Headers.Add("X-CSRF", "1");

        var start = await client.SendAsync(request, Cancellation);
        var options = await client.GetFromJsonAsync<JsonElement>("/api/session/sign-in-options", Cancellation);

        start.StatusCode.Should().Be(HttpStatusCode.NotFound);
        options.GetProperty("entraSignIn").GetBoolean().Should().BeFalse();
    }

    /// <summary>
    /// The API with the simulated Entra, on the clock of now: the cookies that check the way back expire in minutes,
    /// and the cookie jar drops a cookie already expired by its own clock.
    /// </summary>
    private async Task<SessionApp> StartAsync(FakeEntra entra)
    {
        var app = await SessionApp.StartAsync(sqlServer, entra.Configure);
        app.Clock.SetUtcNow(DateTimeOffset.UtcNow);
        return app;
    }

    private static async Task<JsonElement> SessionPersonAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/session", Cancellation);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation)).RootElement.Clone();
    }

    private static AuthenticationTicket SessionTicketOf(SessionApp app, HttpResponseMessage back)
    {
        var cookie = back.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(SessionApp.SessionCookie + "=", StringComparison.Ordinal));
        var value = cookie[(SessionApp.SessionCookie.Length + 1)..cookie.IndexOf(';', StringComparison.Ordinal)];
        var options = app.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        return options.TicketDataFormat.Unprotect(value)!;
    }
}
