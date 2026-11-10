using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Notifications.Application;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: avisos (API side), requirements "Aviso de tarefa atribuída" and "Tarefa sem aviso de atribuição" (ADR-0156).
// The project from the MS Project is in MsProjectImportTests.
public sealed class TaskAssignedNoticeTests(SqlServerFixture sqlServer)
{
    private const string Task = "Parametrizar regras de ICMS";
    private const string ToCarla = "Tarefa atribuída a você: Parametrizar regras de ICMS";
    private const string CarlaEmail = "carla.mendes@empresa.example";
    private const string DiegoEmail = "diego.torres@empresa.example";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>
    /// ERP-FIN, created by Paulo (the Administrator) for a client and managed by Marina, with Rafael as tech lead, Carla as
    /// contributor and Diego as developer, and the phase Configuração. Monday, 19/10/2026.
    /// </summary>
    private sealed record Scene(SessionApp App, Guid Marina, Guid Carla, Guid Diego, Guid Erp, Guid Phase) : IAsyncDisposable
    {
        public string Path => $"/api/projects/{Erp}/schedule";

        /// <summary>Marina includes the task with the owner, or with the client in charge.</summary>
        public async Task<Guid> AddTaskAsync(HttpClient marina, Guid? owner, bool ownedByClient = false)
        {
            var added = await SendAsync(marina, HttpMethod.Post, $"{Path}/items", new
            {
                kind = "Task",
                phaseId = Phase,
                name = Task,
                start = "2026-10-26",
                duration = 3,
                ownerId = owner,
                ownedByClient,
                progress = 0,
                predecessorIds = Array.Empty<Guid>(),
            });
            added.StatusCode.Should().Be(HttpStatusCode.Created, await added.Content.ReadAsStringAsync(Cancellation));
            return (await added.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        }

        /// <summary>Marina changes the owner of the task, the rest as it is.</summary>
        public async Task ChangeOwnerAsync(HttpClient marina, Guid task, Guid owner)
        {
            var changed = await SendAsync(marina, HttpMethod.Put, $"{Path}/items/{task}", new
            {
                phaseId = Phase,
                name = Task,
                start = "2026-10-26",
                duration = 3,
                ownerId = owner,
                progress = (int?)null,
                predecessorIds = Array.Empty<Guid>(),
            });
            changed.StatusCode.Should().Be(HttpStatusCode.OK, await changed.Content.ReadAsStringAsync(Cancellation));
        }

        public ValueTask DisposeAsync() => App.DisposeAsync();
    }

