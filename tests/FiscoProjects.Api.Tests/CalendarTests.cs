using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: calendario, requirements "Calendário só do administrador", "Marcar todos" and "Mudanças do calendário na
// auditoria" (API side), and the errors of each route.
public sealed class CalendarTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>One action of the permission matrix (ADR-0046); the id is a local holiday the Administrator created.</summary>
    private sealed record PermissionRow(string Action, Func<HttpClient, Guid, Task<HttpResponseMessage>> Send, HttpStatusCode Allowed);

    // The permission matrix of the spec, one row per action (ADR-0056): every action only for the Administrator.
    private static readonly PermissionRow[] Matrix =
    [
        new("Read the calendar of a year", (client, _) => client.GetAsync("/api/calendar/2026", Cancellation), HttpStatusCode.OK),
        new("Change the working days", (client, _) => SendAsync(client, HttpMethod.Put, "/api/calendar/working-days",
            new { workingDays = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" } }), HttpStatusCode.NoContent),
        new("Disregard a national holiday", (client, _) => SendAsync(client, HttpMethod.Put,
            "/api/calendar/calculated-holidays/Tiradentes", new { isConsidered = false }), HttpStatusCode.NoContent),
        new("Mark all", (client, id) => SendAsync(client, HttpMethod.Put, "/api/calendar/considered",
            new { isConsidered = true, calculatedHolidays = new[] { "CarnivalMonday" }, localHolidays = new[] { id } }), HttpStatusCode.NoContent),
        new("Add a local holiday", (client, _) => SendAsync(client, HttpMethod.Post, "/api/calendar/local-holidays",
            LocalHoliday("Recesso da empresa", "2026-12-30")), HttpStatusCode.Created),
        new("Change a local holiday", (client, id) => SendAsync(client, HttpMethod.Put, $"/api/calendar/local-holidays/{id}",
            new { name = "Emenda de Tiradentes", date = "2026-04-22", repeatsEveryYear = false, isConsidered = true }), HttpStatusCode.NoContent),
        new("Remove a local holiday", (client, id) => SendAsync(client, HttpMethod.Delete, $"/api/calendar/local-holidays/{id}"),
            HttpStatusCode.NoContent),
    ];

    [Fact(DisplayName = "Administrador altera o calendário")]
    public Task Administrator_ChangesTheCalendar() => AssertMatrixAsync(SystemRole.Administrator);

    [Fact(DisplayName = "Gerente de projeto não altera o calendário")]
    public Task ProjectManager_DoesNotChangeTheCalendar() => AssertMatrixAsync(SystemRole.ProjectManager);

    [Fact(DisplayName = "Usuário não altera o calendário")]
    public Task User_DoesNotChangeTheCalendar() => AssertMatrixAsync(SystemRole.User);

    [Fact(DisplayName = "Calendário sem sessão")]
    public async Task WithoutASession_NothingIsReadNorChanged()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        var id = await AddAsync(admin, "Emenda de Tiradentes", "2026-04-20");
        using var anonymous = app.NewClient();

        foreach (var row in Matrix)
        {
            (await row.Send(anonymous, id)).StatusCode.Should().Be(HttpStatusCode.Unauthorized, row.Action);
        }

        (await YearAsync(admin, 2026)).GetProperty("days").EnumerateArray()
            .Count(day => day.GetProperty("kind").GetString() == "LocalHoliday").Should().Be(1);
    }

    [Fact(DisplayName = "Calendário do ano pela API")]
    public async Task Year_HasTheWorkingDays_AndTheDatesInOrder()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        var year = await YearAsync(admin, 2026);

        year.GetProperty("year").GetInt32().Should().Be(2026);
        year.GetProperty("workingDays").EnumerateArray().Select(day => day.GetString())
            .Should().Equal("Monday", "Tuesday", "Wednesday", "Thursday", "Friday");
        var days = year.GetProperty("days").EnumerateArray().ToList();
        days.Should().HaveCount(17);
        var first = days[0];
        first.GetProperty("date").GetString().Should().Be("2026-01-01");
        first.GetProperty("kind").GetString().Should().Be("NationalHoliday");
        first.GetProperty("key").GetString().Should().Be("NewYearsDay");
        first.GetProperty("id").ValueKind.Should().Be(JsonValueKind.Null);
        first.GetProperty("isConsidered").GetBoolean().Should().BeTrue();
        days.Single(day => day.GetProperty("key").GetString() == "CarnivalMonday")
            .GetProperty("isConsidered").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "Ano fora do calendário")]
    public async Task YearOutsideTheCalendar_IsABadRequest()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        var response = await admin.GetAsync("/api/calendar/2200", Cancellation);

        await ShouldBeInvalidAsync(response, "year");
    }

    [Fact(DisplayName = "Semana sem dia de trabalho")]
    public async Task WeekWithoutAWorkingDay_IsABadRequest()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        var empty = await SendAsync(admin, HttpMethod.Put, "/api/calendar/working-days", new { workingDays = Array.Empty<string>() });
        var missing = await SendAsync(admin, HttpMethod.Put, "/api/calendar/working-days", new { });

        await ShouldBeInvalidAsync(empty, "workingDays");
        await ShouldBeInvalidAsync(missing, "workingDays");
        (await YearAsync(admin, 2026)).GetProperty("workingDays").GetArrayLength().Should().Be(5);
    }

    [Fact(DisplayName = "Feriado calculado desconhecido")]
    public async Task UnknownCalculatedHoliday_IsNotFound()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        var response = await SendAsync(admin, HttpMethod.Put, "/api/calendar/calculated-holidays/FoundationDay", new { isConsidered = true });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Marcar todos com um feriado que não existe")]
    public async Task MarkingAll_WithAMissingHoliday_IsNotFound()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        var response = await SendAsync(admin, HttpMethod.Put, "/api/calendar/considered", new
        {
            isConsidered = true,
            calculatedHolidays = new[] { "CarnivalMonday" },
            localHolidays = new[] { Guid.CreateVersion7() },
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ConsideredAsync(admin, "CarnivalMonday")).Should().BeFalse();
    }

    [Fact(DisplayName = "Feriado local inválido")]
    public async Task InvalidLocalHoliday_IsABadRequest()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        var empty = await SendAsync(admin, HttpMethod.Post, "/api/calendar/local-holidays", new { name = " ", repeatsEveryYear = false });
        var tooLong = await SendAsync(admin, HttpMethod.Post, "/api/calendar/local-holidays", LocalHoliday(new string('a', 81), "2026-01-02"));

        await ShouldBeInvalidAsync(empty, "name", "date");
        await ShouldBeInvalidAsync(tooLong, "name");
    }

    [Fact(DisplayName = "Feriado local que não existe")]
    public async Task MissingLocalHoliday_IsNotFound()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;
        var id = Guid.CreateVersion7();

        var change = await SendAsync(admin, HttpMethod.Put, $"/api/calendar/local-holidays/{id}",
            new { name = "Feriado", date = "2026-01-02", repeatsEveryYear = false, isConsidered = true });
        var remove = await SendAsync(admin, HttpMethod.Delete, $"/api/calendar/local-holidays/{id}");

        change.StatusCode.Should().Be(HttpStatusCode.NotFound);
        remove.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Dias de trabalho na auditoria")]
    public async Task ChangeOfTheWorkingDays_IsAudited()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, adminId) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        await SendAsync(admin, HttpMethod.Put, "/api/calendar/working-days",
            new { workingDays = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" } });

        var entry = (await AuditAsync(admin, "WorkCalendar")).Should().ContainSingle().Subject;
        entry.GetProperty("action").GetString().Should().Be("Updated");
        entry.GetProperty("actor").GetProperty("id").GetString().Should().Be(adminId.ToString());
        entry.GetProperty("itemLabel").ValueKind.Should().Be(JsonValueKind.Null);
        Fields(entry).Should().Equal((
            "WorkingDays",
            "Monday, Tuesday, Wednesday, Thursday, Friday",
            "Monday, Tuesday, Wednesday, Thursday, Friday, Saturday"));
    }

    [Fact(DisplayName = "Escolha de feriado na auditoria")]
    public async Task ChoiceOfACalculatedHoliday_IsAudited()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        await SendAsync(admin, HttpMethod.Put, "/api/calendar/calculated-holidays/CarnivalMonday", new { isConsidered = true });

        var entry = (await AuditAsync(admin, "CalculatedHoliday")).Should().ContainSingle().Subject;
        entry.GetProperty("itemLabel").GetString().Should().Be("CarnivalMonday");
        Fields(entry).Should().Equal(("IsConsidered", "false", "true"));
    }

    [Fact(DisplayName = "Marcar todos na auditoria")]
    public async Task MarkingAll_IsAudited_OnePerHoliday()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        var response = await SendAsync(admin, HttpMethod.Put, "/api/calendar/considered", new
        {
            isConsidered = true,
            calculatedHolidays = new[] { "CarnivalMonday", "CarnivalTuesday", "NewYearsDay" },
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        // The New Year's Day was considered already: nothing changed in it, so no record.
        (await AuditAsync(admin, "CalculatedHoliday")).Select(entry => entry.GetProperty("itemLabel").GetString())
            .Should().BeEquivalentTo("CarnivalMonday", "CarnivalTuesday");
    }

    [Fact(DisplayName = "Feriado local na auditoria")]
    public async Task LocalHoliday_IsAudited_WhenIncludedChangedAndRemoved()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        using var _admin = admin;

        var id = await AddAsync(admin, "Emenda de Tiradentes", "2026-04-20");
        app.Clock.Advance(TimeSpan.FromMinutes(1));
        await SendAsync(admin, HttpMethod.Put, $"/api/calendar/local-holidays/{id}",
            new { name = "Emenda de Tiradentes", date = "2026-04-22", repeatsEveryYear = false, isConsidered = true });
        app.Clock.Advance(TimeSpan.FromMinutes(1));
        await SendAsync(admin, HttpMethod.Delete, $"/api/calendar/local-holidays/{id}");

        var entries = await AuditAsync(admin, "LocalHoliday");
        entries.Select(entry => entry.GetProperty("action").GetString()).Should().Equal("Deleted", "Updated", "Created");
        entries.Should().OnlyContain(entry => entry.GetProperty("itemLabel").GetString() == "Emenda de Tiradentes");
        Fields(entries[1]).Should().Equal(("Date", "2026-04-20", "2026-04-22"));
        Fields(entries[2]).Should().Contain(("Name", null, "Emenda de Tiradentes"));
    }

    private async Task AssertMatrixAsync(SystemRole role)
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        var adminId = await app.CreateUserAsync("Paulo Nogueira", SystemRole.Administrator);
        using var admin = await app.SignInAsync(adminId);
        var id = await AddAsync(admin, "Emenda de Tiradentes", "2026-04-20");
        using var client = role == SystemRole.Administrator ? admin : (await app.SignedInClientAsync("Marina Costa", role)).Client;

        foreach (var row in Matrix)
        {
            var response = await row.Send(client, id);

            response.StatusCode.Should().Be(role == SystemRole.Administrator ? row.Allowed : HttpStatusCode.Forbidden, $"{role}: {row.Action}");
        }

        var expected = role == SystemRole.Administrator;
        (await ConsideredAsync(admin, "Tiradentes")).Should().Be(!expected);
        (await YearAsync(admin, 2026)).GetProperty("workingDays").GetArrayLength().Should().Be(expected ? 6 : 5);
    }

    private static object LocalHoliday(string name, string date, bool repeatsEveryYear = false) =>
        new { name, date, repeatsEveryYear };

    private static async Task<Guid> AddAsync(HttpClient client, string name, string date)
    {
        var response = await SendAsync(client, HttpMethod.Post, "/api/calendar/local-holidays", LocalHoliday(name, date));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static Task<JsonElement> YearAsync(HttpClient client, int year) =>
        client.GetFromJsonAsync<JsonElement>($"/api/calendar/{year}", Cancellation);

    private static async Task<bool> ConsideredAsync(HttpClient client, string key) =>
        (await YearAsync(client, 2026)).GetProperty("days").EnumerateArray()
            .Single(day => day.GetProperty("key").GetString() == key).GetProperty("isConsidered").GetBoolean();

    private static async Task<List<JsonElement>> AuditAsync(HttpClient client, string itemType)
    {
        var page = await client.GetFromJsonAsync<JsonElement>($"/api/audit?itemType={itemType}&pageSize=100", Cancellation);
        return [.. page.GetProperty("items").EnumerateArray()];
    }

    private static List<(string Field, string? Before, string? After)> Fields(JsonElement entry) =>
        [.. entry.GetProperty("changes").EnumerateArray().Select(field => (
            field.GetProperty("field").GetString()!,
            field.GetProperty("before").GetString(),
            field.GetProperty("after").GetString()))];

    private static async Task ShouldBeInvalidAsync(HttpResponseMessage response, params string[] fields)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        problem.GetProperty("errors").EnumerateObject().Select(field => field.Name).Should().BeEquivalentTo(fields);
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
