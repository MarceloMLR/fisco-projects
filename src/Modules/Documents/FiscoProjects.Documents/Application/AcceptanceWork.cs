using FiscoProjects.Documents.Domain;
using FiscoProjects.MyWork.Contracts;
using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.Documents.Application;

/// <summary>
/// The requests of acceptance of the person in Meu trabalho (ADR-0070, ADR-0096, ADR-0129), in the open projects the
/// person sees: the pending ones the person made while managing the terms of the project, due on the day of the
/// milestone. No request waits for the person: the signers sign at the provider.
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

            if (!AcceptanceAccess.Manages(viewer, project))
            {
                continue;
            }

            var milestone = milestones[term.ProjectId].GetValueOrDefault(term.MilestoneId);
            items.Add(new MyWorkItem(
                MyWorkKind.Acceptance, term.Id, project.Id, project.Code, term.Title, MyWorkViews.ForMe | MyWorkViews.OpenedByMe)
            {
                DueOn = milestone?.Day,
                Acceptance = new MyWorkAcceptance(
                    term.RecipientId,
                    [.. term.Signers.OrderBy(signer => signer.Position).Select(signer => signer.Name)],
                    term.RequestedById,
                    term.RequestedAt,
                    milestone?.Name),
            });
        }

        return items;
    }
}
