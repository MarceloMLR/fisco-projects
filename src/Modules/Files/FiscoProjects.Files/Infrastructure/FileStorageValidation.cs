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

        if (!options.IsLocal)
        {
            return ValidateOptionsResult.Fail(
                $"The file storage '{options.Provider}' (FileStorage:Provider) is unknown. Use {FileStorageOptions.LocalProvider}.");
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
}
