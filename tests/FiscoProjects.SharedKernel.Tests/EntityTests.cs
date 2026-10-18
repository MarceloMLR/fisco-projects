namespace FiscoProjects.SharedKernel.Tests;

// Spec: arquitetura-modular, requirement "Identificadores de entidade em GUID versão 7".
public sealed class EntityTests
{
    private sealed class SampleEntity : Entity;

    [Fact(DisplayName = "Entidade nova")]
    public void NewEntity_GetsVersion7Guid()
    {
        var entity = new SampleEntity();

        entity.Id.Should().NotBe(Guid.Empty);
        entity.Id.Version.Should().Be(7);
    }

    [Fact(DisplayName = "Duas entidades")]
    public void TwoEntities_GetDifferentIds()
    {
        var first = new SampleEntity();
        var second = new SampleEntity();

        first.Id.Should().NotBe(second.Id);
    }
}
