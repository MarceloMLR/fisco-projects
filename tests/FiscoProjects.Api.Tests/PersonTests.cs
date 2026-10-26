using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: usuarios-e-papeis, requirement "Dados de uma pessoa" (ADR-0118): the name and the e-mail of the card of a person.
public sealed class PersonTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Dados de uma pessoa")]
    public async Task AnyoneWithASession_ReadsTheNameAndTheEmail_EvenOfADeactivatedPerson()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (marina, _) = await app.SignedInClientAsync();
        var diego = await app.CreateUserAsync("Diego Torres", SystemRole.User);

        var person = await marina.GetFromJsonAsync<JsonElement>($"/api/people/{diego}", Cancellation);
        await app.DeactivateAsync(diego);
        var deactivated = await marina.GetFromJsonAsync<JsonElement>($"/api/people/{diego}", Cancellation);

        person.GetProperty("id").GetGuid().Should().Be(diego);
        person.GetProperty("name").GetString().Should().Be("Diego Torres");
        person.GetProperty("email").GetString().Should().Be("diego.torres@empresa.example");
        deactivated.GetProperty("email").GetString().Should().Be("diego.torres@empresa.example");
    }

    [Fact(DisplayName = "Dados da pessoa anonimizada")]
    public async Task AnonymizedPerson_ComesWithTheIdentifier_AndNoEmail()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var sofia = await app.CreateUserAsync("Sofia Martins", SystemRole.User);
        (await SendAsync(admin, $"/api/users/{sofia}/deactivate")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SendAsync(admin, $"/api/users/{sofia}/anonymization")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var person = await admin.GetFromJsonAsync<JsonElement>($"/api/people/{sofia}", Cancellation);

        person.GetProperty("name").GetString().Should().Be("Pessoa removida nº 1");
        person.GetProperty("email").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "Pessoa que não existe")]
    public async Task UnknownId_IsNotFound()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (marina, _) = await app.SignedInClientAsync();

        (await marina.GetAsync($"/api/people/{Guid.CreateVersion7()}", Cancellation)).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Dados de uma pessoa sem sessão")]
    public async Task WithoutASession_NothingIsRead()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var diego = await app.CreateUserAsync("Diego Torres", SystemRole.User);
        using var anonymous = app.NewClient();

        (await anonymous.GetAsync($"/api/people/{diego}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("X-CSRF", "1");
        return await client.SendAsync(request, Cancellation);
    }
}
