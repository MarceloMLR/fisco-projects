using FiscoProjects.Persistence;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>Services of the Users module: people, their identity links and the local sign-in.</summary>
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

        services.AddOptions<LocalSignInOptions>()
            .Bind(configuration.GetSection(LocalSignInOptions.Section))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<LocalSignInOptions>, LocalSignInValidation>();
        services.AddScoped<SignInLocally>();
        return services;
    }
}
