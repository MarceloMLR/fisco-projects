using FiscoProjects.Documents.Domain;
using FiscoProjects.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Documents.Infrastructure.Persistence;

internal sealed class DocumentsDbContext(DbContextOptions<DocumentsDbContext> options)
    : ModuleDbContext<DocumentsDbContext>(options), IModuleSchema
{
    public static string Schema => "documents";

    public DbSet<DefaultFolder> DefaultFolders => Set<DefaultFolder>();

    public DbSet<DocumentFolder> Folders => Set<DocumentFolder>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentsDbContext).Assembly);
}

// For dotnet ef to create the context without starting the API.
internal sealed class DocumentsDbContextFactory : ModuleDesignTimeDbContextFactory<DocumentsDbContext>
{
    protected override DocumentsDbContext CreateContext(DbContextOptions<DocumentsDbContext> options) => new(options);
}
