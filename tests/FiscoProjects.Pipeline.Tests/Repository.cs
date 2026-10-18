using FiscoProjects.Testing;

namespace FiscoProjects.Pipeline.Tests;

/// <summary>The workflow and scripts these tests read.</summary>
internal static class Repository
{
    public static string WorkflowPath => RepositoryFiles.PathOf(".github", "workflows", "pull-request.yml");

    public static string ScriptsFolder => RepositoryFiles.PathOf("scripts", "ci");

    public static string[] WorkflowLines() => File.ReadAllLines(WorkflowPath);

    public static string Read(params string[] relativePath) => File.ReadAllText(RepositoryFiles.PathOf(relativePath));
}
