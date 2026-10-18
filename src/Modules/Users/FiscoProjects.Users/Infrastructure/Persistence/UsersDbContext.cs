using FiscoProjects.Persistence;
using FiscoProjects.Users.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Users.Infrastructure.Persistence;

internal sealed class UsersDbContext(DbContextOptions<UsersDbContext> options)
    : ModuleDbContext<UsersDbContext>(options), IModuleSchema
{
    public static string Schema => "users";

    public DbSet<User> Users => Set<User>();

    public DbSet<IdentityLink> IdentityLinks => Set<IdentityLink>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
}

// For dotnet ef to create the context without starting the API.
internal sealed class UsersDbContextFactory : ModuleDesignTimeDbContextFactory<UsersDbContext>
{
    protected override UsersDbContext CreateContext(DbContextOptions<UsersDbContext> options) => new(options);
}
