using FiscoProjects.SharedKernel;

namespace FiscoProjects.Projects.Domain;

/// <summary>
/// A company the projects are made for (ADR-0015): the name, the CNPJ, the logo and an observation. Projects may have
/// none, as internal ones. A client starts active; an inactive one stays in the list, dimmed.
/// </summary>
public sealed class Client : Entity
{
    public const int NameMaxLength = 150;
    public const int ObservationMaxLength = 500;

    private Client()
    {
        Name = string.Empty;
    }

    public string Name { get; private set; }

    /// <summary>14 characters without punctuation (<see cref="Domain.Cnpj"/>), or null.</summary>
    public string? Cnpj { get; private set; }

    public string? Observation { get; private set; }

    public bool IsActive { get; private set; } = true;

    /// <summary>Id of the logo in the file storage; null without a logo.</summary>
    public string? LogoFileId { get; private set; }

    /// <summary>Name of the file the Administrator sent, as the audit shows it.</summary>
    public string? LogoFileName { get; private set; }

    public string? LogoContentType { get; private set; }

    /// <summary>When the logo last changed, which also makes its address change.</summary>
    public DateTime? LogoChangedAt { get; private set; }

    public bool HasLogo => LogoFileId is not null;

    /// <summary>The name without the spaces at the ends, when it is not empty and has up to 150 characters.</summary>
    public static string? ValidName(string? name)
    {
        var trimmed = name?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > NameMaxLength ? null : trimmed;
    }

    /// <summary>The observation without the spaces at the ends, null when empty; false when it is too long.</summary>
    public static bool TryNormalizeObservation(string? observation, out string? normalized)
    {
        var trimmed = observation?.Trim();
        normalized = string.IsNullOrEmpty(trimmed) ? null : trimmed;
        return normalized is null || normalized.Length <= ObservationMaxLength;
    }

    public static Client Create(string name, string? cnpj, string? observation)
    {
        var client = new Client();
        client.Change(name, cnpj, observation);
        return client;
    }

    /// <summary>Changes the data; the CNPJ comes normalized and the observation trimmed (null when none).</summary>
    public void Change(string name, string? cnpj, string? observation)
    {
        Name = ValidName(name) ?? throw new ArgumentException(
            $"The name of the client is needed and has up to {NameMaxLength} characters.", nameof(name));
        Cnpj = cnpj is null || Domain.Cnpj.IsValid(cnpj)
            ? cnpj
            : throw new ArgumentException($"'{cnpj}' is no CNPJ.", nameof(cnpj));
        Observation = TryNormalizeObservation(observation, out var normalized)
            ? normalized
            : throw new ArgumentException(
                $"The observation has up to {ObservationMaxLength} characters.", nameof(observation));
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;

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
