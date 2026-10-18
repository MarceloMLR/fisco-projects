using FiscoProjects.Files.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Files.Infrastructure;

/// <summary>
/// Services of the Files module: the file storage the other modules use, chosen in the configuration (ADR-0079).
/// The module has no tables and no routes; each module that keeps files registers them and serves them.
/// </summary>
public static class FilesModule
{
    public static IServiceCollection AddFilesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.Section))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<FileStorageOptions>, FileStorageValidation>();
        services.AddSingleton<IFileStorage>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
            if (!options.IsLocal)
            {
                throw new InvalidOperationException(
                    "No file storage is configured. Set FileStorage:Provider (Local in development) to use files.");
            }

            var contentRoot = provider.GetRequiredService<IHostEnvironment>().ContentRootPath;
            return new LocalFileStorage(Path.Combine(contentRoot, options.Local.RootPath));
        });
        return services;
    }
}
