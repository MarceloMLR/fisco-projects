namespace FiscoProjects.Api.Tests;

/// <summary>A temporary web root with a main page and one build file, standing in for the React build.</summary>
internal sealed class FakeFrontendBuild : IDisposable
{
    public const string IndexMarker = "fake frontend main page";
    public const string AssetPath = "/assets/app-3f9a1c.js";

    private readonly DirectoryInfo root = Directory.CreateTempSubdirectory("fisco-projects-web-");

    public FakeFrontendBuild()
    {
        File.WriteAllText(Path.Combine(root.FullName, "index.html"), $"<!doctype html><title>{IndexMarker}</title>");
        Directory.CreateDirectory(Path.Combine(root.FullName, "assets"));
        File.WriteAllText(Path.Combine(root.FullName, "assets", Path.GetFileName(AssetPath)), "console.log('fake');");
    }

    public string Folder => root.FullName;

    public void Dispose() => root.Delete(recursive: true);
}
