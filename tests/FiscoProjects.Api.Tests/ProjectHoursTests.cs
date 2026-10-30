using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: horas-do-projeto (API side): "Horas contratadas", "Horas adicionais", "Saldo do projeto", "Horas dos projetos",
// "Detalhe das horas do projeto", "Apontamentos de um projeto pelo administrador", "Permissões no saldo das horas" and
// "Saldo na auditoria" (ADR-0128).
public sealed class ProjectHoursTests(SqlServerFixture sqlServer)
{
    private const string Path = "/api/admin/project-hours";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    /// <summary>Today in America/Sao_Paulo, the time zone of the installation, by the clock of the test.</summary>
    private static DateOnly TodayOf(ProjectTeam team) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
        team.App.Clock.GetUtcNow().UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")));

    private static string DayText(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static Task<HttpResponseMessage> ContractAsync(HttpClient client, Guid project, decimal? hours) =>
        SendAsync(client, HttpMethod.Put, $"{Path}/{project}/contracted", new { hours });

    private static Task<HttpResponseMessage> AddAsync(HttpClient client, Guid project, DateOnly? day, decimal hours, string? reason) =>
        SendAsync(client, HttpMethod.Post, $"{Path}/{project}/additional", new { day = day is { } value ? DayText(value) : null, hours, reason });

    private static async Task<Guid> AddedAsync(ProjectTeam team, Guid project, decimal hours, string reason = "Integração com um segundo banco")
    {
        var added = await AddAsync(team.Admin, project, TodayOf(team), hours, reason);
        added.StatusCode.Should().Be(HttpStatusCode.Created, await added.Content.ReadAsStringAsync(Cancellation));
        return (await added.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    /// <summary>Hours logged by the person in the project, today or days before.</summary>
    private static async Task LogAsync(ProjectTeam team, Guid person, Guid project, decimal hours, int daysAgo = 0)
    {
        var client = await team.ClientOfAsync(person);
        var logged = await SendAsync(client, HttpMethod.Post, "/api/time-entries", new
        {
            projectId = project,
            day = DayText(TodayOf(team).AddDays(-daysAgo)),
            hours,
            description = "Trabalho no projeto",
            scheduleItemId = (Guid?)null,
            ticketId = (Guid?)null,
        });
        logged.StatusCode.Should().Be(HttpStatusCode.Created, await logged.Content.ReadAsStringAsync(Cancellation));
    }

    private static async Task<JsonElement> DetailAsync(ProjectTeam team, Guid project) =>
        await team.Admin.GetFromJsonAsync<JsonElement>($"{Path}/{project}", Cancellation);

    private static async Task<JsonElement[]> ListAsync(ProjectTeam team, string query = "") =>
        [.. (await team.Admin.GetFromJsonAsync<JsonElement>($"{Path}{query}", Cancellation)).EnumerateArray()];

    private static async Task<string> FirstErrorFieldAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").EnumerateObject().First().Name;
    }

    private static (decimal? Contracted, decimal Additional, decimal Logged, decimal? Balance, int? Consumption, string? State) Numbers(JsonElement project)
    {
        var balance = project.GetProperty("balance");
        static decimal? Optional(JsonElement value) => value.ValueKind == JsonValueKind.Null ? null : value.GetDecimal();
        return (
            Optional(balance.GetProperty("contracted")),
            balance.GetProperty("additional").GetDecimal(),
            balance.GetProperty("logged").GetDecimal(),
            Optional(balance.GetProperty("balance")),
            balance.GetProperty("consumption").ValueKind == JsonValueKind.Null ? null : balance.GetProperty("consumption").GetInt32(),
            balance.GetProperty("state").GetString());
    }

    [Fact(DisplayName = "Cadastrar as horas contratadas")]
    public async Task TheAdministrator_RegistersAndChangesTheContractedHours()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        (await ContractAsync(team.Admin, team.Erp, 400m)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ContractAsync(team.Admin, team.Erp, 420m)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        Numbers((await DetailAsync(team, team.Erp)).GetProperty("project")).Contracted.Should().Be(420m);
    }

    [Fact(DisplayName = "Tirar as horas contratadas")]
    public async Task TakingOutTheContractedHours_LeavesTheProjectWithoutBalance()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        (await ContractAsync(team.Admin, team.Erp, 400m)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ContractAsync(team.Admin, team.Erp, null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        Numbers((await DetailAsync(team, team.Erp)).GetProperty("project"))
            .Should().Be(((decimal?)null, 0m, 0m, (decimal?)null, (int?)null, "NoContract"));
    }

    [Theory(DisplayName = "Horas contratadas inválidas")]
    [InlineData(0)]
    [InlineData(10.1)]
    public async Task InvalidContractedHours_AreRefused(double hours)
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        (await FirstErrorFieldAsync(await ContractAsync(team.Admin, team.Erp, (decimal)hours))).Should().Be("hours");
        (await ContractAsync(team.Admin, Guid.CreateVersion7(), 10m)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        Numbers((await DetailAsync(team, team.Erp)).GetProperty("project")).State.Should().Be("NoContract");
    }

    [Fact(DisplayName = "Incluir horas adicionais")]
    public async Task AdditionalHours_ShowInTheDetail_WithWhoAddedThem()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        await AddedAsync(team, team.Erp, 16m);

        var detail = await DetailAsync(team, team.Erp);
        var additional = detail.GetProperty("additional").EnumerateArray().Single();
        (additional.GetProperty("day").GetString(), additional.GetProperty("hours").GetDecimal(), additional.GetProperty("reason").GetString())
            .Should().Be((DayText(TodayOf(team)), 16m, "Integração com um segundo banco"));
        additional.GetProperty("createdBy").GetProperty("name").GetString().Should().Be("Paulo Nogueira");
        Numbers(detail.GetProperty("project")).Additional.Should().Be(16m);
    }

    [Fact(DisplayName = "Corrigir e apagar horas adicionais")]
    public async Task AdditionalHours_AreCorrectedAndRemoved()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var additional = await AddedAsync(team, team.Erp, 16m);
        var sped = await team.CreateProjectAsync("SPED-27", "Adequação do SPED Fiscal 2027");

        (await SendAsync(team.Admin, HttpMethod.Put, $"{Path}/{sped}/additional/{additional}", new { day = DayText(TodayOf(team)), hours = 12m, reason = "Outro" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(team.Admin, HttpMethod.Put, $"{Path}/{team.Erp}/additional/{additional}", new { day = DayText(TodayOf(team)), hours = 12m, reason = "Integração" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        Numbers((await DetailAsync(team, team.Erp)).GetProperty("project")).Additional.Should().Be(12m);

        (await SendAsync(team.Admin, HttpMethod.Delete, $"{Path}/{team.Erp}/additional/{additional}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var detail = await DetailAsync(team, team.Erp);
        detail.GetProperty("additional").GetArrayLength().Should().Be(0);
        Numbers(detail.GetProperty("project")).Additional.Should().Be(0m);
    }

    [Fact(DisplayName = "Horas adicionais sem motivo")]
    public async Task InvalidAdditionalHours_AreRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        var refused = await AddAsync(team.Admin, team.Erp, TodayOf(team).AddDays(1), 0m, " ");

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await refused.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").EnumerateObject()
            .Select(error => error.Name).Should().BeEquivalentTo(["day", "hours", "reason"]);
        (await DetailAsync(team, team.Erp)).GetProperty("additional").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "Lista das horas dos projetos")]
    public async Task TheList_HasTheBalanceOfEachOpenProject_MostUsedFirst()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var sped = await team.CreateProjectAsync("SPED-27", "Adequação do SPED Fiscal 2027");
        var portal = await team.CreateProjectAsync("PORTAL-NF", "Portal de notas para fornecedores");
        var closed = await team.CreateProjectAsync("TRIB-RS", "Revisão tributária da filial RS");
        var created = await SendAsync(team.Admin, HttpMethod.Post, "/api/clients", new { name = "Alfa Distribuidora", cnpj = (string?)null, observation = (string?)null });
        var clientId = (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        (await SendAsync(team.Admin, HttpMethod.Put, $"/api/projects/{sped}", new { code = "SPED-27", name = "Adequação do SPED Fiscal 2027", description = (string?)null, clientId }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ContractAsync(team.Admin, team.Erp, 10m)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await AddedAsync(team, team.Erp, 2m);
        (await ContractAsync(team.Admin, sped, 10m)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await LogAsync(team, team.Carla, team.Erp, 8m, daysAgo: 1);
        await LogAsync(team, team.Diego, team.Erp, 6m);
        await LogAsync(team, team.Marina, sped, 9m);
        await LogAsync(team, team.Marina, portal, 3m, daysAgo: 1);
        var marina = await team.ClientOfAsync(team.Marina);
        (await SendAsync(marina, HttpMethod.Put, $"/api/projects/{closed}/status", new { status = "Completed" })).IsSuccessStatusCode.Should().BeTrue();

        var rows = await ListAsync(team);

        rows.Select(row => row.GetProperty("code").GetString()).Should().Equal("ERP-FIN", "SPED-27", "PORTAL-NF");
        Numbers(rows[0]).Should().Be(((decimal?)10m, 2m, 14m, (decimal?)-2m, (int?)117, "Over"));
        Numbers(rows[1]).Should().Be(((decimal?)10m, 0m, 9m, (decimal?)1m, (int?)90, "NearLimit"));
        Numbers(rows[2]).Should().Be(((decimal?)null, 0m, 3m, (decimal?)null, (int?)null, "NoContract"));
        (rows[0].GetProperty("status").GetString(), rows[0].GetProperty("clientName").ValueKind).Should().Be(("Planning", JsonValueKind.Null));
        rows[1].GetProperty("clientName").GetString().Should().Be("Alfa Distribuidora");
    }

    [Fact(DisplayName = "Busca nas horas dos projetos")]
    public async Task TheSearch_FindsCodeOrName_WithoutAccents_InEveryView()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await team.CreateProjectAsync("SPED-27", "Adequação do SPED Fiscal 2027");
        var tms = await team.CreateProjectAsync("TMS-DELTA", "Implantação do TMS");
        var marina = await team.ClientOfAsync(team.Marina);
        (await SendAsync(marina, HttpMethod.Put, $"/api/projects/{tms}/status", new { status = "Cancelled" })).IsSuccessStatusCode.Should().BeTrue();

        (await ListAsync(team, "?view=All&search=implantacao")).Select(row => row.GetProperty("code").GetString())
            .Should().BeEquivalentTo(["ERP-FIN", "TMS-DELTA"]);
        (await ListAsync(team, "?view=Closed")).Select(row => row.GetProperty("code").GetString()).Should().Equal("TMS-DELTA");
        (await ListAsync(team, "?search=sped")).Select(row => row.GetProperty("code").GetString()).Should().Equal("SPED-27");
    }

    [Fact(DisplayName = "Detalhe do projeto")]
    public async Task TheDetail_HasEachPerson_TheMostFirst_WithRoleAndShare()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        (await ContractAsync(team.Admin, team.Erp, 40m)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await LogAsync(team, team.Marina, team.Erp, 2m, daysAgo: 3);
        await LogAsync(team, team.Carla, team.Erp, 4m, daysAgo: 1);
        await LogAsync(team, team.Diego, team.Erp, 6m);
        await LogAsync(team, team.Diego, team.Erp, 8m, daysAgo: 2);

        var detail = await DetailAsync(team, team.Erp);

        detail.GetProperty("firstDay").GetString().Should().Be(DayText(TodayOf(team).AddDays(-3)));
        detail.GetProperty("people").EnumerateArray()
            .Select(person => (person.GetProperty("person").GetProperty("name").GetString(), person.GetProperty("role").GetString(), person.GetProperty("hours").GetDecimal(), person.GetProperty("share").GetInt32()))
            .Should().Equal(
                ("Diego Torres", "Developer", 14m, 70),
                ("Carla Mendes", "Contributor", 4m, 20),
                ("Marina Costa", "Manager", 2m, 10));
        Numbers(detail.GetProperty("project")).Should().Be(((decimal?)40m, 0m, 20m, (decimal?)20m, (int?)50, "Within"));
    }

    [Fact(DisplayName = "Pessoa que saiu no detalhe")]
    public async Task WhoLeftTheProject_StaysInTheDetail_WithoutRole()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await LogAsync(team, team.Carla, team.Erp, 4m);
        var marina = await team.ClientOfAsync(team.Marina);
        (await SendAsync(marina, HttpMethod.Delete, $"/api/projects/{team.Erp}/members/{team.Carla}")).IsSuccessStatusCode.Should().BeTrue();

        var person = (await DetailAsync(team, team.Erp)).GetProperty("people").EnumerateArray().Single();

        (person.GetProperty("person").GetProperty("name").GetString(), person.GetProperty("role").ValueKind, person.GetProperty("share").GetInt32())
            .Should().Be(("Carla Mendes", JsonValueKind.Null, 100));
        (await team.Admin.GetAsync($"{Path}/{Guid.CreateVersion7()}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Apontamentos do projeto")]
    public async Task TheAdministrator_SeesTheEntriesOfEveryPersonOfAProject()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var sped = await team.CreateProjectAsync("SPED-27", "Adequação do SPED Fiscal 2027");
        await LogAsync(team, team.Carla, team.Erp, 4m, daysAgo: 1);
        await LogAsync(team, team.Diego, team.Erp, 6m);
        await LogAsync(team, team.Marina, sped, 3m);
        var today = TodayOf(team);

        var list = await team.Admin.GetFromJsonAsync<JsonElement>(
            $"/api/time-entries?from={DayText(today.AddDays(-30))}&to={DayText(today)}&projectId={team.Erp}", Cancellation);

        list.GetProperty("items").EnumerateArray()
            .Select(item => (item.GetProperty("person").GetProperty("name").GetString(), item.GetProperty("hours").GetDecimal(), item.GetProperty("canChange").GetBoolean()))
            .Should().Equal(("Diego Torres", 6m, true), ("Carla Mendes", 4m, true));
        list.GetProperty("total").GetDecimal().Should().Be(10m);
        (await team.Admin.GetAsync($"/api/time-entries?from={DayText(today)}&to={DayText(today)}&projectId={Guid.CreateVersion7()}", Cancellation))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Apontamentos do projeto sem ser administrador")]
    public async Task TheEntriesOfAProject_AreOnlyForTheAdministrator()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        var today = DayText(TodayOf(team));

        (await marina.GetAsync($"/api/time-entries?from={today}&to={today}&projectId={team.Erp}", Cancellation))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await marina.GetAsync($"/api/time-entries?from={today}&to={today}&projectId={team.Erp}&personId={team.Marina}", Cancellation))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory(DisplayName = "Permissões no saldo por papel")]
    [InlineData("Administrator", true)]
    [InlineData("Manager", false)]
    [InlineData("TechLead", false)]
    [InlineData("Contributor", false)]
    public async Task Permissions_OnlyTheAdministrator(string role, bool allowed)
    {
        // Paulo is the Administrator, Marina a project manager, Rafael a tech lead and Carla a user in the system, the
        // three of them in the project.
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var client = await team.ClientOfAsync(role);
        var existing = await AddedAsync(team, team.Erp, 8m);
        var day = DayText(TodayOf(team));

        var responses = new[]
        {
            await client.GetAsync(Path, Cancellation),
            await client.GetAsync($"{Path}/{team.Erp}", Cancellation),
            await ContractAsync(client, team.Erp, 100m),
            await AddAsync(client, team.Erp, TodayOf(team), 4m, "Pedido do cliente"),
            await SendAsync(client, HttpMethod.Put, $"{Path}/{team.Erp}/additional/{existing}", new { day, hours = 6m, reason = "Pedido do cliente" }),
            await SendAsync(client, HttpMethod.Delete, $"{Path}/{team.Erp}/additional/{existing}"),
        };

        responses.Select(response => response.IsSuccessStatusCode).Should().AllBeEquivalentTo(allowed);
        if (!allowed)
        {
            responses.Select(response => response.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.Forbidden);
            var detail = await DetailAsync(team, team.Erp);
            Numbers(detail.GetProperty("project")).Should().Be(((decimal?)null, 8m, 0m, (decimal?)null, (int?)null, "NoContract"));
        }
    }

    [Fact(DisplayName = "Horas contratadas e adicionais na auditoria")]
    public async Task ContractedAndAdditionalHours_AreInTheAudit()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        (await ContractAsync(team.Admin, team.Erp, 400m)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await AddedAsync(team, team.Erp, 16m);

        var audit = await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation);
        var records = audit.GetProperty("items").EnumerateArray()
            .Where(record => record.GetProperty("itemType").GetString() is "ContractedHours" or "AdditionalHours")
            .ToList();

        records.Select(record => (record.GetProperty("itemType").GetString(), record.GetProperty("action").GetString()))
            .Should().BeEquivalentTo([("ContractedHours", "Created"), ("AdditionalHours", "Created")]);
        records.Should().AllSatisfy(record => record.GetProperty("actor").GetProperty("id").GetGuid().Should().Be(team.Paulo));
        audit.ToString().Should().Contain("ERP-FIN").And.Contain("Integração com um segundo banco");
    }
}
