using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: links (API side): "Categorias de links", "Links do projeto", "Busca nos links" and "Permissões nos links"
// (ADR-0032, ADR-0045, ADR-0092).
public sealed class LinkTests(SqlServerFixture sqlServer)
{
    private const string Categories = "/api/admin/link-categories";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static string Path(Guid project) => $"/api/projects/{project}/links";

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    private static async Task<Guid> CategoryAsync(HttpClient client, string name) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/link-categories", Cancellation)).EnumerateArray()
            .Single(category => category.GetProperty("name").GetString() == name).GetProperty("id").GetGuid();

    private static async Task<JsonElement> ListAsync(HttpClient client, Guid project, string query = "") =>
        await client.GetFromJsonAsync<JsonElement>($"{Path(project)}?{query}", Cancellation);

    private static async Task<List<string>> TitlesAsync(HttpClient client, Guid project, string query = "") =>
        [.. (await ListAsync(client, project, query)).GetProperty("items").EnumerateArray().Select(link => link.GetProperty("title").GetString()!)];

    private static Task<HttpResponseMessage> AddAsync(HttpClient client, Guid project, string title, string url, Guid category, string? description = null) =>
        SendAsync(client, HttpMethod.Post, Path(project), new { title, url, categoryId = category, description });

    private static async Task<Guid> AddedAsync(HttpClient client, Guid project, string title, string url, Guid category, string? description = null)
    {
        var added = await AddAsync(client, project, title, url, category, description);
        added.StatusCode.Should().Be(HttpStatusCode.Created, await added.Content.ReadAsStringAsync(Cancellation));
        return (await added.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    [Fact(DisplayName = "Incluir categoria de link")]
    public async Task AddCategory_ShowsActiveAndForNewLinks()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        var added = await SendAsync(team.Admin, HttpMethod.Post, Categories, new { name = "Documentação" });

        added.StatusCode.Should().Be(HttpStatusCode.Created);
        var all = await team.Admin.GetFromJsonAsync<JsonElement>(Categories, Cancellation);
        all.EnumerateArray().Select(category => category.GetProperty("name").GetString())
            .Should().Equal("Ambientes", "Documentação", "Outros", "Painéis", "Repositórios", "Sistemas");
        all.EnumerateArray().Should().OnlyContain(category => category.GetProperty("isActive").GetBoolean());
        var carla = await team.ClientOfAsync(team.Carla);
        (await CategoryAsync(carla, "Documentação")).Should().NotBeEmpty();
    }

    [Fact(DisplayName = "Nome de categoria repetido")]
    public async Task RepeatedCategoryName_IsAConflict()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);

        var added = await SendAsync(team.Admin, HttpMethod.Post, Categories, new { name = "painéis" });

