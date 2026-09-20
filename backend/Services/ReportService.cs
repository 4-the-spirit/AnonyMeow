using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class ReportService(AppDbContext dbContext, IModerationActionService moderationActionService) : IReportService
{
    public async Task<Report> CreateAsync(
        ReportTargetType targetType, Guid targetId, Guid reporterId, ReportReasonCategory category, string? details,
        CancellationToken cancellationToken = default)
    {
        var report = new Report
        {
            Id = Guid.NewGuid(),
            ReporterId = reporterId,
            TargetType = targetType,
            TargetId = targetId,
            Category = category,
            Reason = details,
            Status = ReportStatus.Open,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);
        return report;
    }

    public async Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Reports.FindAsync([id], cancellationToken);

    public async Task<Guid?> GetCommunityIdForReportAsync(Report report, CancellationToken cancellationToken = default)
    {
        if (report.TargetType == ReportTargetType.Post)
        {
            var post = await dbContext.Posts.FindAsync([report.TargetId], cancellationToken);
            return post?.CommunityId;
        }

        // DirectMessages aren't scoped to any community — they have no mod queue to route into
        // (platform-admin DM review is a later phase's concern, not built yet).
        if (report.TargetType == ReportTargetType.DirectMessage)
        {
            return null;
        }

        var comment = await dbContext.Comments.FindAsync([report.TargetId], cancellationToken);
        if (comment is null)
        {
            return null;
        }

        var parentPost = await dbContext.Posts.FindAsync([comment.PostId], cancellationToken);
        return parentPost?.CommunityId;
    }

    public async Task<(IReadOnlyList<Report> Items, int TotalCount)> ListForCommunityAsync(
        Guid communityId, ReportStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var postIds = await dbContext.Posts
            .Where(p => p.CommunityId == communityId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        var commentIds = await dbContext.Comments
            .Where(c => postIds.Contains(c.PostId))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var query = dbContext.Reports.Where(r =>
            (r.TargetType == ReportTargetType.Post && postIds.Contains(r.TargetId)) ||
            (r.TargetType == ReportTargetType.Comment && commentIds.Contains(r.TargetId)));

        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task ResolveAsync(
        Report report, Guid communityId, Guid modId, ReportOutcome outcome, string? actionReason,
        CancellationToken cancellationToken = default)
    {
        report.Status = outcome == ReportOutcome.ActionTaken ? ReportStatus.ActionTaken : ReportStatus.Dismissed;
        report.ReviewedByModId = modId;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (outcome == ReportOutcome.ActionTaken)
        {
            await moderationActionService.RecordReportResolutionAsync(communityId, modId, report.Id, actionReason, cancellationToken);
        }
    }
}
