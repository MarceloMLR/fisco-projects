namespace FiscoProjects.Architecture.Tests;

/// <summary>
/// Namespaces of one module, following ADR-0047: the module root holds the four layers,
/// and the contracts live in the root followed by ".Contracts".
/// </summary>
internal sealed record ModuleRoot(string Namespace)
{
    public static readonly string[] Layers = ["Domain", "Application", "Infrastructure", "Endpoints"];

    public string Domain => $"{Namespace}.Domain";

    public string Application => $"{Namespace}.Application";

    public string Infrastructure => $"{Namespace}.Infrastructure";

    public string Endpoints => $"{Namespace}.Endpoints";

    public string Contracts => $"{Namespace}.Contracts";

    /// <summary>Types of the module itself, excluding its contracts.</summary>
    public bool IsInterior(string typeNamespace) =>
        NamespaceRules.IsWithin(typeNamespace, Namespace) && !NamespaceRules.IsWithin(typeNamespace, Contracts);

    public bool IsContract(string typeNamespace) => NamespaceRules.IsWithin(typeNamespace, Contracts);

    public override string ToString() => Namespace;
}

internal static class NamespaceRules
{
    /// <summary>True when the namespace is the given one or one of its children.</summary>
    public static bool IsWithin(string typeNamespace, string root) =>
        typeNamespace == root || typeNamespace.StartsWith(root + ".", StringComparison.Ordinal);
}
