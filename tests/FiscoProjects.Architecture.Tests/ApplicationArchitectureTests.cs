namespace FiscoProjects.Architecture.Tests;

/// <summary>
/// Applies every rule to the real application assemblies. New modules are picked up by name
/// (see <see cref="ModuleCatalog"/>), so these tests do not change when a module is added.
/// </summary>
public sealed class ApplicationArchitectureTests
{
    private static readonly IReadOnlyList<System.Reflection.Assembly> Assemblies = ModuleCatalog.ApplicationAssemblies();

    private static readonly ArchUnitNET.Domain.Architecture Application =
        new ArchUnitNET.Loader.ArchLoader().LoadAssemblies([.. Assemblies]).Build();

    private static readonly IReadOnlyList<ModuleRoot> Modules =
        ModuleCatalog.FindModules(Assemblies.Select(assembly => assembly.GetName().Name ?? string.Empty));

    [Fact(DisplayName = "Assemblies da aplicação carregados")]
    public void ApplicationAssemblies_AreLoaded() =>
        Assemblies.Select(assembly => assembly.GetName().Name)
            .Should().Contain(["FiscoProjects.Api", "FiscoProjects.SharedKernel"]);

    [Fact(DisplayName = "Módulos da aplicação seguem todas as regras")]
    public void EveryModule_FollowsTheRules()
    {
        var violations = Modules.SelectMany(module =>
            ArchitectureRules.LayersDependInward(Application, module)
                .Concat(ArchitectureRules.BusinessLayersWithoutTechnology(Application, module))
                .Concat(ArchitectureRules.TypesLiveInALayer(Application, module))
                .Concat(ArchitectureRules.UsesOnlyContractsOfOtherModules(Application, module, Modules))
                .Concat(ArchitectureRules.ContractsDoNotUseModuleInteriors(Application, module, Modules)));

        violations.Should().BeEmpty();
    }

    [Fact(DisplayName = "Persistência compartilhada da aplicação não usa módulos")]
    public void SharedPersistence_DoesNotUseModules() =>
        ArchitectureRules.PersistenceIsIndependent(Application, ModuleCatalog.PersistenceNamespace, Modules)
            .Should().BeEmpty();

    [Fact(DisplayName = "Núcleo compartilhado da aplicação é independente")]
    public void SharedKernel_IsIndependent() =>
        ArchitectureRules.SharedKernelIsIndependent(Application, ModuleCatalog.SharedKernelNamespace, Modules)
            .Should().BeEmpty();
}
