using GerenWare.Architecture.Tests.Fixtures.ValidModule.Application;
using GerenWare.Architecture.Tests.Fixtures.ValidModule.Domain;

namespace GerenWare.Architecture.Tests.Fixtures.ValidModule.Infrastructure;

public sealed class OrderStore : IOrderStore
{
    public Task SaveAsync(Order order, CancellationToken cancellationToken) => Task.CompletedTask;
}
