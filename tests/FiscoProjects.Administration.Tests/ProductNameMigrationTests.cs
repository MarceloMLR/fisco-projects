using FiscoProjects.Administration.Domain;
using FiscoProjects.SharedKernel;
using FiscoProjects.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FiscoProjects.Administration.Tests;

// Spec: configuracao-da-instalacao, requirement "Nome da instalação" (ADR-0159).
public sealed class ProductNameMigrationTests(SqlServerFixture sqlServer)
{
    /// <summary>The migration before the one that brings the new name of the product.</summary>
    private const string Before = "20261102025703_InstallationColors";

    private const string OldProductName = "FiscoProjects";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Nome antigo do produto")]
    public async Task TheOldNameOfTheProduct_BecomesTheNewOne()
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);
        await MigrateAsync(database, Before);
        (await NameAsync(database)).Should().Be(OldProductName);

        await MigrateAsync(database, null);

        (await NameAsync(database)).Should().Be(Product.Name);
        InstallationSettings.DefaultName.Should().Be("GerenWare");
    }

    [Fact(DisplayName = "Nome escolhido continua")]
    public async Task ANameTheAdministratorChose_Stays()
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);
        await MigrateAsync(database, Before);
        await using (var context = database.NewContext())
        {
            await context.Database.ExecuteSqlAsync(
                $"UPDATE [administration].[installation_settings] SET [Name] = {"Consultoria Fictícia"}", Cancellation);
        }

        await MigrateAsync(database, null);

        (await NameAsync(database)).Should().Be("Consultoria Fictícia");
    }

    private static async Task MigrateAsync(AdministrationDatabase database, string? target)
    {
        await using var context = database.NewContext();
        await context.GetService<IMigrator>().MigrateAsync(target, Cancellation);
    }

    private static async Task<string> NameAsync(AdministrationDatabase database)
    {
        await using var context = database.NewContext();
        return await context.Database.SqlQuery<string>($"SELECT [Name] AS [Value] FROM [administration].[installation_settings]")
            .SingleAsync(Cancellation);
    }
}
