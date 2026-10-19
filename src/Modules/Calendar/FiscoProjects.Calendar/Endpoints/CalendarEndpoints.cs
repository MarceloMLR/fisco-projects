using FiscoProjects.Calendar.Application;
using FiscoProjects.Calendar.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Calendar.Endpoints;

/// <summary>
/// The calendar of a year: the working days, from Monday to Sunday, and the dates in order. A calculated holiday has
/// its key (the screen names it); a local holiday has its id and name.
/// </summary>
public sealed record CalendarYearResponse(int Year, IReadOnlyList<DayOfWeek> WorkingDays, IReadOnlyList<CalendarDayResponse> Days);

public sealed record CalendarDayResponse(
    DateOnly Date, CalendarDayKind Kind, string? Key, Guid? Id, string? Name, bool RepeatsEveryYear, bool IsConsidered);

/// <summary>The days of the week the installation works; at least one.</summary>
public sealed record WorkingDaysRequest(IReadOnlyList<DayOfWeek>? WorkingDays);

/// <summary>Whether a calculated holiday is considered, in every year.</summary>
public sealed record CalculatedHolidayChoiceRequest(bool IsConsidered);

/// <summary>Many dates considered or disregarded at once: the keys of calculated holidays and the ids of local ones.</summary>
public sealed record ConsiderHolidaysRequest(
    bool IsConsidered, IReadOnlyList<string>? CalculatedHolidays, IReadOnlyList<Guid>? LocalHolidays);

/// <summary>A new local holiday: the name, the date and whether it repeats every year.</summary>
public sealed record AddLocalHolidayRequest(string? Name, DateOnly? Date, bool RepeatsEveryYear);

/// <summary>A local holiday as a whole: the name, the date, the repetition and whether it is considered.</summary>
public sealed record ChangeLocalHolidayRequest(string? Name, DateOnly? Date, bool RepeatsEveryYear, bool IsConsidered);

public sealed record LocalHolidayCreatedResponse(Guid Id);

/// <summary>The calendar of the installation (ADR-0018, ADR-0083), only for the Administrator (ADR-0046).</summary>
public static class CalendarEndpoints
{
    public static RouteGroupBuilder MapCalendarEndpoints(this RouteGroupBuilder api)
    {
        var calendar = api.MapGroup("/calendar")
            .RequireAuthorization(SystemPolicies.Administrator)
            .WithTags("Calendar")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        calendar.MapGet("/{year:int}", GetYearAsync).WithName("GetCalendarYear");
        calendar.MapPut("/working-days", ChangeWorkingDaysAsync).WithName("ChangeWorkingDays");
        calendar.MapPut("/calculated-holidays/{key}", ChooseAsync).WithName("ChooseCalculatedHoliday");
        calendar.MapPut("/considered", ConsiderAsync).WithName("ConsiderHolidays");
        calendar.MapPost("/local-holidays", AddAsync).WithName("AddLocalHoliday");
        calendar.MapPut("/local-holidays/{id:guid}", ChangeAsync).WithName("ChangeLocalHoliday");
        calendar.MapDelete("/local-holidays/{id:guid}", RemoveAsync).WithName("RemoveLocalHoliday");
        return api;
    }

    private static async Task<Results<Ok<CalendarYearResponse>, ValidationProblem>> GetYearAsync(
        int year, GetCalendarYear getYear, CancellationToken cancellationToken)
    {
        var calendar = await getYear.ExecuteAsync(year, cancellationToken);
        if (calendar is null)
        {
            return Invalid("year", $"The calendar has the years {CalendarYears.First} to {CalendarYears.Last}.");
        }

        return TypedResults.Ok(new CalendarYearResponse(
            calendar.Year,
            calendar.WorkingDays.ToDaysOfWeek(),
            [.. calendar.Days.Select(day => new CalendarDayResponse(
                day.Date, day.Kind, day.Key, day.LocalHolidayId, day.Name, day.RepeatsEveryYear, day.IsConsidered))]));
    }

    private static async Task<Results<NoContent, ValidationProblem>> ChangeWorkingDaysAsync(
        WorkingDaysRequest request, ChangeWorkingDays changeWorkingDays, CancellationToken cancellationToken) =>
        await changeWorkingDays.ExecuteAsync(request.WorkingDays ?? [], cancellationToken)
            ? TypedResults.NoContent()
            : Invalid("workingDays", "The week needs at least one working day.");

    private static async Task<Results<NoContent, NotFound>> ChooseAsync(
        string key, CalculatedHolidayChoiceRequest request, ChooseCalculatedHoliday choose, CancellationToken cancellationToken) =>
        await choose.ExecuteAsync(key, request.IsConsidered, cancellationToken) ? TypedResults.NoContent() : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound>> ConsiderAsync(
        ConsiderHolidaysRequest request, ConsiderHolidays considerHolidays, CancellationToken cancellationToken) =>
        await considerHolidays.ExecuteAsync(
            request.CalculatedHolidays ?? [], request.LocalHolidays ?? [], request.IsConsidered, cancellationToken)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();

    private static async Task<Results<Created<LocalHolidayCreatedResponse>, ValidationProblem>> AddAsync(
        AddLocalHolidayRequest request, AddLocalHoliday addLocalHoliday, CancellationToken cancellationToken)
    {
        var added = await addLocalHoliday.ExecuteAsync(
            new LocalHolidayChanges(request.Name, request.Date, request.RepeatsEveryYear), cancellationToken);
        return added.Id is { } id
            ? TypedResults.Created((string?)null, new LocalHolidayCreatedResponse(id))
            : Invalid(added.Invalid);
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound>> ChangeAsync(
        Guid id, ChangeLocalHolidayRequest request, ChangeLocalHoliday changeLocalHoliday, CancellationToken cancellationToken)
    {
        var changed = await changeLocalHoliday.ExecuteAsync(
            id, new LocalHolidayChanges(request.Name, request.Date, request.RepeatsEveryYear), request.IsConsidered, cancellationToken);
        if (!changed.Found)
        {
            return TypedResults.NotFound();
        }

        return changed.Invalid.Count == 0 ? TypedResults.NoContent() : Invalid(changed.Invalid);
    }

    private static async Task<Results<NoContent, NotFound>> RemoveAsync(
        Guid id, RemoveLocalHoliday removeLocalHoliday, CancellationToken cancellationToken) =>
        await removeLocalHoliday.ExecuteAsync(id, cancellationToken) ? TypedResults.NoContent() : TypedResults.NotFound();

    private static ValidationProblem Invalid(IReadOnlyList<LocalHolidayField> fields) =>
        TypedResults.ValidationProblem(fields.ToDictionary(
            field => field == LocalHolidayField.Name ? "name" : "date",
            field => new[]
            {
                field == LocalHolidayField.Name
                    ? $"The name is needed and has up to {LocalHoliday.NameMaxLength} characters."
                    : $"The date is needed, in the years {CalendarYears.First} to {CalendarYears.Last}.",
            }));

    private static ValidationProblem Invalid(string field, string reason) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [reason] });
}
