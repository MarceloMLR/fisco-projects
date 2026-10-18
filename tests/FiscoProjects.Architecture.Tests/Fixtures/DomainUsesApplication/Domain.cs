using FiscoProjects.Architecture.Tests.Fixtures.DomainUsesApplication.Application;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Architecture.Tests.Fixtures.DomainUsesApplication.Domain;

public sealed class Invoice : Entity
{
    public InvoiceService? Service { get; init; }
}
