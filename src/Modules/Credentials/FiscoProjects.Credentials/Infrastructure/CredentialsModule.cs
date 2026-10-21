using FiscoProjects.Credentials.Application;
using FiscoProjects.Credentials.Infrastructure.Persistence;
using FiscoProjects.Persistence;
using FiscoProjects.SharedKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Credentials.Infrastructure;

/// <summary>Services of the Credentials module: the credentials of the projects and their passwords (ADR-0033, ADR-0034, ADR-0092).</summary>
public static class CredentialsModule
{
    public static IServiceCollection AddCredentialsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddModuleDbContext<CredentialsDbContext>(configuration);
        services.AddScoped<ICredentialStore, CredentialStore>();
        // Read now: a certificate set and not found stops the application (ADR-0033).
        var certificate = CredentialKeyCertificate.Load(configuration);
        services.AddSingleton<CredentialKeyRepository>();
        services.AddSingleton<IPasswordProtector>(provider => new CredentialDataProtection(
            provider.GetRequiredService<CredentialKeyRepository>(), certificate, provider.GetService<ILoggerFactory>()));
        services.AddScoped<CredentialAreas>();
        services.AddScoped<ListCredentials>();
        services.AddScoped<RevealPassword>();
        services.AddScoped<AddCredential>();
        services.AddScoped<ChangeCredential>();
        services.AddScoped<RemoveCredential>();
        // The import of a spreadsheet (ADR-0094).
        services.AddSingleton<ISpreadsheetReader, SpreadsheetReader>();
        services.AddScoped<PreviewCredentialImport>();
        services.AddScoped<ImportCredentials>();
        // The main reads, once after the API starts (ADR-0106).
        services.AddScoped<IWarmUp, CredentialsWarmUp>();
        return services;
    }
}
