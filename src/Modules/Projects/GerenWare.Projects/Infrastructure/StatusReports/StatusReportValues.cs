namespace GerenWare.Projects.Infrastructure.StatusReports;

/// <summary>
/// The fields of the status report (ADR-0165), in one place: the lists, whose row of a table repeats for each item; the
/// slide repeated for each risk; the slide only with "Mostrar horas"; and the shapes by name. The conference of the
/// template of the company (Status-6) checks them.
/// </summary>
internal static class StatusReportFields
{
    /// <summary>The fields of the slide repeated for each risk start with it.</summary>
    public const string Risk = "risco";

    /// <summary>The fields of the slide only with "Mostrar horas" start with it.</summary>
    public const string Hours = "horas";

    public const string Phase = "fase";

    public const string Milestone = "marco";

    public const string LateTask = "atraso";

    public const string TaskHours = "horasTarefa";

    public const string RiskMilestone = "riscoMarco";

    /// <summary>The lists whose rows repeat in a table.</summary>
    public static IReadOnlyList<string> Lists { get; } = [Phase, Milestone, LateTask, TaskHours, RiskMilestone];

    /// <summary>The shape of the logo of the installation: a picture with the logo or, without it, the name.</summary>
    public const string InstallationLogo = "logo.instalacao";

    /// <summary>The shape of the logo of the client: a picture with the logo or, without it, the name.</summary>
    public const string ClientLogo = "logo.cliente";

    /// <summary>The bar of the progress, as wide as the share of its track (<see cref="ProgressTrack"/>).</summary>
    public const string ProgressBar = "barra.andamento";

    public const string ProgressTrack = "barra.andamento.trilho";

    /// <summary>The tag of the level of a risk, colored by it, with its icon (<c>.icone</c>).</summary>
    public const string RiskLevelTag = "risco.classificacao";

    /// <summary>The tag of the state of the hours, colored by it, with its icon (<c>.icone</c>).</summary>
    public const string HoursStateTag = "horas.situacao";

    /// <summary>The name of the icon of a tag.</summary>
    public static string IconOf(string tag) => $"{tag}.icone";

    /// <summary>The prefix of a field, the part before the first dot.</summary>
    public static string PrefixOf(string field) => field.Split('.')[0];
}

/// <summary>How a tag shows a state (ADR-0065): by icon, color and text, with the colors of the design system that never change.</summary>
internal enum ReportTone
{
    Danger = 1,
    Warning = 2,
    Success = 3,
    Neutral = 4,
}

/// <summary>An image of a logo: its content and its type.</summary>
internal sealed record ReportPicture(byte[] Content, string ContentType);

/// <summary>
/// The values of one place of the report: the fields, the lists of items, the tags with their tones, the shares of the
/// bars and the logos. A field not found here is looked for in the place around it (the report, for a risk).
/// </summary>
internal sealed class ReportScope
{
    public Dictionary<string, string> Fields { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, IReadOnlyList<ReportScope>> Lists { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, ReportTone> Tones { get; } = new(StringComparer.Ordinal);

    /// <summary>The share of each bar, from 0 to 1.</summary>
    public Dictionary<string, decimal> Bars { get; } = new(StringComparer.Ordinal);

    /// <summary>The logo of each shape; none keeps the text of the shape, the name.</summary>
    public Dictionary<string, ReportPicture?> Pictures { get; } = new(StringComparer.Ordinal);
}

/// <summary>The values of a status report: of the report, of each risk, and whether the hours go in (ADR-0165, ADR-0166).</summary>
internal sealed record StatusReportValues(ReportScope Report, IReadOnlyList<ReportScope> Risks, bool ShowHours);
