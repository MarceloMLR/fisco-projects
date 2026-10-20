using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using FiscoProjects.Credentials.Infrastructure;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.WebUtilities;

namespace FiscoProjects.Credentials.Tests;

// Spec: acessos, requirement "Senha cifrada" (ADR-0033): the Data Protection of the credentials with the keys in a
// repository in memory, standing for the table of the module.
public sealed class PasswordProtectionTests
{
    private const string Password = "Kx7#pa2V!q";

    /// <summary>The keys as the table keeps them: only added, never removed.</summary>
    private sealed class MemoryKeys : IXmlRepository
    {
        private readonly List<XElement> _keys = [];

        public IReadOnlyCollection<XElement> GetAllElements() => [.. _keys.Select(key => new XElement(key))];

        public void StoreElement(XElement element, string friendlyName) => _keys.Add(new XElement(element));

        public string Xml => string.Join('\n', _keys.Select(key => key.ToString()));

        public int Count => _keys.Count;
    }

    /// <summary>The id of the key that protected a password, at the start of what the Data Protection made.</summary>
    private static Guid KeyOf(string protectedPassword) => new(WebEncoders.Base64UrlDecode(protectedPassword).AsSpan(4, 16));

    private static X509Certificate2 Certificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=FiscoProjects Credentials Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        // Through a .pfx, as the installation gives it.
        return X509CertificateLoader.LoadPkcs12(created.Export(X509ContentType.Pfx, "teste"), "teste");
    }

    [Fact(DisplayName = "Senha cifrada e lida de volta")]
    public void Password_IsProtectedAndReadBack()
    {
        var keys = new MemoryKeys();
        using var protection = new CredentialDataProtection(keys, certificate: null, loggers: null);

        var kept = protection.Protect(Password);

        kept.Should().NotContain(Password);
        protection.Unprotect(kept).Should().Be(Password);
        keys.Count.Should().Be(1, "the key is kept in the repository");
        keys.Xml.Should().Contain("<value>", "without the certificate the key is kept as it is");
    }

    [Fact(DisplayName = "Trocar a chave e ler a senha")]
    public void NewKey_ReadsTheOldPassword_AndProtectsWithTheNewOne()
    {
        var keys = new MemoryKeys();
        string old;
        using (var protection = new CredentialDataProtection(keys, certificate: null, loggers: null))
        {
            old = protection.Protect(Password);
            protection.KeyManager.CreateNewKey(DateTimeOffset.UtcNow.AddSeconds(1), DateTimeOffset.UtcNow.AddDays(90));
        }

        // The application reads the keys again, as after a restart, and the new key is the one in use.
        using var current = new CredentialDataProtection(keys, certificate: null, loggers: null);
        var renewed = current.Protect("Nova#Senha1");

        KeyOf(renewed).Should().NotBe(KeyOf(old));
        current.Unprotect(old).Should().Be(Password);
        current.Unprotect(renewed).Should().Be("Nova#Senha1");
        keys.Count.Should().Be(2, "no key is removed");
    }

    [Fact(DisplayName = "Senha com chave revogada")]
    public void RevokedKeys_StillReadThePassword()
    {
        var keys = new MemoryKeys();
        using var protection = new CredentialDataProtection(keys, certificate: null, loggers: null);
        var old = protection.Protect(Password);

        protection.KeyManager.RevokeAllKeys(DateTimeOffset.UtcNow.AddSeconds(1), "Teste de revogação");

        protection.Unprotect(old).Should().Be(Password);
        using var restarted = new CredentialDataProtection(keys, certificate: null, loggers: null);
        restarted.Unprotect(old).Should().Be(Password, "the keys come back from the repository");
    }

    [Fact(DisplayName = "Chaves cifradas pelo certificado")]
    public void Certificate_ProtectsTheKeys()
    {
        var keys = new MemoryKeys();
        using var certificate = Certificate();
        string kept;
        using (var protection = new CredentialDataProtection(keys, certificate, loggers: null))
        {
            kept = protection.Protect(Password);
        }

        keys.Xml.Should().Contain("EncryptedData").And.NotContain("<value>");
        using var restarted = new CredentialDataProtection(keys, certificate, loggers: null);
        restarted.Unprotect(kept).Should().Be(Password);
        using var withoutCertificate = new CredentialDataProtection(keys, certificate: null, loggers: null);
        withoutCertificate.Invoking(protection => protection.Unprotect(kept)).Should().Throw<CryptographicException>();
    }
}
