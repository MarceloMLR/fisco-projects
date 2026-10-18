using FiscoProjects.Testing;
using FiscoProjects.Testing.Samples;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Persistence.Tests;

// Spec: persistencia, requirement "Migrações de todos os módulos".
public sealed class ModuleMigratorTests(SqlServerFixture sqlServer)
{
    [Fact(DisplayName = "Banco vazio")]
    public async Task EmptyDatabase_GetsEveryModuleUpToDate()
    {
        await using var database = AlphaAndBeta();

        await database.MigrateAsync();

        await using var alpha = database.NewContext<AlphaDbContext>();
        await using var beta = database.NewContext<BetaDbContext>();
        (await alpha.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
        (await beta.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
        (await alpha.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).Should().Equal(alpha.Database.GetMigrations());
        (await beta.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).Should().Equal(beta.Database.GetMigrations());
    }

    [Fact(DisplayName = "Migrações já aplicadas")]
    public async Task MigratingAgain_ChangesNothing()
    {
        await using var database = AlphaAndBeta();
        await database.MigrateAsync();
        var before = await database.AppliedMigrationsAsync(AlphaDbContext.Schema);

        var migrateAgain = database.MigrateAsync;

        await migrateAgain.Should().NotThrowAsync();
        (await database.AppliedMigrationsAsync(AlphaDbContext.Schema)).Should().Equal(before);
        (await database.AppliedMigrationsAsync(BetaDbContext.Schema)).Should().ContainSingle();
    }

    private TestDatabase AlphaAndBeta() =>
        TestDatabase.Create(sqlServer.NewDatabase(), (services, configuration) =>
        {
            services.AddModuleDbContext<AlphaDbContext>(configuration);
            services.AddModuleDbContext<BetaDbContext>(configuration);
        });
}
