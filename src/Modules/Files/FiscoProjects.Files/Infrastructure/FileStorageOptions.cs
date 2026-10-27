namespace FiscoProjects.Files.Infrastructure;

/// <summary>Where the files of the installation live (ADR-0030, ADR-0079, ADR-0120).</summary>
public sealed class FileStorageOptions
{
    public const string Section = "FileStorage";

    /// <summary>The local folder of the disk, for development and tests.</summary>
    public const string LocalProvider = "Local";

    /// <summary>A site of the SharePoint of the company, through Microsoft Graph.</summary>
    public const string SharePointProvider = "SharePoint";

    /// <summary>Which implementation stores the files; empty when none is configured yet.</summary>
    public string? Provider { get; set; }

    public LocalFileStorageOptions Local { get; set; } = new();

    public SharePointStorageOptions SharePoint { get; set; } = new();

    public bool IsLocal => string.Equals(Provider, LocalProvider, StringComparison.OrdinalIgnoreCase);

    public bool IsSharePoint => string.Equals(Provider, SharePointProvider, StringComparison.OrdinalIgnoreCase);
}

public sealed class LocalFileStorageOptions
{
    /// <summary>Folder of the files, absolute or relative to the folder of the application.</summary>
    public string RootPath { get; set; } = "App_Data/files";
}

/// <summary>
/// The place of the files in SharePoint (ADR-0030): the site, its document library and the root folder inside it, which
/// may be an area the company already keeps. The application writes only there (Sites.Selected, ADR-0120).
/// </summary>
public sealed class SharePointStorageOptions
{
    /// <summary>The address of the site, as https://contoso.sharepoint.com/sites/Projetos.</summary>
    public string? SiteUrl { get; set; }

    /// <summary>The name of the document library; empty for the default library of the site.</summary>
    public string? Library { get; set; }

    /// <summary>
    /// The folder inside the library where the files of the system go (folders separated by "/"), as an area of projects
    /// the company already keeps; empty for the library itself, as in a site only for the system.
    /// </summary>
    public string RootFolder { get; set; } = string.Empty;
}
