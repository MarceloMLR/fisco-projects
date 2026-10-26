using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FiscoProjects.Testing;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Infrastructure;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace FiscoProjects.Api.Tests;

/// <summary>
/// Entra ID simulated inside the process (ADR-0056, ADR-0117): the issuer of a fictitious tenant with its own key, the
/// address of the sign-in and the token endpoint, which answers the exchange of the code with an ID token of the person
/// the test chose. Nothing reaches the network.
/// </summary>
internal sealed class FakeEntra : HttpMessageHandler
{
    public const string TenantId = "8c0a9f3e-1b2d-4e5f-9a6b-7c8d9e0f1a2b";
    public const string ClientId = "4d3c2b1a-0f9e-4d8c-b7a6-5e4d3c2b1a0f";

    /// <summary>What the fake managed identity gives as the proof of the application.</summary>
    public const string ManagedIdentityProof = "proof-of-the-managed-identity";

    public const string Host = "login.microsoftonline.com";

    private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "fake-entra-key" };

    public static string Issuer => $"https://{Host}/{TenantId}/v2.0";

    /// <summary>The claims of the next ID token, besides the issuer, the audience, the nonce and the times.</summary>
    public Dictionary<string, object> NextPerson { get; set; } = [];

    /// <summary>The nonce of the sign-in, which the ID token must carry back.</summary>
    public string? Nonce { get; set; }

    /// <summary>The directory of the tenant, read through Microsoft Graph (ADR-0119), empty unless the test fills it.</summary>
    public FakeGraph Graph { get; } = new();

    /// <summary>The form of the last exchange of a code, as the application sent it.</summary>
    public IReadOnlyDictionary<string, string> LastTokenRequest { get; private set; } = new Dictionary<string, string>();

    /// <summary>A member of the tenant, with the claims Entra sends.</summary>
    public static Dictionary<string, object> Member(string objectId, string name, string email) => new()
    {
        ["oid"] = objectId,
        ["sub"] = $"sub-{objectId}",
        ["tid"] = TenantId,
        ["name"] = name,
        ["preferred_username"] = email,
        ["email"] = email,
        ["acct"] = 0,
    };

    /// <summary>Turns the sign-in through Entra ID on, with this simulation in the place of Microsoft.</summary>
    public void Configure(WebApplicationBuilder builder)
    {
        builder.Configuration["EntraSignIn:Enabled"] = "true";
        builder.Configuration["EntraSignIn:TenantId"] = TenantId;
        builder.Configuration["EntraSignIn:ClientId"] = ClientId;
        builder.Services.AddSingleton<IEntraClientAssertion>(new FixedProof());
        builder.Services.AddSingleton<IGraphTokens>(new FixedGraphToken());
        builder.Services.AddHttpClient(MicrosoftGraph.ClientName).ConfigurePrimaryHttpMessageHandler(() => Graph);
        builder.Services.PostConfigure<OpenIdConnectOptions>(EntraSignInOptions.Scheme, options =>
        {
            var configuration = new OpenIdConnectConfiguration
            {
                Issuer = Issuer,
                AuthorizationEndpoint = $"https://{Host}/{TenantId}/oauth2/v2.0/authorize",
                TokenEndpoint = $"https://{Host}/{TenantId}/oauth2/v2.0/token",
            };
            configuration.SigningKeys.Add(_key);
            options.Configuration = configuration;
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            options.Backchannel = new HttpClient(this);
        });
    }

    /// <summary>
    /// Signs in through the simulation as a browser would: the start, the way to Microsoft and the way back with a
    /// code. Gives the start and the answer of the way back, without following it.
    /// </summary>
    public async Task<(EntraStart Start, HttpResponseMessage Back)> SignInAsync(
        HttpClient client, Dictionary<string, object> person, object? start = null)
    {
        var started = await StartAsync(client, start ?? new { email = person["email"], returnUrl = "/projects" });
        NextPerson = person;
        Nonce = started.Query["nonce"];
        var back = await client.PostAsync("/signin-oidc", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = "code-of-the-sign-in",
            ["state"] = started.Query["state"],
        }), TestContext.Current.CancellationToken);
        return (started, back);
    }

    /// <summary>The start of the sign-in: the address of Microsoft the API answers.</summary>
    public static async Task<EntraStart> StartAsync(HttpClient client, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/session/entra") { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF", "1");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var location = new Uri(json.RootElement.GetProperty("location").GetString()!);
        var query = QueryHelpers.ParseQuery(location.Query).ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
        return new EntraStart(response, location, query);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var form = await request.Content!.ReadAsStringAsync(cancellationToken);
        LastTokenRequest = QueryHelpers.ParseQuery(form).ToDictionary(pair => pair.Key, pair => pair.Value.ToString());

        const string accessToken = "access-token-of-the-sign-in";
        var claims = new Dictionary<string, object>(NextPerson) { ["at_hash"] = AtHashOf(accessToken) };
        if (Nonce is not null)
        {
            claims["nonce"] = Nonce;
        }

        var now = DateTime.UtcNow;
        var idToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = ClientId,
            Claims = claims,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddHours(1),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256),
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { id_token = idToken, access_token = accessToken, token_type = "Bearer", expires_in = 3600 }),
        };
    }

    /// <summary>The left half of the SHA-256 of the access token, in base64url, as OpenID Connect asks.</summary>
    private static string AtHashOf(string accessToken)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(accessToken));
        return Base64UrlEncoder.Encode(hash[..(hash.Length / 2)]);
    }

    private sealed class FixedProof : IEntraClientAssertion
    {
        public Task<string> GetAsync(CancellationToken cancellationToken) => Task.FromResult(ManagedIdentityProof);
    }

    private sealed class FixedGraphToken : IGraphTokens
    {
        public Task<string> GetAsync(CancellationToken cancellationToken) => Task.FromResult(FakeGraph.Token);
    }
}

/// <summary>The answer of the start of a sign-in through Entra ID, with the address of Microsoft and its query.</summary>
internal sealed record EntraStart(HttpResponseMessage Response, Uri Location, IReadOnlyDictionary<string, string> Query);
