using FiscoProjects.Audit.Application;
using FiscoProjects.Audit.Contracts;
using FiscoProjects.Audit.Domain;
using FiscoProjects.Testing;

namespace FiscoProjects.Audit.Tests;

// Spec: auditoria, requirement "Tela de Auditoria" (what the listing returns to the screen).
public sealed class ListAuditEntriesTests(SqlServerFixture sqlServer) : IAsyncLifetime
{
    private static readonly Guid Paulo = Guid.CreateVersion7();
    private static readonly Guid Marina = Guid.CreateVersion7();
    private static readonly DateTimeOffset Start = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private AuditDatabase _database = null!;

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _database = await AuditDatabase.CreateAsync(sqlServer);
        _database.Names[Paulo] = "Paulo Nogueira";
        _database.Names[Marina] = "Marina Costa";

        // One record a day, from October 20th: Paulo, Marina, a refused sign-in, Paulo, Marina.
        await RecordAtAsync(0, events => events.SignedInAsync(Paulo, "local", Cancellation));
        await RecordAtAsync(1, events => events.SignedInAsync(Marina, "local", Cancellation));
        await RecordAtAsync(2, events => events.SignInRefusedAsync("joao@parceiro.example", SignInRefusal.NotInDirectory, Cancellation));
        await RecordAtAsync(3, events => events.SignedInAsync(Paulo, "local", Cancellation));
        await RecordAtAsync(4, events => events.SignedInAsync(Marina, "local", Cancellation));
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact(DisplayName = "Lista da auditoria")]
    public async Task Listing_ComesNewestFirst_WithTheAuthorNamesAndTheTotal()
    {
        var page = await ListAsync(new AuditQuery(null, null, null, null, 1, 25));

        page.Total.Should().Be(5);
        page.Items.Select(item => item.Entry.OccurredAt).Should().BeInDescendingOrder();
        page.Items.Select(item => item.ActorName).Should().Equal(
            "Marina Costa", "Paulo Nogueira", null, "Marina Costa", "Paulo Nogueira");
    }

    [Fact(DisplayName = "Filtro por pessoa")]
    public async Task FilterByPerson_KeepsOnlyTheirRecords()
    {
        var page = await ListAsync(new AuditQuery(Marina, null, null, null, 1, 25));

        page.Total.Should().Be(2);
        page.Items.Should().OnlyContain(item => item.Entry.ActorId == Marina);
    }

    [Fact(DisplayName = "Filtro por tipo de item")]
    public async Task FilterByItemType_KeepsOnlyThatType()
    {
        (await ListAsync(new AuditQuery(null, AuditItemTypes.SignIn, null, null, 1, 25))).Total.Should().Be(5);
        (await ListAsync(new AuditQuery(null, "User", null, null, 1, 25))).Total.Should().Be(0);
    }

    [Fact(DisplayName = "Filtro por período")]
    public async Task FilterByPeriod_IncludesTheStartAndExcludesTheEnd()
    {
        var page = await ListAsync(new AuditQuery(null, null, Start.AddDays(1), Start.AddDays(3), 1, 25));

        page.Items.Select(item => item.Entry.OccurredAt).Should().Equal(
            Start.AddDays(2).UtcDateTime, Start.AddDays(1).UtcDateTime);
    }

    [Fact(DisplayName = "Paginação da auditoria")]
    public async Task Paging_SplitsTheRecordsAndKeepsTheTotal()
    {
        var second = await ListAsync(new AuditQuery(null, null, null, null, 2, 2));

        second.Total.Should().Be(5);
        second.Items.Select(item => item.Entry.OccurredAt).Should().Equal(
            Start.AddDays(2).UtcDateTime, Start.AddDays(1).UtcDateTime);
    }

    [Theory(DisplayName = "Consulta inválida")]
    [InlineData(0, 25, false, "page")]
    [InlineData(1, 0, false, "pageSize")]
    [InlineData(1, 101, false, "pageSize")]
    [InlineData(1, 25, true, "to")]
    public void InvalidQuery_IsReported(int page, int pageSize, bool invertedPeriod, string field)
    {
        var query = new AuditQuery(null, null, invertedPeriod ? Start.AddDays(1) : null, invertedPeriod ? Start : null, page, pageSize);

        ListAuditEntries.Validate(query).Should().ContainSingle().Which.Field.Should().Be(field);
    }

    private async Task RecordAtAsync(int day, Func<ISecurityEvents, Task> record)
    {
        _database.Clock.SetUtcNow(Start.AddDays(day));
        await _database.RunAsync(record);
    }

    private Task<AuditEntriesPage> ListAsync(AuditQuery query) =>
        _database.RunAsync<ListAuditEntries, AuditEntriesPage>(list => list.ExecuteAsync(query, Cancellation));
}
