using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Documents.Infrastructure.Persistence;

/// <summary>The acceptance terms, in the tables of the module, saved with the documents and the folders.</summary>
internal sealed class AcceptanceStore(DocumentsDbContext db) : IAcceptanceStore
{
    public async Task<IReadOnlyList<AcceptanceTerm>> ListAsync(Guid projectId, Guid? recipientId, CancellationToken cancellationToken) =>
        await db.AcceptanceTerms.AsNoTracking()
            .Where(term => term.ProjectId == projectId && (recipientId == null || term.RecipientId == recipientId))
            .OrderByDescending(term => term.RequestedAt)
            .ThenByDescending(term => term.Id)
            .ToListAsync(cancellationToken);

    public Task<AcceptanceTerm?> FindAsync(Guid projectId, Guid id, bool withEvents, CancellationToken cancellationToken)
    {
        IQueryable<AcceptanceTerm> terms = withEvents ? db.AcceptanceTerms.Include(term => term.Events) : db.AcceptanceTerms;
        return terms.SingleOrDefaultAsync(term => term.Id == id && term.ProjectId == projectId, cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> SignedDocumentsOfAsync(Guid projectId, Guid recipientId, CancellationToken cancellationToken) =>
        (await db.AcceptanceTerms.AsNoTracking()
            .Where(term => term.ProjectId == projectId && term.RecipientId == recipientId && term.DocumentId != null)
            .Select(term => term.DocumentId!.Value)
            .ToListAsync(cancellationToken))
        .ToHashSet();

    public Task<int> PendingCountAsync(Guid projectId, CancellationToken cancellationToken) =>
        db.AcceptanceTerms.CountAsync(term => term.ProjectId == projectId && term.Status == AcceptanceStatus.Pending, cancellationToken);

    public async Task<IReadOnlyList<AcceptanceTerm>> PendingAsync(CancellationToken cancellationToken) =>
        await db.AcceptanceTerms.AsNoTracking()
            .Where(term => term.Status == AcceptanceStatus.Pending)
            .OrderBy(term => term.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AcceptanceTerm>> PendingOfAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.AcceptanceTerms.AsNoTracking()
            .Where(term => term.Status == AcceptanceStatus.Pending && (term.RecipientId == userId || term.RequestedById == userId))
            .OrderBy(term => term.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<Guid>> PendingMilestonesAsync(
        IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken) =>
        (await db.AcceptanceTerms.AsNoTracking()
            .Where(term => term.Status == AcceptanceStatus.Pending && projectIds.Contains(term.ProjectId))
            .Select(term => term.MilestoneId)
            .Distinct()
            .ToListAsync(cancellationToken))
        .ToHashSet();

    public void Add(AcceptanceTerm term) => db.AcceptanceTerms.Add(term);

    public void Remove(AcceptanceTerm term) => db.AcceptanceTerms.Remove(term);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new AcceptanceChangedException(exception);
        }
    }
}
