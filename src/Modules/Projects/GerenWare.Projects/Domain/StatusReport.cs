using GerenWare.SharedKernel;

namespace GerenWare.Projects.Domain;

/// <summary>How serious a risk of the status report is, as the manager classifies it (ADR-0165).</summary>
public enum RiskLevel
{
    High = 1,
    Medium = 2,
    Low = 3,
}

/// <summary>The limits of the status report (ADR-0165), in one place: what is written and how much of each list fits.</summary>
public static class StatusReportLimits
{
    /// <summary>Each of the four texts of the manager, which fit in the boxes of the default template at its font size.</summary>
    public const int TextMaxLength = 1000;

    /// <summary>The point of attention of a risk.</summary>
    public const int AttentionMaxLength = 500;

    /// <summary>The tickets that go in as risks, one slide each.</summary>
    public const int MaxRisks = 10;

    public const int Phases = 10;

    public const int Milestones = 5;

    public const int LateTasks = 5;

    public const int TaskHours = 15;

    public const int RiskMilestones = 5;
}

/// <summary>A field of the status report the person fills in, when it is refused (400).</summary>
public enum StatusReportField
{
    Summary = 1,
    Focus = 2,
    NextSteps = 3,
    ClientNeeds = 4,
    Risks = 5,
}

/// <summary>The four texts the manager writes (ADR-0165): the summary of the period, the current focus, the next steps and what the client must do.</summary>
public sealed record StatusReportTexts(string Summary, string Focus, string NextSteps, string ClientNeeds)
{
    public static StatusReportTexts Empty { get; } = new(string.Empty, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// A text the person wrote, without the spaces at its ends and with the line breaks as <c>\n</c>, up to the limit;
    /// null when it is longer or has a control character that is not a line break or a tab.
    /// </summary>
    public static string? TextOf(string? text, int maxLength)
    {
        var clean = (text ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Trim();
        return clean.Length > maxLength || clean.Any(character => char.IsControl(character) && character is not ('\n' or '\t'))
            ? null
            : clean;
    }
}

/// <summary>A ticket that goes in the report as a risk, with the level and the point of attention the manager wrote.</summary>
public sealed record StatusReportRisk(int TicketNumber, RiskLevel Level, string Attention);

/// <summary>A risk as the person sent it, before the checks.</summary>
public sealed record StatusReportRiskInput(int Number, RiskLevel? Level, string? Attention);

/// <summary>
/// What the person chose and wrote to generate a status report (ADR-0165, ADR-0166): the four texts, up to 1,000
/// characters each; from 0 to 10 open tickets of the project as risks, without repeating, each with its level and a
/// point of attention up to 500 characters; and whether the hours go in.
/// </summary>
public sealed record StatusReportForm(StatusReportTexts Texts, IReadOnlyList<StatusReportRisk> Risks, bool ShowHours)
{
    /// <returns>The form, or the fields refused.</returns>
    public static (StatusReportForm? Form, IReadOnlyList<StatusReportField> Invalid) Of(
        string? summary,
        string? focus,
        string? nextSteps,
        string? clientNeeds,
        IReadOnlyList<StatusReportRiskInput>? risks,
        bool showHours,
        IReadOnlySet<int> openTickets)
    {
        ArgumentNullException.ThrowIfNull(openTickets);
        var invalid = new List<StatusReportField>();
        string Text(string? text, StatusReportField field)
        {
            var clean = StatusReportTexts.TextOf(text, StatusReportLimits.TextMaxLength);
            if (clean is null)
            {
                invalid.Add(field);
            }

            return clean ?? string.Empty;
        }

        var texts = new StatusReportTexts(
            Text(summary, StatusReportField.Summary),
            Text(focus, StatusReportField.Focus),
            Text(nextSteps, StatusReportField.NextSteps),
            Text(clientNeeds, StatusReportField.ClientNeeds));
        var chosen = risks ?? [];
        var accepted = chosen
            .Select(risk => (Risk: risk, Attention: StatusReportTexts.TextOf(risk.Attention, StatusReportLimits.AttentionMaxLength)))
            .Where(entry => entry.Risk.Level is { } level && Enum.IsDefined(level)
                && entry.Attention is not null && openTickets.Contains(entry.Risk.Number))
            .Select(entry => new StatusReportRisk(entry.Risk.Number, entry.Risk.Level!.Value, entry.Attention!))
            .ToList();
        if (chosen.Count > StatusReportLimits.MaxRisks
            || accepted.Count != chosen.Count
            || accepted.Select(risk => risk.TicketNumber).Distinct().Count() != accepted.Count)
        {
            invalid.Add(StatusReportField.Risks);
        }

        return invalid.Count > 0 ? (null, invalid) : (new StatusReportForm(texts, accepted, showHours), []);
    }
}

/// <summary>
/// What the last status report of a project said (ADR-0165), so the next draft comes filled in: the four texts and the
/// risks, with when and by whom. One per project, replaced at each report; out of the audit, because the document of
/// the report is in it.
/// </summary>
public sealed class StatusReportNotes : Entity
{
    private readonly List<StatusReportRiskNote> _risks = [];

    private StatusReportNotes()
    {
        Summary = string.Empty;
        Focus = string.Empty;
        NextSteps = string.Empty;
        ClientNeeds = string.Empty;
    }

    public StatusReportNotes(Guid projectId)
        : this()
    {
        ProjectId = projectId;
    }

    public Guid ProjectId { get; private init; }

    public string Summary { get; private set; }

    public string Focus { get; private set; }

    public string NextSteps { get; private set; }

    public string ClientNeeds { get; private set; }

    /// <summary>When the last report was generated, in UTC.</summary>
    public DateTime GeneratedAt { get; private set; }

    public Guid GeneratedById { get; private set; }

    /// <summary>The risks of the last report, in their order.</summary>
    public IReadOnlyList<StatusReportRiskNote> Risks => [.. _risks.OrderBy(risk => risk.Position)];

    public StatusReportTexts Texts => new(Summary, Focus, NextSteps, ClientNeeds);

    /// <summary>Keeps what a report generated now said, in place of the last one.</summary>
    public void Keep(StatusReportForm form, Guid userId, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(form);
        Summary = form.Texts.Summary;
        Focus = form.Texts.Focus;
        NextSteps = form.Texts.NextSteps;
        ClientNeeds = form.Texts.ClientNeeds;
        GeneratedAt = now;
        GeneratedById = userId;
        _risks.Clear();
        _risks.AddRange(form.Risks.Select((risk, position) =>
            new StatusReportRiskNote(Id, risk.TicketNumber, risk.Level, risk.Attention, position)));
    }
}

/// <summary>A risk of the last status report of a project: the ticket, the level, the point of attention and its order.</summary>
public sealed class StatusReportRiskNote : Entity
{
    private StatusReportRiskNote()
    {
        Attention = string.Empty;
    }

    internal StatusReportRiskNote(Guid notesId, int ticketNumber, RiskLevel level, string attention, int position)
    {
        NotesId = notesId;
        TicketNumber = ticketNumber;
        Level = level;
        Attention = attention;
        Position = position;
    }

    public Guid NotesId { get; private init; }

    public int TicketNumber { get; private init; }

    public RiskLevel Level { get; private init; }

    public string Attention { get; private init; }

    public int Position { get; private init; }
}

/// <summary>
/// The risks a new draft suggests (ADR-0165), in one place: the risks of the last report that are still open, in their
/// order, with the level and the point of attention of then; then the open tickets that threaten milestones (ADR-0164),
/// in the order of the threats, with the level Medium and no point of attention. At most the limit of the risks.
/// </summary>
public static class StatusReportSuggestions
{
    public static IReadOnlyList<StatusReportRisk> Of(
        StatusReportNotes? last, IReadOnlySet<int> openTickets, IEnumerable<int> threatening)
    {
        ArgumentNullException.ThrowIfNull(openTickets);
        ArgumentNullException.ThrowIfNull(threatening);
        var kept = (last?.Risks ?? [])
            .Where(risk => openTickets.Contains(risk.TicketNumber))
            .Select(risk => new StatusReportRisk(risk.TicketNumber, risk.Level, risk.Attention));
        var suggested = threatening
            .Where(openTickets.Contains)
            .Select(number => new StatusReportRisk(number, RiskLevel.Medium, string.Empty));
        return [.. kept.Concat(suggested).DistinctBy(risk => risk.TicketNumber).Take(StatusReportLimits.MaxRisks)];
    }
}
