using FiscoProjects.SharedKernel;

namespace FiscoProjects.Notifications.Domain;

/// <summary>
/// The application of Teams of the installation (ADR-0149): one row, created on the first reading. It keeps the id of
/// the manifest, which never changes, so each new package updates the same application in Teams; whether the notices
/// go to Teams, which the Administrator turns on once the application is installed; and the version of the package,
/// raised when its content changes.
/// </summary>
public sealed class TeamsApp : Entity
{
    public const int HashMaxLength = 64;

    /// <summary>The id of the one row.</summary>
    public static readonly Guid SingleId = new("0193f0a0-7ea0-7c3e-9a51-2f6b8c1d4e01");

    private TeamsApp()
    {
    }

    private TeamsApp(Guid manifestId)
    {
        Id = SingleId;
        ManifestId = manifestId;
    }

    /// <summary>The id of the application in the manifest; Teams knows each package by it.</summary>
    public Guid ManifestId { get; private init; }

    /// <summary>Whether the notices sent now also go to Teams; off until the Administrator turns it on.</summary>
    public bool NoticesOn { get; private set; }

    /// <summary>The number of the last package, the last part of the version 1.0.n; 0 before the first.</summary>
    public int PackageVersion { get; private set; }

    /// <summary>The summary of the content of the last package, to know when it changed.</summary>
    public string? PackageHash { get; private set; }

    public static TeamsApp Create() => new(Guid.CreateVersion7());

    public void TurnNotices(bool on) => NoticesOn = on;

    /// <summary>The version of the package of this content: a new number when the content changed since the last one.</summary>
    public string Package(string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        if (PackageVersion == 0 || !string.Equals(hash, PackageHash, StringComparison.Ordinal))
        {
            PackageVersion++;
            PackageHash = hash;
        }

        return VersionText;
    }

    /// <summary>The version of the last package, as 1.0.3; null before the first.</summary>
    public string? Version => PackageVersion == 0 ? null : VersionText;

    private string VersionText => $"1.0.{PackageVersion}";
}

/// <summary>The channels of the notices sent now of a person (ADR-0151): the e-mail and Teams, each on or off.</summary>
public readonly record struct NoticeChannels(bool ByEmail, bool ByTeams);

/// <summary>
/// The channels a person chose for the notices sent now (ADR-0151): the e-mail and Teams, each turned on or off by the
/// person, in the audit. Who never chose has <see cref="Default"/>: the e-mail on and Teams off.
/// </summary>
public sealed class NoticePreference : Entity
{
    /// <summary>The channels of who never chose: the e-mail on and Teams off (ADR-0151).</summary>
    public static readonly NoticeChannels Default = new(ByEmail: true, ByTeams: false);

    private NoticePreference()
    {
    }

    public NoticePreference(Guid userId, NoticeChannels channels)
    {
        UserId = userId;
        Choose(channels);
    }

    public Guid UserId { get; private init; }

    public bool ByEmail { get; private set; }

    public bool ByTeams { get; private set; }

    public NoticeChannels Channels => new(ByEmail, ByTeams);

    public void Choose(NoticeChannels channels)
    {
        ByEmail = channels.ByEmail;
        ByTeams = channels.ByTeams;
    }
}
