using FiscoProjects.Administration.Domain;
using FiscoProjects.Files.Contracts;

namespace FiscoProjects.Administration.Application;

/// <summary>Where the settings of the installation are kept.</summary>
public interface IInstallationSettingsStore
{
    /// <summary>The one row of settings, tracked, so a change is saved by <see cref="SaveChangesAsync"/>.</summary>
    Task<InstallationSettings> GetAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// The brand of the installation: its name, when the logo last changed (with a logo) and the main color (null for the
/// default one).
/// </summary>
public sealed record Installation(string Name, DateTime? LogoChangedAt, string? MainColor);

/// <summary>The logo to serve: the file, its type and when it changed.</summary>
public sealed record InstallationLogo(FileDownload File, string ContentType, DateTime ChangedAt);

public sealed class GetInstallation(IInstallationSettingsStore store)
{
    public async Task<Installation> ExecuteAsync(CancellationToken cancellationToken)
    {
        var settings = await store.GetAsync(cancellationToken);
        return new Installation(settings.Name, settings.HasLogo ? settings.LogoChangedAt : null, settings.MainColor);
    }
}

public sealed class GetInstallationLogo(IInstallationSettingsStore store, IFileStorage files)
{
    /// <returns>Null without a logo, or when its file is gone from the storage.</returns>
    public async Task<InstallationLogo?> ExecuteAsync(CancellationToken cancellationToken)
    {
        var settings = await store.GetAsync(cancellationToken);
        if (settings is not { LogoFileId: { } fileId, LogoContentType: { } contentType, LogoChangedAt: { } changedAt })
        {
            return null;
        }

        var file = await files.OpenAsync(fileId, null, cancellationToken);
        return file is null ? null : new InstallationLogo(file, contentType, changedAt);
    }
}

/// <summary>
/// The rules of the installation every person in a session needs: the time zone of the screens and the time of the
/// daily summary, which the profile shows.
/// </summary>
public sealed record GeneralRules(string TimeZone, TimeOnly DailySummaryAt, int NotificationRetentionDays);

public sealed class GetGeneralRules(IInstallationSettingsStore store)
{
    public async Task<GeneralRules> ExecuteAsync(CancellationToken cancellationToken)
    {
        var settings = await store.GetAsync(cancellationToken);
        return new GeneralRules(settings.TimeZone, settings.DailySummaryAt, settings.NotificationRetentionDays);
    }
}

/// <summary>A field of the General settings form.</summary>
public enum InstallationField
{
    Name = 1,
    TimeZone = 2,
    MainColor = 3,
    DailySummaryAt = 4,
    NotificationRetentionDays = 5,
}

/// <summary>
/// The General settings form, saved as a whole: the name shown, the time zone, the main color, the time of the daily
/// summary and how many days the notifications are kept.
/// </summary>
public sealed record InstallationChanges(
    string? Name, string? TimeZone, string? MainColor, TimeOnly? DailySummaryAt, int? NotificationRetentionDays);

/// <summary>
/// Saves the General settings form (ADR-0003, ADR-0013, ADR-0082): every field valid, or nothing changes. One save,
/// so one audit record.
/// </summary>
public sealed class UpdateInstallation(IInstallationSettingsStore store)
{
    /// <returns>The invalid fields; empty when the form was saved.</returns>
    public async Task<IReadOnlyList<InstallationField>> ExecuteAsync(
        InstallationChanges changes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var name = InstallationSettings.ValidName(changes.Name);
        var invalid = new List<InstallationField>();
        if (name is null)
        {
            invalid.Add(InstallationField.Name);
        }

        if (!InstallationSettings.IsValidTimeZone(changes.TimeZone))
        {
            invalid.Add(InstallationField.TimeZone);
        }

        if (!InstallationSettings.TryNormalizeMainColor(changes.MainColor, out var mainColor))
        {
            invalid.Add(InstallationField.MainColor);
        }

        if (changes.DailySummaryAt is null)
        {
            invalid.Add(InstallationField.DailySummaryAt);
        }

        if (changes.NotificationRetentionDays is not { } retention || !InstallationSettings.IsValidNotificationRetention(retention))
        {
            invalid.Add(InstallationField.NotificationRetentionDays);
        }

        if (name is null || invalid.Count > 0)
        {
            return invalid;
        }

        var settings = await store.GetAsync(cancellationToken);
        settings.Rename(name);
        settings.ChangeTimeZone(changes.TimeZone!);
        settings.ChangeMainColor(mainColor);
        settings.ChangeDailySummaryAt(changes.DailySummaryAt!.Value);
        settings.ChangeNotificationRetention(changes.NotificationRetentionDays!.Value);
        await store.SaveChangesAsync(cancellationToken);
        return [];
    }
}

/// <summary>
/// Keeps a new logo: checked by its content, stored in the folder of the system in the file storage (ADR-0030), and
/// only then recorded. The file of a logo of another format is removed after the record is saved.
/// </summary>
public sealed class ReplaceInstallationLogo(IInstallationSettingsStore store, IFileStorage files, TimeProvider clock)
{
    /// <summary>Folder of the files of the system itself, under the root of the storage.</summary>
    public const string Folder = "FiscoProjects";

    public const string FileStem = "installation-logo";

    private const int FileNameMaxLength = 200;

    /// <returns>Null when the logo was kept, or why it was refused; nothing changes when it is refused.</returns>
    public async Task<LogoRefusal?> ExecuteAsync(string? fileName, Stream content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var bytes = await ReadAtMostAsync(content, LogoFile.MaxBytes + 1, cancellationToken);
        var check = LogoFile.Check(bytes);
        if (check.Format is not { } format)
        {
            return check.Refusal ?? LogoRefusal.Format;
        }

        var settings = await store.GetAsync(cancellationToken);
        var previous = settings.LogoFileId;
        var folder = await files.EnsureFolderAsync(null, Folder, cancellationToken);
        await using var stream = new MemoryStream(bytes, writable: false);
        var stored = await files.UploadAsync(
            folder.Id, $"{FileStem}.{format.Extension}", stream, SameNameAction.NewVersion, cancellationToken);

        settings.ReplaceLogo(stored.Id, SentName(fileName, stored.Name), format.ContentType, clock.GetUtcNow().UtcDateTime);
        try
        {
            await store.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Nothing recorded points to the new file: remove it, unless it is the same file with a new version.
            if (stored.Id != previous)
            {
                await files.DeleteFileAsync(stored.Id, CancellationToken.None);
            }

            throw;
        }

        if (previous is not null && previous != stored.Id)
        {
            await files.DeleteFileAsync(previous, cancellationToken);
        }

        return null;
    }

    private static string SentName(string? fileName, string storedName)
    {
        var name = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            return storedName;
        }

        return name.Length <= FileNameMaxLength ? name : name[..FileNameMaxLength];
    }

    private static async Task<byte[]> ReadAtMostAsync(Stream content, int limit, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while (buffer.Length < limit
               && (read = await content.ReadAsync(chunk.AsMemory(0, (int)Math.Min(chunk.Length, limit - buffer.Length)), cancellationToken)) > 0)
        {
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}

/// <summary>Removes the logo: the record first, then the file, so the screen never points to a missing file.</summary>
public sealed class RemoveInstallationLogo(IInstallationSettingsStore store, IFileStorage files)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var settings = await store.GetAsync(cancellationToken);
        if (settings.LogoFileId is not { } fileId)
        {
            return;
        }

        settings.RemoveLogo();
        await store.SaveChangesAsync(cancellationToken);
        await files.DeleteFileAsync(fileId, cancellationToken);
    }
}
