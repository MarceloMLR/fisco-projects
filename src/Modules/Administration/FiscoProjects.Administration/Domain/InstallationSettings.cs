using System.Globalization;
using System.Text.RegularExpressions;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Administration.Domain;

/// <summary>
/// The settings of the installation the Administrator changes in General settings (ADR-0003, ADR-0011): the name
/// shown, the logo, the main color and the time zone. There is one, created with the database.
/// </summary>
public sealed partial class InstallationSettings : Entity
{
    /// <summary>The name shown while nobody changes it: the product name, not the name of a company.</summary>
    public const string DefaultName = "FiscoProjects";

    public const int NameMaxLength = 60;

    /// <summary>The time zone while nobody changes it (ADR-0013).</summary>
    public const string DefaultTimeZone = "America/Sao_Paulo";

    public const int TimeZoneMaxLength = 64;

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

    public void Rename(string name) =>
        Name = ValidName(name) ?? throw new ArgumentException(
            $"The name of the installation is needed and has up to {NameMaxLength} characters.", nameof(name));

    public void ReplaceLogo(string fileId, string fileName, string contentType, DateTime changedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        LogoFileId = fileId;
        LogoFileName = fileName;
        LogoContentType = contentType;
        LogoChangedAt = changedAt;
    }

    public void RemoveLogo()
    {
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
