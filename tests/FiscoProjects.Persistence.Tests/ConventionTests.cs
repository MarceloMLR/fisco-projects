using FiscoProjects.Testing;
using FiscoProjects.Testing.Samples;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Persistence.Tests;

// Spec: persistencia, requirements "Datas e horas em UTC" and "Identificador gerado pela aplicação".
public sealed class ConventionTests(SqlServerFixture sqlServer)
{
    [Fact(DisplayName = "Data e hora em UTC")]
    public async Task UtcDateTime_ComesBackAsTheSameUtcInstant()
    {
        var occurredAt = new DateTime(2026, 10, 17, 15, 30, 0, DateTimeKind.Utc);

        var saved = await SaveAndReadAsync(new AlphaRecord { Name = "utc", OccurredAt = occurredAt, ClosedAt = occurredAt });

        saved.OccurredAt.Kind.Should().Be(DateTimeKind.Utc);
        saved.OccurredAt.Should().Be(occurredAt);
        saved.ClosedAt.Should().NotBeNull();
        saved.ClosedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        saved.ClosedAt.Value.Should().Be(occurredAt);
    }

    [Fact(DisplayName = "Data e hora local")]
    public async Task LocalDateTime_IsStoredAsUtc()
    {
        var local = new DateTime(2026, 10, 17, 12, 30, 0, DateTimeKind.Local);

        var saved = await SaveAndReadAsync(new AlphaRecord { Name = "local", OccurredAt = local });

        saved.OccurredAt.Kind.Should().Be(DateTimeKind.Utc);
        saved.OccurredAt.Should().Be(local.ToUniversalTime());
    }

    [Fact(DisplayName = "Entidade gravada")]
    public async Task SavedEntity_KeepsTheIdItWasCreatedWith()
    {
        var record = new AlphaRecord { Name = "id", OccurredAt = DateTime.UtcNow, Day = new DateOnly(2026, 10, 17) };
        var createdId = record.Id;

        var saved = await SaveAndReadAsync(record);

        saved.Id.Should().Be(createdId);
        saved.Id.Version.Should().Be(7);
        saved.Day.Should().Be(new DateOnly(2026, 10, 17));
    }

    private async Task<AlphaRecord> SaveAndReadAsync(AlphaRecord record)
    {
        await using var database = TestDatabase.Create(sqlServer.NewDatabase(),
            (services, configuration) => services.AddModuleDbContext<AlphaDbContext>(configuration));
        await database.MigrateAsync();

        await using (var writer = database.NewContext<AlphaDbContext>())
        {
            writer.Records.Add(record);
            await writer.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var reader = database.NewContext<AlphaDbContext>();
        return await reader.Records.AsNoTracking().SingleAsync(r => r.Id == record.Id, TestContext.Current.CancellationToken);
    }
}
