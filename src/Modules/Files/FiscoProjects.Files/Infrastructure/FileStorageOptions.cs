namespace FiscoProjects.Files.Infrastructure;

/// <summary>Where the files of the installation live (ADR-0030, ADR-0079).</summary>
public sealed class FileStorageOptions
{
    public const string Section = "FileStorage";

    /// <summary>The local folder of the disk, for development and tests.</summary>
    public const string LocalProvider = "Local";

    /// <summary>Which implementation stores the files; empty when none is configured yet.</summary>
    public string? Provider { get; set; }

    public LocalFileStorageOptions Local { get; set; } = new();

    public bool IsLocal => string.Equals(Provider, LocalProvider, StringComparison.OrdinalIgnoreCase);
}

public sealed class LocalFileStorageOptions
{
    /// <summary>Folder of the files, absolute or relative to the folder of the application.</summary>
    public string RootPath { get; set; } = "App_Data/files";
}
