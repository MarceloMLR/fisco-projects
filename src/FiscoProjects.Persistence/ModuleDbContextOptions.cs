using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Persistence;

internal static class ModuleDbContextOptions
{
    public const string HistoryTable = "__EFMigrationsHistory";

    public static void Configure<TContext>(DbContextOptionsBuilder options, string? connectionString)
        where TContext : ModuleDbContext<TContext>, IModuleSchema
    {
        var migrationsAssembly = typeof(TContext).Assembly.GetName().Name;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Fails only when the database is used, so the API still starts and /health reports it.
            options.UseSqlServer(sql => Configure<TContext>(sql, migrationsAssembly));
        }
        else
        {
            options.UseSqlServer(connectionString, sql => Configure<TContext>(sql, migrationsAssembly));
        }
    }

    private static void Configure<TContext>(
        Microsoft.EntityFrameworkCore.Infrastructure.SqlServerDbContextOptionsBuilder sql, string? migrationsAssembly)
        where TContext : ModuleDbContext<TContext>, IModuleSchema =>
        sql.MigrationsHistoryTable(HistoryTable, TContext.Schema)
            .MigrationsAssembly(migrationsAssembly)
            .EnableRetryOnFailure();
}
