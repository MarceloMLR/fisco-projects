using System.Globalization;
using System.Text.RegularExpressions;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Administration.Domain;

/// <summary>The theme a logo is for (ADR-0100): the light one, the logo of always, or the dark one, optional.</summary>
public enum LogoTheme
{
    Light = 1,
    Dark = 2,
}

/// <summary>
/// The settings of the installation the Administrator changes in General settings (ADR-0003, ADR-0011): the name
/// shown, the logo and the one for the dark theme, the main color, the time zone, the time of the daily summary and how
/// long the notifications are kept. There is one, created with the database.
/// </summary>
public sealed partial class InstallationSettings : Entity
{
    /// <summary>The name shown while nobody changes it: the product name, not the name of a company.</summary>
    public const string DefaultName = "FiscoProjects";

    public const int NameMaxLength = 60;

    /// <summary>The time zone while nobody changes it (ADR-0013).</summary>
    public const string DefaultTimeZone = "America/Sao_Paulo";

    public const int TimeZoneMaxLength = 64;

    /// <summary>The time of the daily summary of e-mails while nobody changes it (ADR-0037).</summary>
    public static readonly TimeOnly DefaultDailySummaryAt = new(7, 30);

    /// <summary>How many days the notifications of the bell are kept while nobody changes it (ADR-0038).</summary>
    public const int DefaultNotificationRetentionDays = 90;

    public const int MinNotificationRetentionDays = 1;

    public const int MaxNotificationRetentionDays = 365;

    /// <summary>How many years the audit is kept while nobody changes it (ADR-0058, ADR-0110).</summary>
    public const int DefaultAuditRetentionYears = 5;

    public const int MinAuditRetentionYears = 1;

    public const int MaxAuditRetentionYears = 10;

    /// <summary>
    /// Contrast a main color needs with white (ADR-0082): it is the background of the white label of the main button
    /// and the color of the links over white surfaces (WCAG AA).
    /// </summary>
    public const double MinimumContrastWithWhite = 4.5;

    /// <summary>Id of the one row, created by the first migration of the module.</summary>
    public static readonly Guid SingleId = new("0199f2a0-6b5c-7d1e-9a42-3c8e5f7a1b20");

    private InstallationSettings()
    {
    }

    public string Name { get; private set; } = DefaultName;

    /// <summary>The IANA id of the time zone the screens show dates and times in (ADR-0013).</summary>
    public string TimeZone { get; private set; } = DefaultTimeZone;

    /// <summary>The time of the daily summary of e-mails, in the time zone of the installation (ADR-0037).</summary>
    public TimeOnly DailySummaryAt { get; private set; } = DefaultDailySummaryAt;

    /// <summary>How many days the notifications of the bell are kept, read or not (ADR-0038).</summary>
    public int NotificationRetentionDays { get; private set; } = DefaultNotificationRetentionDays;

    /// <summary>How many years the audit is kept; the monthly cleanup removes what is older (ADR-0058, ADR-0110).</summary>
    public int AuditRetentionYears { get; private set; } = DefaultAuditRetentionYears;

    /// <summary>
    /// The folder of the projects in the file storage, with the folders separated by "/": where the folder of each new
    /// project is created; empty for the root of the storage (ADR-0121).
    /// </summary>
    public string ProjectsFolder { get; private set; } = string.Empty;

    /// <summary>The main color as #RRGGBB, or null for the color of the design system (ADR-0082).</summary>
    public string? MainColor { get; private set; }

    /// <summary>Id of the logo in the file storage; null without a logo.</summary>
    public string? LogoFileId { get; private set; }

    /// <summary>Name of the file the Administrator sent, as the audit shows it.</summary>
    public string? LogoFileName { get; private set; }

    public string? LogoContentType { get; private set; }

    /// <summary>When the logo last changed, so its address changes with it.</summary>
    public DateTime? LogoChangedAt { get; private set; }

    public bool HasLogo => LogoFileId is not null;

    /// <summary>Id of the logo for the dark theme in the file storage; null without one, and the light one shows (ADR-0100).</summary>
    public string? DarkLogoFileId { get; private set; }

    /// <summary>Name of the file of the logo for the dark theme the Administrator sent, as the audit shows it.</summary>
    public string? DarkLogoFileName { get; private set; }

    public string? DarkLogoContentType { get; private set; }

    /// <summary>When the logo for the dark theme last changed, so its address changes with it.</summary>
    public DateTime? DarkLogoChangedAt { get; private set; }

    public bool HasDarkLogo => DarkLogoFileId is not null;

    /// <summary>The name without the spaces at the ends, when it is not empty and has up to 60 characters.</summary>
    public static string? ValidName(string? name)
    {
        var trimmed = name?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > NameMaxLength ? null : trimmed;
    }

