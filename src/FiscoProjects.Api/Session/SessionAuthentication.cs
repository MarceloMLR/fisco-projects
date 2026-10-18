using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Api.Session;

/// <summary>How long a session lasts without use (ADR-0009), from the "Session" section.</summary>
internal sealed class UserSessionOptions
{
    public const string Section = "Session";

    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromHours(8);
}

/// <summary>
/// The BFF session (ADR-0054): one HttpOnly, Secure, SameSite=Lax cookie holding only the user id, expiring
/// after the idle timeout and renewed on every request, so the timeout counts from the last use. Every request
/// checks that the user still exists and is active. The API answers 401 and 403 instead of redirecting.
/// The cookie handler takes its clock from the TimeProvider registered in the host.
/// </summary>
internal static class SessionAuthentication
{
    public const string CookieName = "__Host-fiscoprojects-session";

    public static IServiceCollection AddSessionAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<UserSessionOptions>()
            .Bind(configuration.GetSection(UserSessionOptions.Section))
            .Validate(options => options.IdleTimeout > TimeSpan.Zero, "Session:IdleTimeout must be greater than zero.")
            .ValidateOnStart();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<IOptions<UserSessionOptions>>((cookie, session) =>
            {
                cookie.Cookie.Name = CookieName;
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                cookie.Cookie.SameSite = SameSiteMode.Lax;
                cookie.Cookie.Path = "/";
                cookie.ExpireTimeSpan = session.Value.IdleTimeout;
                cookie.SlidingExpiration = true;
                cookie.Events.OnValidatePrincipal = ValidateAsync;
                cookie.Events.OnRedirectToLogin = context => Respond(context, StatusCodes.Status401Unauthorized);
                cookie.Events.OnRedirectToAccessDenied = context => Respond(context, StatusCodes.Status403Forbidden);
            });

        services.AddAuthorization();
        return services;
    }

    private static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var userId = UserSession.UserIdOf(context.Principal);
        var access = context.HttpContext.RequestServices.GetRequiredService<IUserAccess>();

        var current = userId is null ? null : await access.FindAsync(userId.Value, context.HttpContext.RequestAborted);
        if (current is not { IsActive: true })
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        // The default only renews after half of the timeout; renewing on every use keeps the full timeout.
        context.ShouldRenew = true;
    }

    // The body comes from UseStatusCodePages, in Problem Details.
    private static Task Respond(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
}
