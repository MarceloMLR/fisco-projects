using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;

namespace FiscoProjects.Api.Tests;

/// <summary>
/// ERP-FIN with a person in each role (ADR-0040): Paulo, the Administrator outside the project; Marina, the manager;
/// Rafael, the tech lead; Carla, contributor; Diego, developer; Lucas, viewer; and Sofia, outside the project. The files
/// go to a folder of the test, removed at the end.
/// </summary>
internal sealed class ProjectTeam : IAsyncDisposable
{
    private readonly Dictionary<Guid, HttpClient> _clients = [];

    private ProjectTeam(SessionApp app, HttpClient admin, string files)
    {
        App = app;
        Admin = admin;
        Files = files;
    }

    public static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public SessionApp App { get; }

    /// <summary>The client of Paulo, the Administrator.</summary>
    public HttpClient Admin { get; }

    /// <summary>The folder of the files of the test.</summary>
    public string Files { get; }

    public Guid Paulo { get; private set; }

    public Guid Marina { get; private set; }

    public Guid Rafael { get; private set; }

    public Guid Carla { get; private set; }

    public Guid Diego { get; private set; }

    public Guid Lucas { get; private set; }

    public Guid Sofia { get; private set; }

    public Guid Erp { get; private set; }

    public static async Task<ProjectTeam> StartAsync(SqlServerFixture sqlServer, Action<WebApplicationBuilder>? configure = null)
    {
        var files = Path.Combine(Path.GetTempPath(), $"fiscoprojects-team-{Guid.NewGuid():N}");
        var app = await SessionApp.StartAsync(sqlServer, builder =>
        {
            builder.Configuration["FileStorage:Provider"] = "Local";
            builder.Configuration["FileStorage:Local:RootPath"] = files;
            configure?.Invoke(builder);
        });
        var (admin, paulo) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var team = new ProjectTeam(app, admin, files) { Paulo = paulo };
        team.Marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        team.Rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.TechLead);
        team.Carla = await app.CreateUserAsync("Carla Mendes", SystemRole.User);
        team.Diego = await app.CreateUserAsync("Diego Torres", SystemRole.User);
        team.Lucas = await app.CreateUserAsync("Lucas Prado", SystemRole.User);
        team.Sofia = await app.CreateUserAsync("Sofia Martins", SystemRole.User);
        team.Erp = await team.CreateProjectAsync("ERP-FIN", "Implantação do ERP financeiro");
        await app.AddProjectMemberAsync(team.Erp, team.Carla, "Contributor");
        await app.AddProjectMemberAsync(team.Erp, team.Diego, "Developer");
        await app.AddProjectMemberAsync(team.Erp, team.Lucas, "Viewer");
        return team;
    }

    /// <summary>A project managed by Marina, with Rafael as tech lead, created by the Administrator.</summary>
    public async Task<Guid> CreateProjectAsync(string code, string name)
    {
        var created = await SendAsync(Admin, HttpMethod.Post, "/api/projects", new { code, name, managerId = Marina, techLeadId = Rafael });
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync(Cancellation));
        return (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    /// <summary>The person of a role of the matrix: Administrator, Manager, TechLead, Contributor, Developer, Viewer or Outside.</summary>
    public Guid PersonOf(string role) => role switch
    {
        "Administrator" => Paulo,
        "Manager" => Marina,
        "TechLead" => Rafael,
        "Contributor" => Carla,
        "Developer" => Diego,
        "Viewer" => Lucas,
        "Outside" => Sofia,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Not a role of the team."),
    };

    /// <summary>A client with a session for the person, made once.</summary>
    public async Task<HttpClient> ClientOfAsync(Guid userId)
    {
        if (userId == Paulo)
        {
            return Admin;
        }

        if (!_clients.TryGetValue(userId, out var client))
        {
            client = await App.SignInAsync(userId);
            _clients[userId] = client;
        }

        return client;
    }

    public Task<HttpClient> ClientOfAsync(string role) => ClientOfAsync(PersonOf(role));

    public static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF", "1");
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request, Cancellation);
    }

    public static async Task<string?> ProblemTypeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString();

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }

        Admin.Dispose();
        await App.DisposeAsync();
        if (Directory.Exists(Files))
        {
            Directory.Delete(Files, recursive: true);
        }
    }
}
