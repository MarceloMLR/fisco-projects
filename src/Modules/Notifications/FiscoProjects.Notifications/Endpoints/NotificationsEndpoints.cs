using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Notifications.Endpoints;

/// <summary>The last test of the sending: where it stands, when and to whom (null once the queue forgot it).</summary>
public sealed record EmailTestResponse(OutboxEmailStatus Status, DateTime At, string? To);

/// <summary>The shared mailbox the e-mails leave from (null while none is configured) and the last test of the sending.</summary>
public sealed record EmailStatusResponse(string? Mailbox, EmailTestResponse? LastTest);

/// <summary>The test e-mail is in the queue, to this address.</summary>
public sealed record EmailTestQueuedResponse(string To);

/// <summary>The e-mails of the installation (ADR-0036): the mailbox and the test of the sending, only for the Administrator (ADR-0046).</summary>
public static class NotificationsEndpoints
{
    public static RouteGroupBuilder MapNotificationsEndpoints(this RouteGroupBuilder api)
    {
        var email = api.MapGroup("/admin/email")
            .WithTags("Notifications")
            .RequireAuthorization(SystemPolicies.Administrator)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        email.MapGet("/", StatusAsync).WithName("GetEmailStatus");
        email.MapPost("/test", TestAsync).WithName("SendTestEmail")
            .WithDescription("Puts in the queue an e-mail to the Administrator, sent in the background.")
            .ProducesProblem(StatusCodes.Status400BadRequest);
        return api;
    }

    private static async Task<Ok<EmailStatusResponse>> StatusAsync(GetEmailStatus getStatus, CancellationToken cancellationToken)
    {
        var status = await getStatus.ExecuteAsync(cancellationToken);
        return TypedResults.Ok(new EmailStatusResponse(
            status.Mailbox,
            status.LastTest is { } test ? new EmailTestResponse(test.Status, test.At, test.To) : null));
    }

    private static async Task<Results<Accepted<EmailTestQueuedResponse>, ProblemHttpResult>> TestAsync(
        HttpContext http, SendTestEmail send, CancellationToken cancellationToken)
    {
        var userId = UserSession.UserIdOf(http.User)
            ?? throw new InvalidOperationException("The test is sent in a session.");
        return await send.ExecuteAsync(userId, cancellationToken) is { } to
            ? TypedResults.Accepted((string?)null, new EmailTestQueuedResponse(to))
            : TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "No e-mail address",
                detail: "The person of the session has no e-mail address to send the test to.");
    }
}
