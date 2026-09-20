using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services.ContentSubmission;
using AnonyMeow.Services.Ranking;
using AnonyMeow.Services.SpamDetection;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class CommentService(
    AppDbContext dbContext,
    IRankingService rankingService,
    INotificationDispatcher notificationDispatcher,
    IMentionParsingService mentionParsingService,
    IContentSubmissionPipeline contentSubmissionPipeline,
    ISpamFlaggingService spamFlaggingService) : ICommentService
{
    private const int PreviewLength = 140;

    public async Task<Comment> CreateAsync(
        Guid postId, Guid authorId, string bodyMarkdown, Guid? parentCommentId, CancellationToken cancellationToken = default)
    {
        var post = await dbContext.Posts.FindAsync([postId], cancellationToken) ?? throw new PostNotFoundException();
        if (post.IsLocked)
        {
            throw new PostLockedException();
        }

        // Retrofit from 1.6: a community-banned author cannot create new comments. Checked
        // before any write.
        var isBanned = await dbContext.CommunityBans.AnyAsync(
            b => b.CommunityId == post.CommunityId && b.AppUserId == authorId, cancellationToken);
        if (isBanned)
        {
            throw new CommunityBannedException();
        }

        // Retrofit from Phase 9: a platform-restricted author cannot comment anywhere. See
        // PostService.CreateAsync for the identical check and its expiry semantics.
        var isPlatformRestricted = await dbContext.PlatformRestrictions.AnyAsync(
            r => r.AppUserId == authorId && r.Status == PlatformRestrictionStatus.Active &&
                 (r.EndAtUtc == null || r.EndAtUtc > DateTimeOffset.UtcNow), cancellationToken);
        if (isPlatformRestricted)
        {
            throw new PlatformRestrictedException();
        }

        // Retrofit from Phase 6: comment body is scanned for PII before any write.
        var evaluation = await contentSubmissionPipeline.EvaluateAsync(
            new ContentSubmissionRequest(ContentSubmissionType.Comment, authorId, bodyMarkdown), cancellationToken);
        if (evaluation.IsBlocked)
        {
            throw new PiiDetectedException(evaluation.DetectedCategories);
        }

        Comment? parentComment = null;
        if (parentCommentId is not null)
        {
            parentComment = await dbContext.Comments.SingleOrDefaultAsync(
                c => c.Id == parentCommentId && c.PostId == postId, cancellationToken);
            if (parentComment is null)
            {
                throw new InvalidParentCommentException();
            }
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            ParentCommentId = parentCommentId,
            AuthorId = authorId,
            BodyMarkdown = bodyMarkdown,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Post-save, same reasoning as PostService: spam heuristics need the comment's id, and a
        // flagged comment still gets published, just flagged for review.
        await spamFlaggingService.FlagIfSpamAsync(
            SpamFlagTargetType.Comment, comment.Id, authorId, ContentSubmissionType.Comment, bodyMarkdown, cancellationToken);

        var preview = bodyMarkdown.Length > PreviewLength ? bodyMarkdown[..PreviewLength] : bodyMarkdown;

        // Reply notification: to the parent comment's author, or the post author for a
        // top-level comment. Skipped if you're replying to your own comment/post.
        var replyRecipientId = parentComment?.AuthorId ?? post.AuthorId;
        if (replyRecipientId != authorId)
        {
            await notificationDispatcher.DispatchAsync(
                replyRecipientId, NotificationType.Reply, NotificationSourceType.Comment, comment.Id, preview, cancellationToken);
        }

        // Mention notifications: independent of the reply notification above — the same person
        // can legitimately receive both for the same comment, so they're not deduped.
        var mentionedUsers = await mentionParsingService.ExtractMentionedUsersAsync(bodyMarkdown, cancellationToken);
        foreach (var mentionedUser in mentionedUsers)
        {
            if (mentionedUser.Id == authorId)
            {
                continue;
            }

            await notificationDispatcher.DispatchAsync(
                mentionedUser.Id, NotificationType.Mention, NotificationSourceType.Comment, comment.Id, preview, cancellationToken);
        }

        return comment;
    }

    public async Task<Comment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Comments.FindAsync([id], cancellationToken);

    public async Task<(IReadOnlyList<Comment> Items, int TotalCount)> ListTopLevelAsync(
        Guid postId, SortOrder sortOrder, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Comments.Where(c => c.PostId == postId && c.ParentCommentId == null && !c.IsRemoved);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await rankingService.ApplyCommentSort(query, sortOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Comment> Items, int TotalCount)> ListRepliesAsync(
        Guid parentCommentId, SortOrder sortOrder, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Comments.Where(c => c.ParentCommentId == parentCommentId && !c.IsRemoved);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await rankingService.ApplyCommentSort(query, sortOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Comment> Items, int TotalCount)> ListByAuthorUsernameAsync(
        string username, Guid? viewerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalized = username.ToLowerInvariant();
        var author = await dbContext.Users.SingleOrDefaultAsync(
            u => u.Username != null && u.Username.ToLower() == normalized, cancellationToken);
        if (author is null)
        {
            return (Array.Empty<Comment>(), 0);
        }

        var query = dbContext.Comments.Where(c => c.AuthorId == author.Id);
        if (author.Id != viewerId)
        {
            query = query.Where(c => !c.IsRemoved);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private const int MaxAncestorChainDepth = 20;

    public async Task<IReadOnlyList<Guid>> GetAncestorChainAsync(
        Guid commentId, CancellationToken cancellationToken = default)
    {
        var chain = new List<Guid>();
        var currentId = commentId;
        for (var i = 0; i < MaxAncestorChainDepth; i++)
        {
            var parentId = await dbContext.Comments
                .Where(c => c.Id == currentId)
                .Select(c => c.ParentCommentId)
                .SingleOrDefaultAsync(cancellationToken);
            if (parentId is null)
            {
                break;
            }

            chain.Add(parentId.Value);
            currentId = parentId.Value;
        }

        chain.Reverse();
        return chain;
    }

    public Task<int> GetReplyCountAsync(Guid commentId, CancellationToken cancellationToken = default) =>
        dbContext.Comments.CountAsync(c => c.ParentCommentId == commentId && !c.IsRemoved, cancellationToken);

    public Task<int> GetCommentCountAsync(Guid postId, CancellationToken cancellationToken = default) =>
        dbContext.Comments.CountAsync(c => c.PostId == postId && !c.IsRemoved, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> GetReplyCountsAsync(
        IReadOnlyCollection<Guid> commentIds, CancellationToken cancellationToken = default) =>
        await dbContext.Comments
            .Where(c => c.ParentCommentId != null && commentIds.Contains(c.ParentCommentId.Value) && !c.IsRemoved)
            .GroupBy(c => c.ParentCommentId!.Value)
            .Select(g => new { ParentCommentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ParentCommentId, x => x.Count, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> GetCommentCountsAsync(
        IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken = default) =>
        await dbContext.Comments
            .Where(c => postIds.Contains(c.PostId) && !c.IsRemoved)
            .GroupBy(c => c.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count, cancellationToken);

    // Walks all requested comments' ParentCommentId up towards their roots in lockstep, one query
    // per tree "level" shared across every id, instead of GetAncestorChainAsync's one query per
    // level per comment. Bounded by MaxAncestorChainDepth either way.
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetAncestorChainsAsync(
        IReadOnlyCollection<Guid> commentIds, CancellationToken cancellationToken = default)
    {
        var chains = commentIds.ToDictionary(id => id, _ => new List<Guid>());
        var frontier = commentIds.ToDictionary(id => id, id => id);

        for (var i = 0; i < MaxAncestorChainDepth && frontier.Count > 0; i++)
        {
            var currentIds = frontier.Values.Distinct().ToList();
            var parentsById = await dbContext.Comments
                .Where(c => currentIds.Contains(c.Id))
                .Select(c => new { c.Id, c.ParentCommentId })
                .ToDictionaryAsync(x => x.Id, x => x.ParentCommentId, cancellationToken);

            var nextFrontier = new Dictionary<Guid, Guid>();
            foreach (var (originalId, currentId) in frontier)
            {
                if (parentsById.TryGetValue(currentId, out var parentId) && parentId is not null)
                {
                    chains[originalId].Add(parentId.Value);
                    nextFrontier[originalId] = parentId.Value;
                }
            }

            frontier = nextFrontier;
        }

        foreach (var chain in chains.Values)
        {
            chain.Reverse();
        }

        return chains.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<Guid>)kv.Value);
    }

    public async Task UpdateAsync(Comment comment, string bodyMarkdown, CancellationToken cancellationToken = default)
    {
        comment.BodyMarkdown = bodyMarkdown;
        comment.EditedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        comment.IsRemoved = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
