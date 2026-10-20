using FiscoProjects.Projects.Contracts;
using FiscoProjects.Projects.Domain;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

/// <summary>Who is in each project, read from the tables of the module for the other modules (ADR-0047).</summary>
internal sealed class ProjectAccessDirectory(ProjectsDbContext db) : IProjectAccess
{
    public async Task<IReadOnlyList<ProjectAccess>> VisibleAsync(
        Guid userId, bool isAdministrator, CancellationToken cancellationToken)
    {
        var projects = db.Projects.AsNoTracking();
        if (!isAdministrator)
        {
            projects = projects.Where(project => project.Members.Any(member => member.UserId == userId));
        }

        var rows = await projects
            .OrderBy(project => project.Code)
            .Select(project => new
            {
                project.Id,
                project.Code,
                project.Name,
                project.Status,
                Role = project.Members.Where(member => member.UserId == userId).Select(member => (ProjectRole?)member.Role).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);
        return [.. rows.Select(row => new ProjectAccess(row.Id, row.Code, row.Name, Project.IsClosedStatus(row.Status), RoleOf(row.Role)))];
    }

    public async Task<ProjectAccess?> FindAsync(
        Guid projectId, Guid userId, bool isAdministrator, CancellationToken cancellationToken)
    {
        var row = await db.Projects.AsNoTracking()
            .Where(project => project.Id == projectId)
            .Select(project => new
            {
                project.Id,
                project.Code,
                project.Name,
                project.Status,
                Role = project.Members.Where(member => member.UserId == userId).Select(member => (ProjectRole?)member.Role).FirstOrDefault(),
            })
            .SingleOrDefaultAsync(cancellationToken);
        return row is null || (row.Role is null && !isAdministrator)
            ? null
            : new ProjectAccess(row.Id, row.Code, row.Name, Project.IsClosedStatus(row.Status), RoleOf(row.Role));
    }

    public async Task<IReadOnlyList<ProjectMemberInfo>> MembersAsync(
        IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken) =>
        [.. (await db.ProjectMembers.AsNoTracking()
                .Where(member => projectIds.Contains(member.ProjectId))
                .Select(member => new { member.ProjectId, member.UserId, member.Role })
                .ToListAsync(cancellationToken))
            .Select(member => new ProjectMemberInfo(member.ProjectId, member.UserId, (ProjectMemberRole)member.Role))];

    private static ProjectMemberRole? RoleOf(ProjectRole? role) => role is { } value ? (ProjectMemberRole)value : null;
}
