using FiscoProjects.Persistence;
using FiscoProjects.SharedKernel;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>Services of the Users module: people, their identity links, the local sign-in and the sign-in through Entra ID.</summary>
public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddModuleDbContext<UsersDbContext>(configuration);
        services.AddScoped<UserStore>();
        services.AddScoped<IUserStore>(provider => provider.GetRequiredService<UserStore>());
        services.AddScoped<IUserAccess>(provider => provider.GetRequiredService<UserStore>());
        services.AddScoped<IUserDirectory>(provider => provider.GetRequiredService<UserStore>());
        services.AddScoped<IAuditReferenceNames, UserReferenceNames>();
        services.AddScoped<SignInWithIdentity>();
        services.AddScoped<GetSignedInUser>();
        services.AddScoped<ChangeSystemRole>();
        services.AddScoped<DeactivateUser>();
        services.AddScoped<ReactivateUser>();
        services.AddScoped<AnonymizeUser>();
        services.AddScoped<ListUsers>();
        services.AddScoped<ListPeople>();
        services.AddScoped<IPeopleDirectory, LocalPeopleDirectory>();
        services.AddScoped<IUserProvisioning, UserFromDirectory>();
        services.AddScoped<GetProfile>();
        services.AddScoped<ChooseTheme>();
        services.AddScoped<ChooseDailySummary>();

        services.AddScoped<IAuthorizationHandler, SystemRoleHandler>();
        services.AddAuthorizationBuilder()
            .AddPolicy(SystemPolicies.Administrator, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new SystemRoleRequirement(SystemRole.Administrator)))
            .AddPolicy(SystemPolicies.ProjectCreators, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new SystemRoleRequirement(SystemRole.Administrator, SystemRole.ProjectManager)));

        services.AddOptions<FirstAdministratorOptions>()
            .Bind(configuration.GetSection(FirstAdministratorOptions.Section))
            .Validate(
                options => options.IsConfigured
                    || (string.IsNullOrWhiteSpace(options.Provider) && string.IsNullOrWhiteSpace(options.ExternalId)),
                "Users:FirstAdministrator needs both Provider and ExternalId, or neither.")
            .ValidateOnStart();

        services.AddOptions<AnonymizationOptions>()
            .Bind(configuration.GetSection(AnonymizationOptions.Section))
            .Validate(options => options.IsValid, "Users:AnonymousName needs the identifier of an anonymized person, with {0} for its number.")
            .ValidateOnStart();

        services.AddOptions<LocalSignInOptions>()
            .Bind(configuration.GetSection(LocalSignInOptions.Section))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<LocalSignInOptions>, LocalSignInValidation>();
        services.AddScoped<SignInLocally>();

        // The sign-in through Entra ID (ADR-0117); the scheme itself is added with the session (AddEntraSignIn).
        services.AddOptions<EntraSignInOptions>()
            .Bind(configuration.GetSection(EntraSignInOptions.Section))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<EntraSignInOptions>, EntraSignInValidation>();
        services.TryAddSingleton<IEntraClientAssertion, ManagedIdentityClientAssertion>();
        services.AddScoped<SignInWithEntra>();
        // The main reads, once after the API starts (ADR-0106).
        services.AddScoped<IWarmUp, UsersWarmUp>();
        return services;
    }
}
