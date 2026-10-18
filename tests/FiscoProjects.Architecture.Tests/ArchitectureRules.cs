using ArchUnitNET.Domain;

namespace FiscoProjects.Architecture.Tests;

/// <summary>
/// Dependency rules of the modular monolith (ADR-0047). Each rule returns the violations found,
/// as "Type -> Dependency" lines, so tests can assert both that valid code passes and that
/// invalid code is caught.
/// </summary>
internal static class ArchitectureRules
{
    /// <summary>Libraries that only Infrastructure and Endpoints may use.</summary>
    public static readonly string[] TechnologyNamespaces =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Microsoft.Data.SqlClient",
        "Microsoft.Graph",
        "Microsoft.Identity",
        "Azure",
    ];

    public static IReadOnlyList<string> LayersDependInward(ArchUnitNET.Domain.Architecture architecture, ModuleRoot module) =>
    [
        .. Violations(architecture, module.Domain,
            target => IsAnyWithin(target, module.Application, module.Infrastructure, module.Endpoints)),
        .. Violations(architecture, module.Application,
            target => IsAnyWithin(target, module.Infrastructure, module.Endpoints)),
        .. Violations(architecture, module.Endpoints,
            target => NamespaceRules.IsWithin(target, module.Infrastructure)),
        .. Violations(architecture, module.Infrastructure,
            target => NamespaceRules.IsWithin(target, module.Endpoints)),
    ];

    public static IReadOnlyList<string> BusinessLayersWithoutTechnology(ArchUnitNET.Domain.Architecture architecture, ModuleRoot module) =>
    [
        .. Violations(architecture, module.Domain, target => IsAnyWithin(target, TechnologyNamespaces)),
        .. Violations(architecture, module.Application, target => IsAnyWithin(target, TechnologyNamespaces)),
    ];

    public static IReadOnlyList<string> UsesOnlyContractsOfOtherModules(
        ArchUnitNET.Domain.Architecture architecture, ModuleRoot module, IEnumerable<ModuleRoot> allModules)
    {
        var others = allModules.Where(other => other != module).ToList();
        return Violations(architecture, module.Namespace, module.IsInterior,
            target => others.Any(other => other.IsInterior(target)));
    }

    public static IReadOnlyList<string> ContractsDoNotUseModuleInteriors(
        ArchUnitNET.Domain.Architecture architecture, ModuleRoot module, IEnumerable<ModuleRoot> allModules)
    {
        var modules = allModules.ToList();
        return Violations(architecture, module.Contracts,
            target => modules.Any(other => other.IsInterior(target)));
    }

    public static IReadOnlyList<string> TypesLiveInALayer(ArchUnitNET.Domain.Architecture architecture, ModuleRoot module) =>
        [.. SourceTypes(architecture, module.Namespace, module.IsInterior)
            .Where(type => !ModuleRoot.Layers.Any(layer =>
                NamespaceRules.IsWithin(type.Namespace.FullName, $"{module.Namespace}.{layer}")))
            .Select(type => $"{type.FullName} is outside the Domain, Application, Infrastructure and Endpoints layers")];

    public static IReadOnlyList<string> SharedKernelIsIndependent(
        ArchUnitNET.Domain.Architecture architecture, string sharedKernelNamespace, IEnumerable<ModuleRoot> allModules)
    {
        var modules = allModules.ToList();
        return Violations(architecture, sharedKernelNamespace,
            target => modules.Any(module => NamespaceRules.IsWithin(target, module.Namespace))
                || IsAnyWithin(target, "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore", "Microsoft.Data.SqlClient"));
    }

    private static List<string> Violations(
        ArchUnitNET.Domain.Architecture architecture, string sourceNamespace, Func<string, bool> isForbiddenTarget) =>
        Violations(architecture, sourceNamespace, _ => true, isForbiddenTarget);

    private static List<string> Violations(
        ArchUnitNET.Domain.Architecture architecture,
        string sourceNamespace,
        Func<string, bool> includeSource,
        Func<string, bool> isForbiddenTarget) =>
        [.. SourceTypes(architecture, sourceNamespace, includeSource)
            .SelectMany(type => type.Dependencies
                .Select(dependency => dependency.Target)
                .Where(target => isForbiddenTarget(target.Namespace.FullName))
                .Select(target => $"{type.FullName} -> {target.FullName}"))
            .Distinct()];

    private static IEnumerable<IType> SourceTypes(
        ArchUnitNET.Domain.Architecture architecture, string sourceNamespace, Func<string, bool> includeSource) =>
        architecture.Types.Where(type =>
            NamespaceRules.IsWithin(type.Namespace.FullName, sourceNamespace)
            && includeSource(type.Namespace.FullName)
            && !type.FullName.Contains('<', StringComparison.Ordinal));

    private static bool IsAnyWithin(string typeNamespace, params string[] roots) =>
        roots.Any(root => NamespaceRules.IsWithin(typeNamespace, root));
}
