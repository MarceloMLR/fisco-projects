using FiscoProjects.Testing;
using FiscoProjects.Testing.Samples;

namespace FiscoProjects.Persistence.Tests;

// Spec: persistencia, requirement "Cada módulo no próprio schema".
public sealed class ModuleSchemaTests(SqlServerFixture sqlServer)
{
    [Fact(DisplayName = "Módulo com schema próprio")]
    public async Task ModuleMigrations_PutTablesAndHistoryInModuleSchema()
    {
        await using var database = TestDatabase.Create(sqlServer.NewDatabase(),
            (services, configuration) => services.AddModuleDbContext<AlphaDbContext>(configuration));

        await database.MigrateAsync();

        var tables = await database.TablesAsync();
        tables.Should().BeEquivalentTo([("alpha", "__EFMigrationsHistory"), ("alpha", "records")]);
    }

    [Fact(DisplayName = "Dois módulos no mesmo banco")]
    public async Task TwoModules_EachKeepTheirOwnTablesAndHistory()
    {
        await using var database = TestDatabase.Create(sqlServer.NewDatabase(), (services, configuration) =>
        {
            services.AddModuleDbContext<AlphaDbContext>(configuration);
            services.AddModuleDbContext<BetaDbContext>(configuration);
        });

        await database.MigrateAsync();

        var tables = await database.TablesAsync();
        tables.Should().BeEquivalentTo(
        [
            ("alpha", "__EFMigrationsHistory"), ("alpha", "records"),
            ("beta", "__EFMigrationsHistory"), ("beta", "records"),
        ]);
        (await database.AppliedMigrationsAsync(AlphaDbContext.Schema)).Should().ContainSingle()
            .Which.Should().EndWith("_Initial");
        (await database.AppliedMigrationsAsync(BetaDbContext.Schema)).Should().ContainSingle()
            .Which.Should().NotBe((await database.AppliedMigrationsAsync(AlphaDbContext.Schema))[0]);
    }
}
