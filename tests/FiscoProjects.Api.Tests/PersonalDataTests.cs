using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;
using Microsoft.Data.SqlClient;

namespace FiscoProjects.Api.Tests;

// Spec: dados-pessoais, requirements "Anonimizar uma pessoa", "Pessoa anonimizada em todo o sistema", "Registro da
// anonimização", "Prazo da auditoria" and "Limpeza mensal da auditoria" (ADR-0058, ADR-0110).
public sealed class PersonalDataTests(SqlServerFixture sqlServer)
{
    private const string CarlaName = "Carla Mendes";
    private const string CarlaEmail = "carla.mendes@empresa.example";
    private const string Identifier = "Pessoa removida nº 1";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Anonimizar pessoa desativada")]
    public async Task Anonymizing_TakesTheNameTheEmailAndTheLinksAway()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await ProjectTeam.SendAsync(team.Admin, HttpMethod.Put, $"/api/users/{team.Carla}/role", new { role = "ProjectManager" });

        (await DeactivateAsync(team, team.Carla)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var anonymize = await AnonymizeAsync(team, team.Carla);

        anonymize.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var carla = await UserAsync(team, team.Carla);
        carla.GetProperty("name").GetString().Should().Be(Identifier);
        carla.GetProperty("email").GetString().Should().BeEmpty();
        carla.GetProperty("role").GetString().Should().Be("User");
        carla.GetProperty("status").GetString().Should().Be("Anonymized");
        (await team.App.CountAsync("SELECT COUNT(*) FROM [users].[identity_links] WHERE UserId = @id", ("@id", team.Carla)))
            .Should().Be(0);
        // The next one takes the next number; the deactivated ones are apart from the anonymized ones.
        await DeactivateAsync(team, team.Lucas);
        await AnonymizeAsync(team, team.Lucas);
        (await UserAsync(team, team.Lucas)).GetProperty("name").GetString().Should().Be("Pessoa removida nº 2");
        var anonymized = await team.Admin.GetFromJsonAsync<JsonElement>("/api/users?status=Anonymized", Cancellation);
        anonymized.GetProperty("total").GetInt32().Should().Be(2);
        var inactive = await team.Admin.GetFromJsonAsync<JsonElement>("/api/users?status=Inactive", Cancellation);
        inactive.GetProperty("total").GetInt32().Should().Be(0);
    }

    [Fact(DisplayName = "Pessoa ativa não é anonimizada")]
    public async Task AnActivePerson_IsNotAnonymized()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        var anonymize = await AnonymizeAsync(team, team.Carla);

