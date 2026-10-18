using FiscoProjects.Testing;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace FiscoProjects.Api.Tests;

/// <summary>
/// The API composed as the host does, on a migrated database of its own, with a clock the test controls and
/// test-only routes: one that opens a session for a user, and a protected API route that counts its changes.
/// </summary>
internal sealed class SessionApp : IAsyncDisposable
{
    public const string ProtectedRoute = "/api/test/protected";
    public const string SessionCookie = "__Host-fiscoprojects-session";

    private readonly WebApplication _app;
    private readonly string _connectionString;

    private SessionApp(WebApplication app, string connectionString, FakeTimeProvider clock)
    {
        _app = app;
        _connectionString = connectionString;
        Clock = clock;
    }

    public FakeTimeProvider Clock { get; }

    public int Changes { get; private set; }

    public static async Task<SessionApp> StartAsync(SqlServerFixture sqlServer, Action<WebApplicationBuilder>? configure = null)
    {
        var connectionString = sqlServer.NewDatabase();
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.Zero));
        SessionApp? session = null;

        // Development, so the API creates the database and applies the migrations when it starts.
        var app = await TestApp.ComposedAsync(TestApp.Development, testApp =>
        {
            testApp.MapPost("/test/sign-in/{userId:guid}", async (Guid userId, HttpContext http) =>
            {
                await http.SignInAsync(UserSession.For(userId));
                return Results.NoContent();
            });

            var api = testApp.MapApiGroup();
            api.MapGet("/test/protected", () => Results.Text("ok"));
            api.MapMethods("/test/protected", [HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete], () =>
            {
                session!.Changes++;
                return Results.Text("changed");
            });
        }, builder =>
        {
            builder.Configuration[TestApp.ConnectionStringKey] = connectionString;
            builder.Services.AddSingleton<TimeProvider>(clock);
            configure?.Invoke(builder);
        });

        session = new SessionApp(app, connectionString, clock);
        return session;
    }

    /// <summary>A browser-like client: https, so the Secure cookie goes back, and with its own cookie jar.</summary>
    public HttpClient NewClient() =>
        new(new CookieContainerHandler { InnerHandler = _app.GetTestServer().CreateHandler() })
        {
            BaseAddress = new Uri("https://localhost"),
        };

    public async Task<Guid> CreateUserAsync(string name, string email)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<SignInWithIdentity>()
            .ExecuteAsync(new ProviderIdentity("local", email, name, email), TestContext.Current.CancellationToken);
        return ((SignInResult.SignedIn)result).UserId;
    }

    /// <summary>A client with a session open for a new user.</summary>
    public async Task<(HttpClient Client, Guid UserId)> SignedInClientAsync()
    {
        var userId = await CreateUserAsync("Marina Costa", "marina.costa@empresa.example");
        var client = NewClient();
        var response = await client.PostAsync($"/test/sign-in/{userId}", null, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (client, userId);
    }

    public async Task DeactivateAsync(Guid userId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE [users].[users] SET IsActive = 0 WHERE Id = @id";
        command.Parameters.AddWithValue("@id", userId);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();
}
