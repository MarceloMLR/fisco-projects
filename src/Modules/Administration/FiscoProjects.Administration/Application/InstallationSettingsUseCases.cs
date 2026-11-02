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
/// The brand of the installation: its name, when each logo last changed (with a logo, ADR-0100) and the three colors
/// (null for the default ones, ADR-0146).
/// </summary>
public sealed record Installation(
    string Name,
    DateTime? LogoChangedAt,
    string? MainColor,
    DateTime? DarkLogoChangedAt = null,
    string? SecondaryColor = null,
    string? SidebarColor = null);

/// <summary>The logo to serve: the file, its type and when it changed.</summary>
public sealed record InstallationLogo(FileDownload File, string ContentType, DateTime ChangedAt);

public sealed class GetInstallation(IInstallationSettingsStore store)
{
    public async Task<Installation> ExecuteAsync(CancellationToken cancellationToken)
    {
        var settings = await store.GetAsync(cancellationToken);
        return new Installation(
            settings.Name,
            settings.HasLogo ? settings.LogoChangedAt : null,
            settings.MainColor,
            settings.HasDarkLogo ? settings.DarkLogoChangedAt : null,
            settings.SecondaryColor,
            settings.SidebarColor);
    }
}

public sealed class GetInstallationLogo(IInstallationSettingsStore store, IFileStorage files)
{
    public Task<InstallationLogo?> ExecuteAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(LogoTheme.Light, cancellationToken);

    /// <returns>Null without a logo of the theme, or when its file is gone from the storage.</returns>
    public async Task<InstallationLogo?> ExecuteAsync(LogoTheme theme, CancellationToken cancellationToken)
    {
        var settings = await store.GetAsync(cancellationToken);
        if (settings.LogoFileIdOf(theme) is not { } fileId
            || settings.LogoContentTypeOf(theme) is not { } contentType
            || settings.LogoChangedAtOf(theme) is not { } changedAt)
        {
            return null;
        }

        var file = await files.OpenAsync(fileId, null, cancellationToken);
        return file is null ? null : new InstallationLogo(file, contentType, changedAt);
    }
}

/// <summary>
/// The rules of the installation every person in a session needs: the time zone of the screens and the time of the
/// daily summary, which the profile shows, and the ones the General settings show.
/// </summary>
public sealed record GeneralRules(
    string TimeZone,
    TimeOnly DailySummaryAt,
    int NotificationRetentionDays,
    int AuditRetentionYears,
    string ProjectsFolder,
    decimal WorkHoursPerDay);

public sealed class GetGeneralRules(IInstallationSettingsStore store)
{
    public async Task<GeneralRules> ExecuteAsync(CancellationToken cancellationToken)
    {
        var settings = await store.GetAsync(cancellationToken);
        return new GeneralRules(
            settings.TimeZone,
            settings.DailySummaryAt,
            settings.NotificationRetentionDays,
            settings.AuditRetentionYears,
            settings.ProjectsFolder,
            settings.WorkHoursPerDay);
    }
}

/// <summary>A field of the settings of the installation that the screens save.</summary>
public enum InstallationField
{
    Name = 1,
    TimeZone = 2,
    DailySummaryAt = 4,
    NotificationRetentionDays = 5,
    AuditRetentionYears = 6,
    ProjectsFolder = 7,
    WorkHoursPerDay = 8,
}

/// <summary>
/// The General rules of the General settings (ADR-0150): the time zone, the time of the daily summary, how many days the
/// notifications are kept, how many years the audit is kept (none keeps the years it had, ADR-0110) and the hours of a
/// working day (none keeps the ones it had, ADR-0142).
/// </summary>
public sealed record InstallationChanges(
    string? TimeZone,
    TimeOnly? DailySummaryAt,
    int? NotificationRetentionDays,
    int? AuditRetentionYears = null,
    decimal? WorkHoursPerDay = null);

