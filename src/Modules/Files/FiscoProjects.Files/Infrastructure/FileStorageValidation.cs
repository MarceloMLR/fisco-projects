using FiscoProjects.Files.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Files.Infrastructure;

/// <summary>Checked when the host starts, so a wrong setup stops the application instead of running.</summary>
internal sealed class FileStorageValidation(IHostEnvironment environment) : IValidateOptions<FileStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, FileStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            return ValidateOptionsResult.Success;
        }

        if (options.IsSharePoint)
        {
            return ValidateSharePoint(options.SharePoint);
        }

        if (!options.IsLocal)
        {
            return ValidateOptionsResult.Fail(
                $"The file storage '{options.Provider}' (FileStorage:Provider) is unknown. Use {FileStorageOptions.SharePointProvider} or {FileStorageOptions.LocalProvider}.");
        }

        if (environment.IsProduction())
        {
            return ValidateOptionsResult.Fail(
                "The local file storage (FileStorage:Provider = Local) is not allowed in production (ADR-0079). Configure the SharePoint storage to start the application.");
        }

        return string.IsNullOrWhiteSpace(options.Local.RootPath)
            ? ValidateOptionsResult.Fail("The local file storage needs a folder (FileStorage:Local:RootPath).")
            : ValidateOptionsResult.Success;
    }

    private static ValidateOptionsResult ValidateSharePoint(SharePointStorageOptions sharePoint)
    {
        var failures = new List<string>();
        if (!Uri.TryCreate(sharePoint.SiteUrl, UriKind.Absolute, out var site) || site.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(site.Query) || !string.IsNullOrEmpty(site.Fragment))
        {
            failures.Add("The SharePoint storage needs the address of the site, in https (FileStorage:SharePoint:SiteUrl).");
        }

        if (!string.IsNullOrWhiteSpace(sharePoint.RootFolder))
        {
            try
            {
                FileNames.Segments(sharePoint.RootFolder);
            }
            catch (ArgumentException)
            {
                failures.Add("The root folder of the SharePoint storage cannot go up (FileStorage:SharePoint:RootFolder).");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