    /// <summary>
    /// An IANA time zone the server knows, such as America/Sao_Paulo; a Windows id is refused, since the browsers
    /// know only the IANA ones.
    /// </summary>
    public static bool IsValidTimeZone(string? timeZone) =>
        !string.IsNullOrWhiteSpace(timeZone)
        && timeZone.Length <= TimeZoneMaxLength
        && TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var zone)
        && zone.HasIanaId;

    public void ChangeTimeZone(string timeZone) =>
        TimeZone = IsValidTimeZone(timeZone)
            ? timeZone
            : throw new ArgumentException($"'{timeZone}' is no IANA time zone known to the server.", nameof(timeZone));

    /// <summary>
    /// The main color as it is kept: null (or empty) for the default color, or #RRGGBB in capitals when it has the
    /// contrast it needs with white; false for anything else.
    /// </summary>
    public static bool TryNormalizeMainColor(string? value, out string? color)
    {
        color = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim();
        if (!HexColor().IsMatch(trimmed) || ContrastWithWhite(trimmed) < MinimumContrastWithWhite)
        {
            return false;
        }

        color = trimmed.ToUpperInvariant();
        return true;
    }

    /// <summary>Contrast ratio of a #RRGGBB color with white, by the relative luminance of WCAG 2.x.</summary>
    public static double ContrastWithWhite(string hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);
        var luminance = (0.2126 * Channel(hex, 1)) + (0.7152 * Channel(hex, 3)) + (0.0722 * Channel(hex, 5));
        return 1.05 / (luminance + 0.05);
    }

    public void ChangeMainColor(string? color) =>
        MainColor = TryNormalizeMainColor(color, out var normalized)
            ? normalized
            : throw new ArgumentException(
                $"'{color}' is no #RRGGBB color with a contrast of {MinimumContrastWithWhite}:1 with white.", nameof(color));

    /// <summary>The daily summary at the hour and minute, without seconds.</summary>
    public void ChangeDailySummaryAt(TimeOnly at) => DailySummaryAt = new TimeOnly(at.Hour, at.Minute);

    public static bool IsValidNotificationRetention(int days) =>
        days is >= MinNotificationRetentionDays and <= MaxNotificationRetentionDays;

    public void ChangeNotificationRetention(int days) =>
        NotificationRetentionDays = IsValidNotificationRetention(days)
            ? days
            : throw new ArgumentOutOfRangeException(
                nameof(days), days, $"The notifications are kept from {MinNotificationRetentionDays} to {MaxNotificationRetentionDays} days.");

    public static bool IsValidAuditRetention(int years) =>
        years is >= MinAuditRetentionYears and <= MaxAuditRetentionYears;

    public void ChangeAuditRetention(int years) =>
        AuditRetentionYears = IsValidAuditRetention(years)
            ? years
            : throw new ArgumentOutOfRangeException(
                nameof(years), years, $"The audit is kept from {MinAuditRetentionYears} to {MaxAuditRetentionYears} years.");

    /// <summary>The folder of the projects, already checked against the rules of names of the storage.</summary>
    public void ChangeProjectsFolder(string folder) => ProjectsFolder = folder;

    public void Rename(string name) =>
        Name = ValidName(name) ?? throw new ArgumentException(
            $"The name of the installation is needed and has up to {NameMaxLength} characters.", nameof(name));

    /// <summary>The file of the logo of a theme in the storage; null without one.</summary>
    public string? LogoFileIdOf(LogoTheme theme) => theme == LogoTheme.Dark ? DarkLogoFileId : LogoFileId;

    public string? LogoContentTypeOf(LogoTheme theme) => theme == LogoTheme.Dark ? DarkLogoContentType : LogoContentType;

    public DateTime? LogoChangedAtOf(LogoTheme theme) => theme == LogoTheme.Dark ? DarkLogoChangedAt : LogoChangedAt;

    public void ReplaceLogo(string fileId, string fileName, string contentType, DateTime changedAt, LogoTheme theme = LogoTheme.Light)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (theme == LogoTheme.Dark)
        {
            DarkLogoFileId = fileId;
            DarkLogoFileName = fileName;
            DarkLogoContentType = contentType;
            DarkLogoChangedAt = changedAt;
            return;
        }

        LogoFileId = fileId;
        LogoFileName = fileName;
        LogoContentType = contentType;
        LogoChangedAt = changedAt;
    }

    public void RemoveLogo(LogoTheme theme = LogoTheme.Light)
    {
        if (theme == LogoTheme.Dark)
        {
            DarkLogoFileId = null;
            DarkLogoFileName = null;
            DarkLogoContentType = null;
            DarkLogoChangedAt = null;
            return;
        }

        LogoFileId = null;
        LogoFileName = null;
        LogoContentType = null;
        LogoChangedAt = null;
    }

    private static double Channel(string hex, int start)
    {
        var value = int.Parse(hex.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColor();
}
