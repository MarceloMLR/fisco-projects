using FiscoProjects.Documents.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Api.Tests;

// Spec: termos-de-aceite, requirements "Provedor de assinatura" and "Provedor local" (ADR-0129).
public sealed class SignatureHostTests
{
    [Fact(DisplayName = "Provedor local em produção")]
    public async Task LocalProviderInProduction_StopsTheApplication()
    {
        var start = () => StartAsync(TestApp.Production, ("Signature:Provider", "Local"));

        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain("only for outside production");
    }

    [Fact(DisplayName = "Provedor de assinatura desconhecido")]
    public async Task UnknownProvider_StopsTheApplication()
    {
        var start = () => StartAsync(TestApp.Development, ("Signature:Provider", "Assinador"));

        (await start.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain("'Assinador'");
    }

    [Fact(DisplayName = "Provedor local no desenvolvimento")]
    public async Task Development_HasTheLocalProvider()
    {
        await using var app = await StartAsync(TestApp.Development);

        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<SignatureProviders>().Current!.Name.Should().Be("Local");
    }

    [Fact(DisplayName = "Produção sem provedor de assinatura")]
    public async Task Production_WithoutAProvider_HasNone()
    {
        await using var app = await StartAsync(TestApp.Production);

        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<SignatureProviders>().Current.Should().BeNull();
    }

    private static Task<WebApplication> StartAsync(string environment, params (string Key, string Value)[] settings) =>
        TestApp.ComposedAsync(environment, _ => { }, builder =>
        {
            // No database here: this checks only how the provider of signatures is set up.
            builder.Configuration["Database:MigrateOnStartup"] = "false";
            if (environment == TestApp.Production)
            {
                builder.Configuration["FileStorage:Provider"] = string.Empty;
            }

            foreach (var (key, value) in settings)
            {
                builder.Configuration[key] = value;
            }
        });
}
