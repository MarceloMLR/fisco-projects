using GerenWare.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace GerenWare.Architecture.Tests.Fixtures.DomainUsesTechnology.Domain;

public sealed class Report : Entity
{
    public HttpContext? Context { get; init; }
}
