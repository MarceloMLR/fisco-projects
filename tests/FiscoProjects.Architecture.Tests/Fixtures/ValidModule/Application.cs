using FiscoProjects.Architecture.Tests.Fixtures.OtherModule.Contracts;
using FiscoProjects.Architecture.Tests.Fixtures.ValidModule.Domain;

namespace FiscoProjects.Architecture.Tests.Fixtures.ValidModule.Application;

public interface IOrderStore
{
    Task SaveAsync(Order order, CancellationToken cancellationToken);
}

public sealed class PlaceOrder(IOrderStore store)
{
    public CustomerInfo? Customer { get; init; }

    public Task ExecuteAsync(CancellationToken cancellationToken) => store.SaveAsync(new Order(), cancellationToken);
}
