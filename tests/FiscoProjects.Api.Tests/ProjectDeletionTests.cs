using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;
using Microsoft.Data.SqlClient;

namespace FiscoProjects.Api.Tests;

// Spec: projetos, requirements "Excluir projeto", "Arquivos do projeto excluído", "Resumo da exclusão" and "Exclusão
// na auditoria" (ADR-0041, ADR-0111).
public sealed class ProjectDeletionTests(SqlServerFixture sqlServer)
{
    private const string ErpFolder = "ERP-FIN - Implantação do ERP financeiro";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    [Fact(DisplayName = "Excluir projeto")]
    public async Task Deleting_RemovesEverythingOfTheProject_AndKeepsTheRest()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var sped = await SpedAsync(team);
        var spedTicket = await OpenTicketAsync(team, sped, "Bloco K do SPED");
        var scene = await SceneAsync(team);
        await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{team.Erp}/schedule/template", new { name = "Implantação", description = "Fases" });
        var clientId = await ClientIdAsync(team);
        (await FindAsync(team.App, scene.Ids)).Should().NotBeEmpty("the scene keeps the project in many tables");

        var deleted = await DeleteAsync(team, "erp-fin ");

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await team.Admin.GetAsync($"/api/projects/{team.Erp}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await FindAsync(team.App, scene.Ids)).Should().BeEmpty();
        // The other project, the client and the model stay.
        (await team.Admin.GetFromJsonAsync<JsonElement>($"/api/tickets/{spedTicket}", Cancellation))
            .GetProperty("project").GetProperty("code").GetString().Should().Be("SPED-27");
        (await team.App.CountAsync("SELECT COUNT(*) FROM [projects].[clients] WHERE Id = @id", ("@id", clientId))).Should().Be(1);
        (await team.App.CountAsync("SELECT COUNT(*) FROM [projects].[project_templates]")).Should().Be(1);
        (await team.App.CountAsync("SELECT COUNT(*) FROM [projects].[projects]")).Should().Be(1);
        // Nothing the system stored for the project is left in the storage.
        Directory.Exists(Path.Combine(team.Files, ErpFolder)).Should().BeFalse();
        Directory.GetFiles(team.Files, "*", SearchOption.AllDirectories)
            .Where(file => !file.Contains(".fiscoprojects", StringComparison.Ordinal))
            .Should().BeEmpty("only the project deleted had files");
    }

    [Fact(DisplayName = "Arquivo de fora fica")]
    public async Task AFileTheSystemDidNotStore_StaysWithItsFolder()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await SceneAsync(team);
        var outside = Path.Combine(team.Files, ErpFolder, "Planilha antiga.xlsx");
        await File.WriteAllTextAsync(outside, "de fora", Cancellation);

        (await DeleteAsync(team, "ERP-FIN")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        File.Exists(outside).Should().BeTrue();
        Directory.GetFileSystemEntries(Path.Combine(team.Files, ErpFolder)).Should().Equal(outside);
    }

    [Fact(DisplayName = "Código errado")]
    public async Task AWrongCode_RemovesNothing()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var scene = await SceneAsync(team);

        var deleted = await DeleteAsync(team, "SPED-27");
        var empty = await DeleteAsync(team, null);

