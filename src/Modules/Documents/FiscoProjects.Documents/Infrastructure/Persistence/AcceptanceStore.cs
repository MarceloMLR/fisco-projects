using FiscoProjects.Documents.Application;
using FiscoProjects.Documents.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Documents.Infrastructure.Persistence;

/// <summary>The acceptance terms, in the tables of the module, saved with the documents and the folders.</summary>
internal sealed class AcceptanceStore(DocumentsDbContext db) : IAcceptanceStore
{
    public async Task<IReadOnlyList<AcceptanceTerm>> ListAsync(Guid projectId, CancellationToken cancellationToken) =>
        await db.AcceptanceTerms.AsNoTracking()
            .Include(term => term.Signers)
            .Where(term => term.ProjectId == projectId)
            .OrderByDescending(term => term.RequestedAt)
            .ThenByDescending(term => term.Id)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public Task<AcceptanceTerm?> FindAsync(Guid projectId, Guid id, bool withEvents, CancellationToken cancellationToken)
    {
        IQueryable<AcceptanceTerm> terms = db.AcceptanceTerms.Include(term => term.Signers);
        if (withEvents)
        {
            terms = terms.Include(term => term.Events);
        }

        return terms.AsSplitQuery().SingleOrDefaultAsync(term => term.Id == id && term.ProjectId == projectId, cancellationToken);
    }

    public Task<AcceptanceTerm?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.AcceptanceTerms.Include(term => term.Signers).SingleOrDefaultAsync(term => term.Id == id, cancellationToken);

    public async Task<Guid?> TermOfDocumentAsync(string provider, string documentId, CancellationToken cancellationToken) =>
        await db.AcceptanceTerms.AsNoTracking()
            .Where(term => term.SignatureProvider == provider && term.ProviderDocumentId == documentId)
            .Select(term => (Guid?)term.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<(Guid Id, Guid ProjectId)>> OpenRequestsAsync(string provider, CancellationToken cancellationToken) =>
        [
            .. (await db.AcceptanceTerms.AsNoTracking()
                .Where(term => term.Status == AcceptanceStatus.Pending && term.SignatureProvider == provider)
                .OrderBy(term => term.RequestedAt)
                .Select(term => new { term.Id, term.ProjectId })
                .ToListAsync(cancellationToken))
            .Select(term => (term.Id, term.ProjectId)),
        ];

    public Task<int> PendingCountAsync(Guid projectId, CancellationToken cancellationToken) =>
        db.AcceptanceTerms.CountAsync(term => term.ProjectId == projectId && term.Status == AcceptanceStatus.Pending, cancellationToken);

    public async Task<IReadOnlyList<AcceptanceTerm>> PendingOfAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.AcceptanceTerms.AsNoTracking()
            .Include(term => term.Signers)
            .Where(term => term.Status == AcceptanceStatus.Pending && term.RequestedById == userId)
            .OrderBy(term => term.RequestedAt)
            .AsSplitQuery()
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
