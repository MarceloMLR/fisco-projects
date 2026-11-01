using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

/// <summary>The work of the people across the open projects, read from the tables of the module for the load of the team (ADR-0142).</summary>
internal sealed class TeamWorkDirectory(ProjectsDbContext db) : ITeamWork
{
    public async Task<IReadOnlyList<OwnedTask>> TasksAsync(DateOnly first, DateOnly last, CancellationToken cancellationToken)
    {
        var rows = await (
            from item in db.ScheduleItems.AsNoTracking()
            join project in db.Projects.AsNoTracking() on item.ProjectId equals project.Id
            where item.Kind == ScheduleItemKind.Task
                && item.OwnerId != null
                && item.Progress < 100
                && item.Start <= last
                && item.End >= first
                && project.Status != ProjectStatus.Completed
                && project.Status != ProjectStatus.Cancelled
            orderby project.Code, item.Start, item.Name
            select new
            {
                OwnerId = item.OwnerId!.Value,
                ProjectId = project.Id,
                project.Code,
                ProjectName = project.Name,
                ItemId = item.Id,
                ItemName = item.Name,
                item.Start,
                item.End,
            })
            .ToListAsync(cancellationToken);
        return [.. rows.Select(row => new OwnedTask(
            row.OwnerId, row.ProjectId, row.Code, row.ProjectName, row.ItemId, row.ItemName, row.Start, row.End))];
    }

    public async Task<IReadOnlyList<AbsenceInfo>> AbsencesAsync(DateOnly first, DateOnly last, CancellationToken cancellationToken)
    {
        var rows = await db.PersonAbsences.AsNoTracking()
            .Where(absence => absence.Start <= last && absence.End >= first)
            .OrderBy(absence => absence.Start)
            .Select(absence => new { absence.UserId, absence.Start, absence.End })
            .ToListAsync(cancellationToken);
        return [.. rows.Select(row => new AbsenceInfo(row.UserId, row.Start, row.End))];
    }

    public async Task<IReadOnlyList<Guid>> PeopleAsync(CancellationToken cancellationToken) =>
        await (
            from member in db.ProjectMembers.AsNoTracking()
            join project in db.Projects.AsNoTracking() on member.ProjectId equals project.Id
            where project.Status != ProjectStatus.Completed && project.Status != ProjectStatus.Cancelled
            select member.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
}
