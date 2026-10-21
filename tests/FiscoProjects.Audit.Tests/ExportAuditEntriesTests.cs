using FiscoProjects.Administration.Contracts;
using FiscoProjects.Audit.Application;
using FiscoProjects.Audit.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.Extensions.Time.Testing;

namespace FiscoProjects.Audit.Tests;

// Spec: auditoria, requirement "Relatório da auditoria" (ADR-0099), without the database.
public sealed class ExportAuditEntriesTests
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static readonly Guid Paulo = Guid.CreateVersion7();

    [Fact(DisplayName = "Registros demais para exportar")]
    public async Task MoreRecordsThanTheLimit_AreNotExported_NorRecorded()
    {
        var store = new Store(ExportAuditEntries.MaxRows + 1);
        var export = Export(store);

        var result = await export.ExecuteAsync(
            new AuditExportQuery(new DateOnly(2026, 1, 1), new DateOnly(2026, 10, 20), null, null), Paulo, Cancellation);

        result.Items.Should().BeNull();
        result.Total.Should().Be(ExportAuditEntries.MaxRows + 1);
        store.Added.Should().BeEmpty();
    }

    [Fact(DisplayName = "Período da exportação no fuso da instalação")]
    public async Task Period_IsTheDaysInTheTimeZoneOfTheInstallation()
    {
        var store = new Store(0);
        var export = Export(store);

        var result = await export.ExecuteAsync(
            new AuditExportQuery(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 20), " SignIn ", null), Paulo, Cancellation);

        result.Items.Should().BeEmpty();
        // Midnight in São Paulo is 03:00 in UTC; the last day goes to the start of the next.
        store.Queries.Should().ContainSingle().Which.Should().Be(new AuditQuery(
            null, "SignIn", new DateTimeOffset(2026, 10, 1, 3, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 21, 3, 0, 0, TimeSpan.Zero), 1, ExportAuditEntries.MaxRows));
        var recorded = store.Added.Should().ContainSingle().Subject;
        recorded.Action.Should().Be(AuditAction.Exported);
        recorded.ItemType.Should().Be(AuditItemTypes.AuditExport);
        recorded.ActorId.Should().Be(Paulo);
        recorded.Changes.Select(field => (field.Field, field.After)).Should().Equal(
            ("From", "2026-10-01"), ("To", "2026-10-20"), ("ItemType", "SignIn"), ("Rows", "0"));
    }

    [Theory(DisplayName = "Período inválido para exportar")]
    [InlineData(null, "2026-10-20")]
    [InlineData("2026-10-20", null)]
    [InlineData("2026-10-21", "2026-10-20")]
    [InlineData("2025-10-19", "2026-10-20")]
    public void InvalidPeriod_IsRefused(string? from, string? to)
    {
        var query = new AuditExportQuery(
            from is null ? null : DateOnly.Parse(from, null), to is null ? null : DateOnly.Parse(to, null), null, null);

        ExportAuditEntries.Validate(query).Should().NotBeEmpty();
    }

    [Fact(DisplayName = "Período de 366 dias")]
    public void PeriodOf366Days_IsAccepted() =>
        ExportAuditEntries.Validate(new AuditExportQuery(new DateOnly(2025, 10, 20), new DateOnly(2026, 10, 20), null, null))
            .Should().BeEmpty();

    private static ExportAuditEntries Export(Store store) =>
        new(store, new ListAuditEntries(store, new NoNames(), []), new Settings(), new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.Zero)));

    private sealed class Store(int total) : IAuditStore
    {
        public List<AuditQuery> Queries { get; } = [];

        public List<AuditEntry> Added { get; } = [];

        public Task<AuditPage> ListAsync(AuditQuery query, CancellationToken cancellationToken)
        {
            Queries.Add(query);
            return Task.FromResult(new AuditPage([], total));
        }

        public Task AddAsync(AuditEntry entry, CancellationToken cancellationToken)
        {
            Added.Add(entry);
            return Task.CompletedTask;
        }

        public Task<bool> HasCleanupAsync(DateOnly month, CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<int> RemoveBeforeAsync(DateTime before, CancellationToken cancellationToken) => Task.FromResult(0);

        public Task<bool> RecordCleanupAsync(AuditCleanupRun cleanup, AuditEntry? entry, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class Settings : IInstallationSettings
    {
        public Task<InstallationInfo> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new InstallationInfo("Empresa Exemplo", "America/Sao_Paulo", new TimeOnly(7, 30), 90));
    }

    private sealed class NoNames : IUserDirectory
    {
        public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<IReadOnlyList<PersonInfo>> PeopleAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PersonInfo>>([]);
    }
}
