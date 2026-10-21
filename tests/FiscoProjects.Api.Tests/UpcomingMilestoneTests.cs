using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: projetos (API side): "Atraso do cronograma na lista" and "Próximos marcos da pessoa" (ADR-0088, ADR-0096). The
// clock starts on Monday, 19/10/2026, 06:00 in São Paulo; 02/11 is a national holiday.
public sealed class UpcomingMilestoneTests(SqlServerFixture sqlServer)
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
        ProjectTeam team, Guid project, Guid phase, string kind, string name, string start, int duration = 0, int progress = 0) =>
        await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{project}/schedule/items", new
        {
            kind,
            phaseId = phase,
            name,
            start,
            duration,
            ownerId = kind == "Task" ? team.Diego : (Guid?)null,
            progress,
            predecessorIds = Array.Empty<Guid>(),
        }));

    private static async Task<JsonElement> ScheduleOfListAsync(HttpClient client, Guid project) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/projects?mine=true", Cancellation)).GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == project).GetProperty("schedule");

    private static async Task<List<JsonElement>> UpcomingAsync(HttpClient client) =>
        [.. (await client.GetFromJsonAsync<JsonElement>("/api/projects/upcoming-milestones", Cancellation)).GetProperty("items").EnumerateArray()];

    [Fact(DisplayName = "Atraso pelo item mais atrasado")]
    public async Task LateDays_AreOfTheMostLateItem()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp);
        // Ended on Thursday, 15/10: 2 business days late; the milestone of Friday, 16/10: 1.
        await ItemAsync(team, team.Erp, phase, "Task", "Validar cadastro de produtos", "2026-10-13", duration: 3, progress: 80);
        await ItemAsync(team, team.Erp, phase, "Milestone", "Levantamento aprovado", "2026-10-16");
        await ItemAsync(team, team.Erp, phase, "Task", "Configurar plano de contas", "2026-10-19", duration: 5);

        var schedule = await ScheduleOfListAsync(await team.ClientOfAsync(team.Marina), team.Erp);

        (schedule.GetProperty("lateDays").GetInt32(), schedule.GetProperty("lateTasks").GetInt32(), schedule.GetProperty("lateMilestones").GetInt32())
            .Should().Be((2, 1, 1));
    }

    [Fact(DisplayName = "Cronograma sem atraso")]
    public async Task Schedule_WithoutDelays_HasNoLateDays()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp);
        await ItemAsync(team, team.Erp, phase, "Task", "Configurar plano de contas", "2026-10-19", duration: 5);

        (await ScheduleOfListAsync(await team.ClientOfAsync(team.Marina), team.Erp)).GetProperty("lateDays").GetInt32().Should().Be(0);
    }

    [Fact(DisplayName = "Próximos marcos pela API")]
    public async Task Upcoming_AreByDay_WithTheBusinessDaysLeft_AndTheRequestedAcceptance()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp);
        var production = await ItemAsync(team, team.Erp, phase, "Milestone", "Entrada em produção", "2026-11-13");
        var homologation = await ItemAsync(team, team.Erp, phase, "Milestone", "Homologação", "2026-11-03");
        var marina = await team.ClientOfAsync(team.Marina);
        using var form = new MultipartFormDataContent
        {
            { new StringContent(homologation.ToString()), "milestoneId" },
            { new StringContent(team.Carla.ToString()), "recipientId" },
            { new StringContent("Aceite da homologação", Encoding.UTF8), "title" },
            { new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.7\ntermo\n%%EOF")), "file", "termo.pdf" },
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/projects/{team.Erp}/acceptances") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        await IdOfAsync(await marina.SendAsync(request, Cancellation));

        var upcoming = await UpcomingAsync(await team.ClientOfAsync(team.Diego));

        upcoming.Select(item => (item.GetProperty("id").GetGuid(), item.GetProperty("day").GetString(),
                item.GetProperty("businessDaysLeft").GetInt32(), item.GetProperty("acceptanceRequested").GetBoolean()))
            .Should().Equal((homologation, "2026-11-03", 10, true), (production, "2026-11-13", 18, false));
        upcoming[0].GetProperty("project").GetProperty("code").GetString().Should().Be("ERP-FIN");
    }

    [Fact(DisplayName = "Marco atingido ou passado fica fora")]
    public async Task ReachedAndPastMilestones_StayOut()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp);
        var reached = await ItemAsync(team, team.Erp, phase, "Milestone", "Escopo aprovado", "2026-10-21");
        await ItemAsync(team, team.Erp, phase, "Milestone", "Levantamento aprovado", "2026-10-16");
        var today = await ItemAsync(team, team.Erp, phase, "Milestone", "Reunião de abertura", "2026-10-19");
        (await SendAsync(team.Admin, HttpMethod.Put, $"/api/projects/{team.Erp}/schedule/items/{reached}/reached", new { reached = true }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var upcoming = await UpcomingAsync(await team.ClientOfAsync(team.Marina));

        upcoming.Select(item => (item.GetProperty("id").GetGuid(), item.GetProperty("businessDaysLeft").GetInt32())).Should().Equal((today, 0));
    }

    [Fact(DisplayName = "Só projetos da pessoa")]
    public async Task Upcoming_AreOnlyOfTheProjectsOfThePerson()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var phase = await PhaseAsync(team, team.Erp);
        await ItemAsync(team, team.Erp, phase, "Milestone", "Homologação", "2026-11-03");

        (await UpcomingAsync(team.Admin)).Should().BeEmpty();
        (await UpcomingAsync(await team.ClientOfAsync(team.Lucas))).Should().ContainSingle();
    }
}
