using FiscoProjects.MyWork.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FiscoProjects.MyWork.Infrastructure;

/// <summary>
/// Services of the MyWork module (ADR-0096): Meu trabalho of the person, put together from the items each module gives
/// (<see cref="Contracts.IMyWorkSource"/>), without tables of its own.
/// </summary>
public static class MyWorkModule
{
    public static IServiceCollection AddMyWorkModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<GetMyWork>();
        return services;
    }
}
