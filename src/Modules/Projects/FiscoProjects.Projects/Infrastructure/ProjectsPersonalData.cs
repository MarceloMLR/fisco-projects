using FiscoProjects.Persistence.PersonalData;
using FiscoProjects.Projects.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FiscoProjects.Projects.Infrastructure;

/// <summary>
/// Erases an anonymized person from the projects (ADR-0110), in the transaction of the anonymization: the absences of the
/// person go (ADR-0141), and the mentions of the person in the comments name the identifier (ADR-0157). The rest of a
/// project points to the person by the id.
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

        // The mentions of the person in the comments name the identifier (ADR-0157); the documents keep the id as text.
        var mentioned = $"%{erasure.PersonId}%";
        var comments = await projects.ScheduleComments
            .Where(comment => EF.Functions.Like(comment.Body, mentioned))
            .ToListAsync(cancellationToken);
        foreach (var comment in comments)
        {
            comment.RenameMention(erasure.PersonId, erasure.Replacement);
        }

        await projects.SaveChangesAsync(cancellationToken);
    }
}
