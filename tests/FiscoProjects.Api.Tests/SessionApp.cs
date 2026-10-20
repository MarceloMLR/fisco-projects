using FiscoProjects.Notifications.Application;
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
using Microsoft.Extensions.DependencyInjection.Extensions;
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

    private SessionApp(WebApplication app, string connectionString, FakeTimeProvider clock, RecordingEmailSender emails)
    {
        _app = app;
        _connectionString = connectionString;
        Clock = clock;
        Emails = emails;
    }

    public FakeTimeProvider Clock { get; }

    /// <summary>The e-mails the queue sent, kept in memory instead of leaving (ADR-0036).</summary>
    public RecordingEmailSender Emails { get; }

    public IServiceProvider Services => _app.Services;

    public int Changes { get; private set; }

    public static async Task<SessionApp> StartAsync(SqlServerFixture sqlServer, Action<WebApplicationBuilder>? configure = null)
    {
        var connectionString = sqlServer.NewDatabase();
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.Zero));
        var emails = new RecordingEmailSender();
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
            builder.Services.TryAddSingleton<IEmailSender>(emails);
        });

        session = new SessionApp(app, connectionString, clock, emails);
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
    public Task<(HttpClient Client, Guid UserId)> SignedInClientAsync() =>
        SignedInClientAsync("Marina Costa", SystemRole.User);

    /// <summary>A client with a session open for a new user with the given role.</summary>
    public async Task<(HttpClient Client, Guid UserId)> SignedInClientAsync(string name, SystemRole role)
    {
        var userId = await CreateUserAsync(name, role);
        return (await SignInAsync(userId), userId);
    }

    /// <summary>A new user, with an e-mail made from the name, and the given role.</summary>
    public async Task<Guid> CreateUserAsync(string name, SystemRole role)
    {
        var email = $"{name.ToLowerInvariant().Replace(' ', '.')}@empresa.example";
        var userId = await CreateUserAsync(name, email);
        await ExecuteAsync("UPDATE [users].[users] SET Role = @role WHERE Id = @id", ("@role", role.ToString()), ("@id", userId));
        return userId;
    }

    /// <summary>A client with a session open for an existing user.</summary>
    public async Task<HttpClient> SignInAsync(Guid userId)
    {
        var client = NewClient();
        var response = await client.PostAsync($"/test/sign-in/{userId}", null, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>Puts a person in a project with a role, straight in the database, to set up a scene.</summary>
    public Task AddProjectMemberAsync(Guid projectId, Guid userId, string role) =>
        ExecuteAsync(
            "INSERT INTO [projects].[project_members] (Id, ProjectId, UserId, Role, AddedAt) VALUES (@id, @project, @user, @role, @at)",
            ("@id", Guid.CreateVersion7()),
            ("@project", projectId),
            ("@user", userId),
            ("@role", role),
            ("@at", Clock.GetUtcNow().UtcDateTime));

    public Task DeactivateAsync(Guid userId) =>
        ExecuteAsync("UPDATE [users].[users] SET IsActive = 0 WHERE Id = @id", ("@id", userId));

    /// <summary>Role and situation of a user, read straight from the database.</summary>
    public async Task<(SystemRole Role, bool IsActive)> StateOfAsync(Guid userId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Role, IsActive FROM [users].[users] WHERE Id = @id";
        command.Parameters.AddWithValue("@id", userId);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        await reader.ReadAsync(TestContext.Current.CancellationToken);
        return (Enum.Parse<SystemRole>(reader.GetString(0)), reader.GetBoolean(1));
    }

    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();
}
