namespace FiscoProjects.Architecture.Tests;

// Spec: arquitetura-modular, requirement "Regras valem para todo módulo novo".
public sealed class ModuleDiscoveryTests
{
    [Fact(DisplayName = "Novo módulo segue a convenção de nomes")]
    public void ModuleAssemblies_AreFoundByName()
    {
        var modules = ModuleCatalog.FindModules(
            ["FiscoProjects.Tickets", "FiscoProjects.Tickets.Contracts", "FiscoProjects.Projects.Contracts"]);

        modules.Select(module => module.Namespace).Should().Equal("FiscoProjects.Projects", "FiscoProjects.Tickets");
    }

    [Fact(DisplayName = "Assemblies que não são módulos")]
    public void HostKernelAndTestAssemblies_AreNotModules()
    {
        var modules = ModuleCatalog.FindModules(
        [
            "FiscoProjects.Api",
            "FiscoProjects.SharedKernel",
            "FiscoProjects.Api.Tests",
            "FiscoProjects.Architecture.Tests",
            "Microsoft.AspNetCore.OpenApi",
        ]);

        modules.Should().BeEmpty();
    }
}
