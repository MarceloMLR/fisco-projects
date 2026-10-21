using FiscoProjects.Administration.Contracts;
using FiscoProjects.Audit.Application;
using FiscoProjects.Audit.Domain;
using Microsoft.Extensions.Time.Testing;

namespace FiscoProjects.Audit.Tests;

// Spec: dados-pessoais, requirements "Limpeza mensal da auditoria" and "Pessoa anonimizada em todo o sistema"
// (ADR-0058, ADR-0110), without the database.
public sealed class CleanUpAuditTests
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Limpeza no mês da instalação")]
    public async Task TheCleanup_KeepsTheYearsOfTheRetention_CountedFromTodayInTheInstallation()
    {
        // 02:00 of November 1 in UTC is still October 31 in São Paulo.
        var store = new Store(removes: 3);
        var cleanup = CleanUp(store, new DateTimeOffset(2026, 11, 1, 2, 0, 0, TimeSpan.Zero), years: 7);

        var result = await cleanup.ExecuteAsync(Cancellation);

        result.Should().Be(new AuditCleanupResult(new DateOnly(2019, 10, 31), 3));
        // Midnight in São Paulo is 03:00 in UTC.
        store.RemovedBefore.Should().Equal(new DateTime(2019, 10, 31, 3, 0, 0, DateTimeKind.Utc));
        var run = store.Runs.Should().ContainSingle().Subject;
        (run.Month, run.KeptFrom, run.Removed).Should().Be((new DateOnly(2026, 10, 1), new DateOnly(2019, 10, 31), 3));
        var entry = store.Entries.Should().ContainSingle().Subject;
        entry.Action.Should().Be(AuditAction.Cleaned);
        entry.ItemType.Should().Be(AuditItemTypes.AuditCleanup);
        entry.ActorId.Should().BeNull();
        entry.Changes.Select(field => (field.Field, field.After)).Should().Equal(
            (AuditCleanupFields.KeptFrom, "2019-10-31"), (AuditCleanupFields.Removed, "3"));
    }

    [Fact(DisplayName = "Limpeza sem registros antigos")]
    public async Task ACleanupThatRemovedNothing_IsNotRecordedInTheAudit()
    {
        var store = new Store(removes: 0);

        var result = await CleanUp(store, new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero), years: 5)
            .ExecuteAsync(Cancellation);

        result.Should().Be(new AuditCleanupResult(new DateOnly(2021, 10, 19), 0));
        store.Runs.Should().ContainSingle();
        store.Entries.Should().BeEmpty();
    }

    [Fact(DisplayName = "Uma limpeza por mês")]
    public async Task AMonthWithItsCleanup_IsLeftAlone()
    {
        var store = new Store(removes: 3) { Done = true };

        var result = await CleanUp(store, new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero), years: 5)
            .ExecuteAsync(Cancellation);

        result.Should().BeNull();
        store.RemovedBefore.Should().BeEmpty();
        store.Runs.Should().BeEmpty();
    }

    [Fact(DisplayName = "Registro da própria pessoa anonimizada")]
    public void EraseOwner_KeepsOnlyThatThePersonalFieldsChanged()
    {
        var entry = AuditEntry.ForChange(
            new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc), Guid.CreateVersion7(), "users", "User",
            Guid.CreateVersion7(), AuditAction.Updated, "Carla Mendes",
            [
                new AuditField("Name", "Carla Mendes", "Pessoa removida nº 1", false),
                new AuditField("Email", "carla.mendes@empresa.example", string.Empty, false),
                new AuditField("Role", "ProjectManager", "User", false),
            ]);

        entry.EraseOwner("Pessoa removida nº 1", ["Name", "Email"]);

        entry.ItemLabel.Should().Be("Pessoa removida nº 1");
        entry.Changes.Should().Equal(
            new AuditField("Name", null, null, true),
            new AuditField("Email", null, null, true),
            new AuditField("Role", "ProjectManager", "User", false));
    }

    [Fact(DisplayName = "Entrada recusada com o e-mail da pessoa")]
    public void EraseIdentity_NamesTheIdentifier()
    {
        var entry = AuditEntry.SignInRefused(DateTime.UtcNow, "carla.mendes@empresa.example", "Inactive");

        entry.EraseIdentity("Pessoa removida nº 1");

        entry.ItemLabel.Should().Be("Pessoa removida nº 1");
    }

    private static CleanUpAudit CleanUp(Store store, DateTimeOffset now, int years) =>
        new(store, new Settings(years), new FakeTimeProvider(now));

    private sealed class Store(int removes) : IAuditStore
    {
        public bool Done { get; init; }

        public List<DateTime> RemovedBefore { get; } = [];

        public List<AuditCleanupRun> Runs { get; } = [];

        public List<AuditEntry> Entries { get; } = [];

        public Task<AuditPage> ListAsync(AuditQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new AuditPage([], 0));

        public Task AddAsync(AuditEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<bool> HasCleanupAsync(DateOnly month, CancellationToken cancellationToken) => Task.FromResult(Done);

        public Task<int> RemoveBeforeAsync(DateTime before, CancellationToken cancellationToken)
        {
            RemovedBefore.Add(before);
            return Task.FromResult(removes);
        }

        public Task<bool> RecordCleanupAsync(AuditCleanupRun cleanup, AuditEntry? entry, CancellationToken cancellationToken)
        {
            Runs.Add(cleanup);
            if (entry is not null)
            {
                Entries.Add(entry);
            }

            return Task.FromResult(true);
        }
    }

    private sealed class Settings(int years) : IInstallationSettings
    {
        public Task<InstallationInfo> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new InstallationInfo("Empresa Exemplo", "America/Sao_Paulo", new TimeOnly(7, 30), 90, years));
    }
}
