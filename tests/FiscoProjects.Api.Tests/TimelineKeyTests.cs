using System.Net;
using FiscoProjects.Testing;

namespace FiscoProjects.Api.Tests;

// ADR-0134: the timeline is drawn by FiscoProjects; the installation keeps no license key for it, nor sends one to the
// browser, even when an old one is still in its configuration.
public sealed class TimelineKeyTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Instalação que não guarda a chave")]
    public async Task TheRouteOfTheKey_IsGone()
    {
        await using var app = await SessionApp.StartAsync(sqlServer, builder =>
            builder.Configuration["Syncfusion:LicenseKey"] = "chave-esquecida-na-configuracao");
        var (client, _) = await app.SignedInClientAsync();

        using var response = await client.GetAsync("/api/client-settings", Cancellation);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(Cancellation)).Should().NotContain("chave-esquecida");
        client.Dispose();
    }
}
