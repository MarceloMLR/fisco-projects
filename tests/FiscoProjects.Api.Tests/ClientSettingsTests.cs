using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// ADR-0087: the license key of the timeline comes from the configuration of the installation, with a session.
public sealed class ClientSettingsTests(SqlServerFixture sqlServer)
{
    private const string LicenseKey = "chave-de-teste-da-linha-do-tempo";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Chave da linha do tempo pela configuração")]
    public async Task ClientSettings_ComeFromTheConfiguration()
    {
        await using var app = await SessionApp.StartAsync(sqlServer, builder =>
            builder.Configuration["Syncfusion:LicenseKey"] = LicenseKey);
        var (client, _) = await app.SignedInClientAsync();
        using var anonymous = app.NewClient();

        var settings = await client.GetFromJsonAsync<JsonElement>("/api/client-settings", Cancellation);

        settings.GetProperty("timelineLicense").GetString().Should().Be(LicenseKey);
        (await anonymous.GetAsync("/api/client-settings", Cancellation)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.Dispose();
    }

    [Fact(DisplayName = "Linha do tempo sem chave configurada")]
    public async Task ClientSettings_WithoutAKey_HaveNone()
    {
        await using var app = await SessionApp.StartAsync(sqlServer, builder =>
            builder.Configuration["Syncfusion:LicenseKey"] = string.Empty);
        var (client, _) = await app.SignedInClientAsync();

        var settings = await client.GetFromJsonAsync<JsonElement>("/api/client-settings", Cancellation);

        settings.GetProperty("timelineLicense").ValueKind.Should().Be(JsonValueKind.Null);
        client.Dispose();
    }
}
