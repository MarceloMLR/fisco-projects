using GerenWare.Architecture.Tests.Fixtures.OtherModule.Domain;
using Microsoft.AspNetCore.Http;

namespace GerenWare.Architecture.Tests.Fixtures.BrokenKernel;

public sealed class KernelHelper
{
    public Customer? Customer { get; init; }

    public HttpContext? Context { get; init; }
}
