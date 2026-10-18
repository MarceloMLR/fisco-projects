using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FiscoProjects.Persistence;

/// <summary>Applies the pending migrations of every registered module context, one module after the other.</summary>
public sealed class ModuleMigrator(IServiceScopeFactory scopeFactory)
{
    public async Task MigrateAllAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        foreach (var registration in scope.ServiceProvider.GetServices<ModuleDbContextRegistration>())
        {
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(registration.ContextType);
            await context.Database.MigrateAsync(cancellationToken);
        }
    }
}
