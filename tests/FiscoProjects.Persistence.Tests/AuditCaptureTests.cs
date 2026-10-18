using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Testing;
using FiscoProjects.Testing.Samples;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FiscoProjects.Persistence.Tests;

// Spec: auditoria, requirements "Registro automático das alterações", "Registro junto com a alteração",
// "Campos técnicos fora da auditoria" and "Valores secretos fora da auditoria".
public sealed class AuditCaptureTests(SqlServerFixture sqlServer) : IAsyncLifetime
{
    private static readonly DateOnly Due = new(2026, 10, 20);

    private readonly JournalAuditor _auditor = new();
    private TestDatabase _database = null!;

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _database = TestDatabase.Create(sqlServer.NewDatabase(), (services, configuration) =>
        {
            services.AddModuleDbContext<GammaDbContext>(configuration);
            services.AddModuleDbContext<BetaDbContext>(configuration);
            services.AddSingleton<IChangeAuditor>(_auditor);
        });
        await _database.MigrateAsync();
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact(DisplayName = "Criar um item")]
    public async Task CreatingAnItem_RecordsEveryFieldAfter()
    {
        var record = await CreateAsync();

        var change = _auditor.Recorded.Should().ContainSingle().Subject;
        change.Should().BeEquivalentTo(new
        {
            Module = "gamma",
            ItemType = nameof(GammaRecord),
            ItemId = record.Id,
            Action = AuditedAction.Created,
            ItemLabel = "Contrato",
        });
        change.Fields.Should().BeEquivalentTo(
        [
            new AuditedField("Active", null, "true", false),
            new AuditedField("Due", null, "2026-10-20", false),
            new AuditedField("Name", null, "Contrato", false),
            new AuditedField("Password", null, null, true),
            new AuditedField("Quantity", null, "3", false),
            new AuditedField("Status", null, "Open", false),
        ]);
        (await JournalAsync()).Should().Equal("Created GammaRecord Contrato");
    }

    [Fact(DisplayName = "Alterar um item")]
    public async Task UpdatingAnItem_RecordsOnlyTheChangedFields()
    {
        var record = await CreateAsync();

        await ChangeAsync(record.Id, saved =>
        {
            saved.Name = "Contrato";
            saved.Quantity = 5;
            saved.Status = GammaStatus.Closed;
        });

        var change = _auditor.Recorded.Last();
        change.Action.Should().Be(AuditedAction.Updated);
        change.ItemLabel.Should().Be("Contrato");
        change.Fields.Should().BeEquivalentTo(
        [
            new AuditedField("Quantity", "3", "5", false),
            new AuditedField("Status", "Open", "Closed", false),
        ]);
    }

    [Fact(DisplayName = "Excluir um item")]
    public async Task DeletingAnItem_RecordsEveryFieldBefore()
    {
        var record = await CreateAsync();

        await using (var db = _database.NewContext<GammaDbContext>())
        {
            db.Records.Remove(await db.Records.SingleAsync(r => r.Id == record.Id, Cancellation));
            await db.SaveChangesAsync(Cancellation);
        }

        var change = _auditor.Recorded.Last();
        change.Action.Should().Be(AuditedAction.Deleted);
        change.ItemLabel.Should().Be("Contrato");
        change.Fields.Should().ContainEquivalentOf(new AuditedField("Quantity", "3", null, false))
            .And.ContainEquivalentOf(new AuditedField("Password", null, null, true))
            .And.OnlyContain(field => field.After == null);
    }

    [Fact(DisplayName = "Campo técnico fora da auditoria")]
    public async Task ChangingOnlyAnExcludedFieldOrItem_RecordsNothing()
    {
        var record = await CreateAsync();
        var seenAt = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

        await ChangeAsync(record.Id, saved => saved.LastSeenAt = seenAt);
        await using (var db = _database.NewContext<GammaDbContext>())
        {
            db.Notes.Add(new GammaNote { Text = "Nota" });
            await db.SaveChangesAsync(Cancellation);
        }

        _auditor.Recorded.Should().ContainSingle("only the creation is audited");
        await using var reader = _database.NewContext<GammaDbContext>();
        (await reader.Records.SingleAsync(r => r.Id == record.Id, Cancellation)).LastSeenAt.Should().Be(seenAt);
        (await reader.Notes.CountAsync(Cancellation)).Should().Be(1);
    }

    [Fact(DisplayName = "Senha alterada")]
    public async Task ChangingASecretField_RecordsOnlyThatItChanged()
    {
        var record = await CreateAsync();

        await ChangeAsync(record.Id, saved => saved.Password = "outra-senha");

        var change = _auditor.Recorded.Last();
        change.Fields.Should().Equal(new AuditedField("Password", null, null, Secret: true));
        _auditor.Recorded.SelectMany(recorded => recorded.Fields)
            .Should().NotContain(field => field.Before == "s3cret" || field.After == "s3cret" || field.After == "outra-senha");
    }

    [Fact(DisplayName = "Alteração que não é salva")]
    public async Task FailedSave_WritesNoRecord()
    {
        await using var db = _database.NewContext<GammaDbContext>();
        db.Records.Add(new GammaRecord { Name = new string('x', 21) });

        var save = () => db.SaveChangesAsync(Cancellation);

        await save.Should().ThrowAsync<DbUpdateException>();
        (await JournalAsync()).Should().BeEmpty();
    }

    [Fact(DisplayName = "Registro que não é gravado")]
    public async Task FailedRecord_UndoesTheSave()
    {
        _auditor.Fail = true;
        await using var db = _database.NewContext<GammaDbContext>();
        db.Records.Add(new GammaRecord { Name = "Contrato" });

        var save = () => db.SaveChangesAsync(Cancellation);

        await save.Should().ThrowAsync<InvalidOperationException>().WithMessage("Audit unavailable.");
        await using var reader = _database.NewContext<GammaDbContext>();
        (await reader.Records.CountAsync(Cancellation)).Should().Be(0);
    }

    [Fact(DisplayName = "Transação do módulo com auditoria")]
    public async Task ModuleTransaction_CarriesTheRecord()
    {
        await using var db = _database.NewContext<GammaDbContext>();

        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(Cancellation);
            db.Records.Add(new GammaRecord { Name = "Desfeito" });
            await db.SaveChangesAsync(Cancellation);
            await transaction.RollbackAsync(Cancellation);
        });

        _auditor.Recorded.Should().ContainSingle("the record was written in the module transaction");
        (await JournalAsync()).Should().BeEmpty("the module transaction was rolled back");
        await using var reader = _database.NewContext<GammaDbContext>();
        (await reader.Records.CountAsync(Cancellation)).Should().Be(0);
    }

    [Fact(DisplayName = "Gravação síncrona recusada")]
    public void SynchronousSave_IsRefused()
    {
        using var db = _database.NewContext<GammaDbContext>();
        db.Records.Add(new GammaRecord { Name = "Contrato" });

        var save = () => db.SaveChanges();

        save.Should().Throw<NotSupportedException>();
    }

    private async Task<GammaRecord> CreateAsync()
    {
        var record = new GammaRecord
        {
            Name = "Contrato",
            Quantity = 3,
            Status = GammaStatus.Open,
            Active = true,
            Due = Due,
            Password = "s3cret",
            LastSeenAt = new DateTime(2026, 10, 18, 9, 0, 0, DateTimeKind.Utc),
        };
        await using var db = _database.NewContext<GammaDbContext>();
        db.Records.Add(record);
        await db.SaveChangesAsync(Cancellation);
        return record;
    }

    private async Task ChangeAsync(Guid id, Action<GammaRecord> change)
    {
        await using var db = _database.NewContext<GammaDbContext>();
        change(await db.Records.SingleAsync(r => r.Id == id, Cancellation));
        await db.SaveChangesAsync(Cancellation);
    }

    private async Task<List<string>> JournalAsync()
    {
        await using var db = _database.NewContext<BetaDbContext>();
        return await db.Records.Select(r => r.Title).ToListAsync(Cancellation);
    }

    /// <summary>Stands in for the Audit module: writes a line per change in the beta table, in the saving transaction.</summary>
    private sealed class JournalAuditor : IChangeAuditor
    {
        public List<AuditedChange> Recorded { get; } = [];

        public bool Fail { get; set; }

        public async Task RecordAsync(DbContext context, IReadOnlyList<AuditedChange> changes, CancellationToken cancellationToken)
        {
            if (Fail)
            {
                throw new InvalidOperationException("Audit unavailable.");
            }

            var options = new DbContextOptionsBuilder<BetaDbContext>().UseSqlServer(context.Database.GetDbConnection()).Options;
            await using var journal = new BetaDbContext(options);
            await journal.Database.UseTransactionAsync(context.Database.CurrentTransaction!.GetDbTransaction(), cancellationToken);
            journal.Records.AddRange(changes.Select(change =>
                new BetaRecord { Title = $"{change.Action} {change.ItemType} {change.ItemLabel}" }));
            await journal.SaveChangesAsync(cancellationToken);
            Recorded.AddRange(changes);
        }
    }
}
