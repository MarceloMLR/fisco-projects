using System.Text.Encodings.Web;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>
/// The token Teams gives the page of FiscoProjects in Teams (ADR-0152), accepted only by the readings of Teams
/// (<see cref="TeamsAccess"/>): an access token v2 of Entra ID for the application of the installation, signed by the
/// tenant of the installation, in its time, with the scope of the page and issued to a client of Teams. The person is the
/// user of the link "entra" (<see cref="SignInFromTeams"/>), checked again on every request; no session is opened and
/// the token is not kept. Without the sign-in through Entra ID, the scheme never recognizes anyone.
/// </summary>
public static class TeamsSignIn
{
    /// <summary>The scope the application exposes to Teams, which the token must carry.</summary>
    public const string Scope = "access_as_user";

    /// <summary>
    /// The clients of Microsoft that ask the token for the page: Teams on the computer and on the phone, and Teams on the
    /// web. Public identifiers of Microsoft, the same in every tenant.
    /// </summary>
    public static readonly IReadOnlySet<string> TeamsClients = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "1fec8e78-bce4-4aaf-ab1b-5451cc387264",
        "5e3ce6c0-2b1f-4285-8d4b-75ee78787346",
    };

    public static AuthenticationBuilder AddTeamsSignIn(this AuthenticationBuilder authentication, IConfiguration configuration)
    {
        if (configuration.GetSection(EntraSignInOptions.Section).Get<EntraSignInOptions>() is not { Enabled: true })
        {
            return authentication.AddScheme<AuthenticationSchemeOptions, NoTeamsSignIn>(TeamsAccess.Scheme, _ => { });
        }

        authentication.AddJwtBearer(TeamsAccess.Scheme, _ => { });
        authentication.Services.AddOptions<JwtBearerOptions>(TeamsAccess.Scheme)
            .Configure<IOptions<EntraSignInOptions>>((jwt, entra) => Configure(jwt, entra.Value));
        return authentication;
    }

    private static void Configure(JwtBearerOptions jwt, EntraSignInOptions entra)
    {
        jwt.Authority = entra.Authority;
        jwt.Audience = entra.ClientId;
        jwt.MapInboundClaims = false;
        jwt.SaveToken = false;
        jwt.IncludeErrorDetails = false;
        jwt.TokenValidationParameters.ValidIssuer = entra.Authority;
        jwt.TokenValidationParameters.ValidAudience = entra.ClientId;
        jwt.Events = new JwtBearerEvents { OnTokenValidated = RecognizeAsync };
    }

    private static async Task RecognizeAsync(TokenValidatedContext context)
    {
        var principal = context.Principal!;
        var scopes = principal.FindFirst("scp")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var client = principal.FindFirst("azp")?.Value;
        if (!scopes.Contains(Scope, StringComparer.Ordinal) || client is null || !TeamsClients.Contains(client))
        {
            context.Fail("The token is not one of Teams for the application of the installation.");
            return;
        }

        var signIn = context.HttpContext.RequestServices.GetRequiredService<SignInFromTeams>();
        var result = await signIn.ExecuteAsync(EntraIdentity.From(principal), context.HttpContext.RequestAborted);
        if (result is TeamsSignInResult.Recognized recognized)
        {
            context.Principal = UserSession.For(recognized.UserId);
            return;
        }

        context.Fail($"The person of the token of Teams was refused: {((TeamsSignInResult.Refused)result).Reason}.");
    }
}

/// <summary>The scheme of Teams without the sign-in through Entra ID: it recognizes no one, and reads no token.</summary>
internal sealed class NoTeamsSignIn(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
}
