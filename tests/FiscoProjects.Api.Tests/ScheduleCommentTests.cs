using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using static FiscoProjects.Api.Tests.ProjectTeam;

namespace FiscoProjects.Api.Tests;

// Spec: cronograma, requirements "Comentários do item", "Comentar um item", "Apagar um comentário", "Quantidade de
// comentários" and "Comentários fora da auditoria"; avisos, "Aviso de menção num comentário" and "Aviso de comentário ao
// responsável"; dados-pessoais, "Menções dos comentários na anonimização" (ADR-0157).
public sealed class ScheduleCommentTests(SqlServerFixture sqlServer)
{
    private const string TaskName = "Parametrizar regras de ICMS";
    private const string CarlaEmail = "carla.mendes@empresa.example";
    private const string DiegoEmail = "diego.torres@empresa.example";

    /// <summary>
    /// The team of ERP-FIN (<see cref="ProjectTeam"/>) with the phase Configuração, the task of Carla and a milestone, both
    /// included by Marina.
    /// </summary>
    private sealed record Scene(ProjectTeam Team, Guid Task, Guid Milestone) : IAsyncDisposable
    {
        public string Schedule => $"/api/projects/{Team.Erp}/schedule";

        public string CommentsOf(Guid item) => $"{Schedule}/items/{item}/comments";

        /// <summary>A comment of the person on the item, which has to be saved.</summary>
        public async Task<Guid> CommentAsync(Guid person, Guid item, string text, Guid? mention = null, string? label = null)
        {
            var client = await Team.ClientOfAsync(person);
            var added = await SendAsync(client, HttpMethod.Post, CommentsOf(item), new { body = Body(text, mention, label) });
            added.StatusCode.Should().Be(HttpStatusCode.Created, await added.Content.ReadAsStringAsync(Cancellation));
            return (await added.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        }

        /// <summary>The comments of the item, as the person reads them.</summary>
        public async Task<JsonElement> ListAsync(Guid person, Guid item) =>
            await (await Team.ClientOfAsync(person)).GetFromJsonAsync<JsonElement>(CommentsOf(item), Cancellation);

        public ValueTask DisposeAsync() => Team.DisposeAsync();
    }

