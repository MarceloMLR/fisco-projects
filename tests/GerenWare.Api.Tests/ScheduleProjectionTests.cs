using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GerenWare.Testing;

namespace GerenWare.Api.Tests;

// Spec: cronograma, requirement "Projeção do atraso pela API" (ADR-0164). The clock starts on Monday, 19/10/2026, 06:00
// in São Paulo.
public sealed class ScheduleProjectionTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> PhaseAsync(ProjectTeam team, Guid project) =>
        await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{project}/schedule/phases", new { name = "Homologação" }));

    private static async Task<Guid> ItemAsync(
        ProjectTeam team, Guid project, Guid phase, string kind, string name, string start, int duration = 0, int progress = 0, Guid[]? after = null) =>
        await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{project}/schedule/items", new
        {
            kind,
            phaseId = phase,
            name,
            start,
            duration,
            ownerId = (Guid?)null,
            progress,
            predecessorIds = after ?? [],
        }));

    private static async Task<Dictionary<string, JsonElement>> ItemsAsync(HttpClient client, Guid project) =>
        (await client.GetFromJsonAsync<JsonElement>($"/api/projects/{project}/schedule", Cancellation))
            .GetProperty("phases").EnumerateArray()
            .SelectMany(phase => phase.GetProperty("items").EnumerateArray())
            .ToDictionary(item => item.GetProperty("name").GetString()!);

    [Fact(DisplayName = "Projeção pela API")]
    public async Task LateTask_HasItsProjection_WithTheMilestone_AndTheOthersNone()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp);
        // 13/10 to 19/10, at 0%: it should have started on 13/10.
        var tests = await ItemAsync(team, team.Erp, phase, "Task", "Testes integrados", "2026-10-13", 5);
        var acceptance = await ItemAsync(team, team.Erp, phase, "Milestone", "Aceite da homologação", "2026-10-21", after: [tests]);
        await ItemAsync(team, team.Erp, phase, "Task", "Treinar usuários", "2026-10-19", 3);

        // A reader of the project sees it too.
        var items = await ItemsAsync(await team.ClientOfAsync(team.Lucas), team.Erp);

        var projection = items["Testes integrados"].GetProperty("projection");
        (projection.GetProperty("start").GetString(), projection.GetProperty("end").GetString(), projection.GetProperty("days").GetInt32())
            .Should().Be(("2026-10-19", "2026-10-23", 4));
        var milestone = projection.GetProperty("milestones").EnumerateArray().Should().ContainSingle().Subject;
        (milestone.GetProperty("id").GetGuid(), milestone.GetProperty("name").GetString(), milestone.GetProperty("day").GetString(),
            milestone.GetProperty("projectedDay").GetString(), milestone.GetProperty("days").GetInt32())
            .Should().Be((acceptance, "Aceite da homologação", "2026-10-21", "2026-10-23", 2));
        var end = projection.GetProperty("projectEnd");
        (end.GetProperty("end").GetString(), end.GetProperty("projectedEnd").GetString(), end.GetProperty("days").GetInt32())
            .Should().Be(("2026-10-21", "2026-10-23", 2));
        items["Aceite da homologação"].GetProperty("projection").ValueKind.Should().Be(JsonValueKind.Null);
        items["Treinar usuários"].GetProperty("projection").ValueKind.Should().Be(JsonValueKind.Null);
        // Nothing is saved.
        (items["Testes integrados"].GetProperty("end").GetString(), items["Aceite da homologação"].GetProperty("start").GetString())
            .Should().Be(("2026-10-19", "2026-10-21"));
    }

    [Fact(DisplayName = "Projeto encerrado não projeta")]
    public async Task ClosedProject_HasNoProjection()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp);
        await ItemAsync(team, team.Erp, phase, "Task", "Testes integrados", "2026-10-13", 3, 40);
        var marina = await team.ClientOfAsync(team.Marina);
        (await ItemsAsync(marina, team.Erp))["Testes integrados"].GetProperty("projection").ValueKind.Should().Be(JsonValueKind.Object);

        (await SendAsync(marina, HttpMethod.Put, $"/api/projects/{team.Erp}/status", new { status = "Completed" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ItemsAsync(marina, team.Erp))["Testes integrados"].GetProperty("projection").ValueKind.Should().Be(JsonValueKind.Null);
        (await marina.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}/overview", Cancellation))
            .GetProperty("attention").GetProperty("tasks").EnumerateArray().Single()
            .GetProperty("projection").ValueKind.Should().Be(JsonValueKind.Null, "the late task stays, without a projection");
    }
}
