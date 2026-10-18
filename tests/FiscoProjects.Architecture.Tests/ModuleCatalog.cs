using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyModel;

namespace FiscoProjects.Architecture.Tests;

/// <summary>
/// Finds the modules of the application by assembly name, so every new module is checked
/// without changing the tests: FiscoProjects.&lt;Module&gt; and FiscoProjects.&lt;Module&gt;.Contracts.
/// </summary>
internal static partial class ModuleCatalog
{
    public const string SharedKernelNamespace = "FiscoProjects.SharedKernel";

    public const string PersistenceNamespace = "FiscoProjects.Persistence";

    private static readonly HashSet<string> NotModules = ["Api", "SharedKernel", "Persistence", "Testing"];

    public static IReadOnlyList<ModuleRoot> FindModules(IEnumerable<string> assemblyNames) =>
        [.. assemblyNames
            .Select(name => ModuleAssemblyName().Match(name))
            .Where(match => match.Success && !NotModules.Contains(match.Groups["module"].Value))
            .Select(match => $"FiscoProjects.{match.Groups["module"].Value}")
            .Distinct()
            .Order(StringComparer.Ordinal)
            .Select(root => new ModuleRoot(root))];

    /// <summary>
    /// FiscoProjects projects that this test project depends on, directly or through the host,
    /// read from the dependency manifest (not from the bin folder, which may hold stale files).
    /// </summary>
    public static IReadOnlyList<Assembly> ApplicationAssemblies() =>
        [.. (DependencyContext.Default?.RuntimeLibraries ?? [])
            .Where(library => library.Type == "project"
                && library.Name.StartsWith("FiscoProjects.", StringComparison.Ordinal)
                && !library.Name.EndsWith(".Tests", StringComparison.Ordinal))
            .Select(library => library.Name)
            .Order(StringComparer.Ordinal)
            .Select(name => Assembly.Load(new AssemblyName(name)))];

    [GeneratedRegex(@"^FiscoProjects\.(?<module>[A-Za-z][A-Za-z0-9]*)(\.Contracts)?$")]
    private static partial Regex ModuleAssemblyName();
}
