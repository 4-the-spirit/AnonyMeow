using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Services.ContentSubmission;
using AnonyMeow.Services.Ranking;
using AnonyMeow.Services.SpamDetection;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class PostService(
    AppDbContext dbContext,
    IRankingService rankingService,
    IContentSubmissionPipeline contentSubmissionPipeline,
    ISpamFlaggingService spamFlaggingService,
    IImageUploadService imageUploadService,
    IFlairService flairService) : IPostService
{
    public async Task<Post> CreateAsync(
        Guid communityId, Guid authorId, CreatePostRequest request, CancellationToken cancellationToken = default)
    {
        // A tag is mandatory on every new post — verified to exist and belong to this community
        // before any write, same as the image-ownership check below.
        await flairService.EnsureValidForCommunityAsync(request.FlairId, communityId, cancellationToken);

        // Retrofit from 1.6: a community-banned author cannot create new posts. Checked before
        // any write.
        var isBanned = await dbContext.CommunityBans.AnyAsync(
            b => b.CommunityId == communityId && b.AppUserId == authorId, cancellationToken);
        if (isBanned)
        {
            throw new CommunityBannedException();
        }

        // Retrofit from Phase 9: a platform-restricted author cannot post anywhere, not just the
        // one community a CommunityBan scopes to. An expired-but-not-yet-lifted restriction
        // (EndAtUtc in the past) no longer blocks — only Status flips on an explicit admin lift.
        var isPlatformRestricted = await dbContext.PlatformRestrictions.AnyAsync(
            r => r.AppUserId == authorId && r.Status == PlatformRestrictionStatus.Active &&
                 (r.EndAtUtc == null || r.EndAtUtc > DateTimeOffset.UtcNow), cancellationToken);
        if (isPlatformRestricted)
        {
            throw new PlatformRestrictedException();
        }

        // Retrofit from Phase 6: title + body are scanned for PII before any write. BodyMarkdown
        // may be null when the post's content is carried by its images/link/poll instead.
        var submittedText = request.BodyMarkdown is null ? request.Title : $"{request.Title}\n{request.BodyMarkdown}";
        var evaluation = await contentSubmissionPipeline.EvaluateAsync(
            new ContentSubmissionRequest(ContentSubmissionType.Post, authorId, submittedText), cancellationToken);
        if (evaluation.IsBlocked)
        {
            throw new PiiDetectedException(evaluation.DetectedCategories);
        }

        // A write-only upload SAS can't cap content-length up front, so ownership/size/content-type
        // are enforced here — after upload, before the post (and its images) are persisted.
        if (request.ImageUrls is { Count: > 0 } imageUrls)
        {
            foreach (var url in imageUrls)
            {
                await imageUploadService.ValidateImageAsync(url, authorId, cancellationToken);
            }
        }

        var post = new Post
        {
            Id = Guid.NewGuid(),
            CommunityId = communityId,
            AuthorId = authorId,
            Title = request.Title,
            BodyMarkdown = request.BodyMarkdown,
            Url = request.Url,
            FlairId = request.FlairId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Posts.Add(post);

        if (request.ImageUrls is { Count: > 0 })
        {
            for (var i = 0; i < request.ImageUrls.Count; i++)
            {
                dbContext.PostImages.Add(new PostImage { Id = Guid.NewGuid(), PostId = post.Id, Url = request.ImageUrls[i], Position = i });
            }
        }

        if (request.PollOptions is not null)
        {
            foreach (var optionText in request.PollOptions.Where(o => !string.IsNullOrWhiteSpace(o)))
            {
                dbContext.PollOptions.Add(new PollOption { Id = Guid.NewGuid(), PostId = post.Id, Text = optionText });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Post-save, unlike the PII check above: spam heuristics need the post's id to file a
        // Report against it, and a flagged post still gets published (it's flagged for review,
        // not blocked).
        await spamFlaggingService.FlagIfSpamAsync(
            SpamFlagTargetType.Post, post.Id, authorId, ContentSubmissionType.Post, submittedText, cancellationToken);

        return post;
    }

    public async Task<Post?> GetByIdAsync(Guid id, Guid? viewerId, CancellationToken cancellationToken = default)
    {
        var post = await dbContext.Posts.FindAsync([id], cancellationToken);
        if (post is null || !post.IsRemoved)
        {
            return post;
        }

        if (viewerId is null)
        {
            return null;
        }

        if (post.AuthorId == viewerId)
        {
            return post;
        }

        var isModerator = await dbContext.CommunityMemberships.AnyAsync(
            m => m.CommunityId == post.CommunityId && m.AppUserId == viewerId && m.Role == CommunityRole.Moderator,
            cancellationToken);

        return isModerator ? post : null;
    }

    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> ListByCommunityAsync(
        Guid communityId, int page, int pageSize, SortOrder sortOrder, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Posts.Where(p => p.CommunityId == communityId && !p.IsRemoved);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await rankingService.ApplyPostSort(query, sortOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> ListByAuthorUsernameAsync(
        string username, Guid? viewerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalized = username.ToLowerInvariant();
        var author = await dbContext.Users.SingleOrDefaultAsync(
            u => u.Username != null && u.Username.ToLower() == normalized, cancellationToken);
        if (author is null)
        {
            return (Array.Empty<Post>(), 0);
        }

        var query = dbContext.Posts.Where(p => p.AuthorId == author.Id);
        if (author.Id != viewerId)
        {
            query = query.Where(p => !p.IsRemoved);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task UpdateAsync(
        Post post, string? title, string? bodyMarkdown, string? url, CancellationToken cancellationToken = default)
    {
        if (title is not null)
        {
            post.Title = title;
        }

        if (bodyMarkdown is not null)
        {
            post.BodyMarkdown = bodyMarkdown;
        }

        if (url is not null)
        {
            post.Url = url;
        }

        post.EditedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Post post, CancellationToken cancellationToken = default)
    {
        post.IsRemoved = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<PollOptionResponse>> GetPollOptionsAsync(
        Guid postId, CancellationToken cancellationToken = default) =>
        MapPollOptionsAsync(postId, cancellationToken);

    public async Task<IReadOnlyList<string>> GetImageUrlsAsync(
        Guid postId, CancellationToken cancellationToken = default) =>
        await dbContext.PostImages
            .Where(i => i.PostId == postId)
            .OrderBy(i => i.Position)
            .Select(i => i.Url)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PollOptionResponse>> CastPollVoteAsync(
        Guid postId, Guid voterId, Guid pollOptionId, CancellationToken cancellationToken = default)
    {
        var optionBelongsToPost = await dbContext.PollOptions.AnyAsync(
            o => o.Id == pollOptionId && o.PostId == postId, cancellationToken);
        if (!optionBelongsToPost)
        {
            throw new PollOptionNotFoundException();
        }

        var existingVote = await dbContext.PollVotes.SingleOrDefaultAsync(
            v => v.PostId == postId && v.AppUserId == voterId, cancellationToken);

        if (existingVote is null)
        {
            dbContext.PollVotes.Add(new PollVote
            {
                Id = Guid.NewGuid(), PostId = postId, AppUserId = voterId, PollOptionId = pollOptionId
            });
        }
        else
        {
            existingVote.PollOptionId = pollOptionId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapPollOptionsAsync(postId, cancellationToken);
    }

    private async Task<IReadOnlyList<PollOptionResponse>> MapPollOptionsAsync(
        Guid postId, CancellationToken cancellationToken) =>
        await dbContext.PollOptions
            .Where(o => o.PostId == postId)
            .Select(o => new PollOptionResponse(o.Id, o.Text, dbContext.PollVotes.Count(v => v.PollOptionId == o.Id)))
            .ToListAsync(cancellationToken);
}