        anonymize.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(anonymize)).Should().Be("urn:fiscoprojects:problem:user-active");
        (await UserAsync(team, team.Carla)).GetProperty("name").GetString().Should().Be(CarlaName);
        (await AnonymizeAsync(team, Guid.CreateVersion7())).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Anonimização definitiva")]
    public async Task Anonymization_IsForGood()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await DeactivateAsync(team, team.Carla);
        await AnonymizeAsync(team, team.Carla);

        var responses = new[]
        {
            await ProjectTeam.SendAsync(team.Admin, HttpMethod.Post, $"/api/users/{team.Carla}/reactivate"),
            await ProjectTeam.SendAsync(team.Admin, HttpMethod.Put, $"/api/users/{team.Carla}/role", new { role = "Administrator" }),
            await AnonymizeAsync(team, team.Carla),
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ProjectTeam.ProblemTypeAsync(response)).Should().Be("urn:fiscoprojects:problem:user-anonymized");
        }

        (await team.App.StateOfAsync(team.Carla)).Should().Be((SystemRole.User, false));
        (await UserAsync(team, team.Carla)).GetProperty("name").GetString().Should().Be(Identifier);
    }

    [Fact(DisplayName = "Só o administrador anonimiza")]
    public async Task OnlyTheAdministrator_Anonymizes()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await DeactivateAsync(team, team.Carla);
        var marina = await team.ClientOfAsync(team.Marina);

        var anonymize = await ProjectTeam.SendAsync(marina, HttpMethod.Post, $"/api/users/{team.Carla}/anonymization");

        anonymize.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await UserAsync(team, team.Carla)).GetProperty("name").GetString().Should().Be(CarlaName);
    }

    [Fact(DisplayName = "Nada da pessoa nas tabelas")]
    public async Task AfterTheAnonymization_NoTableHasTheNameOrTheEmail()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (first, second) = await SceneAsync(team);
        // Some e-mails sent, others still waiting.
        await team.App.EmailsAsync();
        await MentionCarlaAsync(team, second);
        (await FindAsync(team.App, CarlaName)).Should().NotBeEmpty("the scene keeps the name in many tables");
        await DeactivateAsync(team, team.Carla);
        // A refused sign-in keeps the e-mail used.
        using (var stranger = team.App.NewClient())
        {
            (await ProjectTeam.SendAsync(stranger, HttpMethod.Post, "/api/session/local", new { email = CarlaEmail }))
                .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        (await AnonymizeAsync(team, team.Carla)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await FindAsync(team.App, CarlaName)).Should().BeEmpty();
        (await FindAsync(team.App, CarlaEmail)).Should().BeEmpty();
        (await team.App.CountAsync("SELECT COUNT(*) FROM [tickets].[tickets] WHERE Number = @number", ("@number", first)))
            .Should().Be(1);
    }

    [Fact(DisplayName = "Chamados e mensagens acessíveis")]
    public async Task TicketsAndMessages_StayWithTheIdentifier()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (first, second) = await SceneAsync(team);
        await DeactivateAsync(team, team.Carla);

        await AnonymizeAsync(team, team.Carla);

        var rafael = await team.ClientOfAsync(team.Rafael);
        var opened = await rafael.GetFromJsonAsync<JsonElement>($"/api/tickets/{first}", Cancellation);
        var mentioned = await rafael.GetFromJsonAsync<JsonElement>($"/api/tickets/{second}", Cancellation);
        opened.GetProperty("requester").GetProperty("name").GetString().Should().Be(Identifier);
        foreach (var ticket in new[] { opened, mentioned })
        {
            var message = ticket.GetProperty("messages").EnumerateArray().Single().GetProperty("body").GetString();
            message.Should().Contain(Identifier).And.NotContain(CarlaName);
        }
    }

    [Fact(DisplayName = "E-mail esperando envio")]
    public async Task AnEmailWaitingForCarla_IsNotSent()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var (first, _) = await SceneAsync(team);
        var sentToCarla = (await team.App.EmailsToAsync(CarlaEmail)).Count;
        await MentionCarlaAsync(team, first);
        (await team.App.CountAsync(
            "SELECT COUNT(*) FROM [notifications].[outbox_emails] WHERE ToAddress = @to AND Status = 'Pending'", ("@to", CarlaEmail)))
            .Should().BePositive();
        await DeactivateAsync(team, team.Carla);

        await AnonymizeAsync(team, team.Carla);
        var sent = await team.App.EmailsAsync();

        sent.Count(email => email.ToAddress == CarlaEmail).Should().Be(sentToCarla);
        (await team.App.CountAsync("SELECT COUNT(*) FROM [notifications].[outbox_emails] WHERE ToAddress = @to", ("@to", CarlaEmail)))
            .Should().Be(0);
        (await team.App.CountAsync(
            "SELECT COUNT(*) FROM [notifications].[outbox_emails] WHERE Status = 'Failed' AND ForgottenAt IS NOT NULL AND HtmlBody IS NULL"))
            .Should().BePositive();
        (await team.App.CountAsync("SELECT COUNT(*) FROM [notifications].[notifications] WHERE UserId = @id", ("@id", team.Carla)))
            .Should().Be(0);
    }

    [Fact(DisplayName = "Registro sem o nome")]
    public async Task TheAnonymization_IsRecordedWithoutTheName()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await DeactivateAsync(team, team.Carla);

        await AnonymizeAsync(team, team.Carla);

        var page = await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=User&pageSize=100", Cancellation);
        var entries = page.GetProperty("items").EnumerateArray()
            .Where(entry => entry.GetProperty("itemId").GetGuid() == team.Carla)
            .ToList();
        entries.Should().NotBeEmpty();
        entries.Should().OnlyContain(entry => entry.GetProperty("itemLabel").GetString() == Identifier);
        // The anonymization is the change that gave the number; at the same moment as the deactivation.
        var anonymization = entries.Single(entry => entry.GetProperty("changes").EnumerateArray()
            .Any(field => field.GetProperty("field").GetString() == "AnonymousNumber"));
        anonymization.GetProperty("action").GetString().Should().Be("Updated");
        anonymization.GetProperty("actor").GetProperty("id").GetGuid().Should().Be(team.Paulo);
        var fields = anonymization.GetProperty("changes").EnumerateArray()
            .ToDictionary(field => field.GetProperty("field").GetString()!);
        foreach (var personal in new[] { "Name", "Email" })
        {
            fields[personal].GetProperty("secret").GetBoolean().Should().BeTrue();
            fields[personal].GetProperty("before").ValueKind.Should().Be(JsonValueKind.Null);
            fields[personal].GetProperty("after").ValueKind.Should().Be(JsonValueKind.Null);
        }

        fields["AnonymousNumber"].GetProperty("after").GetString().Should().Be("1");
        fields.Should().NotContainKey("AnonymizedAt");
        page.GetRawText().Should().NotContain(CarlaName).And.NotContain(CarlaEmail);
    }

    [Fact(DisplayName = "Prazo da auditoria")]
    public async Task TheRetentionOfTheAudit_IsChosenFrom1To10Years()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var before = await team.Admin.GetFromJsonAsync<JsonElement>("/api/installation/rules", Cancellation);

        var seven = await SaveRetentionAsync(team, 7);
        var eleven = await SaveRetentionAsync(team, 11);
        var after = await team.Admin.GetFromJsonAsync<JsonElement>("/api/installation/rules", Cancellation);

        before.GetProperty("auditRetentionYears").GetInt32().Should().Be(5);
        seven.StatusCode.Should().Be(HttpStatusCode.NoContent);
        eleven.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await eleven.Content.ReadFromJsonAsync<JsonElement>(Cancellation))
            .GetProperty("errors").GetProperty("auditRetentionYears")[0].GetString()
            .Should().Be("Keep the audit from 1 to 10 years.");
        after.GetProperty("auditRetentionYears").GetInt32().Should().Be(7);
    }

    [Fact(DisplayName = "Limpeza mensal")]
    public async Task TheMonthlyCleanup_RemovesWhatIsOlderThanTheRetention()
    {
        await using var app = await SessionApp.StartAsync(sqlServer);
        // The cleanup of October ran when the API started, with nothing to remove.
        await UntilAsync(async () => await app.CountAsync("SELECT COUNT(*) FROM [audit].[cleanups]") == 1);
        await AddEntryAsync(app, "Registro de 2020", new DateTime(2020, 10, 1, 12, 0, 0, DateTimeKind.Utc));
        await AddEntryAsync(app, "Registro de 2024", new DateTime(2024, 10, 1, 12, 0, 0, DateTimeKind.Utc));

        // November comes: the cleanup of the month removes what is older than 5 years, and only once.
        app.Clock.Advance(TimeSpan.FromDays(13) + TimeSpan.FromHours(6));
        await UntilAsync(async () => await app.CountAsync("SELECT COUNT(*) FROM [audit].[cleanups]") == 2);
        app.Clock.Advance(TimeSpan.FromHours(2));

        (await app.CountAsync("SELECT COUNT(*) FROM [audit].[entries] WHERE ItemLabel = 'Registro de 2020'")).Should().Be(0);
        (await app.CountAsync("SELECT COUNT(*) FROM [audit].[entries] WHERE ItemLabel = 'Registro de 2024'")).Should().Be(1);
        (await app.CountAsync("SELECT COUNT(*) FROM [audit].[cleanups]")).Should().Be(2);
        (await app.CountAsync(
            "SELECT COUNT(*) FROM [audit].[entries] WHERE Action = 'Cleaned' AND ActorId IS NULL AND Changes LIKE '%\"After\":\"2021-11-01\"%' AND Changes LIKE '%\"After\":\"1\"%'"))
            .Should().Be(1);
    }

    /// <summary>
    /// Carla opens two tickets, and Rafael answers both mentioning her. Gives the numbers of the two tickets.
    /// </summary>
    private static async Task<(int First, int Second)> SceneAsync(ProjectTeam team)
    {
        var carla = await team.ClientOfAsync(team.Carla);
        var first = await OpenTicketAsync(carla, team.Erp, "Functional", "Ajuste no cadastro de produtos");
        await MentionCarlaAsync(team, first);
        var second = await OpenTicketAsync(carla, team.Erp, "Development", "Revisar o relatório de vendas");
        await MentionCarlaAsync(team, second);
        return (first, second);
    }

    private static async Task MentionCarlaAsync(ProjectTeam team, int number)
    {
        var rafael = await team.ClientOfAsync(team.Rafael);
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Doc(Mention(team.Carla, CarlaName), Text(", veja o retorno.")), Encoding.UTF8), "body" },
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/tickets/{number}/messages") { Content = form };
        request.Headers.Add("X-CSRF", "1");
        var sent = await rafael.SendAsync(request, Cancellation);
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));
    }

    private static async Task<int> OpenTicketAsync(HttpClient client, Guid projectId, string team, string title)
    {
        var opened = await ProjectTeam.SendAsync(client, HttpMethod.Post, "/api/tickets", new
        {
            projectId,
            team,
            priority = "Medium",
            title,
            description = "Descrição do chamado.",
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        return (await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
    }

    private static object Text(string text) => new { type = "text", text };

    private static object Mention(Guid id, string label) => new { type = "mention", attrs = new { id, label } };

    private static string Doc(params object[] content) =>
        JsonSerializer.Serialize(new { type = "doc", content = new[] { new { type = "paragraph", content } } });

    private static Task<HttpResponseMessage> DeactivateAsync(ProjectTeam team, Guid userId) =>
        ProjectTeam.SendAsync(team.Admin, HttpMethod.Post, $"/api/users/{userId}/deactivate");

    private static Task<HttpResponseMessage> AnonymizeAsync(ProjectTeam team, Guid userId) =>
        ProjectTeam.SendAsync(team.Admin, HttpMethod.Post, $"/api/users/{userId}/anonymization");

    private static async Task<JsonElement> UserAsync(ProjectTeam team, Guid userId)
    {
        var page = await team.Admin.GetFromJsonAsync<JsonElement>("/api/users?pageSize=100", Cancellation);
        return page.GetProperty("items").EnumerateArray().Single(user => user.GetProperty("id").GetGuid() == userId);
    }

    private static Task<HttpResponseMessage> SaveRetentionAsync(ProjectTeam team, int years) =>
        ProjectTeam.SendAsync(team.Admin, HttpMethod.Put, "/api/installation", new
        {
            name = "FiscoProjects",
            timeZone = "America/Sao_Paulo",
            mainColor = (string?)null,
            dailySummaryAt = "07:30:00",
            notificationRetentionDays = 90,
            auditRetentionYears = years,
        });

    private static Task AddEntryAsync(SessionApp app, string label, DateTime occurredAt) =>
        app.ExecuteAsync(
            "INSERT INTO [audit].[entries] (Id, OccurredAt, Action, ItemType, ItemId, ItemLabel, Module, Changes) "
            + "VALUES (@id, @at, 'Created', 'Client', @item, @label, 'projects', '[]')",
            ("@id", Guid.CreateVersion7()),
            ("@at", occurredAt),
            ("@item", Guid.CreateVersion7()),
            ("@label", label));

    /// <summary>Every text column of every table, as "schema.table.column", that has the text, in any case.</summary>
    private static async Task<IReadOnlyList<string>> FindAsync(SessionApp app, string text)
    {
        await using var connection = new SqlConnection(app.ConnectionString);
        await connection.OpenAsync(Cancellation);
        var columns = new List<(string Schema, string Table, string Column)>();
        await using (var list = connection.CreateCommand())
        {
            list.CommandText =
                "SELECT c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS c "
                + "JOIN INFORMATION_SCHEMA.TABLES t ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME "
                + "WHERE t.TABLE_TYPE = 'BASE TABLE' AND c.DATA_TYPE IN ('nvarchar', 'varchar', 'nchar', 'char', 'ntext', 'text')";
            await using var reader = await list.ExecuteReaderAsync(Cancellation);
            while (await reader.ReadAsync(Cancellation))
            {
                columns.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
            }
        }

        var found = new List<string>();
        foreach (var (schema, table, column) in columns)
        {
            await using var search = connection.CreateCommand();
            search.CommandText = $"SELECT COUNT(*) FROM [{schema}].[{table}] WHERE [{column}] LIKE @text";
            search.Parameters.AddWithValue("@text", $"%{text}%");
            if ((int)(await search.ExecuteScalarAsync(Cancellation))! > 0)
            {
                found.Add($"{schema}.{table}.{column}");
            }
        }

        return found;
    }

    private static async Task UntilAsync(Func<Task<bool>> condition)
    {
        for (var attempt = 0; attempt < 300; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(100, Cancellation);
        }

        throw new TimeoutException("The background process did not finish in 30 seconds.");
    }
}
