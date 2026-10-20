using FiscoProjects.Credentials.Domain;
using FiscoProjects.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Credentials.Infrastructure.Persistence;

internal sealed class CredentialsDbContext(DbContextOptions<CredentialsDbContext> options)
    : ModuleDbContext<CredentialsDbContext>(options), IModuleSchema
{
    public static string Schema => "credentials";

    public DbSet<PortalCredential> PortalCredentials => Set<PortalCredential>();

    public DbSet<CredentialKey> Keys => Set<CredentialKey>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CredentialsDbContext).Assembly);
}

// For dotnet ef to create the context without starting the API.
internal sealed class CredentialsDbContextFactory : ModuleDesignTimeDbContextFactory<CredentialsDbContext>
{
    protected override CredentialsDbContext CreateContext(DbContextOptions<CredentialsDbContext> options) => new(options);
}
