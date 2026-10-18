using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FiscoProjects.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> create a module context. Adding a migration needs no database; commands that touch
/// the database read the connection string from the ConnectionStrings__FiscoProjects environment variable.
/// </summary>
public abstract class ModuleDesignTimeDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : ModuleDbContext<TContext>, IModuleSchema
{
    private const string PlaceholderConnectionString = "Server=localhost;Database=FiscoProjects;TrustServerCertificate=True";

    public TContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            $"ConnectionStrings__{PersistenceServiceCollectionExtensions.ConnectionStringName}");
        var options = new DbContextOptionsBuilder<TContext>();
        ModuleDbContextOptions.Configure<TContext>(options, connectionString ?? PlaceholderConnectionString);
        return CreateContext(options.Options);
    }

    protected abstract TContext CreateContext(DbContextOptions<TContext> options);
}
