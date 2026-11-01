using FiscoProjects.SharedKernel;

namespace FiscoProjects.TimeTracking.Domain;

/// <summary>The hours of the module are counted in quarters of an hour, from a least to a most (ADR-0128).</summary>
public static class Quarters
{
    /// <summary>A quarter of an hour, the step of every number of hours.</summary>
    public const decimal Step = 0.25m;

    /// <summary>Whether the hours go from the least to the most, in quarters of an hour.</summary>
    public static bool IsValid(decimal hours, decimal least, decimal most) =>
        hours >= least && hours <= most && decimal.Remainder(hours, Step) == 0;
}

/// <summary>
/// The hours sold with a project (ADR-0128): only the Administrator registers them, and without them the project has
/// no balance. One per project, removed when the Administrator takes them out.
/// </summary>
public sealed class ContractedHours : Entity
{
    /// <summary>The most contracted hours of a project.</summary>
    public const decimal Most = 99_999m;

    private ContractedHours()
    {
    }

    public ContractedHours(Guid projectId, decimal hours, DateTime now)
    {
        ProjectId = projectId;
        Change(hours, now);
    }

    public Guid ProjectId { get; private init; }

    public decimal Hours { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>More than zero, up to 99,999, in quarters of an hour.</summary>
    public static bool IsValid(decimal hours) => Quarters.IsValid(hours, Quarters.Step, Most);

    public void Change(decimal hours, DateTime now)
    {
        if (!IsValid(hours))
        {
            throw new ArgumentOutOfRangeException(nameof(hours), hours, "Contracted hours go from 0.25 to 99,999, in quarters.");
        }

        Hours = hours;
        UpdatedAt = now;
    }
}

/// <summary>
/// The hours a task of the schedule should take (ADR-0138): only the Administrator puts, changes and takes them out, on
/// any task (also a summary task or a subtask, never a milestone or a phase), compared with the hours logged in it. One
/// per task, out of the schedule: the dates, the undo, the baseline and the templates do not see them.
/// </summary>
public sealed class PlannedHours : Entity
{
    /// <summary>The most planned hours of a task.</summary>
    public const decimal Most = 9_999m;

    private PlannedHours()
    {
    }

    public PlannedHours(Guid projectId, Guid scheduleItemId, decimal hours, DateTime now)
    {
        ProjectId = projectId;
        ScheduleItemId = scheduleItemId;
        Change(hours, now);
    }

    public Guid ProjectId { get; private init; }

    /// <summary>The task of the schedule of the project.</summary>
    public Guid ScheduleItemId { get; private init; }

    public decimal Hours { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>More than zero, up to 9,999, in quarters of an hour.</summary>
    public static bool IsValid(decimal hours) => Quarters.IsValid(hours, Quarters.Step, Most);

    public void Change(decimal hours, DateTime now)
    {
        if (!IsValid(hours))
        {
            throw new ArgumentOutOfRangeException(nameof(hours), hours, "Planned hours go from 0.25 to 9,999, in quarters.");
        }

        Hours = hours;
        UpdatedAt = now;
    }
}

/// <summary>
/// The hours of a task (ADR-0138): the planned ones (null without them), the ones logged in it by anyone and the balance,
/// by the rule of the balance of a project in one place (<see cref="HoursBalance"/>), the task done in the place of the
/// closed project: a done task is never near the planned hours, as it gets no more work.
/// </summary>
public sealed record TaskHours(Guid ItemId, string Name, decimal? Planned, decimal Logged, HoursBalance Balance)
{
    public static TaskHours Of(Guid itemId, string name, decimal? planned, decimal logged, bool isDone) =>
        new(itemId, name, planned, logged, HoursBalance.Of(planned, 0, logged, isDone));
}

/// <summary>The fields of additional hours, to tell which ones are not valid.</summary>
public enum AdditionalHoursField
{
    Day = 1,
    Hours = 2,
    Reason = 3,
}

/// <summary>
/// Hours added to a project when the client asks, in the middle of it, for something out of the scope (ADR-0128):
/// the day, the hours and the reason, registered by the Administrator, who is kept.
/// </summary>
public sealed class AdditionalHours : Entity
{
    public const int ReasonMaxLength = 500;

    /// <summary>The most hours of one addition.</summary>
    public const decimal Most = 9_999m;

    private AdditionalHours()
    {
        Reason = string.Empty;
    }

    public AdditionalHours(Guid projectId, Guid createdBy, AdditionalHoursForm form, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(form);
        ProjectId = projectId;
        CreatedBy = createdBy;
        CreatedAt = now;
        Reason = form.Reason;
        Change(form, now);
    }

