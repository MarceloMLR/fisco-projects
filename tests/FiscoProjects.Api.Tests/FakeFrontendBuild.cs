namespace FiscoProjects.Api.Tests;

/// <summary>A temporary web root with a main page and one build file, standing in for the React build.</summary>
internal sealed class FakeFrontendBuild : IDisposable
{
    public const string IndexMarker = "fake frontend main page";
    public const string AssetPath = "/assets/app-3f9a1c.js";

    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("fisco-projects-web-");

    public FakeFrontendBuild()
    {
        File.WriteAllText(Path.Combine(_root.FullName, "index.html"), $"<!doctype html><title>{IndexMarker}</title>");
        Directory.CreateDirectory(Path.Combine(_root.FullName, "assets"));
        File.WriteAllText(Path.Combine(_root.FullName, "assets", Path.GetFileName(AssetPath)), "console.log('fake');");
    }

    public string Folder => _root.FullName;

    public void Dispose() => _root.Delete(recursive: true);
}
