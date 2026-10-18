namespace FiscoProjects.Testing;

/// <summary>Files of the repository, found from the test output folder (the root is where FiscoProjects.slnx is).</summary>
public static class RepositoryFiles
{
    public static string Root { get; } = FindRoot();

    public static string PathOf(params string[] relativePath) => Path.Combine([Root, .. relativePath]);

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
