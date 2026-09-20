using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using AnonyMeow.Services.ContentSubmission;
using AnonyMeow.Services.Ranking;
using AnonyMeow.Services.SpamDetection;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class CommentServiceTests
{
    private class PassthroughRankingService : IRankingService
    {
        public IQueryable<Post> ApplyPostSort(IQueryable<Post> posts, SortOrder sortOrder) =>
            posts.OrderByDescending(p => p.CreatedAtUtc);

        public IQueryable<Comment> ApplyCommentSort(IQueryable<Comment> comments, SortOrder sortOrder) =>
            comments.OrderByDescending(c => c.CreatedAtUtc);
    }

    private class FakeNotificationDispatcher : INotificationDispatcher
    {
        public List<(Guid RecipientId, NotificationType Type, Guid SourceId)> Calls { get; } = [];

        public Task DispatchAsync(
            Guid recipientId, NotificationType type, NotificationSourceType sourceType, Guid sourceId, string previewText,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((recipientId, type, sourceId));
            return Task.CompletedTask;
        }
    }

    private class FakeMentionParsingService(AppDbContext dbContext) : IMentionParsingService
    {
        public async Task<IReadOnlyList<AppUser>> ExtractMentionedUsersAsync(
            string bodyMarkdown, CancellationToken cancellationToken = default)
        {
            // Minimal stand-in for the real regex-based parser: any username appearing after
            // "@" in the body that exists in the seeded users is treated as mentioned.
            var users = await dbContext.Users.ToListAsync(cancellationToken);
            return users.Where(u => u.Username is not null && bodyMarkdown.Contains($"@{u.Username}")).ToList();
        }
    }

    private class FakeContentSubmissionPipeline(bool block = false) : IContentSubmissionPipeline
    {
        public Task<ContentSubmissionResult> EvaluateAsync(ContentSubmissionRequest submission, CancellationToken cancellationToken = default) =>
            Task.FromResult(block ? new ContentSubmissionResult(true, ["Email"]) : ContentSubmissionResult.Allowed);
    }

    private class FakeSpamFlaggingService : ISpamFlaggingService
    {
        public Task FlagIfSpamAsync(
            SpamFlagTargetType targetType, Guid targetId, Guid authorId, ContentSubmissionType contentType, string text,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CommentService CreateService(
        AppDbContext dbContext, FakeNotificationDispatcher? dispatcher = null, bool blockContent = false) =>
        new(dbContext, new PassthroughRankingService(), dispatcher ?? new FakeNotificationDispatcher(),
            new FakeMentionParsingService(dbContext), new FakeContentSubmissionPipeline(blockContent), new FakeSpamFlaggingService());

    private static async Task<(AppUser Author, Post Post)> SeedAsync(AppDbContext dbContext, bool isLocked = false)
    {
        var author = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "author", CreatedAtUtc = DateTimeOffset.UtcNow };
        var community = new Community { Id = Guid.NewGuid(), Name = "c1", CreatedByUserId = author.Id, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        var post = new Post { Id = Guid.NewGuid(), CommunityId = community.Id, AuthorId = author.Id, Title = "T", BodyMarkdown = "B", IsLocked = isLocked, CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(author);
        dbContext.Communities.Add(community);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();
        return (author, post);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenPostIsLocked()
    {
        var dbContext = CreateDbContext();
        var (author, post) = await SeedAsync(dbContext, isLocked: true);
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<PostLockedException>(() => service.CreateAsync(post.Id, author.Id, "Body", null));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenParentCommentBelongsToDifferentPost()
    {
        var dbContext = CreateDbContext();
        var (author, post) = await SeedAsync(dbContext);
        var (_, otherPost) = await SeedAsync(dbContext);
        var service = CreateService(dbContext);
        var foreignParent = await service.CreateAsync(otherPost.Id, author.Id, "Parent on a different post", null);

        await Assert.ThrowsAsync<InvalidParentCommentException>(
            () => service.CreateAsync(post.Id, author.Id, "Reply", foreignParent.Id));
    }

    [Fact]
    public async Task CreateAsync_Succeeds_WhenParentBelongsToSamePost()
    {
        var dbContext = CreateDbContext();
        var (author, post) = await SeedAsync(dbContext);
        var service = CreateService(dbContext);
        var parent = await service.CreateAsync(post.Id, author.Id, "Parent", null);

        var reply = await service.CreateAsync(post.Id, author.Id, "Reply", parent.Id);

        Assert.Equal(parent.Id, reply.ParentCommentId);
    }

    [Fact]
    public async Task GetAncestorChainAsync_ReturnsEmpty_ForTopLevelComment()
    {
        var dbContext = CreateDbContext();
        var (author, post) = await SeedAsync(dbContext);
        var service = CreateService(dbContext);
        var topLevel = await service.CreateAsync(post.Id, author.Id, "Top level", null);

        var chain = await service.GetAncestorChainAsync(topLevel.Id);

        Assert.Empty(chain);
    }

    [Fact]
    public async Task GetAncestorChainAsync_ReturnsRootToImmediateParent_ForNestedReply()
    {
        var dbContext = CreateDbContext();
        var (author, post) = await SeedAsync(dbContext);
        var service = CreateService(dbContext);
        var root = await service.CreateAsync(post.Id, author.Id, "Root", null);
        var child = await service.CreateAsync(post.Id, author.Id, "Child", root.Id);
        var grandchild = await service.CreateAsync(post.Id, author.Id, "Grandchild", child.Id);

        var chain = await service.GetAncestorChainAsync(grandchild.Id);

        Assert.Equal([root.Id, child.Id], chain);
    }

    [Fact]
    public async Task CreateAsync_TopLevelComment_NotifiesPostAuthor()
    {
        var dbContext = CreateDbContext();
        var (postAuthor, post) = await SeedAsync(dbContext);
        var commenter = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "commenter", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(commenter);
        await dbContext.SaveChangesAsync();
        var dispatcher = new FakeNotificationDispatcher();
        var service = CreateService(dbContext, dispatcher);

        var comment = await service.CreateAsync(post.Id, commenter.Id, "Nice post", null);

        var call = Assert.Single(dispatcher.Calls);
        Assert.Equal(postAuthor.Id, call.RecipientId);
        Assert.Equal(NotificationType.Reply, call.Type);
        Assert.Equal(comment.Id, call.SourceId);
    }

    [Fact]
    public async Task CreateAsync_SelfReplyToOwnPost_DoesNotDispatchNotification()
    {
        var dbContext = CreateDbContext();
        var (author, post) = await SeedAsync(dbContext);
        var dispatcher = new FakeNotificationDispatcher();
        var service = CreateService(dbContext, dispatcher);

        await service.CreateAsync(post.Id, author.Id, "Commenting on my own post", null);

        Assert.Empty(dispatcher.Calls);
    }

    [Fact]
    public async Task CreateAsync_Reply_NotifiesParentCommentAuthor_NotPostAuthor()
    {
        var dbContext = CreateDbContext();
        var (postAuthor, post) = await SeedAsync(dbContext);
        var parentAuthor = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "parentauthor", CreatedAtUtc = DateTimeOffset.UtcNow };
        var replier = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "replier", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.AddRange(parentAuthor, replier);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        var parent = await service.CreateAsync(post.Id, parentAuthor.Id, "Parent comment", null);

        var dispatcher = new FakeNotificationDispatcher();
        var replyService = CreateService(dbContext, dispatcher);
        var reply = await replyService.CreateAsync(post.Id, replier.Id, "Replying", parent.Id);

        var call = Assert.Single(dispatcher.Calls);
        Assert.Equal(parentAuthor.Id, call.RecipientId);
        Assert.NotEqual(postAuthor.Id, call.RecipientId);
        Assert.Equal(reply.Id, call.SourceId);
    }

    [Fact]
    public async Task CreateAsync_SelfReplyToOwnComment_DoesNotDispatchNotification()
    {
        var dbContext = CreateDbContext();
        var (author, post) = await SeedAsync(dbContext);
        var service = CreateService(dbContext);
        var parent = await service.CreateAsync(post.Id, author.Id, "Parent", null);

        var dispatcher = new FakeNotificationDispatcher();
        var replyService = CreateService(dbContext, dispatcher);
        await replyService.CreateAsync(post.Id, author.Id, "Replying to myself", parent.Id);

        Assert.Empty(dispatcher.Calls);
    }

    [Fact]
    public async Task CreateAsync_WithMention_DispatchesMentionNotification_ToMentionedUser()
    {
        var dbContext = CreateDbContext();
        var (author, post) = await SeedAsync(dbContext);
        var mentioned = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "mentioned", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(mentioned);
        await dbContext.SaveChangesAsync();
        var dispatcher = new FakeNotificationDispatcher();
        var service = CreateService(dbContext, dispatcher);

        var comment = await service.CreateAsync(post.Id, author.Id, "Hey @mentioned check this out", null);

        Assert.Contains(dispatcher.Calls, c => c.Type == NotificationType.Mention && c.RecipientId == mentioned.Id && c.SourceId == comment.Id);
    }

    [Fact]
    public async Task CreateAsync_SelfMention_DoesNotDispatchMentionNotification()
    {
        var dbContext = CreateDbContext();
        var (author, post) = await SeedAsync(dbContext);
        var dispatcher = new FakeNotificationDispatcher();
        var service = CreateService(dbContext, dispatcher);

        await service.CreateAsync(post.Id, author.Id, "Talking to myself @author", null);

        Assert.DoesNotContain(dispatcher.Calls, c => c.Type == NotificationType.Mention);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenContentSubmissionPipelineBlocks()
    {
        var dbContext = CreateDbContext();
        var (author, post) = await SeedAsync(dbContext);
        var service = CreateService(dbContext, blockContent: true);

        var ex = await Assert.ThrowsAsync<PiiDetectedException>(
            () => service.CreateAsync(post.Id, author.Id, "email me at a@b.com", null));
        Assert.Contains("Email", ex.Extensions["detectedCategories"] as IReadOnlyList<string> ?? []);
    }
}
