using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyModel;

namespace GerenWare.Architecture.Tests;

/// <summary>
/// Finds the modules of the application by assembly name, so every new module is checked
/// without changing the tests: GerenWare.&lt;Module&gt; and GerenWare.&lt;Module&gt;.Contracts.
/// </summary>
internal static partial class ModuleCatalog
{
    public const string SharedKernelNamespace = "GerenWare.SharedKernel";

    public const string PersistenceNamespace = "GerenWare.Persistence";

    private static readonly HashSet<string> NotModules = ["Api", "SharedKernel", "Persistence", "Testing"];

    public static IReadOnlyList<ModuleRoot> FindModules(IEnumerable<string> assemblyNames) =>
        [.. assemblyNames
            .Select(name => ModuleAssemblyName().Match(name))
            .Where(match => match.Success && !NotModules.Contains(match.Groups["module"].Value))
            .Select(match => $"GerenWare.{match.Groups["module"].Value}")
            .Distinct()
            .Order(StringComparer.Ordinal)
            .Select(root => new ModuleRoot(root))];

    /// <summary>
    /// GerenWare projects that this test project depends on, directly or through the host,
    /// read from the dependency manifest (not from the bin folder, which may hold stale files).
    /// </summary>
    public static IReadOnlyList<Assembly> ApplicationAssemblies() =>
        [.. (DependencyContext.Default?.RuntimeLibraries ?? [])
            .Where(library => library.Type == "project"
                && library.Name.StartsWith("GerenWare.", StringComparison.Ordinal)
                && !library.Name.EndsWith(".Tests", StringComparison.Ordinal))
            .Select(library => library.Name)
            .Order(StringComparer.Ordinal)
            .Select(name => Assembly.Load(new AssemblyName(name)))];

    [GeneratedRegex(@"^GerenWare\.(?<module>[A-Za-z][A-Za-z0-9]*)(\.Contracts)?$")]
    private static partial Regex ModuleAssemblyName();
}
