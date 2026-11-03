using System.Net;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FiscoProjects.Api.Tests;

// Spec: entrada-e-sessao, requirements "Entrada pelo Teams" and "Token do Teams só nas leituras do Teams" (ADR-0005,
// ADR-0007, ADR-0008, ADR-0054, ADR-0152).
public sealed class TeamsSignInTests(SqlServerFixture sqlServer)
{
    private const string Ticket = "/api/teams/tickets/1";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Token do Teams aceito")]
    public async Task TheTokenOfTeams_ReadsAsThePerson_WithoutASession()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);

        var fromTheComputer = await scene.ReadAsync(scene.Team.Diego, Ticket);
        var fromTheWeb = await scene.ReadAsync(scene.Team.Diego, Ticket, new TeamsTokenShape { Client = FakeEntra.TeamsWeb });

        fromTheComputer.StatusCode.Should().Be(HttpStatusCode.OK, await fromTheComputer.Content.ReadAsStringAsync(Cancellation));
        fromTheComputer.Headers.Contains("Set-Cookie").Should().BeFalse();
        fromTheWeb.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Token de outro aplicativo recusado")]
    public async Task ATokenThatIsNotOfTeamsForTheApplication_IsRefused()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);
        TeamsTokenShape[] shapes =
        [
            new() { Audience = "00000003-0000-0000-c000-000000000000" },
            new() { Issuer = "https://login.microsoftonline.com/1f2e3d4c-5b6a-4978-8a9b-0c1d2e3f4a5b/v2.0" },
            new() { Scope = "User.Read" },
            new() { Client = "bc59ab01-8403-45c6-8796-ac3ef710b3e3" },
            new() { OtherKey = true },
            new() { Expired = true },
        ];

        foreach (var shape in shapes)
        {
            var response = await scene.ReadAsync(scene.Team.Diego, Ticket, shape);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, shape.ToString());
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        }
    }

    [Fact(DisplayName = "Convidado recusado pelo Teams")]
    public async Task AGuest_IsRefused_AndNoUserIsCreated()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);
        var users = await scene.App.CountAsync("SELECT COUNT(*) FROM [users].[users]");

        var asGuest = await scene.ReadWithTokenAsync(
            scene.Entra.TeamsToken("7d6c5b4a-3e2f-4a1b-9c8d-7e6f5a4b3c2d", new TeamsTokenShape { AccountStatus = 1 }), Ticket);
        var fromElsewhere = await scene.ReadAsync(
            scene.Team.Diego, Ticket, new TeamsTokenShape { IdentityProvider = "https://sts.windows.net/9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d/" });

        asGuest.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        fromElsewhere.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await scene.App.CountAsync("SELECT COUNT(*) FROM [users].[users]")).Should().Be(users);
    }

    [Fact(DisplayName = "Quem nunca entrou recusado pelo Teams")]
    public async Task APersonWithoutTheLink_IsRefused_AndNoUserIsCreated()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);
        var users = await scene.App.CountAsync("SELECT COUNT(*) FROM [users].[users]");
        var links = await scene.App.CountAsync("SELECT COUNT(*) FROM [users].[identity_links]");

        var response = await scene.ReadWithTokenAsync(scene.Entra.TeamsToken("5c4b3a29-1807-4f6e-8d5c-4b3a29180f6e"), Ticket);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await scene.App.CountAsync("SELECT COUNT(*) FROM [users].[users]")).Should().Be(users);
        (await scene.App.CountAsync("SELECT COUNT(*) FROM [users].[identity_links]")).Should().Be(links);
    }

    [Fact(DisplayName = "Desativado recusado pelo Teams")]
    public async Task ADeactivatedPerson_IsRefused()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);
        await scene.App.DeactivateAsync(scene.Team.Diego);

        (await scene.ReadAsync(scene.Team.Diego, Ticket)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Teams sem a entrada pelo Entra")]
    public async Task WithoutTheSignInThroughEntra_TheRoutesOfTeams_RecognizeNoOne()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer, entraOn: false);

        var response = await scene.ReadAsync(scene.Team.Diego, Ticket);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact(DisplayName = "Token do Teams fora das rotas do Teams")]
    public async Task TheTokenOfTeams_IsWorthNothingOnTheOtherRoutes()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);

        foreach (var path in new[] { "/api/session", "/api/tickets/1", SessionApp.ProtectedRoute })
        {
            (await scene.ReadAsync(scene.Team.Diego, path)).StatusCode.Should().Be(HttpStatusCode.Unauthorized, path);
        }
    }

    [Fact(DisplayName = "Sessão nas rotas do Teams")]
    public async Task TheSession_IsWorthNothingOnTheRoutesOfTeams()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);
        var diego = await scene.Team.ClientOfAsync(scene.Team.Diego);

        (await diego.GetAsync("/api/tickets/1", Cancellation)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await diego.GetAsync(Ticket, Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Rotas do Teams só leem")]
    public async Task TheRoutesOfTeams_OnlyRead_AndOnlyWithThePolicyOfTeams()
    {
        await using var scene = await TeamsScene.StartAsync(sqlServer);

        var routes = scene.App.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/teams/", StringComparison.Ordinal) == true)
            .ToList();

        routes.Should().NotBeEmpty();
        routes.Should().AllSatisfy(endpoint =>
        {
            endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.HttpMethodMetadata>()!.HttpMethods.Should().Equal("GET");
            endpoint.Metadata.GetOrderedMetadata<Microsoft.AspNetCore.Authorization.IAuthorizeData>()
                .Select(data => data.Policy).Should().Equal(TeamsAccess.Policy);
        });
    }
}
