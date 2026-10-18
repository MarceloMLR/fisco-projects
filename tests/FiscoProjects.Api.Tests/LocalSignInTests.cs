using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Api.Tests;

// Spec: entrada-e-sessao, requirements "Entrada local", "Sessão protegida" and "Sair".
public sealed class LocalSignInTests(SqlServerFixture sqlServer)
{
    private const string Marina = "marina.costa@empresa.example";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Entrada com uma pessoa da demonstração")]
    public async Task DemoPerson_SignsInWithTheEmailInAnyCase()
    {
        await using var app = await SessionApp.StartAsync(sqlServer, WithMarina);
        using var client = app.NewClient();

        var response = await SignInAsync(client, " Marina.COSTA@Empresa.example ");
        var session = await client.GetAsync("/api/session", Cancellation);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        session.StatusCode.Should().Be(HttpStatusCode.OK);
        using var person = JsonDocument.Parse(await session.Content.ReadAsStringAsync(Cancellation));
        person.RootElement.GetProperty("name").GetString().Should().Be("Marina Costa");
        person.RootElement.GetProperty("email").GetString().Should().Be(Marina);
        Guid.Parse(person.RootElement.GetProperty("id").GetString()!).Version.Should().Be(7);
    }

    [Fact(DisplayName = "Cookie da sessão na entrada local")]
    public async Task LocalSignIn_SetsOnlyTheProtectedCookie()
    {
        await using var app = await SessionApp.StartAsync(sqlServer, WithMarina);
        using var client = app.NewClient();

        var response = await SignInAsync(client, Marina);

        var cookie = response.Headers.GetValues("Set-Cookie").Should().ContainSingle().Subject.ToLowerInvariant();
        cookie.Should().StartWith(SessionApp.SessionCookie.ToLowerInvariant() + "=")
            .And.Contain("httponly").And.Contain("secure").And.Contain("samesite=lax");
        (await response.Content.ReadAsStringAsync(Cancellation)).Should().BeEmpty();
    }

    [Fact(DisplayName = "Mesma pessoa, mesmo usuário")]
    public async Task SigningInAgain_KeepsTheSameUser()
    {
        await using var app = await SessionApp.StartAsync(sqlServer, WithMarina);
        using var first = app.NewClient();
        using var second = app.NewClient();
        await SignInAsync(first, Marina);
        await SignInAsync(second, Marina.ToUpperInvariant());

        var firstId = await SessionUserIdAsync(first);
        var secondId = await SessionUserIdAsync(second);

        secondId.Should().Be(firstId);
    }

    [Fact(DisplayName = "E-mail fora da demonstração")]
    public async Task EmailOutsideTheDirectory_IsRefused()
    {
        await using var app = await SessionApp.StartAsync(sqlServer, WithMarina);
        using var client = app.NewClient();

        var response = await SignInAsync(client, "someone@empresa.example");
        var session = await client.GetAsync("/api/session", Cancellation);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.ShouldHaveMediaType(ResponseAssertions.ProblemJson);
        (await ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:not-in-demo-directory");
        response.Headers.Contains("Set-Cookie").Should().BeFalse();
        session.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Usuário inativo na entrada local")]
    public async Task InactiveUser_IsRefused()
    {
        await using var app = await SessionApp.StartAsync(sqlServer, WithMarina);
        using var client = app.NewClient();
        await SignInAsync(client, Marina);
        await app.DeactivateAsync(await SessionUserIdAsync(client));
        using var again = app.NewClient();

        var response = await SignInAsync(again, Marina);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:user-inactive");
    }

    [Fact(DisplayName = "Entrada local desligada")]
    public async Task LocalSignInOff_IsNotAvailable()
    {
        await using var app = await SessionApp.StartAsync(sqlServer,
            builder => builder.Configuration["LocalSignIn:Enabled"] = "false");
        using var client = app.NewClient();

        var options = await client.GetFromJsonAsync<JsonElement>("/api/session/sign-in-options", Cancellation);
        var response = await SignInAsync(client, Marina);

        options.GetProperty("localSignIn").GetBoolean().Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.ShouldHaveMediaType(ResponseAssertions.ProblemJson);
    }

    [Fact(DisplayName = "Opções de entrada")]
    public async Task SignInOptions_TellTheLocalSignInIsOn()
    {
        await using var app = await SessionApp.StartAsync(sqlServer, WithMarina);
        using var client = app.NewClient();

        var options = await client.GetFromJsonAsync<JsonElement>("/api/session/sign-in-options", Cancellation);

        options.GetProperty("localSignIn").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "Entrada local sem o cabeçalho X-CSRF")]
    public async Task LocalSignInWithoutCsrfHeader_IsForbidden()
    {
        await using var app = await SessionApp.StartAsync(sqlServer, WithMarina);
        using var client = app.NewClient();

        var response = await client.PostAsJsonAsync("/api/session/local", new { email = Marina }, Cancellation);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Contains("Set-Cookie").Should().BeFalse();
    }

    [Fact(DisplayName = "Entrada local em produção")]
    public async Task LocalSignInInProduction_StopsTheApplication()
    {
        var start = () => TestApp.ComposedAsync(TestApp.Production, _ => { },
            builder => builder.Configuration["LocalSignIn:Enabled"] = "true");

        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain("not allowed in production");
    }

    [Theory(DisplayName = "Diretório da demonstração só no desenvolvimento")]
    [InlineData(TestApp.Development, true)]
    [InlineData(TestApp.Production, false)]
    public async Task DemoDirectory_IsOnOnlyInDevelopment(string environment, bool localSignIn)
    {
        await using var factory = TestApp.RealHost(environment);
        using var client = factory.CreateClient();

        var options = await client.GetFromJsonAsync<JsonElement>("/api/session/sign-in-options", Cancellation);

        options.GetProperty("localSignIn").GetBoolean().Should().Be(localSignIn);
    }

    [Fact(DisplayName = "Sair")]
    public async Task SigningOut_EndsTheSession()
    {
        await using var app = await SessionApp.StartAsync(sqlServer, WithMarina);
        using var client = app.NewClient();
        await SignInAsync(client, Marina);

        using var signOut = new HttpRequestMessage(HttpMethod.Post, "/api/session/sign-out");
        signOut.Headers.Add("X-CSRF", "1");
        var response = await client.SendAsync(signOut, Cancellation);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync("/api/session", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync(SessionApp.ProtectedRoute, Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static void WithMarina(WebApplicationBuilder builder)
    {
        builder.Configuration["LocalSignIn:Enabled"] = "true";
        builder.Configuration["LocalSignIn:People:0:Name"] = "Marina Costa";
        builder.Configuration["LocalSignIn:People:0:Email"] = Marina;
    }

    private static async Task<HttpResponseMessage> SignInAsync(HttpClient client, string email)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/session/local")
        {
            Content = JsonContent.Create(new { email }),
        };
        request.Headers.Add("X-CSRF", "1");
        return await client.SendAsync(request, Cancellation);
    }

    private static async Task<string?> ProblemTypeAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation));
        return problem.RootElement.GetProperty("type").GetString();
    }

    private static async Task<Guid> SessionUserIdAsync(HttpClient client)
    {
        var person = await client.GetFromJsonAsync<JsonElement>("/api/session", Cancellation);
        return Guid.Parse(person.GetProperty("id").GetString()!);
    }
}
