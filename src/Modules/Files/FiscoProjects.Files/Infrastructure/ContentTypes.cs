using Microsoft.AspNetCore.StaticFiles;

namespace FiscoProjects.Files.Infrastructure;

/// <summary>The type of a file for the browser, from its name, the same in every implementation of the storage.</summary>
internal static class ContentTypes
{
    private const string Default = "application/octet-stream";

    private static readonly FileExtensionContentTypeProvider Known = new();

    public static string Of(string name) => Known.TryGetContentType(name, out var contentType) ? contentType : Default;
}
