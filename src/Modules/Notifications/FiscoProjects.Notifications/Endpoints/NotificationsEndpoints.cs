using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Contracts;
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

/// <summary>
/// A notification of the bell (ADR-0038): what happened, the current name of who did it (null for the system), the
/// address of the item, its data (codes as the names of the enums, days as yyyy-MM-dd), when it arrived and whether it
/// was read.
/// </summary>
public sealed record NotificationResponse(
    Guid Id, NoticeKind Kind, string? Actor, string Path, IReadOnlyDictionary<string, string> Values, DateTime CreatedAt, bool Read);

/// <summary>A page of the bell, the newest first, with how many are not read and whether there are older ones.</summary>
public sealed record NotificationPageResponse(IReadOnlyList<NotificationResponse> Items, int Unread, bool HasMore);

/// <summary>How many notifications of the person are not read.</summary>
public sealed record UnreadNotificationsResponse(int Unread);

/// <summary>
/// The bell of the person in the session (ADR-0038), and the e-mails of the installation (ADR-0036): the mailbox and
/// the test of the sending, only for the Administrator (ADR-0046).
/// </summary>
public static class NotificationsEndpoints
{
    public static RouteGroupBuilder MapNotificationsEndpoints(this RouteGroupBuilder api)
    {
        var bell = api.MapGroup("/notifications").WithTags("Notifications").ProducesProblem(StatusCodes.Status401Unauthorized);
        bell.MapGet("/", ListAsync).WithName("ListNotifications")
            .WithDescription("The notifications of the person, the newest first, 30 at a time; with unread, only the ones not read.");
        bell.MapGet("/unread-count", UnreadAsync).WithName("CountUnreadNotifications");
        bell.MapPut("/{id:guid}/read", ReadAsync).WithName("ReadNotification").ProducesProblem(StatusCodes.Status404NotFound);
        bell.MapPut("/read-all", ReadAllAsync).WithName("ReadAllNotifications");

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

    private static async Task<Ok<NotificationPageResponse>> ListAsync(
        HttpContext http, ListNotifications list, bool? unread, int? skip, CancellationToken cancellationToken)
    {
        var page = await list.ExecuteAsync(UserOf(http), unread ?? false, skip ?? 0, cancellationToken);
        return TypedResults.Ok(new NotificationPageResponse(
            [.. page.Items.Select(item => new NotificationResponse(
                item.Id, item.Kind, item.Actor, item.Path, item.Values, item.CreatedAt, item.Read))],
            page.Unread,
            page.HasMore));
    }

    private static async Task<Ok<UnreadNotificationsResponse>> UnreadAsync(
        HttpContext http, CountUnreadNotifications count, CancellationToken cancellationToken) =>
        TypedResults.Ok(new UnreadNotificationsResponse(await count.ExecuteAsync(UserOf(http), cancellationToken)));

    private static async Task<Results<NoContent, NotFound>> ReadAsync(
        Guid id, HttpContext http, ReadNotification read, CancellationToken cancellationToken) =>
        await read.ExecuteAsync(id, UserOf(http), cancellationToken) ? TypedResults.NoContent() : TypedResults.NotFound();

    private static async Task<NoContent> ReadAllAsync(HttpContext http, ReadAllNotifications readAll, CancellationToken cancellationToken)
    {
        await readAll.ExecuteAsync(UserOf(http), cancellationToken);
        return TypedResults.NoContent();
    }

    private static Guid UserOf(HttpContext http) =>
        UserSession.UserIdOf(http.User) ?? throw new InvalidOperationException("The bell is read in a session.");

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
