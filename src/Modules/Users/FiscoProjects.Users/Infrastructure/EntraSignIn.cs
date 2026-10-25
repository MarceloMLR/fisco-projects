using Azure.Core;
using Azure.Identity;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>Proves to Entra ID that a request comes from the application of the installation (ADR-0117).</summary>
public interface IEntraClientAssertion
{
    Task<string> GetAsync(CancellationToken cancellationToken);
}

/// <summary>
/// The proof of the App Service: a token of its managed identity for the token exchange of Entra ID, which the
/// application of the installation trusts by a federated credential (ADR-0117). No secret nor certificate exists.
/// The Azure SDK keeps the token until it is about to expire.
/// </summary>
internal sealed class ManagedIdentityClientAssertion : IEntraClientAssertion
{
    private static readonly TokenRequestContext TokenExchange = new(["api://AzureADTokenExchange/.default"]);

    private readonly ManagedIdentityCredential _credential = new(ManagedIdentityId.SystemAssigned);

    public async Task<string> GetAsync(CancellationToken cancellationToken) =>
        (await _credential.GetTokenAsync(TokenExchange, cancellationToken)).Token;
}

/// <summary>
/// The sign-in through Entra ID (ADR-0005, ADR-0007, ADR-0009, ADR-0117): OpenID Connect with an authorization code and
/// PKCE, the e-mail of the sign-in page as the hint, the managed identity as the credential of the application and the
/// session cookie of the BFF at the end (ADR-0054). The tokens of Microsoft are not kept. A refused sign-in goes back to
/// the sign-in page, which says why.
/// </summary>
public static class EntraSignIn
{
    public const string Scheme = EntraSignInOptions.Scheme;

    /// <summary>Outside /api: Entra comes back with a form, which cannot carry the X-CSRF header (ADR-0054).</summary>
    public const string CallbackPath = "/signin-oidc";

    public const string SignInPage = "/sign-in";

    /// <summary>Why the sign-in page was opened again, in its "refused" parameter.</summary>
    public static class Refusals
    {
        public const string Guest = "guest";
        public const string NotAssigned = "notAssigned";
        public const string Inactive = "inactive";
        public const string Failed = "failed";
    }

    private const string LoginHintKey = EntraSignInOptions.LoginHintItem;
    private const string PreviousUserKey = EntraSignInOptions.PreviousUserItem;

    /// <summary>The refusal of Entra ID for a person not assigned to the application.</summary>
    private const string NotAssignedError = "AADSTS50105";

    /// <summary>Adds the scheme only when the sign-in through Entra ID is on.</summary>
    public static AuthenticationBuilder AddEntraSignIn(this AuthenticationBuilder authentication, IConfiguration configuration)
    {
        if (configuration.GetSection(EntraSignInOptions.Section).Get<EntraSignInOptions>() is not { Enabled: true })
        {
            return authentication;
        }

        authentication.AddOpenIdConnect(Scheme, _ => { });
        authentication.Services.AddOptions<OpenIdConnectOptions>(Scheme)
            .Configure<IOptions<EntraSignInOptions>>((oidc, entra) => Configure(oidc, entra.Value));
        return authentication;
    }

    private static void Configure(OpenIdConnectOptions oidc, EntraSignInOptions entra)
    {
        oidc.Authority = entra.Authority;
        oidc.ClientId = entra.ClientId;
        oidc.ResponseType = OpenIdConnectResponseType.Code;
        oidc.UsePkce = true;
        oidc.Scope.Clear();
        oidc.Scope.Add("openid");
        oidc.Scope.Add("profile");
        oidc.Scope.Add("email");
        oidc.CallbackPath = CallbackPath;
        oidc.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        oidc.SaveTokens = false;
        oidc.MapInboundClaims = false;
        oidc.GetClaimsFromUserInfoEndpoint = false;
        // Entra ID has no pushed authorization requests; without them the hint goes in the address of Microsoft.
        oidc.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
        oidc.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = AddLoginHint,
            OnAuthorizationCodeReceived = AddClientAssertionAsync,
            OnTokenValidated = SignInAsync,
            OnAccessDenied = context => Refuse(context, ErrorDescriptionOf(context.Request)),
            OnRemoteFailure = context => Refuse(context, context.Failure?.Message),
        };
    }

    private static Task AddLoginHint(RedirectContext context)
    {
        // Out of the properties, so the e-mail does not go in the state either.
        if (context.Properties.Items.TryGetValue(LoginHintKey, out var hint))
        {
            context.Properties.Items.Remove(LoginHintKey);
            context.ProtocolMessage.LoginHint = hint;
        }

        return Task.CompletedTask;
    }

    private static async Task AddClientAssertionAsync(AuthorizationCodeReceivedContext context)
    {
        var assertion = context.HttpContext.RequestServices.GetRequiredService<IEntraClientAssertion>();
        context.TokenEndpointRequest!.ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
        context.TokenEndpointRequest.ClientAssertion = await assertion.GetAsync(context.HttpContext.RequestAborted);
    }

    private static async Task SignInAsync(TokenValidatedContext context)
    {
        var signIn = context.HttpContext.RequestServices.GetRequiredService<SignInWithEntra>();
        var result = await signIn.ExecuteAsync(EntraIdentity.From(context.Principal!), context.HttpContext.RequestAborted);
        switch (result)
        {
            case EntraSignInResult.SignedIn signedIn:
                context.Principal = UserSession.For(signedIn.UserId);
                // Someone else in the place of the person whose session expired goes to the home screen.
                if (context.Properties!.Items.TryGetValue(PreviousUserKey, out var previous)
                    && previous != signedIn.UserId.ToString())
                {
                    context.Properties.RedirectUri = "/";
                }

                context.Properties.Items.Remove(PreviousUserKey);
                break;
            case EntraSignInResult.Guest:
                RedirectToSignInPage(context, Refusals.Guest);
                break;
            case EntraSignInResult.Inactive:
                RedirectToSignInPage(context, Refusals.Inactive);
                break;
            default:
                RedirectToSignInPage(context, Refusals.Failed);
                break;
        }
    }

    private static Task Refuse<TOptions>(HandleRequestContext<TOptions> context, string? error)
        where TOptions : AuthenticationSchemeOptions
    {
        var notAssigned = error?.Contains(NotAssignedError, StringComparison.Ordinal) == true;
        RedirectToSignInPage(context, notAssigned ? Refusals.NotAssigned : Refusals.Failed);
        return Task.CompletedTask;
    }

    private static void RedirectToSignInPage<TOptions>(HandleRequestContext<TOptions> context, string refusal)
        where TOptions : AuthenticationSchemeOptions
    {
        context.Response.Redirect($"{SignInPage}?refused={refusal}");
        context.HandleResponse();
    }

    private static string? ErrorDescriptionOf(HttpRequest request) =>
        request.HasFormContentType ? request.Form["error_description"].ToString() : request.Query["error_description"].ToString();
}
