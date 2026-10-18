using FiscoProjects.Audit.Contracts;
using FiscoProjects.Audit.Domain;

namespace FiscoProjects.Audit.Application;

/// <summary>Records the security events other modules report: sign-ins and refused sign-ins (ADR-0039).</summary>
public sealed class SecurityEventLog(IAuditStore store, TimeProvider time) : ISecurityEvents
{
    public Task SignedInAsync(Guid userId, string provider, CancellationToken cancellationToken) =>
        store.AddAsync(AuditEntry.SignedIn(Now(), userId, provider), cancellationToken);

    public Task SignInRefusedAsync(string identity, SignInRefusal reason, CancellationToken cancellationToken) =>
        store.AddAsync(AuditEntry.SignInRefused(Now(), identity, reason.ToString()), cancellationToken);

    private DateTime Now() => time.GetUtcNow().UtcDateTime;
}
