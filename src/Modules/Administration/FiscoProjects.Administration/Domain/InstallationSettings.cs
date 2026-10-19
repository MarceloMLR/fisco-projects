using FiscoProjects.SharedKernel;

namespace FiscoProjects.Administration.Domain;

/// <summary>
/// The settings of the installation the Administrator changes in General settings (ADR-0003, ADR-0011): for now the
/// name shown and the logo. There is one, created with the database.
/// </summary>
public sealed class InstallationSettings : Entity
{
    /// <summary>The name shown while nobody changes it: the product name, not the name of a company.</summary>
    public const string DefaultName = "FiscoProjects";

    public const int NameMaxLength = 60;

    /// <summary>Id of the one row, created by the first migration of the module.</summary>
    public static readonly Guid SingleId = new("0199f2a0-6b5c-7d1e-9a42-3c8e5f7a1b20");

    private InstallationSettings()
    {
    }

    public string Name { get; private set; } = DefaultName;

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
}
