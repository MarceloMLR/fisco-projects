using FiscoProjects.Architecture.Tests.Fixtures;

namespace FiscoProjects.Architecture.Tests;

// Spec: arquitetura-modular, requirements "Camadas dependem só das camadas internas"
// and "Regras de negócio sem tecnologia externa".
public sealed class LayerRulesTests
{
    private static readonly ArchUnitNET.Domain.Architecture Fixtures = FixtureModules.Architecture;

    [Fact(DisplayName = "Módulo que segue as camadas")]
    public void ValidModule_PassesLayerRules()
    {
        ArchitectureRules.LayersDependInward(Fixtures, FixtureModules.Valid).Should().BeEmpty();
        ArchitectureRules.BusinessLayersWithoutTechnology(Fixtures, FixtureModules.Valid).Should().BeEmpty();
        ArchitectureRules.TypesLiveInALayer(Fixtures, FixtureModules.Valid).Should().BeEmpty();
    }

    [Fact(DisplayName = "Domínio usa uma camada externa")]
    public void DomainUsingApplication_IsReported()
    {
        var violations = ArchitectureRules.LayersDependInward(Fixtures, FixtureModules.DomainUsesApplication);

        violations.Should().ContainSingle().Which.Should().Contain("Domain.Invoice").And.Contain("Application.InvoiceService");
    }

    [Fact(DisplayName = "Aplicação usa infraestrutura")]
    public void ApplicationUsingInfrastructure_IsReported()
    {
        var violations = ArchitectureRules.LayersDependInward(Fixtures, FixtureModules.ApplicationUsesInfrastructure);

        violations.Should().ContainSingle().Which.Should().Contain("Application.CloseInvoice").And.Contain("Infrastructure.InvoiceTable");
    }

    [Fact(DisplayName = "Endpoints usam infraestrutura direto")]
    public void EndpointsUsingInfrastructure_IsReported()
    {
        var violations = ArchitectureRules.LayersDependInward(Fixtures, FixtureModules.EndpointsUseInfrastructure);

        violations.Should().ContainSingle().Which.Should().Contain("Endpoints.InvoiceEndpoints").And.Contain("Infrastructure.InvoiceTable");
    }

    [Fact(DisplayName = "Domínio usa biblioteca de banco ou web")]
    public void DomainUsingWebFramework_IsReported()
    {
        var violations = ArchitectureRules.BusinessLayersWithoutTechnology(Fixtures, FixtureModules.DomainUsesTechnology);

        violations.Should().ContainSingle().Which.Should().Contain("Domain.Report").And.Contain("Microsoft.AspNetCore.Http.HttpContext");
    }

    [Fact(DisplayName = "Tipo fora das camadas")]
    public void TypeOutsideLayers_IsReported()
    {
        var violations = ArchitectureRules.TypesLiveInALayer(Fixtures, FixtureModules.TypeOutsideLayers);

        violations.Should().ContainSingle().Which.Should().Contain("TypeOutsideLayers.Loose");
    }
}