    public Guid ProjectId { get; private init; }

    public DateOnly Day { get; private set; }

    public decimal Hours { get; private set; }

    /// <summary>The request out of the scope that brought the hours.</summary>
    public string Reason { get; private set; }

    /// <summary>The Administrator who registered them.</summary>
    public Guid CreatedBy { get; private init; }

    public DateTime CreatedAt { get; private init; }

    public DateTime UpdatedAt { get; private set; }

    public void Change(AdditionalHoursForm form, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(form);
        Day = form.Day;
        Hours = form.Hours;
        Reason = form.Reason;
        UpdatedAt = now;
    }
}

/// <summary>Additional hours as the Administrator filled them in, already checked against the day of today.</summary>
public sealed record AdditionalHoursForm(DateOnly Day, decimal Hours, string Reason)
{
    /// <summary>
    /// The form, with the reason trimmed, or the fields that are not valid: the day needed and not after today; the
    /// hours from 0.25 to 9,999 in quarters of an hour; the reason needed, up to 500 characters.
    /// </summary>
    public static (AdditionalHoursForm? Form, IReadOnlyList<AdditionalHoursField> Invalid) Of(
        DateOnly? day, decimal? hours, string? reason, DateOnly today)
    {
        var invalid = new List<AdditionalHoursField>();
        if (day is not { } validDay || validDay > today)
        {
            invalid.Add(AdditionalHoursField.Day);
        }

        if (hours is not { } validHours || !Quarters.IsValid(validHours, Quarters.Step, AdditionalHours.Most))
        {
            invalid.Add(AdditionalHoursField.Hours);
        }

        var text = reason?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > AdditionalHours.ReasonMaxLength || text.Any(IsForbidden))
        {
            invalid.Add(AdditionalHoursField.Reason);
        }

        return invalid.Count > 0 ? (null, invalid) : (new AdditionalHoursForm(day!.Value, hours!.Value, text!), invalid);
    }

    // Line breaks and tabs are text the Administrator typed; the other control characters are not.
    private static bool IsForbidden(char character) => char.IsControl(character) && character is not ('\n' or '\r' or '\t');
}

/// <summary>The state of the balance of a project (ADR-0128), shown by icon, color and text (ADR-0065).</summary>
public enum HoursBalanceState
{
    /// <summary>The logged hours went past the contracted and the additional ones.</summary>
    Over = 1,

    /// <summary>An open project that used 90% or more of its hours.</summary>
    NearLimit = 2,

    /// <summary>Within the hours.</summary>
    Within = 3,

    /// <summary>A project without contracted hours, which has no balance.</summary>
    NoContract = 4,
}

/// <summary>
/// The balance of the hours of a project, in one place (ADR-0128): the contracted and the additional hours less the
/// logged ones, the share used, rounded to a whole percentage, and the state. Only hours, never money.
/// </summary>
public sealed record HoursBalance(
    decimal? Contracted, decimal Additional, decimal Logged, decimal? Total, decimal? Balance, int? Consumption, HoursBalanceState State)
{
    /// <summary>From this share up, an open project is near its limit.</summary>
    public const int NearLimitShare = 90;

    public static HoursBalance Of(decimal? contracted, decimal additional, decimal logged, bool isClosed)
    {
        if (contracted is not { } sold)
        {
            return new HoursBalance(null, additional, logged, null, null, null, HoursBalanceState.NoContract);
        }

        var total = sold + additional;
        var consumption = (int)Math.Round(logged * 100m / total, MidpointRounding.AwayFromZero);
        var state = logged > total
            ? HoursBalanceState.Over
            : !isClosed && logged * 100m >= total * NearLimitShare
                ? HoursBalanceState.NearLimit
                : HoursBalanceState.Within;
        return new HoursBalance(sold, additional, logged, total, total - logged, consumption, state);
    }

    /// <summary>
    /// The order of the list of the projects: the most used first, by the exact share, and the projects without
    /// contracted hours last; ties by the code.
    /// </summary>
    public static IOrderedEnumerable<T> Order<T>(IEnumerable<T> rows, Func<T, HoursBalance> balanceOf, Func<T, string> codeOf) =>
        rows.OrderBy(row => balanceOf(row).Total is null ? 1 : 0)
            .ThenByDescending(row => balanceOf(row) is { Total: { } total } balance ? balance.Logged / total : 0m)
            .ThenBy(codeOf, StringComparer.OrdinalIgnoreCase);
}
