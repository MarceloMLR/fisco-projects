using FiscoProjects.Persistence;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Architecture.Tests.Fixtures.DomainUsesPersistence.Domain;

public sealed class Archive : Entity
{
    public ModuleMigrator? Migrator { get; init; }
}
