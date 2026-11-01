using FiscoProjects.Persistence.PersonalData;
using FiscoProjects.Projects.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FiscoProjects.Projects.Infrastructure;

/// <summary>
/// Erases an anonymized person from the projects (ADR-0110), in the transaction of the anonymization: the absences of the
/// person go (ADR-0141). The rest of a project points to the person by the id.
/// </summary>
internal sealed class ProjectsPersonalData : IPersonalDataEraser
{
    public async Task EraseAsync(DbContext context, PersonErasure erasure, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(erasure);
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("The erasure runs in the transaction of the anonymization.");
        var options = new DbContextOptionsBuilder<ProjectsDbContext>()
            .UseSqlServer(context.Database.GetDbConnection())
            .Options;
        await using var projects = new ProjectsDbContext(options);
        await projects.Database.UseTransactionAsync(transaction, cancellationToken);
        await projects.PersonAbsences.Where(absence => absence.UserId == erasure.PersonId).ExecuteDeleteAsync(cancellationToken);
    }
}