    private async Task<Scene> StartAsync()
    {
        var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        var rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.TechLead);
        var carla = await app.CreateUserAsync("Carla Mendes", SystemRole.User);
        var diego = await app.CreateUserAsync("Diego Torres", SystemRole.User);
        var client = await IdOfAsync(await SendAsync(admin, HttpMethod.Post, "/api/clients", new { name = "Cliente Fictício" }));
        var erp = await IdOfAsync(await SendAsync(admin, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-FIN",
            name = "Implantação do ERP financeiro",
            clientId = client,
            managerId = marina,
            techLeadId = rafael,
        }));
        await app.AddProjectMemberAsync(erp, carla, "Contributor");
        await app.AddProjectMemberAsync(erp, diego, "Developer");
        using var asMarina = await app.SignInAsync(marina);
        var phase = await IdOfAsync(await SendAsync(asMarina, HttpMethod.Post, $"/api/projects/{erp}/schedule/phases", new { name = "Configuração" }));
        // What the project sent so far (the inclusions) is out of the way.
        await app.EmailsAsync();
        return new Scene(app, marina, carla, diego, erp, phase);
    }

    private static IEnumerable<EmailMessage> Assigned(IEnumerable<EmailMessage> emails) =>
        emails.Where(email => email.Subject.StartsWith("Tarefa atribuída a você", StringComparison.Ordinal));

    [Fact(DisplayName = "Tarefa atribuída")]
    public async Task IncludedWithAnOwner_TellsTheOwnerNow_WithTheLinkOfTheTask()
    {
        await using var scene = await StartAsync();
        using var marina = await scene.App.SignInAsync(scene.Marina);

        var task = await scene.AddTaskAsync(marina, scene.Carla);

        var email = Assigned(await scene.App.EmailsToAsync(CarlaEmail)).Should().ContainSingle().Subject;
        email.Subject.Should().Be(ToCarla);
        email.TextBody.Should().Contain(
            "Marina Costa passou para você a tarefa Parametrizar regras de ICMS, do projeto ERP-FIN, que termina em 28/10/2026.");
        email.HtmlBody.Should().Contain($"href=\"http://localhost:5173/projects/{scene.Erp}/schedule?item={task}\"");
        using var carla = await scene.App.SignInAsync(scene.Carla);
        var bell = (await carla.GetFromJsonAsync<JsonElement>("/api/notifications", Cancellation)).GetProperty("items")
            .EnumerateArray().Should().ContainSingle(item => item.GetProperty("kind").GetString() == "TaskAssigned").Subject;
        bell.GetProperty("actor").GetString().Should().Be("Marina Costa");
        bell.GetProperty("path").GetString().Should().Be($"/projects/{scene.Erp}/schedule?item={task}");
        (bell.GetProperty("values").GetProperty("item").GetString(), bell.GetProperty("values").GetProperty("end").GetString())
            .Should().Be((Task, "2026-10-28"));
    }

    [Fact(DisplayName = "Responsável trocado")]
    public async Task ANewOwner_HearsAboutIt_AndTheOldOneDoesNot()
    {
        await using var scene = await StartAsync();
        using var marina = await scene.App.SignInAsync(scene.Marina);
        var task = await scene.AddTaskAsync(marina, scene.Carla);
        Assigned(await scene.App.EmailsToAsync(CarlaEmail)).Should().ContainSingle();

        await scene.ChangeOwnerAsync(marina, task, scene.Diego);

        Assigned(await scene.App.EmailsToAsync(DiegoEmail)).Should().ContainSingle()
            .Which.Subject.Should().Be("Tarefa atribuída a você: Parametrizar regras de ICMS");
        Assigned(await scene.App.EmailsToAsync(CarlaEmail)).Should().ContainSingle("Carla heard only when she got the task");
        // Saving the task again with the same owner tells nobody.
        await scene.ChangeOwnerAsync(marina, task, scene.Diego);
        Assigned(await scene.App.EmailsAsync()).Should().HaveCount(2);
    }

    [Fact(DisplayName = "Quem se atribui não recebe")]
    public async Task WhoTakesTheTask_HearsNothing()
    {
        await using var scene = await StartAsync();
        using var marina = await scene.App.SignInAsync(scene.Marina);

        await scene.AddTaskAsync(marina, scene.Marina);

        Assigned(await scene.App.EmailsAsync()).Should().BeEmpty();
        (await marina.GetFromJsonAsync<JsonElement>("/api/notifications", Cancellation)).GetProperty("items")
            .EnumerateArray().Should().NotContain(item => item.GetProperty("kind").GetString() == "TaskAssigned");
    }

    [Fact(DisplayName = "Tarefa atribuída no Teams")]
    public async Task TheAssignment_GoesToTheActivityOfTeams()
    {
        await using var scene = await StartAsync();
        await scene.App.TurnTeamsOnAsync();
        await scene.App.ChooseNoticesAsync(scene.Carla, email: true, teams: true);
        using var marina = await scene.App.SignInAsync(scene.Marina);

        var task = await scene.AddTaskAsync(marina, scene.Carla);

        var message = (await scene.App.TeamsAsync()).Should().ContainSingle(delivery => delivery.UserId == scene.Carla).Subject.Message;
        message.ActivityType.Should().Be("taskAssigned");
        message.Parameters.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["person"] = "Marina Costa",
            ["item"] = Task,
        });
        (message.Topic, message.Preview).Should().Be(("ERP-FIN", Task));
        message.Path.Should().Be($"/projects/{scene.Erp}/schedule?item={task}");
    }

    [Fact(DisplayName = "Cliente como responsável")]
    public async Task TheClientInCharge_TellsNobody()
    {
        await using var scene = await StartAsync();
        using var marina = await scene.App.SignInAsync(scene.Marina);

        await scene.AddTaskAsync(marina, owner: null, ownedByClient: true);

        Assigned(await scene.App.EmailsAsync()).Should().BeEmpty();
    }

    [Fact(DisplayName = "Desfazer a troca do responsável")]
    public async Task UndoingTheChange_TellsNobodyAgain()
    {
        await using var scene = await StartAsync();
        using var marina = await scene.App.SignInAsync(scene.Marina);
        var task = await scene.AddTaskAsync(marina, scene.Carla);
        await scene.ChangeOwnerAsync(marina, task, scene.Diego);
        Assigned(await scene.App.EmailsAsync()).Should().HaveCount(2);

        (await SendAsync(marina, HttpMethod.Post, $"{scene.Path}/undo")).StatusCode.Should().Be(HttpStatusCode.OK);

        var schedule = await marina.GetFromJsonAsync<JsonElement>(scene.Path, Cancellation);
        schedule.GetProperty("phases")[0].GetProperty("items")[0].GetProperty("owner").GetProperty("name").GetString()
            .Should().Be("Carla Mendes");
        Assigned(await scene.App.EmailsAsync()).Should().HaveCount(2, "Carla got her task back without a new notice");
    }

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

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
