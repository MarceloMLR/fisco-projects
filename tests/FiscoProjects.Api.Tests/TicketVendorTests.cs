using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Api.Tests;

// Spec: chamados (API side): "Fornecedores", "Chamado no fornecedor", "Fornecedor parado" and the view "Com fornecedor"
// (ADR-0028, ADR-0044, ADR-0046).
public sealed class TicketVendorTests(SqlServerFixture sqlServer)
{
    private const string Vendors = "/api/admin/vendors";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>ERP-FIN, managed by Marina, with Rafael as tech lead, Carla as contributor and Diego as developer.</summary>
    private sealed record Scene(SessionApp App, HttpClient Admin, Guid Marina, Guid Rafael, Guid Carla, Guid Diego, Guid Erp)
        : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => App.DisposeAsync();
    }

    private async Task<Scene> StartAsync()
    {
        var app = await SessionApp.StartAsync(sqlServer);
        var (admin, _) = await app.SignedInClientAsync("Paulo Nogueira", SystemRole.Administrator);
        var marina = await app.CreateUserAsync("Marina Costa", SystemRole.ProjectManager);
        var rafael = await app.CreateUserAsync("Rafael Souza", SystemRole.User);
        var carla = await app.CreateUserAsync("Carla Mendes", SystemRole.User);
        var diego = await app.CreateUserAsync("Diego Torres", SystemRole.User);
        var created = await SendAsync(admin, HttpMethod.Post, "/api/projects", new
        {
            code = "ERP-FIN",
            name = "Implantação do ERP financeiro",
            managerId = marina,
            techLeadId = rafael,
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync(Cancellation));
        var erp = (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
        await app.AddProjectMemberAsync(erp, carla, "Contributor");
        await app.AddProjectMemberAsync(erp, diego, "Developer");
        return new Scene(app, admin, marina, rafael, carla, diego, erp);
    }

    private static async Task<Guid> AddVendorAsync(HttpClient admin, string name, string? portalUrl = null)
    {
        var added = await SendAsync(admin, HttpMethod.Post, Vendors, new { name, portalUrl });
        added.StatusCode.Should().Be(HttpStatusCode.Created, await added.Content.ReadAsStringAsync(Cancellation));
        return (await added.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static async Task<int> OpenAsync(Scene scene, string title = "Rejeição 539 ao emitir nota de devolução")
    {
        using var carla = await scene.App.SignInAsync(scene.Carla);
        var opened = await SendAsync(carla, HttpMethod.Post, "/api/tickets", new
        {
            projectId = scene.Erp,
            team = "Development",
            priority = "High",
            title,
            description = "A SEFAZ retorna a rejeição 539.",
        });
        opened.StatusCode.Should().Be(HttpStatusCode.Created, await opened.Content.ReadAsStringAsync(Cancellation));
        return (await opened.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("number").GetInt32();
    }

    private static Task<HttpResponseMessage> RegisterAsync(
        HttpClient client, int number, Guid vendor, string code, string status = "Open", string? note = null) =>
        SendAsync(client, HttpMethod.Post, $"/api/tickets/{number}/vendor-tickets", new
        {
            vendorId = vendor,
            code,
            openedOn = "2026-10-19",
            status,
            link = $"https://suporte.example/{code}",
            note,
        });

    private static async Task<Guid> RegisteredAsync(HttpClient client, int number, Guid vendor, string code, string status = "Open")
    {
        var registered = await RegisterAsync(client, number, vendor, code, status);
        registered.StatusCode.Should().Be(HttpStatusCode.Created, await registered.Content.ReadAsStringAsync(Cancellation));
        return (await registered.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }

    private static Task<JsonElement> TicketAsync(HttpClient client, int number) =>
        client.GetFromJsonAsync<JsonElement>($"/api/tickets/{number}", Cancellation);

    private static JsonElement[] VendorTicketsOf(JsonElement ticket) => [.. ticket.GetProperty("vendorTickets").EnumerateArray()];

    private static async Task<JsonElement[]> ChoicesAsync(HttpClient client) =>
        [.. (await client.GetFromJsonAsync<JsonElement>("/api/tickets/vendor-choices", Cancellation)).EnumerateArray()];

    [Fact(DisplayName = "Incluir fornecedor")]
    public async Task Administrator_AddsAVendor()
    {
        await using var scene = await StartAsync();

        await AddVendorAsync(scene.Admin, "Emissor Exemplo", "https://suporte.emissor.example");

        var vendor = (await scene.Admin.GetFromJsonAsync<JsonElement>(Vendors, Cancellation)).EnumerateArray().Single();
        (vendor.GetProperty("name").GetString(), vendor.GetProperty("portalUrl").GetString(), vendor.GetProperty("isActive").GetBoolean())
            .Should().Be(("Emissor Exemplo", "https://suporte.emissor.example", true));
        using var diego = await scene.App.SignInAsync(scene.Diego);
        (await ChoicesAsync(diego)).Select(choice => choice.GetProperty("name").GetString()).Should().Equal("Emissor Exemplo");
        var audit = await scene.Admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=Vendor", Cancellation);
        var entry = audit.GetProperty("items").EnumerateArray().Single();
        (entry.GetProperty("action").GetString(), entry.GetProperty("itemLabel").GetString()).Should().Be(("Created", "Emissor Exemplo"));
    }

    [Fact(DisplayName = "Nome repetido")]
    public async Task SameName_IsRefused()
    {
        await using var scene = await StartAsync();
        await AddVendorAsync(scene.Admin, "Emissor Exemplo");
        var other = await AddVendorAsync(scene.Admin, "Fiscal Exemplo");

        var added = await SendAsync(scene.Admin, HttpMethod.Post, Vendors, new { name = "emissor exemplo" });
        var renamed = await SendAsync(scene.Admin, HttpMethod.Put, $"{Vendors}/{other}", new { name = "Emissor Exemplo", isActive = true });

        added.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await added.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString()
            .Should().Be("urn:fiscoprojects:problem:vendor-name-in-use");
        renamed.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await scene.Admin.GetFromJsonAsync<JsonElement>(Vendors, Cancellation)).EnumerateArray()
            .Select(vendor => vendor.GetProperty("name").GetString()).Should().Equal("Emissor Exemplo", "Fiscal Exemplo");
    }

    [Fact(DisplayName = "Portal inválido")]
    public async Task PortalThatIsNotAWebAddress_IsRefused()
    {
        await using var scene = await StartAsync();

        var added = await SendAsync(scene.Admin, HttpMethod.Post, Vendors, new { name = "Emissor Exemplo", portalUrl = "ftp://suporte.example" });

        added.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await added.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").TryGetProperty("portalUrl", out _)
            .Should().BeTrue();
    }

    [Fact(DisplayName = "Fornecedor inativo")]
    public async Task InactiveVendor_LeavesTheChoices_AndKeepsItsRecords()
    {
        await using var scene = await StartAsync();
        var vendor = await AddVendorAsync(scene.Admin, "Emissor Exemplo");
        var number = await OpenAsync(scene);
        using var diego = await scene.App.SignInAsync(scene.Diego);
        await RegisteredAsync(diego, number, vendor, "FX-20931");

        var changed = await SendAsync(scene.Admin, HttpMethod.Put, $"{Vendors}/{vendor}", new { name = "Emissor Exemplo", isActive = false });

        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ChoicesAsync(diego)).Should().BeEmpty();
        VendorTicketsOf(await TicketAsync(diego, number)).Single().GetProperty("vendor").GetString().Should().Be("Emissor Exemplo");
        var refused = await RegisterAsync(diego, number, vendor, "FX-20932");
        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await refused.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("errors").TryGetProperty("vendorId", out _)
            .Should().BeTrue();
        VendorTicketsOf(await TicketAsync(diego, number)).Should().HaveCount(1);
    }

    [Fact(DisplayName = "Fornecedores só do administrador")]
    public async Task OnlyTheAdministrator_KeepsTheVendors()
    {
        await using var scene = await StartAsync();
        using var marina = await scene.App.SignInAsync(scene.Marina);

        (await SendAsync(marina, HttpMethod.Post, Vendors, new { name = "Emissor Exemplo" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await marina.GetAsync(Vendors, Cancellation)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await scene.Admin.GetFromJsonAsync<JsonElement>(Vendors, Cancellation)).GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "Registrar no fornecedor")]
    public async Task Developer_RegistersATicketAtAVendor()
    {
        await using var scene = await StartAsync();
        var vendor = await AddVendorAsync(scene.Admin, "Emissor Exemplo");
        var number = await OpenAsync(scene);
        using var diego = await scene.App.SignInAsync(scene.Diego);

        var registered = await RegisterAsync(diego, number, vendor, "FX-20931", "InAnalysis", "Aberto com o XML da nota.");

        registered.StatusCode.Should().Be(HttpStatusCode.Created, await registered.Content.ReadAsStringAsync(Cancellation));
        using var carla = await scene.App.SignInAsync(scene.Carla);
        var ticket = await TicketAsync(carla, number);
        var record = VendorTicketsOf(ticket).Single();
        (record.GetProperty("vendor").GetString(),
                record.GetProperty("code").GetString(),
                record.GetProperty("openedOn").GetString(),
                record.GetProperty("status").GetString(),
                record.GetProperty("note").GetString(),
                record.GetProperty("registeredById").GetGuid())
            .Should().Be(("Emissor Exemplo", "FX-20931", "2026-10-19", "InAnalysis", "Aberto com o XML da nota.", scene.Diego));
        record.GetProperty("staleDays").ValueKind.Should().Be(JsonValueKind.Null);
        var registration = ticket.GetProperty("events").EnumerateArray().Single();
        (registration.GetProperty("kind").GetString(),
                registration.GetProperty("actorId").GetGuid(),
                registration.GetProperty("vendorTicketId").GetGuid(),
                registration.GetProperty("toVendorStatus").GetString())
            .Should().Be(("VendorTicketRegistered", scene.Diego, record.GetProperty("id").GetGuid(), "InAnalysis"));
        // Our ticket keeps its own status and deadlines (ADR-0028).
        ticket.GetProperty("status").GetString().Should().Be("Open");
        ticket.GetProperty("permissions").GetProperty("canRegisterVendorTicket").GetBoolean().Should().BeFalse();
        (await TicketAsync(diego, number)).GetProperty("permissions").GetProperty("canRegisterVendorTicket").GetBoolean().Should().BeTrue();
        var audit = await scene.Admin.GetFromJsonAsync<JsonElement>("/api/audit?itemType=VendorTicket", Cancellation);
        var entry = audit.GetProperty("items").EnumerateArray().Single();
        (entry.GetProperty("action").GetString(), entry.GetProperty("itemLabel").GetString()).Should().Be(("Created", "FX-20931"));
        var fields = entry.GetProperty("changes").EnumerateArray().ToDictionary(field => field.GetProperty("field").GetString()!);
        fields["VendorId"].GetProperty("after").GetString().Should().Be("Emissor Exemplo");
        fields["TicketId"].GetProperty("after").GetString().Should().Be($"#{number} Rejeição 539 ao emitir nota de devolução");
    }

    [Fact(DisplayName = "Fornecedores diferentes")]
    public async Task OneTicket_TakesTicketsAtDifferentVendors()
    {
        await using var scene = await StartAsync();
        var emissor = await AddVendorAsync(scene.Admin, "Emissor Exemplo");
        var fiscal = await AddVendorAsync(scene.Admin, "Fiscal Exemplo");
        var number = await OpenAsync(scene);
        using var rafael = await scene.App.SignInAsync(scene.Rafael);
        await RegisteredAsync(rafael, number, emissor, "FX-20931", "InAnalysis");
        scene.App.Clock.Advance(TimeSpan.FromMinutes(5));

        await RegisteredAsync(rafael, number, fiscal, "OB-118");

        var records = VendorTicketsOf(await TicketAsync(rafael, number));
        records.Select(record => (record.GetProperty("vendor").GetString(), record.GetProperty("code").GetString()))
            .Should().Equal(("Emissor Exemplo", "FX-20931"), ("Fiscal Exemplo", "OB-118"));
    }

    [Fact(DisplayName = "Com fornecedor")]
    public async Task WithVendorView_ListsOnlyTheTicketsAtVendors()
    {
        await using var scene = await StartAsync();
        var emissor = await AddVendorAsync(scene.Admin, "Emissor Exemplo");
        var fiscal = await AddVendorAsync(scene.Admin, "Fiscal Exemplo");
        var atVendors = await OpenAsync(scene);
        await OpenAsync(scene, "Sem fornecedor");
        using var diego = await scene.App.SignInAsync(scene.Diego);
        var first = await RegisteredAsync(diego, atVendors, emissor, "FX-20931");
        scene.App.Clock.Advance(TimeSpan.FromMinutes(5));
        await RegisteredAsync(diego, atVendors, fiscal, "OB-118", "Resolved");

        var list = await diego.GetFromJsonAsync<JsonElement>("/api/tickets?view=WithVendor", Cancellation);
        var counts = await diego.GetFromJsonAsync<JsonElement>("/api/tickets/counts", Cancellation);

        var item = list.GetProperty("items").EnumerateArray().Single();
        item.GetProperty("number").GetInt32().Should().Be(atVendors);
        // The one still open at the vendor, though registered first.
        var vendor = item.GetProperty("vendor");
        (vendor.GetProperty("id").GetGuid(), vendor.GetProperty("vendor").GetString(), vendor.GetProperty("code").GetString())
            .Should().Be((first, "Emissor Exemplo", "FX-20931"));
        counts.GetProperty("withVendor").GetInt32().Should().Be(1);
        var all = await diego.GetFromJsonAsync<JsonElement>("/api/tickets", Cancellation);
        all.GetProperty("items").EnumerateArray().Single(row => row.GetProperty("number").GetInt32() != atVendors)
            .GetProperty("vendor").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "Atualizar o status no fornecedor")]
    public async Task Attendant_UpdatesTheStatusWithANote()
    {
        await using var scene = await StartAsync();
        var vendor = await AddVendorAsync(scene.Admin, "Emissor Exemplo");
        var number = await OpenAsync(scene);
        using var diego = await scene.App.SignInAsync(scene.Diego);
        var id = await RegisteredAsync(diego, number, vendor, "FX-20931", "InAnalysis");
        scene.App.Clock.Advance(TimeSpan.FromMinutes(30));

        var updated = await SendAsync(diego, HttpMethod.Put, $"/api/tickets/{number}/vendor-tickets/{id}", new
        {
            status = "WaitingForUs",
            note = "Pediram o XML da nota rejeitada.",
        });

        updated.StatusCode.Should().Be(HttpStatusCode.NoContent, await updated.Content.ReadAsStringAsync(Cancellation));
        var ticket = await TicketAsync(diego, number);
        var record = VendorTicketsOf(ticket).Single();
        record.GetProperty("status").GetString().Should().Be("WaitingForUs");
        record.GetProperty("updatedAt").GetDateTime().Should().Be(scene.App.Clock.GetUtcNow().UtcDateTime);
        var update = ticket.GetProperty("events").EnumerateArray().Last();
        (update.GetProperty("kind").GetString(),
                update.GetProperty("vendorTicketId").GetGuid(),
                update.GetProperty("fromVendorStatus").GetString(),
                update.GetProperty("toVendorStatus").GetString(),
                update.GetProperty("note").GetString())
            .Should().Be(("VendorStatusChanged", id, "InAnalysis", "WaitingForUs", "Pediram o XML da nota rejeitada."));
    }

    [Fact(DisplayName = "Solicitante não registra no fornecedor")]
    public async Task Requester_MayNotRegisterAtAVendor()
    {
        await using var scene = await StartAsync();
        var vendor = await AddVendorAsync(scene.Admin, "Emissor Exemplo");
        var number = await OpenAsync(scene);
        using var carla = await scene.App.SignInAsync(scene.Carla);

        var registered = await RegisterAsync(carla, number, vendor, "FX-20931");

        registered.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var ticket = await TicketAsync(scene.Admin, number);
        VendorTicketsOf(ticket).Should().BeEmpty();
        ticket.GetProperty("events").GetArrayLength().Should().Be(0);
    }

    [Fact(DisplayName = "Chamado fechado não recebe registro")]
    public async Task ClosedTicket_TakesNoRegistration()
    {
        await using var scene = await StartAsync();
        var vendor = await AddVendorAsync(scene.Admin, "Emissor Exemplo");
        var number = await OpenAsync(scene);
        using var diego = await scene.App.SignInAsync(scene.Diego);
        var id = await RegisteredAsync(diego, number, vendor, "FX-20931");
        var body = JsonSerializer.Serialize(new { type = "doc", content = new[] { new { type = "paragraph", content = new[] { new { type = "text", text = "Aberto por engano." } } } } });
        using var carla = await scene.App.SignInAsync(scene.Carla);
        (await SendAsync(carla, HttpMethod.Post, $"/api/tickets/{number}/transitions", new { action = "Cancel", body }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var registered = await RegisterAsync(diego, number, vendor, "FX-20932");
        var updated = await SendAsync(diego, HttpMethod.Put, $"/api/tickets/{number}/vendor-tickets/{id}", new { status = "Closed" });

        registered.StatusCode.Should().Be(HttpStatusCode.Conflict);
        updated.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await updated.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("type").GetString()
            .Should().Be("urn:fiscoprojects:problem:ticket-closed");
    }

    [Fact(DisplayName = "Sem retorno do fornecedor")]
    public async Task VendorTicketWithoutUpdate_IsStaleAfterFiveBusinessDays()
    {
        await using var scene = await StartAsync();
        var vendor = await AddVendorAsync(scene.Admin, "Emissor Exemplo");
        var number = await OpenAsync(scene);
        using (var diego = await scene.App.SignInAsync(scene.Diego))
        {
            await RegisteredAsync(diego, number, vendor, "FX-20931", "InAnalysis");
        }

        // Friday 23/10: four business days without an update.
        scene.App.Clock.Advance(TimeSpan.FromDays(4));
        using (var diego = await scene.App.SignInAsync(scene.Diego))
        {
            VendorTicketsOf(await TicketAsync(diego, number)).Single().GetProperty("staleDays").ValueKind.Should().Be(JsonValueKind.Null);
        }

        // Tuesday 27/10: six business days, from Tuesday 20/10.
        scene.App.Clock.Advance(TimeSpan.FromDays(4));
        using var later = await scene.App.SignInAsync(scene.Diego);

        var record = VendorTicketsOf(await TicketAsync(later, number)).Single();

        record.GetProperty("staleDays").GetInt32().Should().Be(6);
        var item = (await later.GetFromJsonAsync<JsonElement>("/api/tickets?view=WithVendor", Cancellation)).GetProperty("items")[0];
        item.GetProperty("vendor").GetProperty("staleDays").GetInt32().Should().Be(6);
        var updated = await SendAsync(later, HttpMethod.Put, $"/api/tickets/{number}/vendor-tickets/{record.GetProperty("id").GetGuid()}", new
        {
            status = "InAnalysis",
            note = "Cobrado no portal.",
        });
        updated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        VendorTicketsOf(await TicketAsync(later, number)).Single().GetProperty("staleDays").ValueKind.Should().Be(JsonValueKind.Null);
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
