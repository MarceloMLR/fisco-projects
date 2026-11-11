using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using FiscoProjects.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

/// <summary>The comments of the schedules, in the table of the module (ADR-0157).</summary>
internal sealed class ScheduleCommentStore(ProjectsDbContext db) : IScheduleCommentStore
{
    public Task<CommentedItem?> ItemAsync(Guid projectId, Guid itemId, CancellationToken cancellationToken) =>
        db.ScheduleItems.AsNoTracking()
            .Where(item => item.ProjectId == projectId && item.Id == itemId)
            .Select(item => new CommentedItem(item.Id, item.Name, item.Kind, item.OwnerId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ScheduleComment>> OfItemAsync(Guid projectId, Guid itemId, CancellationToken cancellationToken)
    {
        var comments = await db.ScheduleComments.AsNoTracking()
            .Where(comment => comment.ProjectId == projectId && comment.ItemId == itemId)
            .ToListAsync(cancellationToken);
        // By the moment, then by the id, a version 7 GUID in the order it was made: sorted here, because SQL Server
        // sorts the GUIDs by other bytes first.
        return [.. comments.OrderBy(comment => comment.SentAt).ThenBy(comment => comment.Id)];
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountsAsync(Guid projectId, CancellationToken cancellationToken) =>
        await db.ScheduleComments.AsNoTracking()
            .Where(comment => comment.ProjectId == projectId)
            .GroupBy(comment => comment.ItemId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);

    public Task<ScheduleComment?> FindAsync(Guid projectId, Guid itemId, Guid commentId, CancellationToken cancellationToken) =>
        db.ScheduleComments.SingleOrDefaultAsync(
            comment => comment.Id == commentId && comment.ProjectId == projectId && comment.ItemId == itemId, cancellationToken);

    public void Add(ScheduleComment comment) => db.ScheduleComments.Add(comment);

    public void Remove(ScheduleComment comment) => db.ScheduleComments.Remove(comment);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

// The comments are a conversation and never change: they stay out of the audit, as the messages of the tickets, which
// would get a record per comment (ADR-0039, ADR-0157). No key to the item: a removed item comes back with the same id.
internal sealed class ScheduleCommentConfiguration : IEntityTypeConfiguration<ScheduleComment>
{
    public void Configure(EntityTypeBuilder<ScheduleComment> comment)
    {
        comment.ToTable("schedule_comments");
        comment.Property(c => c.Body).HasMaxLength(RichText.MaxJsonLength);
        comment.Property(c => c.Text).HasMaxLength(RichText.MaxTextLength);
        comment.HasOne<Project>().WithMany().HasForeignKey(c => c.ProjectId).OnDelete(DeleteBehavior.Cascade);
        comment.HasIndex(c => new { c.ProjectId, c.ItemId, c.SentAt });
        comment.ExcludeFromAudit();
    }
}
