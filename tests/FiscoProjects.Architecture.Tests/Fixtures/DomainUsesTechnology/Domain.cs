using FiscoProjects.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace FiscoProjects.Architecture.Tests.Fixtures.DomainUsesTechnology.Domain;

public sealed class Report : Entity
{
    public HttpContext? Context { get; init; }
}
