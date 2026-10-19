using FiscoProjects.Audit.Application;
using FiscoProjects.Audit.Domain;
using FiscoProjects.Testing;
using FiscoProjects.Testing.Samples;

namespace FiscoProjects.Audit.Tests;

// Spec: auditoria, requirement "Referências a outros itens".
public sealed class AuditReferenceTests(SqlServerFixture sqlServer) : IAsyncLifetime
{
    private static readonly Guid Marina = Guid.CreateVersion7();

    private AuditDatabase _database = null!;

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _database = await AuditDatabase.CreateAsync(sqlServer);
        _database.Names[Marina] = "Marina Costa";
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact(DisplayName = "Referência pelo nome")]
    public async Task ReferenceFields_AndLabel_ComeWithTheName()
    {
        var recordId = Guid.CreateVersion7();
        await AddLinkAsync(Marina, recordId);

        var entry = await LinkEntryAsync();

        entry.Entry.ItemLabelReference.Should().Be(GammaDbContext.PersonReference);
        entry.ItemLabel.Should().Be("Marina Costa");
        entry.Changes.Should().ContainEquivalentOf(
            new AuditFieldView("PersonId", null, "Marina Costa", false, GammaDbContext.PersonReference));
        // The records have no source of names: the field keeps its type, without a name.
        entry.Changes.Should().ContainEquivalentOf(
            new AuditFieldView("RecordId", null, null, false, GammaDbContext.RecordReference));
        entry.Changes.Should().ContainEquivalentOf(new AuditFieldView("Kind", null, "Dono", false, null));
        entry.Entry.Changes.Should().ContainEquivalentOf(
            new AuditField("RecordId", null, recordId.ToString(), false, GammaDbContext.RecordReference));
    }

    [Fact(DisplayName = "Nome atual na referência")]
    public async Task RenamedItem_ShowsTheCurrentName()
    {
        await AddLinkAsync(Marina, null);

        _database.Names[Marina] = "Marina Costa Lima";

        (await LinkEntryAsync()).ItemLabel.Should().Be("Marina Costa Lima");
    }

    [Fact(DisplayName = "Item que não existe mais")]
    public async Task MissingItem_ComesWithoutAName()
    {
        await AddLinkAsync(Guid.CreateVersion7(), null);

        var entry = await LinkEntryAsync();

        entry.ItemLabel.Should().BeNull();
        entry.Changes.Should().ContainEquivalentOf(
            new AuditFieldView("PersonId", null, null, false, GammaDbContext.PersonReference));
    }

    private Task AddLinkAsync(Guid personId, Guid? recordId) =>
        _database.RunAsync<GammaDbContext>(async db =>
        {
            db.Links.Add(new GammaLink { PersonId = personId, RecordId = recordId, Kind = "Dono" });
            await db.SaveChangesAsync(Cancellation);
        });

    private async Task<AuditEntryView> LinkEntryAsync()
    {
        var page = await _database.RunAsync<ListAuditEntries, AuditEntriesPage>(list =>
            list.ExecuteAsync(new AuditQuery(null, nameof(GammaLink), null, null, 1, 25), Cancellation));
        return page.Items.Should().ContainSingle().Subject;
    }
}
