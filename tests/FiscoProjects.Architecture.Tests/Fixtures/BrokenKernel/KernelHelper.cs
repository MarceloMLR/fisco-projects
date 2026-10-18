using FiscoProjects.Architecture.Tests.Fixtures.OtherModule.Domain;
using Microsoft.AspNetCore.Http;

namespace FiscoProjects.Architecture.Tests.Fixtures.BrokenKernel;

public sealed class KernelHelper
{
    public Customer? Customer { get; init; }

    public HttpContext? Context { get; init; }
}
