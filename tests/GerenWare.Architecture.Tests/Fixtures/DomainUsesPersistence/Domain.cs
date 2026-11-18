using GerenWare.Persistence;
using GerenWare.SharedKernel;

namespace GerenWare.Architecture.Tests.Fixtures.DomainUsesPersistence.Domain;

public sealed class Archive : Entity
{
    public ModuleMigrator? Migrator { get; init; }
}