    private async Task<Scene> StartAsync()
    {
        var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        var phase = await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"/api/projects/{team.Erp}/schedule/phases", new { name = "Configuração" }));
        var task = await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"/api/projects/{team.Erp}/schedule/items", new
        {
            kind = "Task",
            phaseId = phase,
            name = TaskName,
            start = "2026-10-26",
            duration = 3,
            ownerId = team.Carla,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
        }));
        var milestone = await IdOfAsync(await SendAsync(marina, HttpMethod.Post, $"/api/projects/{team.Erp}/schedule/items", new
        {
            kind = "Milestone",
            phaseId = phase,
            name = "Configuração aprovada",
            start = "2026-10-30",
            duration = 0,
            progress = 0,
            predecessorIds = Array.Empty<Guid>(),
        }));
        // What the project sent so far (the inclusions and the task of Carla) is out of the way.
        await team.App.EmailsAsync();
        return new Scene(team, task, milestone);
    }

    /// <summary>The document of the editor with a paragraph: the mention first, when there is one, then the text.</summary>
    private static string Body(string text, Guid? mention = null, string? label = null)
    {
        var content = new List<object>();
        if (mention is { } person)
        {
            content.Add(new { type = "mention", attrs = new { id = person.ToString(), label = label ?? "Pessoa" } });
            content.Add(new { type = "text", text = " " });
        }

        if (text.Length > 0)
        {
            content.Add(new { type = "text", text });
        }

        return JsonSerializer.Serialize(new { type = "doc", content = new[] { new { type = "paragraph", content } } });
    }

    /// <summary>The e-mails of the comments: a mention or a new comment (the queue keeps what the project sent before).</summary>
    private static bool IsOfAComment(string subject) =>
        subject.Contains("mencionou você em", StringComparison.Ordinal) || subject.StartsWith("Novo comentário", StringComparison.Ordinal);

    private static IEnumerable<JsonElement> Items(JsonElement list) => list.GetProperty("items").EnumerateArray();

    private static string TextOf(JsonElement comment) => comment.GetProperty("body").GetString()!;

    [Fact(DisplayName = "Ver os comentários")]
    public async Task TheViewer_ReadsTheComments_TheOldestFirst()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        await scene.CommentAsync(team.Carla, scene.Task, "Mandei o plano de contas.");
        team.App.Clock.Advance(TimeSpan.FromMinutes(5));
        await scene.CommentAsync(team.Diego, scene.Task, "Recebi, vou importar hoje.");

        var list = await scene.ListAsync(team.Lucas, scene.Task);

        var comments = Items(list).ToList();
        comments.Select(comment => comment.GetProperty("author").GetProperty("name").GetString())
            .Should().Equal("Carla Mendes", "Diego Torres");
        TextOf(comments[0]).Should().Contain("Mandei o plano de contas.");
        comments.Should().OnlyContain(comment => !comment.GetProperty("canRemove").GetBoolean());
        list.GetProperty("canComment").GetBoolean().Should().BeFalse();
        // A milestone has comments of its own; an item of another project, none.
        Items(await scene.ListAsync(team.Lucas, scene.Milestone)).Should().BeEmpty();
        (await (await team.ClientOfAsync(team.Carla)).GetAsync(scene.CommentsOf(Guid.CreateVersion7()), Cancellation))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Comentário do item excluído")]
    public async Task ARemovedItem_BringsItsComments_BackWithTheUndo()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        await scene.CommentAsync(team.Carla, scene.Task, "Mandei o plano de contas.");
        var marina = await team.ClientOfAsync(team.Marina);

        (await SendAsync(marina, HttpMethod.Delete, $"{scene.Schedule}/items/{scene.Task}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await marina.GetAsync(scene.CommentsOf(scene.Task), Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(marina, HttpMethod.Post, $"{scene.Schedule}/undo")).StatusCode.Should().Be(HttpStatusCode.OK);

        Items(await scene.ListAsync(team.Marina, scene.Task)).Should().ContainSingle()
            .Which.GetProperty("author").GetProperty("name").GetString().Should().Be("Carla Mendes");
    }

    [Fact(DisplayName = "Comentar com menção")]
    public async Task AComment_KeepsTheMention()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;

        await scene.CommentAsync(team.Carla, scene.Task, "pode importar o plano?", team.Diego, "Diego Torres");

        var comment = Items(await scene.ListAsync(team.Carla, scene.Task)).Should().ContainSingle().Subject;
        var body = JsonDocument.Parse(TextOf(comment)).RootElement;
        var mention = body.GetProperty("content")[0].GetProperty("content")[0];
        (mention.GetProperty("type").GetString(), mention.GetProperty("attrs").GetProperty("id").GetGuid())
            .Should().Be(("mention", team.Diego));
        comment.GetProperty("canRemove").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "Menção fora do projeto")]
    public async Task AMentionOfSomeoneOutside_IsRefused_AndNothingIsSaved()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        var carla = await team.ClientOfAsync(team.Carla);

        var refused = await SendAsync(carla, HttpMethod.Post, scene.CommentsOf(scene.Task), new { body = Body("veja", team.Sofia, "Sofia Martins") });
        var empty = await SendAsync(carla, HttpMethod.Post, scene.CommentsOf(scene.Task), new { body = Body("") });
        var notJson = await SendAsync(carla, HttpMethod.Post, scene.CommentsOf(scene.Task), new { body = "texto solto" });

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ProblemTypeAsync(refused)).Should().Be("urn:fiscoprojects:problem:comment-mention-refused");
        empty.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await empty.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").TryGetProperty("body", out _).Should().BeTrue();
        notJson.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        Items(await scene.ListAsync(team.Carla, scene.Task)).Should().BeEmpty();
    }

    [Fact(DisplayName = "Leitor não comenta")]
    public async Task TheViewer_DoesNotComment()
    {
        await using var scene = await StartAsync();
        var lucas = await scene.Team.ClientOfAsync(scene.Team.Lucas);

        (await SendAsync(lucas, HttpMethod.Post, scene.CommentsOf(scene.Task), new { body = Body("ok") }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await scene.ListAsync(scene.Team.Lucas, scene.Task)).GetProperty("canComment").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "Projeto encerrado sem comentários novos")]
    public async Task AClosedProject_KeepsTheComments_AndTakesNoChange()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        var comment = await scene.CommentAsync(team.Carla, scene.Task, "Mandei o plano de contas.");
        (await SendAsync(team.Admin, HttpMethod.Put, $"/api/projects/{team.Erp}/status", new { status = "Cancelled" }))
            .IsSuccessStatusCode.Should().BeTrue();
        var marina = await team.ClientOfAsync(team.Marina);

        var added = await SendAsync(marina, HttpMethod.Post, scene.CommentsOf(scene.Task), new { body = Body("ok") });
        var removed = await SendAsync(marina, HttpMethod.Delete, $"{scene.CommentsOf(scene.Task)}/{comment}");

        added.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProblemTypeAsync(added)).Should().Be("urn:fiscoprojects:problem:project-closed");
        removed.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var list = await scene.ListAsync(team.Marina, scene.Task);
        list.GetProperty("canComment").GetBoolean().Should().BeFalse();
        Items(list).Should().ContainSingle().Which.GetProperty("canRemove").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "Apagar o próprio comentário")]
    public async Task WhoWrote_RemovesTheOwnComment()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        var comment = await scene.CommentAsync(team.Carla, scene.Task, "Mandei o plano de contas.");
        var carla = await team.ClientOfAsync(team.Carla);

        (await SendAsync(carla, HttpMethod.Delete, $"{scene.CommentsOf(scene.Task)}/{comment}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        Items(await scene.ListAsync(team.Carla, scene.Task)).Should().BeEmpty();
        (await SendAsync(carla, HttpMethod.Delete, $"{scene.CommentsOf(scene.Task)}/{comment}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Gerente apaga qualquer comentário")]
    public async Task TheManager_RemovesAnyComment()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        var comment = await scene.CommentAsync(team.Diego, scene.Task, "Recebi, vou importar hoje.");
        (await scene.ListAsync(team.Marina, scene.Task)).GetProperty("items")[0].GetProperty("canRemove").GetBoolean().Should().BeTrue();

        (await SendAsync(await team.ClientOfAsync(team.Marina), HttpMethod.Delete, $"{scene.CommentsOf(scene.Task)}/{comment}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        Items(await scene.ListAsync(team.Diego, scene.Task)).Should().BeEmpty();
    }

    [Fact(DisplayName = "Apagar o comentário de outra pessoa")]
    public async Task AContributor_DoesNotRemoveTheCommentOfSomeoneElse()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        var comment = await scene.CommentAsync(team.Diego, scene.Task, "Recebi, vou importar hoje.");

        (await SendAsync(await team.ClientOfAsync(team.Carla), HttpMethod.Delete, $"{scene.CommentsOf(scene.Task)}/{comment}"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        Items(await scene.ListAsync(team.Carla, scene.Task)).Should().ContainSingle()
            .Which.GetProperty("canRemove").GetBoolean().Should().BeFalse();
    }

    [Theory(DisplayName = "Permissões nos comentários por papel")]
    [InlineData("Administrator", HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent)]
    [InlineData("Manager", HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent)]
    [InlineData("TechLead", HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.Forbidden)]
    [InlineData("Contributor", HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.Forbidden)]
    [InlineData("Developer", HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.Forbidden)]
    [InlineData("Viewer", HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden)]
    [InlineData("Outside", HttpStatusCode.NotFound, HttpStatusCode.NotFound, HttpStatusCode.NotFound)]
    public async Task Permissions_FollowTheRoles(string role, HttpStatusCode see, HttpStatusCode add, HttpStatusCode removeOther)
    {
        // A comment of Rafael, the tech lead, removed by each role; the tech lead removes the own one in its own case.
        await using var scene = await StartAsync();
        var team = scene.Team;
        var other = await scene.CommentAsync(role == "TechLead" ? team.Diego : team.Rafael, scene.Task, "Ajustei a regra.");
        var client = await team.ClientOfAsync(role);

        (await client.GetAsync(scene.CommentsOf(scene.Task), Cancellation)).StatusCode.Should().Be(see);
        var added = await SendAsync(client, HttpMethod.Post, scene.CommentsOf(scene.Task), new { body = Body("Conferido.") });
        added.StatusCode.Should().Be(add);
        (await SendAsync(client, HttpMethod.Delete, $"{scene.CommentsOf(scene.Task)}/{other}")).StatusCode.Should().Be(removeOther);

        if (add == HttpStatusCode.Created)
        {
            // Who comments removes the own comment.
            var own = (await added.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
            (await SendAsync(client, HttpMethod.Delete, $"{scene.CommentsOf(scene.Task)}/{own}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }

    [Fact(DisplayName = "Contagem na Lista e no Quadro")]
    public async Task TheSchedule_SaysHowManyCommentsEachItemHas()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        await scene.CommentAsync(team.Carla, scene.Task, "Um.");
        await scene.CommentAsync(team.Diego, scene.Task, "Dois.");
        await scene.CommentAsync(team.Marina, scene.Task, "Três.");

        var schedule = await (await team.ClientOfAsync(team.Lucas)).GetFromJsonAsync<JsonElement>(scene.Schedule, Cancellation);

        var items = schedule.GetProperty("phases")[0].GetProperty("items").EnumerateArray()
            .ToDictionary(item => item.GetProperty("id").GetGuid(), item => item.GetProperty("commentCount").GetInt32());
        items.Should().BeEquivalentTo(new Dictionary<Guid, int> { [scene.Task] = 3, [scene.Milestone] = 0 });
    }

    [Fact(DisplayName = "Comentário fora da auditoria")]
    public async Task TheComments_StayOutOfTheAudit()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        var before = await team.App.CountAsync("SELECT COUNT(*) FROM [audit].[entries]");

        var comment = await scene.CommentAsync(team.Carla, scene.Task, "Mandei o plano de contas.");
        (await SendAsync(await team.ClientOfAsync(team.Carla), HttpMethod.Delete, $"{scene.CommentsOf(scene.Task)}/{comment}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await team.App.CountAsync("SELECT COUNT(*) FROM [audit].[entries]")).Should().Be(before);
    }

    [Fact(DisplayName = "Projeto excluído com comentários")]
    public async Task ADeletedProject_TakesItsComments()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        await scene.CommentAsync(team.Carla, scene.Task, "Mandei o plano de contas.");
        await scene.CommentAsync(team.Diego, scene.Milestone, "Combinado.");

        (await SendAsync(team.Admin, HttpMethod.Post, $"/api/projects/{team.Erp}/deletion", new { code = "ERP-FIN" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await team.App.CountAsync("SELECT COUNT(*) FROM [projects].[schedule_comments]")).Should().Be(0);
    }

    [Fact(DisplayName = "Menção no comentário")]
    public async Task TheMentionedPerson_HearsAboutItNow()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;

        await scene.CommentAsync(team.Carla, scene.Milestone, "pode conferir a configuração?", team.Diego, "Diego Torres");

        var email = (await team.App.EmailsToAsync(DiegoEmail)).Should()
            .ContainSingle(email => email.Subject.Contains("mencionou você", StringComparison.Ordinal)).Subject;
        email.Subject.Should().Be("Carla Mendes mencionou você em Configuração aprovada");
        email.TextBody.Should().Contain("Carla Mendes mencionou você num comentário de Configuração aprovada, do projeto ERP-FIN.");
        email.HtmlBody.Should().Contain($"href=\"http://localhost:5173/projects/{team.Erp}/schedule?item={scene.Milestone}\"");
        var bell = (await (await team.ClientOfAsync(team.Diego)).GetFromJsonAsync<JsonElement>("/api/notifications", Cancellation))
            .GetProperty("items").EnumerateArray().Should().ContainSingle(item => item.GetProperty("kind").GetString() == "TaskMentioned").Subject;
        bell.GetProperty("actor").GetString().Should().Be("Carla Mendes");
        bell.GetProperty("path").GetString().Should().Be($"/projects/{team.Erp}/schedule?item={scene.Milestone}");
        bell.GetProperty("values").GetProperty("item").GetString().Should().Be("Configuração aprovada");
    }

    [Fact(DisplayName = "Menção no Teams")]
    public async Task TheMention_GoesToTheActivityOfTeams()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        await team.App.TurnTeamsOnAsync();
        await team.App.ChooseNoticesAsync(team.Diego, email: true, teams: true);

        await scene.CommentAsync(team.Carla, scene.Milestone, "pode conferir?", team.Diego, "Diego Torres");

        var sent = await team.App.TeamsAsync(deliveries => deliveries.Any(delivery => delivery.UserId == team.Diego));
        var message = sent.Should().ContainSingle(delivery => delivery.UserId == team.Diego).Subject.Message;
        message.ActivityType.Should().Be("taskMentioned");
        message.Parameters.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["person"] = "Carla Mendes",
            ["item"] = "Configuração aprovada",
        });
        message.Path.Should().Be($"/projects/{team.Erp}/schedule?item={scene.Milestone}");
    }

    [Fact(DisplayName = "Quem se menciona não recebe")]
    public async Task WhoMentionsThemself_HearsNothing()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;

        await scene.CommentAsync(team.Carla, scene.Milestone, "lembrete para mim", team.Carla, "Carla Mendes");

        (await team.App.EmailsAsync()).Select(email => email.Subject).Where(IsOfAComment).Should().BeEmpty();
    }

    [Fact(DisplayName = "Comentário na tarefa do responsável")]
    public async Task TheOwner_HearsAboutANewComment()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;

        await scene.CommentAsync(team.Marina, scene.Task, "O cliente mandou os arquivos.");

        var email = (await team.App.EmailsToAsync(CarlaEmail)).Should().ContainSingle(email => email.Subject.StartsWith("Novo comentário", StringComparison.Ordinal)).Subject;
        email.Subject.Should().Be($"Novo comentário na tarefa {TaskName}");
        email.TextBody.Should().Contain($"Marina Costa comentou na tarefa {TaskName}, do projeto ERP-FIN, de que você é responsável.");
        (await (await team.ClientOfAsync(team.Carla)).GetFromJsonAsync<JsonElement>("/api/notifications", Cancellation))
            .GetProperty("items").EnumerateArray().Should().ContainSingle(item => item.GetProperty("kind").GetString() == "TaskCommented");
    }

    [Fact(DisplayName = "Responsável mencionado")]
    public async Task TheMentionedOwner_HearsOnlyAboutTheMention()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;

        await scene.CommentAsync(team.Marina, scene.Task, "o cliente mandou os arquivos.", team.Carla, "Carla Mendes");

        (await team.App.EmailsToAsync(CarlaEmail)).Select(email => email.Subject).Where(IsOfAComment)
            .Should().Equal($"Marina Costa mencionou você em {TaskName}");
    }

    [Fact(DisplayName = "Responsável comenta")]
    public async Task TheOwnerWhoComments_HearsNothing()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;

        await scene.CommentAsync(team.Carla, scene.Task, "Comecei hoje.");

        (await team.App.EmailsAsync()).Select(email => email.Subject).Where(IsOfAComment).Should().BeEmpty();
    }

    [Fact(DisplayName = "Menção no comentário anonimizada")]
    public async Task AnAnonymizedPerson_LeavesTheMentionsOfTheComments()
    {
        await using var scene = await StartAsync();
        var team = scene.Team;
        await scene.CommentAsync(team.Carla, scene.Task, "pode importar o plano?", team.Diego, "Diego Torres");
        await scene.CommentAsync(team.Diego, scene.Task, "Importei.");

        (await SendAsync(team.Admin, HttpMethod.Post, $"/api/users/{team.Diego}/deactivate")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SendAsync(team.Admin, HttpMethod.Post, $"/api/users/{team.Diego}/anonymization")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await team.App.CountAsync(
            "SELECT COUNT(*) FROM [projects].[schedule_comments] WHERE Body LIKE '%Diego%' OR Text LIKE '%Diego%'")).Should().Be(0);
        var comments = Items(await scene.ListAsync(team.Carla, scene.Task)).ToList();
        comments.Should().HaveCount(2);
        var mention = comments.Should().ContainSingle(comment => comment.GetProperty("author").GetProperty("id").GetGuid() == team.Carla).Subject;
        TextOf(mention).Should().NotContain("Diego").And.Contain(team.Diego.ToString());
        var own = comments.Should().ContainSingle(comment => comment.GetProperty("author").GetProperty("id").GetGuid() == team.Diego).Subject;
        own.GetProperty("author").GetProperty("name").GetString().Should().NotContain("Diego");
    }

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Cancellation));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }
}
