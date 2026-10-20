using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// Spec: acessos (API side): "Acessos do projeto", "Senha cifrada", "Senha mascarada", "Busca nos acessos" and
// "Permissões nos acessos" (ADR-0033, ADR-0034, ADR-0045, ADR-0092).
public sealed class CredentialTests(SqlServerFixture sqlServer)
{
    private const string Password = "Kx7#pa2V!q";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static string Path(Guid project) => $"/api/projects/{project}/credentials";

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null) =>
        ProjectTeam.SendAsync(client, method, path, body);

    private static Task<HttpResponseMessage> AddAsync(
        HttpClient client, Guid project, string portal, string environment, string username, string? password = Password,
        string? url = null, string? notes = null) =>
        SendAsync(client, HttpMethod.Post, Path(project), new { portal, environment, url, username, password, notes });

    private static async Task<Guid> AddedAsync(
        HttpClient client, Guid project, string portal, string environment, string username, string? password = Password,
        string? url = null, string? notes = null)
    {
        var added = await AddAsync(client, project, portal, environment, username, password, url, notes);
        added.StatusCode.Should().Be(HttpStatusCode.Created, await added.Content.ReadAsStringAsync(Cancellation));
        return (await added.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> ListAsync(HttpClient client, Guid project, string query = "") =>
        await client.GetFromJsonAsync<JsonElement>($"{Path(project)}?{query}", Cancellation);

    private static async Task<List<string>> PortalsAsync(HttpClient client, Guid project, string query = "") =>
        [.. (await ListAsync(client, project, query)).GetProperty("items").EnumerateArray().Select(item => item.GetProperty("portal").GetString()!)];

    private static async Task<string?> PasswordOfAsync(HttpClient client, Guid project, Guid credential) =>
        (await client.GetFromJsonAsync<JsonElement>($"{Path(project)}/{credential}/password", Cancellation)).GetProperty("password").GetString();

    [Fact(DisplayName = "Incluir acesso")]
    public async Task AddCredential_ShowsInItsEnvironment_WithoutThePassword()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var carla = await team.ClientOfAsync(team.Carla);

        await AddedAsync(carla, team.Erp, "ERP", "Homologação", "consultoria.fiscal", url: "https://hml-erp.cliente.example", notes: "Perfil fiscal.");

        var list = await ListAsync(carla, team.Erp);
        var item = list.GetProperty("items").EnumerateArray().Single();
        (item.GetProperty("portal").GetString(), item.GetProperty("environment").GetString(), item.GetProperty("username").GetString())
            .Should().Be(("ERP", "Homologação", "consultoria.fiscal"));
        item.TryGetProperty("password", out _).Should().BeFalse("the list never has the passwords");
        list.ToString().Should().NotContain(Password);
        list.GetProperty("environments").EnumerateArray().Select(environment => environment.GetProperty("name").GetString())
            .Should().Equal("Homologação");
        (await team.App.CountAsync(
            "SELECT COUNT(*) FROM [credentials].[portal_credentials] WHERE CreatedById = @carla AND CreatedAt = @at",
            ("@carla", team.Carla), ("@at", team.App.Clock.GetUtcNow().UtcDateTime))).Should().Be(1, "who included it and when are kept");
    }

    [Fact(DisplayName = "Editar sem trocar a senha")]
    public async Task ChangeWithoutPassword_KeepsIt()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var diego = await team.ClientOfAsync(team.Diego);
        var id = await AddedAsync(diego, team.Erp, "ERP", "Produção", "consultoria.leitura");

        var changed = await SendAsync(diego, HttpMethod.Put, $"{Path(team.Erp)}/{id}", new
        {
            portal = "ERP",
            environment = "Produção",
            username = "consultoria.leitura",
            password = "",
            notes = "Somente leitura.",
        });

        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ListAsync(diego, team.Erp)).GetProperty("items")[0].GetProperty("notes").GetString().Should().Be("Somente leitura.");
        (await PasswordOfAsync(diego, team.Erp, id)).Should().Be(Password);
    }

    [Fact(DisplayName = "Acesso sem portal")]
    public async Task CredentialWithoutPortalOrUser_IsRefused()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);

        var withoutPortal = await AddAsync(marina, team.Erp, " ", "Produção", "usuario");
        var withoutUser = await AddAsync(marina, team.Erp, "ERP", "Produção", "");

        withoutPortal.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        withoutUser.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await withoutUser.Content.ReadAsStringAsync(Cancellation);
        problem.Should().Contain("username").And.NotContain(Password, "the password never goes back");
        (await PortalsAsync(marina, team.Erp)).Should().BeEmpty();
    }

    [Fact(DisplayName = "Senha nunca em texto puro no banco")]
    public async Task Password_IsNeverInTheDatabaseAsItIs()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var id = await AddedAsync(team.Admin, team.Erp, "SEFAZ", "Produção", "12.345.678/0001-90");

        (await team.App.CountAsync(
            "SELECT COUNT(*) FROM [credentials].[portal_credentials] WHERE ProtectedPassword LIKE @password",
            ("@password", $"%{Password}%"))).Should().Be(0);
        (await team.App.CountAsync("SELECT COUNT(*) FROM [credentials].[data_protection_keys]")).Should().Be(1);
        (await PasswordOfAsync(team.Admin, team.Erp, id)).Should().Be(Password);
    }

    [Fact(DisplayName = "Chaves no banco cifradas pelo certificado")]
    public async Task Certificate_ProtectsTheKeysInTheDatabase()
    {
        var pfx = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"fiscoprojects-keys-{Guid.NewGuid():N}.pfx");
        using (var rsa = RSA.Create(2048))
        {
            var request = new CertificateRequest("CN=FiscoProjects Credentials Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
            await File.WriteAllBytesAsync(pfx, certificate.Export(X509ContentType.Pfx, "teste"), Cancellation);
        }

        try
        {
            await using var team = await ProjectTeam.StartAsync(sqlServer, builder =>
            {
                builder.Configuration["Credentials:KeyCertificate:Path"] = pfx;
                builder.Configuration["Credentials:KeyCertificate:Password"] = "teste";
            });
            var id = await AddedAsync(team.Admin, team.Erp, "Banco", "Produção", "alfa.fiscal");

            (await team.App.CountAsync("SELECT COUNT(*) FROM [credentials].[data_protection_keys] WHERE Xml LIKE '%EncryptedData%'"))
                .Should().Be(1);
            (await team.App.CountAsync("SELECT COUNT(*) FROM [credentials].[data_protection_keys] WHERE Xml LIKE '%<value>%'"))
                .Should().Be(0);
            (await PasswordOfAsync(team.Admin, team.Erp, id)).Should().Be(Password);
        }
        finally
        {
            File.Delete(pfx);
        }
    }

    [Fact(DisplayName = "Mostrar a senha")]
    public async Task Password_ComesOnlyWhenAsked_AndIsNotKept()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var id = await AddedAsync(team.Admin, team.Erp, "ERP", "Produção", "consultoria.leitura");
        var lucas = await team.ClientOfAsync(team.Lucas);

        var response = await lucas.GetAsync($"{Path(team.Erp)}/{id}/password", Cancellation);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        (await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("password").GetString().Should().Be(Password);
    }

    [Fact(DisplayName = "Troca de senha na auditoria")]
    public async Task ChangedPassword_IsInTheAuditWithoutItsValue()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var id = await AddedAsync(team.Admin, team.Erp, "ERP", "Produção", "consultoria.leitura");

        (await SendAsync(team.Admin, HttpMethod.Put, $"{Path(team.Erp)}/{id}", new
        {
            portal = "ERP",
            environment = "Produção",
            username = "consultoria.leitura",
            password = "Nova#Senha1",
        })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var audit = await team.Admin.GetFromJsonAsync<JsonElement>("/api/audit?pageSize=100", Cancellation);
        var change = audit.GetProperty("items").EnumerateArray()
            .First(entry => entry.GetProperty("itemType").GetString() == "PortalCredential" && entry.GetProperty("action").GetString() == "Updated")
            .GetProperty("changes").EnumerateArray().Single(field => field.GetProperty("field").GetString() == "ProtectedPassword");
        change.GetProperty("secret").GetBoolean().Should().BeTrue();
        (change.GetProperty("before").ValueKind, change.GetProperty("after").ValueKind).Should().Be((JsonValueKind.Null, JsonValueKind.Null));
        audit.ToString().Should().NotContain(Password).And.NotContain("Nova#Senha1");
    }

    [Fact(DisplayName = "Senha nunca por e-mail")]
    public async Task Password_NeverGoesByEmail()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await AddedAsync(marina, team.Erp, "ERP", "Produção", "consultoria.leitura");
        await SendAsync(marina, HttpMethod.Put, $"{Path(team.Erp)}/{id}", new
        {
            portal = "ERP",
            environment = "Produção",
            username = "consultoria.leitura",
            password = "Nova#Senha1",
        });

        var emails = await team.App.EmailsAsync();
        var summaries = await team.App.DailySummaryAsync();

        emails.Concat(summaries).Should().NotContain(email =>
            $"{email.Subject} {email.HtmlBody} {email.TextBody}".Contains(Password) || $"{email.HtmlBody} {email.TextBody}".Contains("Nova#Senha1"));
    }

    [Fact(DisplayName = "Buscar por portal ou usuário")]
    public async Task Search_LooksInThePortalAndTheUser()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await AddedAsync(team.Admin, team.Erp, "ERP", "Produção", "consultoria.leitura");
        await AddedAsync(team.Admin, team.Erp, "SEFAZ", "Produção", "12.345.678/0001-90");
        await AddedAsync(team.Admin, team.Erp, "Banco", "Homologação", "erp.integracao");
        await AddedAsync(team.Admin, team.Erp, "Prefeitura · NFS-e", "Produção", "alfa-dist");

        (await PortalsAsync(team.Admin, team.Erp, "search=ERP")).Should().Equal("Banco", "ERP");
        (await PortalsAsync(team.Admin, team.Erp, "search=nfs")).Should().Equal("Prefeitura · NFS-e");
    }

    [Fact(DisplayName = "Filtrar por ambiente")]
    public async Task Filter_ShowsOnlyTheEnvironment()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        await AddedAsync(team.Admin, team.Erp, "ERP", "Produção", "consultoria.leitura");
        await AddedAsync(team.Admin, team.Erp, "ERP", "Homologação", "consultoria.fiscal");
        await AddedAsync(team.Admin, team.Erp, "Portal de suporte", "Fornecedores", "marina.costa");

        (await PortalsAsync(team.Admin, team.Erp, "environment=Produção")).Should().Equal("ERP");
        var list = await ListAsync(team.Admin, team.Erp, "environment=Produção");
        list.GetProperty("total").GetInt32().Should().Be(3);
        list.GetProperty("environments").EnumerateArray().Select(environment => (environment.GetProperty("name").GetString(), environment.GetProperty("count").GetInt32()))
            .Should().Equal(("Fornecedores", 1), ("Homologação", 1), ("Produção", 1));
    }

    [Theory(DisplayName = "Permissões nos acessos por papel")]
    [InlineData("Administrator", true, true)]
    [InlineData("Manager", true, true)]
    [InlineData("TechLead", true, true)]
    [InlineData("Contributor", true, false)]
    [InlineData("Developer", true, false)]
    [InlineData("Viewer", false, false)]
    public async Task Permissions_FollowTheMatrix(string role, bool edits, bool deletes)
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var existing = await AddedAsync(team.Admin, team.Erp, "ERP", "Produção", "consultoria.leitura");
        var client = await team.ClientOfAsync(role);

        var list = await ListAsync(client, team.Erp);
        var password = await PasswordOfAsync(client, team.Erp, existing);
        var added = await AddAsync(client, team.Erp, $"Portal de {role}", "Produção", "usuario");
        var changed = await SendAsync(client, HttpMethod.Put, $"{Path(team.Erp)}/{existing}", new
        {
            portal = "ERP",
            environment = "Produção",
            username = "consultoria.leitura",
            notes = "Revisto.",
        });
        var removed = await SendAsync(client, HttpMethod.Delete, $"{Path(team.Erp)}/{existing}");

        password.Should().Be(Password, "everyone in the project sees the password");
        (list.GetProperty("permissions").GetProperty("canEdit").GetBoolean(), list.GetProperty("permissions").GetProperty("canDelete").GetBoolean())
            .Should().Be((edits, deletes));
        (added.StatusCode, changed.StatusCode).Should().Be(edits
            ? (HttpStatusCode.Created, HttpStatusCode.NoContent)
            : (HttpStatusCode.Forbidden, HttpStatusCode.Forbidden));
        removed.StatusCode.Should().Be(deletes ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Acessos de um projeto de fora")]
    public async Task ProjectOfOthers_IsNotFound()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var id = await AddedAsync(team.Admin, team.Erp, "ERP", "Produção", "consultoria.leitura");
        var sofia = await team.ClientOfAsync(team.Sofia);

        (await sofia.GetAsync(Path(team.Erp), Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await sofia.GetAsync($"{Path(team.Erp)}/{id}/password", Cancellation)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await AddAsync(sofia, team.Erp, "Intruso", "Produção", "intruso")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Acessos de projeto encerrado")]
    public async Task ClosedProject_IsReadOnly()
    {
        await using var team = await ProjectTeam.StartAsync(sqlServer);
        var marina = await team.ClientOfAsync(team.Marina);
        var id = await AddedAsync(marina, team.Erp, "ERP", "Produção", "consultoria.leitura");
        (await SendAsync(marina, HttpMethod.Put, $"/api/projects/{team.Erp}/status", new { status = "Completed" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var added = await AddAsync(marina, team.Erp, "SEFAZ", "Produção", "12.345.678/0001-90");
        var removed = await SendAsync(marina, HttpMethod.Delete, $"{Path(team.Erp)}/{id}");

        new[] { added.StatusCode, removed.StatusCode }.Should().AllBeEquivalentTo(HttpStatusCode.Conflict);
        (await ProjectTeam.ProblemTypeAsync(added)).Should().Be("urn:fiscoprojects:problem:project-closed");
        var list = await ListAsync(marina, team.Erp);
        list.GetProperty("permissions").GetProperty("canEdit").GetBoolean().Should().BeFalse();
        (await PasswordOfAsync(marina, team.Erp, id)).Should().Be(Password, "a closed project is still read");
    }
}
