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

/// <summary>The brand of the installation: its name and, with a logo, when the logo last changed.</summary>
public sealed record Installation(string Name, DateTime? LogoChangedAt);

/// <summary>The logo to serve: the file, its type and when it changed.</summary>
public sealed record InstallationLogo(FileDownload File, string ContentType, DateTime ChangedAt);

public sealed class GetInstallation(IInstallationSettingsStore store)
{
    public async Task<Installation> ExecuteAsync(CancellationToken cancellationToken)
    {
        var settings = await store.GetAsync(cancellationToken);
        return new Installation(settings.Name, settings.HasLogo ? settings.LogoChangedAt : null);
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

/// <summary>Changes the name shown of the installation (ADR-0003).</summary>
public sealed class RenameInstallation(IInstallationSettingsStore store)
{
    /// <returns>False when the name is empty or longer than 60 characters; nothing changes then.</returns>
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