/// <summary>
/// Saves the General rules (ADR-0003, ADR-0013, ADR-0150): every field valid, or nothing changes. One save, so one audit
/// record. The name and the folder of the projects are saved by their own screens.
/// </summary>
public sealed class UpdateInstallation(IInstallationSettingsStore store)
{
    /// <returns>The invalid fields; empty when the rules were saved.</returns>
    public async Task<IReadOnlyList<InstallationField>> ExecuteAsync(
        InstallationChanges changes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var invalid = new List<InstallationField>();
        if (!InstallationSettings.IsValidTimeZone(changes.TimeZone))
        {
            invalid.Add(InstallationField.TimeZone);
        }

        if (changes.DailySummaryAt is null)
        {
            invalid.Add(InstallationField.DailySummaryAt);
        }

        if (changes.NotificationRetentionDays is not { } retention || !InstallationSettings.IsValidNotificationRetention(retention))
        {
            invalid.Add(InstallationField.NotificationRetentionDays);
        }

        if (changes.AuditRetentionYears is { } years && !InstallationSettings.IsValidAuditRetention(years))
        {
            invalid.Add(InstallationField.AuditRetentionYears);
        }

        if (changes.WorkHoursPerDay is { } workHours && !InstallationSettings.IsValidWorkHoursPerDay(workHours))
        {
            invalid.Add(InstallationField.WorkHoursPerDay);
        }

        if (invalid.Count > 0)
        {
            return invalid;
        }

        var settings = await store.GetAsync(cancellationToken);
        settings.ChangeTimeZone(changes.TimeZone!);
        settings.ChangeDailySummaryAt(changes.DailySummaryAt!.Value);
        settings.ChangeNotificationRetention(changes.NotificationRetentionDays!.Value);
        if (changes.AuditRetentionYears is { } auditYears)
        {
            settings.ChangeAuditRetention(auditYears);
        }

        if (changes.WorkHoursPerDay is { } hoursPerDay)
        {
            settings.ChangeWorkHoursPerDay(hoursPerDay);
        }

        await store.SaveChangesAsync(cancellationToken);
        return [];
    }
}

