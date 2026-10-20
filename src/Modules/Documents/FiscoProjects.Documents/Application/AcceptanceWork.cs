using FiscoProjects.Documents.Domain;
using FiscoProjects.MyWork.Contracts;
using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.Documents.Application;

/// <summary>
/// The requests of acceptance of the person in Meu trabalho (ADR-0070, ADR-0096), in the open projects the person sees:
/// the pending ones for the person, waiting for the answer since they were asked; and the pending ones the person made
/// while managing the terms of the project (ADR-0045), due on the day of the milestone.
/// </summary>
public sealed class AcceptanceWorkSource(IAcceptanceStore acceptances, IProjectAccess projects, IScheduleItems schedules)
    : IMyWorkSource
{
    public async Task<IReadOnlyList<MyWorkItem>> ItemsAsync(MyWorkQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var terms = await acceptances.PendingOfAsync(query.UserId, cancellationToken);
        if (terms.Count == 0)
        {
            return [];
        }

        var viewer = new DocumentViewer(query.UserId, query.IsAdministrator);
        var open = (await projects.VisibleAsync(query.UserId, query.IsAdministrator, cancellationToken))
            .Where(project => !project.IsClosed)
            .ToDictionary(project => project.Id);
        var milestones = new Dictionary<Guid, Dictionary<Guid, ScheduleItemInfo>>();
        foreach (var projectId in terms.Select(term => term.ProjectId).Distinct().Where(open.ContainsKey))
        {
            milestones[projectId] = (await schedules.ItemsAsync(projectId, cancellationToken))
                .Where(item => item.Type == ScheduleItemType.Milestone)
                .ToDictionary(item => item.Id);
        }

        var items = new List<MyWorkItem>();
        foreach (var term in terms)
        {
            if (!open.TryGetValue(term.ProjectId, out var project))
            {
                continue;
            }

            var forYou = term.RecipientId == query.UserId;
            if (!forYou && !AcceptanceAccess.Manages(viewer, project))
            {
                continue;
            }

            var milestone = milestones[term.ProjectId].GetValueOrDefault(term.MilestoneId);
            items.Add(new MyWorkItem(
                MyWorkKind.Acceptance,
                term.Id,
                project.Id,
                project.Code,
                term.Title,
                forYou ? MyWorkViews.ForMe : MyWorkViews.ForMe | MyWorkViews.OpenedByMe)
            {
                DueOn = milestone?.Day,
                WaitsForYou = forYou,
                WaitingSince = forYou ? term.RequestedAt : null,
                Acceptance = new MyWorkAcceptance(term.RecipientId, term.RequestedById, term.RequestedAt, milestone?.Name),
            });
        }

        return items;
    }
}