        added.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(added)).Should().Be("urn:fiscoprojects:problem:link-category-name-in-use");
        (await team.Admin.GetFromJsonAsync<JsonElement>(Categories, Cancellation)).GetArrayLength().Should().Be(5);
    }

    [Fact(DisplayName = "Categoria inativa não aparece para novos links")]
    public async Task InactiveCategory_IsNotForNewLinks()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);
        var panels = await CategoryAsync(team.Admin, "Painéis");
        var old = await AddedAsync(carla, team.Erp, "Painel de faturamento", "https://bi.cliente.example/faturamento", panels);

        (await SendAsync(team.Admin, HttpMethod.Put, $"{Categories}/{panels}", new { name = "Painéis", isActive = false }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await carla.GetFromJsonAsync<JsonElement>("/api/link-categories", Cancellation)).EnumerateArray()
            .Select(category => category.GetProperty("name").GetString()).Should().NotContain("Painéis");
        var added = await AddAsync(carla, team.Erp, "Painel de estoque", "https://bi.cliente.example/estoque", panels);
        added.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await added.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").TryGetProperty("categoryId", out _).Should().BeTrue();
        var kept = await SendAsync(carla, HttpMethod.Put, $"{Path(team.Erp)}/{old}", new
        {
            title = "Painel de faturamento (BI)",
            url = "https://bi.cliente.example/faturamento",
            categoryId = panels,
        });
        kept.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var link = (await ListAsync(carla, team.Erp)).GetProperty("items").EnumerateArray().Single();
        (link.GetProperty("title").GetString(), link.GetProperty("categoryName").GetString()).Should().Be(("Painel de faturamento (BI)", "Painéis"));
    }

    [Fact(DisplayName = "Categorias só do administrador")]
    public async Task Categories_AreOnlyTheAdministrators()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);

        (await SendAsync(marina, HttpMethod.Post, Categories, new { name = "Documentação" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await marina.GetAsync(Categories, Cancellation)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Incluir link")]
    public async Task AddLink_ShowsInItsCategoryWithWhoAndWhen()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var diego = await team.ClientOfAsync(team.Diego);
        var environments = await CategoryAsync(diego, "Ambientes");

        await AddedAsync(diego, team.Erp, "ERP · Homologação", "https://hml-erp.cliente.example", environments, "Testes integrados com o time fiscal.");

        var list = await ListAsync(diego, team.Erp);
        var link = list.GetProperty("items").EnumerateArray().Single();
        link.GetProperty("title").GetString().Should().Be("ERP · Homologação");
        link.GetProperty("url").GetString().Should().Be("https://hml-erp.cliente.example");
        link.GetProperty("description").GetString().Should().Be("Testes integrados com o time fiscal.");
        link.GetProperty("categoryName").GetString().Should().Be("Ambientes");
        link.GetProperty("created").GetProperty("name").GetString().Should().Be("Diego Torres");
        link.GetProperty("created").GetProperty("at").GetDateTime().Should().Be(team.App.Clock.GetUtcNow().UtcDateTime);
        list.GetProperty("total").GetInt32().Should().Be(1);
        list.GetProperty("canEdit").GetBoolean().Should().BeTrue();
        var audit = await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation);
        audit.GetProperty("items").EnumerateArray().Should().Contain(entry =>
            entry.GetProperty("itemLabel").GetString() == "ERP · Homologação" && entry.GetProperty("action").GetString() == "Created");
    }

    [Fact(DisplayName = "Link sem título ou com endereço inválido")]
    public async Task LinkWithoutTitleOrWithABadAddress_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);
        var systems = await CategoryAsync(carla, "Sistemas");

        var untitled = await AddAsync(carla, team.Erp, " ", "https://erp.cliente.example", systems);
        var ftp = await AddAsync(carla, team.Erp, "Arquivos", "ftp://arquivos.example", systems);
        var script = await AddAsync(carla, team.Erp, "Script", "javascript:alert(1)", systems);

        new[] { untitled.StatusCode, ftp.StatusCode, script.StatusCode }.Should().AllBeEquivalentTo(HttpStatusCode.BadRequest);
        (await untitled.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").TryGetProperty("title", out _).Should().BeTrue();
        (await ftp.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").TryGetProperty("url", out _).Should().BeTrue();
        (await TitlesAsync(carla, team.Erp)).Should().BeEmpty();
    }

    [Fact(DisplayName = "Editar link")]
    public async Task ChangeLink_ShowsTheNewTitleAndDescription()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);
        var systems = await CategoryAsync(carla, "Sistemas");
        var id = await AddedAsync(team.Admin, team.Erp, "ERP", "https://erp.cliente.example", systems);

        var changed = await SendAsync(carla, HttpMethod.Put, $"{Path(team.Erp)}/{id}", new
        {
            title = "ERP · Produção",
            url = "https://erp.cliente.example",
            categoryId = systems,
            description = "Ambiente usado pelo cliente.",
        });

        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var link = (await ListAsync(carla, team.Erp)).GetProperty("items").EnumerateArray().Single();
        (link.GetProperty("title").GetString(), link.GetProperty("description").GetString()).Should().Be(("ERP · Produção", "Ambiente usado pelo cliente."));
        link.GetProperty("created").GetProperty("name").GetString().Should().Be("Paulo Nogueira", "who included it stays");
    }

    [Fact(DisplayName = "Excluir link")]
    public async Task RemoveLink_LeavesTheList()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var rafael = await team.ClientOfAsync(team.Rafael);
        var repositories = await CategoryAsync(rafael, "Repositórios");
        var id = await AddedAsync(rafael, team.Erp, "Integrações fiscais", "https://git.empresa.example/fiscal/integracoes", repositories);

        (await SendAsync(rafael, HttpMethod.Delete, $"{Path(team.Erp)}/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await ListAsync(rafael, team.Erp);
        list.GetProperty("items").GetArrayLength().Should().Be(0);
        list.GetProperty("categories").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "Buscar por título e endereço")]
    public async Task Search_LooksInTheTitleAndTheAddress()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var environments = await CategoryAsync(team.Admin, "Ambientes");
        var systems = await CategoryAsync(team.Admin, "Sistemas");
        await AddedAsync(team.Admin, team.Erp, "ERP · Homologação", "https://erp.cliente.example/qa", environments);
        await AddedAsync(team.Admin, team.Erp, "Portal fiscal", "https://hml-portal.cliente.example", environments);
        await AddedAsync(team.Admin, team.Erp, "ERP · Produção", "https://erp.cliente.example", systems);
        var lucas = await team.ClientOfAsync(team.Lucas);

        (await TitlesAsync(lucas, team.Erp, "search=HOMOLOGACAO")).Should().Equal("ERP · Homologação");
        (await TitlesAsync(lucas, team.Erp, "search=hml")).Should().Equal("Portal fiscal");
        var list = await ListAsync(lucas, team.Erp, "search=hml");
        list.GetProperty("total").GetInt32().Should().Be(3, "the total for the tab does not follow the search");
        list.GetProperty("categories").EnumerateArray().Select(category => (category.GetProperty("name").GetString(), category.GetProperty("count").GetInt32()))
            .Should().Equal(("Ambientes", 2), ("Sistemas", 1));
    }

    [Fact(DisplayName = "Filtrar por categoria")]
    public async Task Filter_ShowsOnlyTheCategory()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var repositories = await CategoryAsync(team.Admin, "Repositórios");
        var systems = await CategoryAsync(team.Admin, "Sistemas");
        await AddedAsync(team.Admin, team.Erp, "Integrações fiscais", "https://git.empresa.example/fiscal/integracoes", repositories);
        await AddedAsync(team.Admin, team.Erp, "Conectores", "https://git.empresa.example/fiscal/conectores", repositories);
        await AddedAsync(team.Admin, team.Erp, "ERP · Produção", "https://erp.cliente.example", systems);

        (await TitlesAsync(team.Admin, team.Erp, $"categoryId={repositories}")).Should().Equal("Conectores", "Integrações fiscais");
    }

    [Theory(DisplayName = "Permissões nos links por papel")]
    [InlineData("Administrator", true)]
    [InlineData("Manager", true)]
    [InlineData("TechLead", true)]
    [InlineData("Contributor", true)]
    [InlineData("Developer", true)]
    [InlineData("Viewer", false)]
    public async Task Permissions_FollowTheMatrix(string role, bool edits)
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var systems = await CategoryAsync(team.Admin, "Sistemas");
        var existing = await AddedAsync(team.Admin, team.Erp, "ERP", "https://erp.cliente.example", systems);
        var client = await team.ClientOfAsync(role);

        var list = await ListAsync(client, team.Erp);
        var added = await AddAsync(client, team.Erp, $"Link de {role}", "https://erp.cliente.example/novo", systems);
        var changed = await SendAsync(client, HttpMethod.Put, $"{Path(team.Erp)}/{existing}", new
        {
            title = "ERP · Produção",
            url = "https://erp.cliente.example",
            categoryId = systems,
        });
        var removed = await SendAsync(client, HttpMethod.Delete, $"{Path(team.Erp)}/{existing}");

        list.GetProperty("items").GetArrayLength().Should().Be(1, "everyone in the project sees the links");
        list.GetProperty("canEdit").GetBoolean().Should().Be(edits);
        (added.StatusCode, changed.StatusCode, removed.StatusCode).Should().Be(edits
            ? (HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.NoContent)
            : (HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden));
    }

    [Fact(DisplayName = "Links de um projeto de fora")]
    public async Task ProjectOfOthers_IsNotFound()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var systems = await CategoryAsync(team.Admin, "Sistemas");
        var id = await AddedAsync(team.Admin, team.Erp, "ERP", "https://erp.cliente.example", systems);
        var sofia = await team.ClientOfAsync(team.Sofia);

        (await sofia.GetAsync(Path(team.Erp), Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await AddAsync(sofia, team.Erp, "Intruso", "https://intruso.example", systems)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(sofia, HttpMethod.Delete, $"{Path(team.Erp)}/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Links de projeto encerrado")]
    public async Task ClosedProject_IsReadOnly()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        var systems = await CategoryAsync(marina, "Sistemas");
        var id = await AddedAsync(marina, team.Erp, "ERP", "https://erp.cliente.example", systems);
        (await SendAsync(marina, HttpMethod.Put, $"/api/projects/{team.Erp}/status", new { status = "Cancelled" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var added = await AddAsync(marina, team.Erp, "Portal", "https://portal.cliente.example", systems);
        var removed = await SendAsync(marina, HttpMethod.Delete, $"{Path(team.Erp)}/{id}");

        new[] { added.StatusCode, removed.StatusCode }.Should().AllBeEquivalentTo(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(added)).Should().Be("urn:fiscoprojects:problem:project-closed");
        var list = await ListAsync(marina, team.Erp);
        list.GetProperty("canEdit").GetBoolean().Should().BeFalse();
        list.GetProperty("items").GetArrayLength().Should().Be(1);
    }
}