/// <summary>Renames the installation, in the Brand of Visual (ADR-0003, ADR-0150): the name shown, trimmed, up to 60 characters.</summary>
public sealed class RenameInstallation(IInstallationSettingsStore store)
{
    /// <returns>False, and nothing changes, when the name is empty or too long.</returns>
    public async Task<bool> ExecuteAsync(string? name, CancellationToken cancellationToken)
    {
        if (InstallationSettings.ValidName(name) is not { } valid)
        {
            return false;
        }

        var settings = await store.GetAsync(cancellationToken);
        settings.Rename(valid);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>
/// Changes the folder of the projects in the file storage, in Default folders (ADR-0121, ADR-0150): only for the projects
/// that have no folder yet.
/// </summary>
public sealed class ChangeProjectsFolder(IInstallationSettingsStore store)
{
    /// <returns>False, and nothing changes, when the storage would not keep the folder as typed.</returns>
    public async Task<bool> ExecuteAsync(string? typed, CancellationToken cancellationToken)
    {
        if (ProjectsFolderOf(typed) is not { } folder)
        {
            return false;
        }

        var settings = await store.GetAsync(cancellationToken);
        settings.ChangeProjectsFolder(folder);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// The folder of the projects as kept: without spaces nor "/" at the ends, empty for the root of the storage, and
    /// null when the storage would not keep it as typed (ADR-0121).
    /// </summary>
    public static string? ProjectsFolderOf(string? typed)
    {
        var folder = (typed ?? string.Empty).Trim().Trim('/').Trim();
        return folder.Length == 0 || FileNames.IsKeptAsTyped(folder) ? folder : null;
    }
}

/// <summary>A color of the Colors section (ADR-0146).</summary>
public enum InstallationColorField
{
    Main = 1,
    Secondary = 2,
    Sidebar = 3,
}

/// <summary>The three colors of the installation, each #RRGGBB or null (or empty) for the default one.</summary>
public sealed record InstallationColors(string? Main, string? Secondary, string? Sidebar);

/// <summary>
/// Saves the Colors section (ADR-0146): any #RRGGBB color, each one valid or nothing changes. One save, so one audit
/// record with the colors changed.
/// </summary>
public sealed class ChangeInstallationColors(IInstallationSettingsStore store)
{
    /// <returns>The invalid colors; empty when the colors were saved.</returns>
    public async Task<IReadOnlyList<InstallationColorField>> ExecuteAsync(
        InstallationColors colors, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(colors);
        var invalid = new List<InstallationColorField>();
        if (!InstallationSettings.TryNormalizeColor(colors.Main, out var main))
        {
            invalid.Add(InstallationColorField.Main);
        }

        if (!InstallationSettings.TryNormalizeColor(colors.Secondary, out var secondary))
        {
            invalid.Add(InstallationColorField.Secondary);
        }

        if (!InstallationSettings.TryNormalizeColor(colors.Sidebar, out var sidebar))
        {
            invalid.Add(InstallationColorField.Sidebar);
        }

        if (invalid.Count > 0)
        {
            return invalid;
        }

        var settings = await store.GetAsync(cancellationToken);
        settings.ChangeColors(main, secondary, sidebar);
        await store.SaveChangesAsync(cancellationToken);
        return [];
    }
}

/// <summary>
/// Keeps a new logo of a theme (ADR-0100): checked by its content, stored in the folder of the system in the file
/// storage (ADR-0030), and only then recorded. The file of a logo of another format is removed after the record is saved.
/// </summary>
public sealed class ReplaceInstallationLogo(IInstallationSettingsStore store, IFileStorage files, TimeProvider clock)
{
    /// <summary>Folder of the files of the system itself, under the root of the storage.</summary>
    public const string Folder = "FiscoProjects";

    public const string FileStem = "installation-logo";

    /// <summary>The name of the file of the logo of a theme, without the extension.</summary>
    public static string FileStemOf(LogoTheme theme) => theme == LogoTheme.Dark ? $"{FileStem}-dark" : FileStem;

    private const int FileNameMaxLength = 200;

    /// <returns>Null when the logo was kept, or why it was refused; nothing changes when it is refused.</returns>
    public Task<LogoRefusal?> ExecuteAsync(string? fileName, Stream content, CancellationToken cancellationToken) =>
        ExecuteAsync(fileName, content, LogoTheme.Light, cancellationToken);

    public async Task<LogoRefusal?> ExecuteAsync(
        string? fileName, Stream content, LogoTheme theme, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var bytes = await ReadAtMostAsync(content, LogoFile.MaxBytes + 1, cancellationToken);
        var check = LogoFile.Check(bytes);
        if (check.Format is not { } format)
        {
            return check.Refusal ?? LogoRefusal.Format;
        }

        var settings = await store.GetAsync(cancellationToken);
        var previous = settings.LogoFileIdOf(theme);
        var folder = await files.EnsureFolderAsync(null, Folder, cancellationToken);
        await using var stream = new MemoryStream(bytes, writable: false);
        var stored = await files.UploadAsync(
            folder.Id, $"{FileStemOf(theme)}.{format.Extension}", stream, SameNameAction.NewVersion, cancellationToken);

        settings.ReplaceLogo(
            stored.Id, SentName(fileName, stored.Name), format.ContentType, clock.GetUtcNow().UtcDateTime, theme);
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

/// <summary>Removes the logo of a theme: the record first, then the file, so the screen never points to a missing file.</summary>
public sealed class RemoveInstallationLogo(IInstallationSettingsStore store, IFileStorage files)
{
    public Task ExecuteAsync(CancellationToken cancellationToken) => ExecuteAsync(LogoTheme.Light, cancellationToken);

    public async Task ExecuteAsync(LogoTheme theme, CancellationToken cancellationToken)
    {
        var settings = await store.GetAsync(cancellationToken);
        if (settings.LogoFileIdOf(theme) is not { } fileId)
        {
            return;
        }

        settings.RemoveLogo(theme);
        await store.SaveChangesAsync(cancellationToken);
        await files.DeleteFileAsync(fileId, cancellationToken);
    }
}
