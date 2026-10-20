using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using FiscoProjects.Credentials.Application;
using FiscoProjects.Credentials.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FiscoProjects.Credentials.Infrastructure;

/// <summary>
/// The keys of the Data Protection of the credentials in the database of the module (ADR-0033), so they go with the
/// backups and stay through new deploys. Keys are only added, never removed. The Data Protection asks for them without
/// waiting, so the reading and the writing here are synchronous, each in a scope of its own.
/// </summary>
internal sealed class CredentialKeyRepository(IServiceScopeFactory scopes) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CredentialsDbContext>();
        return [.. db.Keys.AsNoTracking().OrderBy(key => key.Id).Select(key => key.Xml).ToList().Select(xml => XElement.Parse(xml))];
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        ArgumentNullException.ThrowIfNull(element);
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CredentialsDbContext>();
        var xml = element.ToString(SaveOptions.DisableFormatting);
        // Straight to the table: the keys stay out of the audit, and saving the context is only asynchronous.
        db.Database.ExecuteSql($"INSERT INTO [credentials].[data_protection_keys] ([FriendlyName], [Xml]) VALUES ({friendlyName}, {xml})");
    }
}

/// <summary>
/// The Data Protection of the passwords of the credentials (ADR-0033): a provider of its own, apart from the one of the
/// session cookie, with the name FiscoProjects.Credentials, the keys in the database and a purpose only for the
/// passwords. With the certificate of the installation, the keys are kept protected by it. A password protected with
/// an old or revoked key is still read.
/// </summary>
internal sealed class CredentialDataProtection : IPasswordProtector, IDisposable
{
    public const string ApplicationName = "FiscoProjects.Credentials";
    public const string Purpose = "FiscoProjects.Credentials.Password";

    private readonly ServiceProvider _services;
    private readonly IPersistedDataProtector _protector;

    public CredentialDataProtection(IXmlRepository repository, X509Certificate2? certificate, ILoggerFactory? loggers)
    {
        var services = new ServiceCollection();
        if (loggers is null)
        {
            services.AddLogging();
        }
        else
        {
            services.AddSingleton(loggers);
        }

        var builder = services.AddDataProtection().SetApplicationName(ApplicationName);
        services.Configure<KeyManagementOptions>(options => options.XmlRepository = repository);
        if (certificate is not null)
        {
            builder.ProtectKeysWithCertificate(certificate).UnprotectKeysWithAnyCertificate(certificate);
        }

        _services = services.BuildServiceProvider();
        _protector = (IPersistedDataProtector)_services.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose);
        KeyManager = _services.GetRequiredService<IKeyManager>();
    }

    /// <summary>The keys, for the tests to make a new key or revoke them.</summary>
    internal IKeyManager KeyManager { get; }

    public string Protect(string password) =>
        WebEncoders.Base64UrlEncode(_protector.Protect(Encoding.UTF8.GetBytes(password)));

    public string Unprotect(string protectedPassword) =>
        Encoding.UTF8.GetString(_protector.DangerousUnprotect(
            WebEncoders.Base64UrlDecode(protectedPassword), ignoreRevocationErrors: true, out _, out _));

    public void Dispose() => _services.Dispose();
}

/// <summary>
/// The certificate that protects the keys of the credentials, an option of the installation (ADR-0033):
/// Credentials:KeyCertificate:Path and Password, a .pfx file, or Credentials:KeyCertificate:Thumbprint, a certificate
/// installed in the machine. Without them, the keys are kept without it. A certificate set and not found stops the
/// application, so the keys never go unprotected by mistake.
/// </summary>
internal static class CredentialKeyCertificate
{
    public static X509Certificate2? Load(IConfiguration configuration)
    {
        var section = configuration.GetSection("Credentials:KeyCertificate");
        var path = section["Path"];
        if (!string.IsNullOrWhiteSpace(path))
        {
            return X509CertificateLoader.LoadPkcs12FromFile(path, section["Password"]);
        }

        var thumbprint = section["Thumbprint"];
        if (string.IsNullOrWhiteSpace(thumbprint))
        {
            return null;
        }

        foreach (var location in new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine })
        {
            using var store = new X509Store(StoreName.My, location);
            store.Open(OpenFlags.ReadOnly);
            var found = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint.Trim(), validOnly: false);
            if (found.Count > 0)
            {
                return found[0];
            }
        }

        throw new InvalidOperationException("The certificate of Credentials:KeyCertificate:Thumbprint is not installed in the machine.");
    }
}
