using System.ComponentModel;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Users.Endpoints;

/// <summary>
/// A person to choose, such as the manager of a project or a person to include in it: a user (<c>id</c>) or a person of
/// the company directory who never signed in (<c>directoryId</c>), who becomes a user when included (ADR-0005). Only an
/// active Administrator or Project manager can be a manager (ADR-0085), and only an active Administrator or Tech lead a
/// tech lead (ADR-0097).
/// </summary>
public sealed record PersonResponse(
    Guid? Id, string? DirectoryId, string Name, string Email, bool CanBeManager, bool CanBeTechLead);

/// <summary>The name and the e-mail of a user, for the card of the person (ADR-0118); the anonymized has no e-mail.</summary>
public sealed record PersonDetailsResponse(Guid Id, string Name, string? Email);

/// <summary>
/// The people of the company to choose (ADR-0005, ADR-0072), the card of a person (ADR-0118) and the photo of the avatar
/// (ADR-0124). Anyone with a session asks: the tech lead of a project also includes people, and the names and the photos
/// of the company are known to the people of the company (ADR-0007).
/// </summary>
internal static class PeopleEndpoints
{
    /// <summary>A photo is only an image: nothing in it runs (as the logos).</summary>
    private const string PhotoPolicy = "default-src 'none'; sandbox";

    /// <summary>How long the browser keeps a photo, or that there is none (ADR-0124).</summary>
    private const string PhotoCache = "private, max-age=3600";

    public static void MapPeopleEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/people", ListAsync).WithName("ListPeople").WithTags("Users")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        api.MapGet("/people/{id:guid}", GetAsync).WithName("GetPerson").WithTags("Users")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        // Kept by the browser for an hour, so the session is not renewed by it (KeepSession).
        api.MapGet("/people/{id:guid}/photo", GetPhotoAsync).WithName("GetPersonPhoto").WithTags("Users")
            .WithMetadata(KeepSession.Instance)
            .Produces(StatusCodes.Status200OK, contentType: "image/jpeg", additionalContentTypes: "image/png")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetPhotoAsync(
        Guid id, HttpContext http, GetPersonPhoto getPhoto, CancellationToken cancellationToken)
    {
        var photo = await getPhoto.ExecuteAsync(id, cancellationToken);
        var headers = http.Response.Headers;
        // Also the answer without a photo, so the avatars of the screen do not ask again.
        headers.CacheControl = PhotoCache;
        headers.XContentTypeOptions = "nosniff";
        if (photo is null)
        {
            return TypedResults.NotFound();
        }

        headers.ContentSecurityPolicy = PhotoPolicy;
        return TypedResults.File(photo.Content, photo.ContentType);
    }

    private static async Task<Results<Ok<PersonDetailsResponse>, NotFound>> GetAsync(
        Guid id, GetPerson getPerson, CancellationToken cancellationToken) =>
        await getPerson.ExecuteAsync(id, cancellationToken) is { } person
            ? TypedResults.Ok(new PersonDetailsResponse(person.Id, person.Name, person.Email))
            : TypedResults.NotFound();

    private static async Task<Results<Ok<IReadOnlyList<PersonResponse>>, ValidationProblem>> ListAsync(
        ListPeople listPeople,
        CancellationToken cancellationToken,
        [Description("Part of the name or of the e-mail, without telling case or accents apart.")] string? search = null,
        [Description("Only the Administrators and the Project managers, who may manage a project.")] bool managers = false,
        [Description("Only the Administrators and the Tech leads, who may lead a project technically.")] bool techLeads = false,
        [Description("How many people at most, from 1 to 100.")] int limit = 20)
    {
        if (limit is < 1 or > PeopleQuery.MaxLimit)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(limit)] = [$"Must be between 1 and {PeopleQuery.MaxLimit}."],
            });
        }

        var people = await listPeople.ExecuteAsync(new PeopleQuery(search, managers, limit, techLeads), cancellationToken);
        return TypedResults.Ok<IReadOnlyList<PersonResponse>>(
            [.. people.Select(person => new PersonResponse(
                person.Id, person.DirectoryId, person.Name, person.Email, person.CanBeManager, person.CanBeTechLead))]);
    }
}
