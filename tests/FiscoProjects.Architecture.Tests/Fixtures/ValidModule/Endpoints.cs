using FiscoProjects.Architecture.Tests.Fixtures.ValidModule.Application;
using Microsoft.AspNetCore.Http;

namespace FiscoProjects.Architecture.Tests.Fixtures.ValidModule.Endpoints;

public sealed class OrderEndpoints
{
    public PlaceOrder? UseCase { get; init; }

    public HttpContext? Context { get; init; }
}
