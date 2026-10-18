using FiscoProjects.Architecture.Tests.Fixtures.ValidModule.Application;
using FiscoProjects.Architecture.Tests.Fixtures.ValidModule.Domain;

namespace FiscoProjects.Architecture.Tests.Fixtures.ValidModule.Infrastructure;

public sealed class OrderStore : IOrderStore
{
    public Task SaveAsync(Order order, CancellationToken cancellationToken) => Task.CompletedTask;
}