        foreach (var response in new[] { deleted, empty })
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors")
                .GetProperty("code")[0].GetString().Should().Be("Type the code of the project to delete it.");
        }

        (await team.Admin.GetAsync($"/api/projects/{team.Erp}", Cancellation)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await team.App.CountAsync("SELECT COUNT(*) FROM [tickets].[tickets] WHERE Id = @id", ("@id", scene.Ticket))).Should().Be(1);
        Directory.Exists(Path.Combine(team.Files, ErpFolder)).Should().BeTrue();
    }

    [Theory(DisplayName = "Só o administrador exclui")]
    [InlineData("Manager", HttpStatusCode.Forbidden)]
    [InlineData("TechLead", HttpStatusCode.Forbidden)]
    [InlineData("Contributor", HttpStatusCode.Forbidden)]
    [InlineData("Viewer", HttpStatusCode.Forbidden)]
    [InlineData("Outside", HttpStatusCode.NotFound)]
    public async Task OnlyTheAdministrator_Deletes(string role, HttpStatusCode refused)
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var person = await team.ClientOfAsync(role);

        var summary = await person.GetAsync($"/api/projects/{team.Erp}/deletion", Cancellation);
        var deleted = await SendAsync(person, HttpMethod.Post, $"/api/projects/{team.Erp}/deletion", new { code = "ERP-FIN" });
        var project = await person.GetAsync($"/api/projects/{team.Erp}", Cancellation);
        var admin = await team.Admin.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}", Cancellation);

        summary.StatusCode.Should().Be(refused);
        deleted.StatusCode.Should().Be(refused);
        (await team.App.CountAsync("SELECT COUNT(*) FROM [projects].[projects] WHERE Id = @id", ("@id", team.Erp))).Should().Be(1);
        if (project.IsSuccessStatusCode)
        {
            (await project.Content.ReadFromJsonAsync<JsonElement>(Cancellation))
                .GetProperty("permissions").GetProperty("canDelete").GetBoolean().Should().BeFalse();
        }

        admin.GetProperty("permissions").GetProperty("canDelete").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "Resumo da exclusão")]
    public async Task TheSummary_CountsWhatTheProjectHas()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await SceneAsync(team);
        await OpenTicketAsync(team, team.Erp, "Segundo chamado");

        var summary = await team.Admin.GetFromJsonAsync<JsonElement>($"/api/projects/{team.Erp}/deletion", Cancellation);

        summary.GetProperty("code").GetString().Should().Be("ERP-FIN");
        summary.GetProperty("name").GetString().Should().Be("Implantação do ERP financeiro");
        new[] { "tickets", "documents", "links", "credentials", "acceptanceTerms", "scheduleItems", "members", "timeEntries" }
            .Select(kind => summary.GetProperty(kind).GetInt32())
            .Should().Equal(2, 1, 1, 1, 1, 2, 5, 1);
    }

    [Fact(DisplayName = "Número de chamado não reaproveitado")]
    public async Task TheNumbersOfTheDeletedTickets_AreNotGivenAgain()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var sped = await SpedAsync(team);
        await OpenTicketAsync(team, team.Erp, "Primeiro");
        var second = await OpenTicketAsync(team, team.Erp, "Segundo");
        await DeleteAsync(team, "ERP-FIN");

        var next = await OpenTicketAsync(team, sped, "Depois da exclusão");

        next.Should().Be(second + 1);
    }

    [Fact(DisplayName = "Exclusão na auditoria")]
    public async Task TheDeletion_IsOneRecordWithTheSummary()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await SceneAsync(team);
        var before = await team.App.CountAsync("SELECT COUNT(*) FROM [audit].[entries]");

        await DeleteAsync(team, "ERP-FIN");

        (await team.App.CountAsync("SELECT COUNT(*) FROM [audit].[entries]")).Should().Be(before + 1);
        var page = await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=Project&pageSize=100", Cancellation);
        var entries = page.GetProperty("items").EnumerateArray()
            .Where(entry => entry.GetProperty("itemId").GetGuid() == team.Erp).ToList();
        // The record of the creation stays; both at the same moment of the clock of the test.
        entries.Select(entry => entry.GetProperty("action").GetString()).Should().BeEquivalentTo("Deleted", "Created");
        var deletion = entries.Single(entry => entry.GetProperty("action").GetString() == "Deleted");
        deletion.GetProperty("actor").GetProperty("id").GetGuid().Should().Be(team.Paulo);
        deletion.GetProperty("itemLabel").GetString().Should().Be("Implantação do ERP financeiro");
        var fields = deletion.GetProperty("changes").EnumerateArray().ToDictionary(
            field => field.GetProperty("field").GetString()!,
            field => (field.GetProperty("before").GetString(), field.GetProperty("after").GetString()));
        fields["Code"].Should().Be(("ERP-FIN", null));
        fields["Status"].Should().Be(("Planning", null));
        new[] { "Tickets", "Documents", "Links", "Credentials", "AcceptanceTerms", "ScheduleItems", "Members" }
            .Select(kind => fields[kind].Item2)
            .Should().Equal("1", "1", "1", "1", "1", "2", "5");
    }

    /// <summary>The ids of what the scene made in the project, and the ticket.</summary>
    private sealed record Scene(Guid Ticket, IReadOnlyList<Guid> Ids);

    /// <summary>
    /// ERP-FIN with a phase, a task and a milestone with a dependency and the baseline; a ticket with an attachment, a
    /// message and a follower; a document; a link; a credential; a request of acceptance to Carla, with its PDF; and hours
    /// Carla logged on the task.
    /// </summary>
    private static async Task<Scene> SceneAsync(ProjectTeam team)
    {
        var project = $"/api/projects/{team.Erp}";
        var phase = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{project}/schedule/phases", new { name = "Homologação" }));
        var task = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{project}/schedule/items", new
        {
            kind = "Task",
            phaseId = phase,
            name = "Testes com o cliente",
            start = "2026-10-19",
            duration = 5,
            ownerId = team.Carla,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
        }));
        var milestone = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{project}/schedule/items", new
        {
            kind = "Milestone",
            phaseId = phase,
            name = "Homologação concluída",
            start = "2026-10-30",
            duration = 0,
            ownerId = (Guid?)null,
            progress = 0,
            predecessorIds = new[] { task },
        }));
        (await SendAsync(team.Admin, HttpMethod.Put, $"{project}/schedule/baseline")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var number = await OpenTicketAsync(team, team.Erp, "Rejeição 539 na emissão");
        var ticket = (await team.Admin.GetFromJsonAsync<JsonElement>($"/api/tickets/{number}", Cancellation)).GetProperty("id").GetGuid();
        var rafael = await team.ClientOfAsync(team.Rafael);
        var sent = await PostFormAsync(rafael, $"/api/tickets/{number}/messages", new Dictionary<string, string?>
        {
            ["body"] = """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"Segue o XML."}]}]}""",
        }, "nota.xml", Encoding.UTF8.GetBytes("<nfe/>"));
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync(Cancellation));

        var folders = await team.Admin.GetFromJsonAsync<JsonElement>($"{project}/documents/folders", Cancellation);
        var requirements = folders.GetProperty("folders").EnumerateArray()
            .First(folder => !folder.GetProperty("isAcceptance").GetBoolean()).GetProperty("id").GetGuid();
        var document = await IdOfAsync(await PostFormAsync(team.Admin, $"{project}/documents",
            new Dictionary<string, string?> { ["folderId"] = requirements.ToString() }, "Escopo.pdf", Encoding.ASCII.GetBytes("%PDF-1.7\nescopo\n%%EOF")));

        var category = (await team.Admin.GetFromJsonAsync<JsonElement>("/api/link-categories", Cancellation))[0].GetProperty("id").GetGuid();
        var link = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{project}/links", new
        {
            title = "Homologação",
            url = "https://homologacao.example",
            categoryId = category,
            description = (string?)null,
        }));
        var credential = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"{project}/credentials", new
        {
            portal = "ERP",
            environment = "Homologação",
            url = (string?)null,
            username = "erp.homologacao",
            password = "Senha-Ficticia-1",
            notes = (string?)null,
        }));
        var term = await IdOfAsync(await PostFormAsync(team.Admin, $"{project}/acceptances", new Dictionary<string, string?>
        {
            ["milestoneId"] = milestone.ToString(),
            ["recipientId"] = team.Carla.ToString(),
            ["title"] = "Aceite da homologação",
            ["message"] = "Assine o termo, por favor.",
        }, "termo.pdf", Encoding.ASCII.GetBytes("%PDF-1.7\ntermo\n%%EOF")));

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            team.App.Clock.GetUtcNow().UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")));
        var hours = await IdOfAsync(await SendAsync(await team.ClientOfAsync(team.Carla), HttpMethod.Post, "/api/time-entries", new
        {
            projectId = team.Erp,
            day = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            hours = 2m,
            description = "Testes com o cliente",
            scheduleItemId = task,
            ticketId = (Guid?)null,
        }));

        (await SendAsync(team.Admin, HttpMethod.Put, $"/api/admin/project-hours/{team.Erp}/contracted", new { hours = 400m }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        var additional = await IdOfAsync(await SendAsync(team.Admin, HttpMethod.Post, $"/api/admin/project-hours/{team.Erp}/additional", new
        {
            day = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            hours = 16m,
            reason = "Integração com um segundo banco",
        }));

        return new Scene(ticket, [team.Erp, phase, task, milestone, ticket, document, link, credential, term, hours, additional]);
    }

    private static async Task<int> OpenTicketAsync(ProjectTeam team, Guid projectId, string title)
    {
        var carla = await team.ClientOfAsync(team.Carla);
        var opened = await SendAsync(carla, HttpMethod.Post, "/api/tickets", new
        {
            projectId,
            team = "Functional",
            priority = "Medium",
            title,
            description = "Descrição do chamado.",
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        return (await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
    }

    /// <summary>Another project, SPED-27, where Carla opens tickets too.</summary>
    private static async Task<Guid> SpedAsync(ProjectTeam team)
    {
        var sped = await team.CreateProjectAsync("SPED-27", "Entrega do SPED");
        await team.App.AddProjectMemberAsync(sped, team.Carla, "Contributor");
        return sped;
    }

    private static Task<HttpResponseMessage> DeleteAsync(ProjectTeam team, string? code) =>
        SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{team.Erp}/deletion", new { code });

    private static async Task<Guid> ClientIdAsync(ProjectTeam team)
    {
        var created = await SendAsync(team.Admin, HttpMethod.Post, "/api/clients", new { name = "Alfa Distribuidora", cnpj = (string?)null, observation = (string?)null });
        var clientId = await IdOfAsync(created);
        (await SendAsync(team.Admin, HttpMethod.Put, $"/api/projects/{team.Erp}", new
        {
            code = "ERP-FIN",
            name = "Implantação do ERP financeiro",
            description = (string?)null,
            clientId,
        })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        return clientId;
    }

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string path, IReadOnlyDictionary<string, string?> fields, string fileName, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        foreach (var (name, value) in fields)
        {
            if (value is not null)
            {
                form.Add(new StringContent(value, Encoding.UTF8), name);
            }
        }

        form.Add(new ByteArrayContent(content), fileName.EndsWith(".xml", StringComparison.Ordinal) ? "files" : "file", fileName);
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = form };
        request.Headers.Add("X-CSRF", "1");
        return await client.SendAsync(request, Cancellation);
    }

    /// <summary>
    /// Every column of an id of every table outside the audit, as "schema.table.column", that has any of the ids: the
    /// audit keeps the records of before (ADR-0039).
    /// </summary>
    private static async Task<IReadOnlyList<string>> FindAsync(SessionApp app, IReadOnlyList<Guid> ids)
    {
        await using var connection = new SqlConnection(app.ConnectionString);
        await connection.OpenAsync(Cancellation);
        var columns = new List<(string Schema, string Table, string Column)>();
        await using (var list = connection.CreateCommand())
        {
            list.CommandText =
                "SELECT c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS c "
                + "JOIN INFORMATION_SCHEMA.TABLES t ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME "
                + "WHERE t.TABLE_TYPE = 'BASE TABLE' AND c.TABLE_SCHEMA <> 'audit' AND c.DATA_TYPE = 'uniqueidentifier'";
            await using var reader = await list.ExecuteReaderAsync(Cancellation);
            while (await reader.ReadAsync(Cancellation))
            {
                columns.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
            }
        }

        var found = new List<string>();
        var values = string.Join(", ", ids.Select((_, index) => $"@id{index}"));
        foreach (var (schema, table, column) in columns)
        {
            await using var search = connection.CreateCommand();
            search.CommandText = $"SELECT COUNT(*) FROM [{schema}].[{table}] WHERE [{column}] IN ({values})";
            for (var index = 0; index < ids.Count; index++)
            {
                search.Parameters.AddWithValue($"@id{index}", ids[index]);
            }

            if ((int)(await search.ExecuteScalarAsync(Cancellation))! > 0)
            {
                found.Add($"{schema}.{table}.{column}");
            }
        }

        return found;
    }
}
