using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

/// <summary>The absences of the people, in the table of the module (ADR-0141).</summary>
internal sealed class AbsenceStore(ProjectsDbContext db) : IAbsenceStore
{
    public async Task<IReadOnlyList<PersonAbsence>> OfPersonAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.PersonAbsences.AsNoTracking()
            .Where(absence => absence.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PersonAbsence>> EndingFromAsync(
        IReadOnlyCollection<Guid> userIds, DateOnly day, CancellationToken cancellationToken) =>
        userIds.Count == 0
            ? []
            : await db.PersonAbsences.AsNoTracking()
                .Where(absence => userIds.Contains(absence.UserId) && absence.End >= day)
                .ToListAsync(cancellationToken);

    public Task<PersonAbsence?> FindAsync(Guid userId, Guid absenceId, CancellationToken cancellationToken) =>
        db.PersonAbsences.SingleOrDefaultAsync(absence => absence.Id == absenceId && absence.UserId == userId, cancellationToken);

    public Task<int> RemoveEndedBeforeAsync(DateOnly day, CancellationToken cancellationToken) =>
        db.PersonAbsences.Where(absence => absence.End < day).ExecuteDeleteAsync(cancellationToken);

    public void Add(PersonAbsence absence) => db.PersonAbsences.Add(absence);

    public void Remove(PersonAbsence absence) => db.PersonAbsences.Remove(absence);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

// The absences (ADR-0141): in the audit by the name of the person and of who registered them (ADR-0084); the dates are
// personal data kept for a year only, so the audit, kept for years (ADR-0058), says only that they changed.
internal sealed class PersonAbsenceConfiguration : IEntityTypeConfiguration<PersonAbsence>
{
    public void Configure(EntityTypeBuilder<PersonAbsence> absence)
    {
        absence.ToTable("person_absences");
        absence.HasIndex(a => a.UserId);
        absence.HasIndex(a => a.End);
        absence.Ignore(a => a.Period);

        absence.HasAuditLabelReference(a => a.UserId, UserAudit.Reference);
        absence.Property(a => a.UserId).IsAuditReference(UserAudit.Reference);
        absence.Property(a => a.Start).IsAuditSecret();
        absence.Property(a => a.End).IsAuditSecret();
        absence.Property(a => a.CreatedBy).IsAuditReference(UserAudit.Reference);
        absence.Property(a => a.CreatedAt).ExcludeFromAudit();
    }
}
