using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Notifications.Infrastructure.Persistence;

internal sealed class OutboxStore(NotificationsDbContext db) : IOutboxStore
{
    public void Add(OutboxEmail email) => db.OutboxEmails.Add(email);

    public async Task<IReadOnlyList<OutboxEmail>> DueAsync(DateTime now, int take, CancellationToken cancellationToken) =>
        await db.OutboxEmails
            .Where(email => email.Status == OutboxEmailStatus.Pending && email.NextAttemptAt <= now)
            .OrderBy(email => email.NextAttemptAt).ThenBy(email => email.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OutboxEmail>> ToForgetAsync(DateTime before, int take, CancellationToken cancellationToken) =>
        await db.OutboxEmails
            .Where(email => email.Status != OutboxEmailStatus.Pending && email.CreatedAt < before && email.ForgottenAt == null)
            .OrderBy(email => email.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<OutboxEmail?> LastOfKindAsync(string kind, CancellationToken cancellationToken) =>
        db.OutboxEmails.AsNoTracking()
            .Where(email => email.Kind == kind)
            .OrderByDescending(email => email.CreatedAt).ThenByDescending(email => email.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public async Task<bool> TrySaveAsync(OutboxEmail email, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else changed it: forget the local change, so the next saves do not try it again.
            db.Entry(email).State = EntityState.Detached;
            return false;
        }
    }
}
