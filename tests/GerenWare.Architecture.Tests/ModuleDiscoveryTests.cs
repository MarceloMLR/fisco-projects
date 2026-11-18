namespace GerenWare.Architecture.Tests;

// Spec: arquitetura-modular, requirement "Regras valem para todo módulo novo".
public sealed class ModuleDiscoveryTests
{
    [Fact(DisplayName = "Novo módulo segue a convenção de nomes")]
    public void ModuleAssemblies_AreFoundByName()
    {
        var modules = ModuleCatalog.FindModules(
            ["GerenWare.Tickets", "GerenWare.Tickets.Contracts", "GerenWare.Projects.Contracts"]);

        modules.Select(module => module.Namespace).Should().Equal("GerenWare.Projects", "GerenWare.Tickets");
    }

    [Fact(DisplayName = "Assemblies que não são módulos")]
    public void HostKernelAndTestAssemblies_AreNotModules()
    {
        var modules = ModuleCatalog.FindModules(
        [
            "GerenWare.Api",
            "GerenWare.SharedKernel",
            "GerenWare.Persistence",
            "GerenWare.Testing",
            "GerenWare.Api.Tests",
            "GerenWare.Architecture.Tests",
            "Microsoft.AspNetCore.OpenApi",
        ]);

        modules.Should().BeEmpty();
    }
}
