using System.ComponentModel;
using FiscoProjects.Audit.Application;
using FiscoProjects.Audit.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Audit.Endpoints;

/// <summary>Who did it; null on the record means the system.</summary>
public sealed record AuditActorResponse(Guid Id, string? Name);

/// <summary>
/// A changed field: the value before and after as invariant text, or none for a secret. A field that points to another
/// item, of the type in <paramref name="Reference"/> (such as User), comes with the item's current name, or none when
/// the item no longer exists (ADR-0084).
/// </summary>
public sealed record AuditFieldResponse(string Field, string? Before, string? After, bool Secret, string? Reference);

/// <summary>One record of the Audit screen.</summary>
public sealed record AuditEntryResponse(
    Guid Id,
    DateTime OccurredAt,
    AuditActorResponse? Actor,
    AuditAction Action,
    string ItemType,
    Guid? ItemId,
    string? ItemLabel,
    [property: Description("The identity provider of a sign-in, or why a sign-in was refused.")] string? Detail,
    IReadOnlyList<AuditFieldResponse> Changes);

public sealed record AuditPageResponse(IReadOnlyList<AuditEntryResponse> Items, int Total);

/// <summary>
/// Routes of the Audit module: only reading, and only by the Administrator (ADR-0039, ADR-0046). There is no
/// route to change or remove a record.
/// </summary>
public static class AuditEndpoints
{
    public static RouteGroupBuilder MapAuditEndpoints(this RouteGroupBuilder api)
    {
        var audit = api.MapGroup("/audit")
            .RequireAuthorization(SystemPolicies.Administrator)
            .WithTags("Audit")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        audit.MapGet("/", ListAsync).WithName("ListAuditEntries");
        return api;
    }

    private static async Task<Results<Ok<AuditPageResponse>, ValidationProblem>> ListAsync(
        ListAuditEntries listAuditEntries,
        CancellationToken cancellationToken,
        [Description("Only the records made by this user.")] Guid? actorId = null,
        [Description("Only the records of this item type, such as User or SignIn.")] string? itemType = null,
        [Description("Start of the period, included.")] DateTimeOffset? from = null,
        [Description("End of the period, excluded.")] DateTimeOffset? to = null,
        int page = 1,
        int pageSize = 25)
    {
        var query = new AuditQuery(actorId, itemType, from, to, page, pageSize);
        var errors = ListAuditEntries.Validate(query);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors
                .GroupBy(error => error.Field)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray()));
        }

        var result = await listAuditEntries.ExecuteAsync(query, cancellationToken);
        return TypedResults.Ok(new AuditPageResponse([.. result.Items.Select(ToResponse)], result.Total));
    }

    private static AuditEntryResponse ToResponse(AuditEntryView view)
    {
        var entry = view.Entry;
        return new AuditEntryResponse(
            entry.Id,
            entry.OccurredAt,
            entry.ActorId is { } actorId ? new AuditActorResponse(actorId, view.ActorName) : null,
            entry.Action,
            entry.ItemType,
            entry.ItemId,
            view.ItemLabel,
            entry.Detail,
            [.. view.Changes.Select(field => new AuditFieldResponse(field.Field, field.Before, field.After, field.Secret, field.Reference))]);
    }
}
