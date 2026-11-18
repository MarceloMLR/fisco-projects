using GerenWare.Architecture.Tests.Fixtures.DomainUsesApplication.Application;
using GerenWare.SharedKernel;

namespace GerenWare.Architecture.Tests.Fixtures.DomainUsesApplication.Domain;

public sealed class Invoice : Entity
{
    public InvoiceService? Service { get; init; }
}
