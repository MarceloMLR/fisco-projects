namespace FiscoProjects.Pipeline.Tests;

/// <summary>Files of the repository these tests read, found from the test output folder.</summary>
internal static class Repository
{
    public static readonly string Root = FindRoot();

    public static string WorkflowPath => Path.Combine(Root, ".github", "workflows", "pull-request.yml");

    public static string ScriptsFolder => Path.Combine(Root, "scripts", "ci");

    public static string[] WorkflowLines() => File.ReadAllLines(WorkflowPath);

    public static string Read(params string[] relativePath) => File.ReadAllText(Path.Combine([Root, .. relativePath]));

    private static string FindRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "FiscoProjects.slnx")))
            {
                return folder.FullName;
            }
        }

        throw new InvalidOperationException("Repository root (FiscoProjects.slnx) not found.");
    }
}
