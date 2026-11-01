using FiscoProjects.SharedKernel;

namespace FiscoProjects.Projects.Domain;

/// <summary>A period of an absence, from its first to its last day.</summary>
public sealed record AbsencePeriod(DateOnly Start, DateOnly End);

/// <summary>
/// A period in which a person does not work, as vacations or a day off (ADR-0141): only the first and the last day, from
/// one to 366 calendar days, never over another absence of the same person, without a reason. Registered by the
/// Administrator or the manager of a project of the person, kept until a year after its end, and erased with the
/// anonymization of the person (ADR-0058, ADR-0110).
/// </summary>
public sealed class PersonAbsence : Entity
{
    /// <summary>The most calendar days of one absence.</summary>
    public const int MaxDays = 366;

    private PersonAbsence()
    {
    }

    public PersonAbsence(Guid userId, DateOnly start, DateOnly end, Guid createdBy, DateTime now)
    {
        if (!IsValid(start, end))
        {
            throw new ArgumentException("An absence goes from 1 to 366 calendar days, from 2000 to 2099.", nameof(end));
        }

        UserId = userId;
        Start = start;
        End = end;
        CreatedBy = createdBy;
        CreatedAt = now;
    }

    /// <summary>The person away.</summary>
    public Guid UserId { get; private init; }

    public DateOnly Start { get; private init; }

    public DateOnly End { get; private init; }

    /// <summary>Who registered it.</summary>
    public Guid CreatedBy { get; private init; }

    public DateTime CreatedAt { get; private init; }

    public AbsencePeriod Period => new(Start, End);

    /// <summary>The first day on or before the last one, from 1 to 366 calendar days, in the days a schedule may use.</summary>
    public static bool IsValid(DateOnly start, DateOnly end) =>
        start <= end && end.DayNumber - start.DayNumber < MaxDays && Schedule.IsValidDay(start) && Schedule.IsValidDay(end);

    /// <summary>Whether the absence has a day in the period, the ends included.</summary>
    public bool Overlaps(DateOnly start, DateOnly end) => Start <= end && start <= End;

    /// <summary>The last day the absence is kept: a year after its end (ADR-0141).</summary>
    public static DateOnly KeptUntil(DateOnly end) => end.AddYears(1);
}
