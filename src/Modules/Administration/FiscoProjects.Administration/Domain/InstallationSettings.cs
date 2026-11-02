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
/// shown, the logo and the one for the dark theme, the time zone, the time of the daily summary and how long the
/// notifications are kept; and the colors the Administrator changes in Colors (ADR-0146). There is one, created with the
/// database.
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

    /// <summary>How many hours fit in a working day while nobody changes it, for the load of the team (ADR-0142).</summary>
    public const decimal DefaultWorkHoursPerDay = 8m;

    public const decimal MinWorkHoursPerDay = 1m;

    public const decimal MaxWorkHoursPerDay = 12m;

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

    /// <summary>How many hours fit in a business day, in quarters of an hour, for the capacity of the load of the team (ADR-0142).</summary>
    public decimal WorkHoursPerDay { get; private set; } = DefaultWorkHoursPerDay;

    /// <summary>
    /// The folder of the projects in the file storage, with the folders separated by "/": where the folder of each new
    /// project is created; empty for the root of the storage (ADR-0121).
    /// </summary>
    public string ProjectsFolder { get; private set; } = string.Empty;

    /// <summary>The main color as #RRGGBB, or null for the color of the design system (ADR-0082, ADR-0146).</summary>
    public string? MainColor { get; private set; }

    /// <summary>
    /// The secondary color as #RRGGBB, or null: the selected row, the unread notification, the mention and the panel of
    /// the sign-in page (ADR-0146).
    /// </summary>
    public string? SecondaryColor { get; private set; }

    /// <summary>The color of the sidebar as #RRGGBB, or null for the sidebar of the design system (ADR-0146).</summary>
    public string? SidebarColor { get; private set; }

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
    /// A color as it is kept: null (or empty) for the default one, or #RRGGBB in capitals; false for anything else. Any
    /// color is accepted: the screens work out the text over it (ADR-0146).
    /// </summary>
    public static bool TryNormalizeColor(string? value, out string? color)
    {
        color = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim();
        if (!HexColor().IsMatch(trimmed))
        {
            return false;
        }

        color = trimmed.ToUpperInvariant();
        return true;
    }

    /// <summary>The three colors of the installation, each #RRGGBB or null for the default one (ADR-0146).</summary>
    public void ChangeColors(string? mainColor, string? secondaryColor, string? sidebarColor)
    {
        MainColor = Normalized(mainColor, nameof(mainColor));
        SecondaryColor = Normalized(secondaryColor, nameof(secondaryColor));
        SidebarColor = Normalized(sidebarColor, nameof(sidebarColor));
    }

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

    /// <summary>From 1 to 12 hours, in quarters of an hour (ADR-0142).</summary>
    public static bool IsValidWorkHoursPerDay(decimal hours) =>
        hours is >= MinWorkHoursPerDay and <= MaxWorkHoursPerDay && decimal.Remainder(hours * 4m, 1m) == 0m;

    public void ChangeWorkHoursPerDay(decimal hours) =>
        WorkHoursPerDay = IsValidWorkHoursPerDay(hours)
            ? hours
            : throw new ArgumentOutOfRangeException(
                nameof(hours), hours, $"A working day has from {MinWorkHoursPerDay} to {MaxWorkHoursPerDay} hours, in quarters of an hour.");

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

    private static string? Normalized(string? color, string parameter) =>
        TryNormalizeColor(color, out var normalized)
            ? normalized
            : throw new ArgumentException($"'{color}' is no #RRGGBB color.", parameter);

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColor();
}
