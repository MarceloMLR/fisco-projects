using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: usuarios-e-papeis, requirement "Foto da pessoa" (ADR-0124): the photo of Microsoft 365 for the avatar. Entra ID
// and Microsoft Graph are simulated inside the process (FakeEntra, FakeGraph).
public sealed class PersonPhotoTests(SqlServerFixture sqlServer)
{
    private const string HelenaId = "6f1e2d3c-4b5a-4978-8695-a4b3c2d1e0f9";
    private const string HelenaEmail = "helena.duarte@empresa.example";

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Foto do Microsoft 365")]
    public async Task PersonOfEntra_HasThePhoto_KeptByTheBrowserAndTheServer()
    {
        var entra = new FakeEntra();
        entra.Graph.Photos[HelenaId] = new FakeGraphPhoto(Jpeg);
        await using var app = await StartAsync(entra);
        var helena = await SignInHelenaAsync(app, entra);
        var (marina, _) = await app.SignedInClientAsync();

        using var response = await marina.GetAsync($"/api/people/{helena}/photo", Cancellation);
        using var again = await marina.GetAsync($"/api/people/{helena}/photo", Cancellation);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
        (await response.Content.ReadAsByteArrayAsync(Cancellation)).Should().Equal(Jpeg);
        KeptForAnHour(response).Should().BeTrue();
        response.Headers.Contains("Set-Cookie").Should().BeFalse("the session is not renewed by a photo");
        response.Headers.GetValues("Content-Security-Policy").Should().ContainSingle()
            .Which.Should().Be("default-src 'none'; sandbox");
        again.StatusCode.Should().Be(HttpStatusCode.OK);
        entra.Graph.Requests.Count(address => address.AbsolutePath.EndsWith("/$value", StringComparison.Ordinal))
            .Should().Be(1, "the second photo comes from the memory");
    }

    [Fact(DisplayName = "Pessoa sem foto")]
    public async Task LocalSignIn_AndPersonWithoutPhoto_AreNotFound()
    {
        var entra = new FakeEntra();
        await using var app = await StartAsync(entra);
        var helena = await SignInHelenaAsync(app, entra);
        var (marina, marinaId) = await app.SignedInClientAsync();

        using var local = await marina.GetAsync($"/api/people/{marinaId}/photo", Cancellation);
        using var withoutPhoto = await marina.GetAsync($"/api/people/{helena}/photo", Cancellation);
        using var unknown = await marina.GetAsync($"/api/people/{Guid.CreateVersion7()}/photo", Cancellation);

        local.StatusCode.Should().Be(HttpStatusCode.NotFound);
        KeptForAnHour(local).Should().BeTrue("the avatars do not ask again");
        withoutPhoto.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Foto de quem foi anonimizado")]
    public async Task AnonymizedPerson_HasNoPhoto_EvenReadBefore()
    {
        var entra = new FakeEntra();
        entra.Graph.Photos[HelenaId] = new FakeGraphPhoto(Jpeg);
        await using var app = await StartAsync(entra);
        var helena = await SignInHelenaAsync(app, entra);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        (await admin.GetAsync($"/api/people/{helena}/photo", Cancellation)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await SendAsync(admin, $"/api/users/{helena}/deactivate")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SendAsync(admin, $"/api/users/{helena}/anonymization")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await admin.GetAsync($"/api/people/{helena}/photo", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Foto com o Microsoft 365 fora do ar")]
    public async Task GraphDown_NoPhoto_WithoutAnError()
    {
        var entra = new FakeEntra();
        await using var app = await StartAsync(entra);
        var helena = await SignInHelenaAsync(app, entra);
        entra.Graph.Down = true;
        var (marina, _) = await app.SignedInClientAsync();

        (await marina.GetAsync($"/api/people/{helena}/photo", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Foto sem sessão")]
    public async Task WithoutASession_NoPhoto()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var diego = await app.CreateUserAsync("Diego Torres", SystemRole.User);
        using var anonymous = app.NewClient();

        (await anonymous.GetAsync($"/api/people/{diego}/photo", Cancellation)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>Whether only the browser of the person keeps the answer, for an hour (ADR-0124).</summary>
    private static bool KeptForAnHour(HttpResponseMessage response) =>
        response.Headers.CacheControl is { Private: true, MaxAge: var maxAge, NoStore: false } && maxAge == TimeSpan.FromHours(1);

    private async Task<SessionApp> StartAsync(FakeEntra entra)
    {
        var app = await SessionApp.StartAsync(sqlServer, entra.Configure);
        app.Clock.SetUtcNow(DateTimeOffset.UtcNow);
        return app;
    }

    /// <summary>Helena signs in through Entra ID, as a browser would, and becomes a user.</summary>
    private static async Task<Guid> SignInHelenaAsync(SessionApp app, FakeEntra entra)
    {
        using var browser = app.NewClient();
        var (_, back) = await entra.SignInAsync(browser, FakeEntra.Member(HelenaId, "Helena Duarte", HelenaEmail));
        back.StatusCode.Should().Be(HttpStatusCode.Found);
        var session = await browser.GetFromJsonAsync<JsonElement>("/api/session", Cancellation);
        return session.GetProperty("id").GetGuid();
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("X-CSRF", "1");
        return await client.SendAsync(request, Cancellation);
    }
}
