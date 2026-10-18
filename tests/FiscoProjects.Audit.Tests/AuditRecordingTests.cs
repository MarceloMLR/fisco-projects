using FiscoProjects.Audit.Contracts;
using FiscoProjects.Audit.Domain;
using FiscoProjects.Testing;
using FiscoProjects.Testing.Samples;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Audit.Tests;

// Spec: auditoria, requirements "Registro automático das alterações", "Registro junto com a alteração",
// "Eventos de segurança" and "Auditoria somente leitura".
public sealed class AuditRecordingTests(SqlServerFixture sqlServer) : IAsyncLifetime
{
    private static readonly Guid Marina = Guid.CreateVersion7();

    private AuditDatabase _database = null!;

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => _database = await AuditDatabase.CreateAsync(sqlServer);

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact(DisplayName = "Autor da alteração")]
    public async Task ChangeInASession_IsRecordedWithThatPersonAsTheAuthor()
    {
        var record = new GammaRecord { Name = "Contrato", Quantity = 3, Password = "s3cret" };

        await _database.RunAsync<GammaDbContext>(async db =>
        {
            db.Records.Add(record);
            await db.SaveChangesAsync(Cancellation);
        }, sessionUser: Marina);

        var entry = (await EntriesAsync()).Should().ContainSingle().Subject;
        entry.Should().BeEquivalentTo(new
        {
            OccurredAt = _database.Clock.GetUtcNow().UtcDateTime,
            ActorId = (Guid?)Marina,
            Action = AuditAction.Created,
            Module = "gamma",
            ItemType = nameof(GammaRecord),
            ItemId = (Guid?)record.Id,
            ItemLabel = "Contrato",
        });
        entry.Changes.Should().ContainEquivalentOf(new AuditField("Name", null, "Contrato", false))
            .And.ContainEquivalentOf(new AuditField("Quantity", null, "3", false))
            .And.ContainEquivalentOf(new AuditField("Password", null, null, true));
    }

    [Fact(DisplayName = "Alteração sem sessão")]
    public async Task ChangeWithoutASession_IsRecordedWithoutAnAuthor()
    {
        await _database.RunAsync<GammaDbContext>(async db =>
        {
            db.Records.Add(new GammaRecord { Name = "Contrato" });
            await db.SaveChangesAsync(Cancellation);
        });

        (await EntriesAsync()).Should().ContainSingle().Which.ActorId.Should().BeNull();
    }

    [Fact(DisplayName = "Registro na transação do módulo")]
    public async Task RolledBackModuleTransaction_LeavesNoRecord()
    {
        await _database.RunAsync<GammaDbContext>(db => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(Cancellation);
            db.Records.Add(new GammaRecord { Name = "Desfeito" });
            await db.SaveChangesAsync(Cancellation);
            await transaction.RollbackAsync(Cancellation);
        }));

        (await EntriesAsync()).Should().BeEmpty();
    }

    [Fact(DisplayName = "Alteração que não é salva")]
    public async Task FailedSave_LeavesNoRecord()
    {
        var save = () => _database.RunAsync<GammaDbContext>(async db =>
        {
            db.Records.Add(new GammaRecord { Name = new string('x', 21) });
            await db.SaveChangesAsync(Cancellation);
        });

        await save.Should().ThrowAsync<DbUpdateException>();
        (await EntriesAsync()).Should().BeEmpty();
    }

    [Fact(DisplayName = "Entrada registrada")]
    public async Task SignIn_IsRecordedWithThePersonAndTheProvider()
    {
        await _database.RunAsync<ISecurityEvents>(events => events.SignedInAsync(Marina, "local", Cancellation));

        (await EntriesAsync()).Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            OccurredAt = _database.Clock.GetUtcNow().UtcDateTime,
            ActorId = (Guid?)Marina,
            Action = AuditAction.SignedIn,
            ItemType = AuditItemTypes.SignIn,
            ItemId = (Guid?)Marina,
            Detail = "local",
        });
    }

    [Fact(DisplayName = "Entrada recusada registrada")]
    public async Task RefusedSignIn_IsRecordedBySystemWithTheIdentityAndTheReason()
    {
        await _database.RunAsync<ISecurityEvents>(events =>
            events.SignInRefusedAsync("  joao.silva@parceiro.example ", SignInRefusal.NotInDirectory, Cancellation));
        await _database.RunAsync<ISecurityEvents>(events =>
            events.SignInRefusedAsync(new string('a', 300), SignInRefusal.Inactive, Cancellation));

        var entries = await EntriesAsync();
        entries.Should().AllSatisfy(entry =>
        {
            entry.ActorId.Should().BeNull();
            entry.Action.Should().Be(AuditAction.SignInRefused);
            entry.ItemType.Should().Be(AuditItemTypes.SignIn);
        });
        entries.Select(entry => (entry.ItemLabel, entry.Detail)).Should().BeEquivalentTo(
        [
            ("joao.silva@parceiro.example", "NotInDirectory"),
            (new string('a', 256), "Inactive"),
        ]);
    }

    [Fact(DisplayName = "Gravação só acrescenta")]
    public async Task AuditContext_RefusesToChangeOrRemoveARecord()
    {
        await _database.RunAsync<ISecurityEvents>(events => events.SignedInAsync(Marina, "local", Cancellation));

        await using (var db = _database.NewAuditContext())
        {
            var entry = await db.Entries.SingleAsync(Cancellation);
            db.Entry(entry).Property(e => e.Detail).CurrentValue = "entra";
            var change = () => db.SaveChangesAsync(Cancellation);
            await change.Should().ThrowAsync<InvalidOperationException>();
        }

        await using (var db = _database.NewAuditContext())
        {
            db.Entries.Remove(await db.Entries.SingleAsync(Cancellation));
            var remove = () => db.SaveChangesAsync(Cancellation);
            await remove.Should().ThrowAsync<InvalidOperationException>();
        }

        (await EntriesAsync()).Should().ContainSingle().Which.Detail.Should().Be("local");
    }

    private async Task<List<AuditEntry>> EntriesAsync()
    {
        await using var db = _database.NewAuditContext();
        return await db.Entries.AsNoTracking().OrderBy(entry => entry.OccurredAt).ThenBy(entry => entry.Id).ToListAsync(Cancellation);
    }
}
