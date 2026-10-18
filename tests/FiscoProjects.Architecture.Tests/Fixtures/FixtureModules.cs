namespace FiscoProjects.Architecture.Tests.Fixtures;

internal static class FixtureModules
{
    private const string Root = "FiscoProjects.Architecture.Tests.Fixtures";

    public static readonly ModuleRoot Valid = new($"{Root}.ValidModule");
    public static readonly ModuleRoot Other = new($"{Root}.OtherModule");
    public static readonly ModuleRoot DomainUsesApplication = new($"{Root}.DomainUsesApplication");
    public static readonly ModuleRoot ApplicationUsesInfrastructure = new($"{Root}.ApplicationUsesInfrastructure");
    public static readonly ModuleRoot EndpointsUseInfrastructure = new($"{Root}.EndpointsUseInfrastructure");
    public static readonly ModuleRoot DomainUsesTechnology = new($"{Root}.DomainUsesTechnology");
    public static readonly ModuleRoot UsesOtherModuleInterior = new($"{Root}.UsesOtherModuleInterior");
    public static readonly ModuleRoot ContractUsesInterior = new($"{Root}.ContractUsesInterior");
    public static readonly ModuleRoot TypeOutsideLayers = new($"{Root}.TypeOutsideLayers");

    public const string BrokenKernelNamespace = $"{Root}.BrokenKernel";

    public static readonly IReadOnlyList<ModuleRoot> All =
    [
        Valid, Other, DomainUsesApplication, ApplicationUsesInfrastructure, EndpointsUseInfrastructure,
        DomainUsesTechnology, UsesOtherModuleInterior, ContractUsesInterior, TypeOutsideLayers,
    ];

    public static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchUnitNET.Loader.ArchLoader().LoadAssemblies(typeof(FixtureModules).Assembly).Build();
}
