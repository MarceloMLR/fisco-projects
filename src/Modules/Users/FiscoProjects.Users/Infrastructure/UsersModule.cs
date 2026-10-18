using FiscoProjects.Persistence;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>Services of the Users module: people and their identity links.</summary>
public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddModuleDbContext<UsersDbContext>(configuration);
        services.AddScoped<UserStore>();
        services.AddScoped<IUserStore>(provider => provider.GetRequiredService<UserStore>());
        services.AddScoped<IUserAccess>(provider => provider.GetRequiredService<UserStore>());
        services.AddScoped<SignInWithIdentity>();
        services.AddScoped<GetSignedInUser>();
        return services;
    }
}
