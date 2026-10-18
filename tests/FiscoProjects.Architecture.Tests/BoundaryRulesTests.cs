using FiscoProjects.Architecture.Tests.Fixtures;

namespace FiscoProjects.Architecture.Tests;

// Spec: arquitetura-modular, requirements "Módulos só se conhecem pelos contratos"
// and "Contratos e núcleo compartilhado não dependem de módulos".
public sealed class BoundaryRulesTests
{
    private static readonly ArchUnitNET.Domain.Architecture Fixtures = FixtureModules.Architecture;

    [Fact(DisplayName = "Módulo usa só o contrato de outro módulo")]
    public void ModuleUsingOnlyContracts_Passes() =>
        ArchitectureRules.UsesOnlyContractsOfOtherModules(Fixtures, FixtureModules.Valid, FixtureModules.All)
            .Should().BeEmpty();

    [Fact(DisplayName = "Módulo usa o interior de outro módulo")]
    public void ModuleUsingAnotherModuleInterior_IsReported()
    {
        var violations = ArchitectureRules.UsesOnlyContractsOfOtherModules(
            Fixtures, FixtureModules.UsesOtherModuleInterior, FixtureModules.All);

        violations.Should().ContainSingle().Which.Should().Contain("Application.AssignCustomer").And.Contain("OtherModule.Domain.Customer");
    }

    [Fact(DisplayName = "Contrato usa o interior de um módulo")]
    public void ContractUsingModuleInterior_IsReported()
    {
        ArchitectureRules.ContractsDoNotUseModuleInteriors(Fixtures, FixtureModules.Other, FixtureModules.All)
            .Should().BeEmpty();

        var violations = ArchitectureRules.ContractsDoNotUseModuleInteriors(
            Fixtures, FixtureModules.ContractUsesInterior, FixtureModules.All);

        violations.Should().ContainSingle().Which.Should().Contain("Contracts.TicketSummary").And.Contain("Domain.Ticket");
    }

    [Fact(DisplayName = "Núcleo compartilhado usa um módulo")]
    public void SharedKernelUsingModuleOrWebFramework_IsReported()
    {
        var violations = ArchitectureRules.SharedKernelIsIndependent(
            Fixtures, FixtureModules.BrokenKernelNamespace, FixtureModules.All);

        violations.Should().HaveCount(2);
        violations.Should().Contain(violation => violation.Contains("OtherModule.Domain.Customer"));
        violations.Should().Contain(violation => violation.Contains("Microsoft.AspNetCore.Http.HttpContext"));
    }
}
