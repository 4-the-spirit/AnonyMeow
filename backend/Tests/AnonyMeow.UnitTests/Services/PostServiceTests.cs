using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Flairs;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Services;
using AnonyMeow.Services.ContentSubmission;
using AnonyMeow.Services.Ranking;
using AnonyMeow.Services.SpamDetection;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class PostServiceTests
{
    private class PassthroughRankingService : IRankingService
    {
        public IQueryable<Post> ApplyPostSort(IQueryable<Post> posts, SortOrder sortOrder) =>
            posts.OrderByDescending(p => p.CreatedAtUtc);

        public IQueryable<Comment> ApplyCommentSort(IQueryable<Comment> comments, SortOrder sortOrder) =>
            comments.OrderByDescending(c => c.CreatedAtUtc);
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

    // Defaults to accepting every image — set mode to exercise a specific rejection branch.
    private class FakeImageUploadService(FakeImageUploadService.Mode mode = FakeImageUploadService.Mode.Valid) : IImageUploadService
    {
        public enum Mode { Valid, WrongOwner, TooLarge, NotFound, BadContentType }

        public Task<(string UploadUrl, string BlobUrl)> CreateUploadSasAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(("https://upload.example/sas", "https://blob.example/image.png"));

        public Task ValidateImageAsync(string blobUrl, Guid ownerId, CancellationToken cancellationToken = default) => mode switch
        {
            Mode.Valid => Task.CompletedTask,
            Mode.WrongOwner => throw new ImageOwnershipMismatchException(blobUrl),
            Mode.TooLarge or Mode.NotFound => throw new PostImageTooLargeException(blobUrl, 8 * 1024 * 1024),
            Mode.BadContentType => throw new UnsupportedImageContentTypeException(blobUrl),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    // Accepts any flair id by default — flair validation itself is covered by FlairServiceTests;
    // PostService is only responsible for calling through to it.
    private class FakeFlairService(bool rejectAll = false) : IFlairService
    {
        public Task<Flair> CreateAsync(Guid communityId, string name, string colorHex, Guid modId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Flair?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Flair>> ListForCommunityAsync(Guid communityId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task DeleteAsync(Flair flair, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AssignToPostAsync(Post post, Guid? flairId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Flair> UpdateAsync(Flair flair, string name, string colorHex, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task EnsureValidForCommunityAsync(Guid flairId, Guid communityId, CancellationToken cancellationToken = default) =>
            rejectAll ? throw new FlairNotFoundException() : Task.CompletedTask;

        public Task<FlairResponse?> GetResponseForPostAsync(Post post, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyDictionary<Guid, FlairResponse>> GetResponseMapAsync(
            IEnumerable<Guid> flairIds, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private static readonly Guid TestFlairId = Guid.NewGuid();

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static PostService CreateService(
        AppDbContext dbContext, bool blockContent = false, IImageUploadService? imageUploadService = null) =>
        new(dbContext, new PassthroughRankingService(), new FakeContentSubmissionPipeline(blockContent), new FakeSpamFlaggingService(),
            imageUploadService ?? new FakeImageUploadService(), new FakeFlairService());

    private static async Task<(AppUser Author, AppUser Other, Community Community)> SeedAsync(AppDbContext dbContext)
    {
        var author = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "author", CreatedAtUtc = DateTimeOffset.UtcNow };
        var other = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "other", CreatedAtUtc = DateTimeOffset.UtcNow };
        var community = new Community { Id = Guid.NewGuid(), Name = "c1", CreatedByUserId = author.Id, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        dbContext.Users.AddRange(author, other);
        dbContext.Communities.Add(community);
        await dbContext.SaveChangesAsync();
        return (author, other, community);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRemovedPost_ForAuthor()
    {
        var dbContext = CreateDbContext();
        var (author, _, community) = await SeedAsync(dbContext);
        var service = CreateService(dbContext);
        var post = await service.CreateAsync(community.Id, author.Id,
            new CreatePostRequest("T", "B", null, null, null, TestFlairId));
        await service.SoftDeleteAsync(post);

        var result = await service.GetByIdAsync(post.Id, author.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetByIdAsync_HidesRemovedPost_FromNonAuthorNonModerator()
    {
        var dbContext = CreateDbContext();
        var (author, other, community) = await SeedAsync(dbContext);
        var service = CreateService(dbContext);
        var post = await service.CreateAsync(community.Id, author.Id,
            new CreatePostRequest("T", "B", null, null, null, TestFlairId));
        await service.SoftDeleteAsync(post);

        var result = await service.GetByIdAsync(post.Id, other.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRemovedPost_ForCommunityModerator()
    {
        var dbContext = CreateDbContext();
        var (author, other, community) = await SeedAsync(dbContext);
        dbContext.CommunityMemberships.Add(new CommunityMembership
        {
            CommunityId = community.Id, AppUserId = other.Id, Role = CommunityRole.Moderator, JoinedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        var post = await service.CreateAsync(community.Id, author.Id,
            new CreatePostRequest("T", "B", null, null, null, TestFlairId));
        await service.SoftDeleteAsync(post);

        var result = await service.GetByIdAsync(post.Id, other.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task CastPollVoteAsync_ChangesSelection_OnSecondVoteFromSameVoter()
    {
        var dbContext = CreateDbContext();
        var (author, voter, community) = await SeedAsync(dbContext);
        var service = CreateService(dbContext);
        var post = await service.CreateAsync(community.Id, author.Id,
            new CreatePostRequest("T", null, null, null, ["A", "B"], TestFlairId));
        var options = await service.GetPollOptionsAsync(post.Id);
        var optionA = options.First(o => o.Text == "A");
        var optionB = options.First(o => o.Text == "B");

        await service.CastPollVoteAsync(post.Id, voter.Id, optionA.Id);
        var afterFirstVote = await service.GetPollOptionsAsync(post.Id);
        Assert.Equal(1, afterFirstVote.First(o => o.Id == optionA.Id).VoteCount);

        await service.CastPollVoteAsync(post.Id, voter.Id, optionB.Id);
        var afterSecondVote = await service.GetPollOptionsAsync(post.Id);

        Assert.Equal(0, afterSecondVote.First(o => o.Id == optionA.Id).VoteCount);
        Assert.Equal(1, afterSecondVote.First(o => o.Id == optionB.Id).VoteCount);
        Assert.Equal(1, await dbContext.PollVotes.CountAsync(v => v.PostId == post.Id && v.AppUserId == voter.Id));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenContentSubmissionPipelineBlocks()
    {
        var dbContext = CreateDbContext();
        var (author, _, community) = await SeedAsync(dbContext);
        var service = CreateService(dbContext, blockContent: true);

        var ex = await Assert.ThrowsAsync<PiiDetectedException>(() => service.CreateAsync(
            community.Id, author.Id, new CreatePostRequest("T", "email me at a@b.com", null, null, null, TestFlairId)));
        Assert.Contains("Email", ex.Extensions["detectedCategories"] as IReadOnlyList<string> ?? []);
    }

    [Fact]
    public async Task CreateAsync_PersistsImages_InSubmittedOrder()
    {
        var dbContext = CreateDbContext();
        var (author, _, community) = await SeedAsync(dbContext);
        var service = CreateService(dbContext);
        var imageUrls = new List<string> { "https://blob.example/1.png", "https://blob.example/2.png", "https://blob.example/3.png" };

        var post = await service.CreateAsync(community.Id, author.Id,
            new CreatePostRequest("T", null, null, imageUrls, null, TestFlairId));

        var persisted = await dbContext.PostImages
            .Where(i => i.PostId == post.Id)
            .OrderBy(i => i.Position)
            .ToListAsync();
        Assert.Equal(imageUrls, persisted.Select(i => i.Url));
        Assert.Equal([0, 1, 2], persisted.Select(i => i.Position));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenImageExceedsMaxSize()
    {
        var dbContext = CreateDbContext();
        var (author, _, community) = await SeedAsync(dbContext);
        var service = CreateService(
            dbContext, imageUploadService: new FakeImageUploadService(FakeImageUploadService.Mode.TooLarge));

        await Assert.ThrowsAsync<PostImageTooLargeException>(() => service.CreateAsync(
            community.Id, author.Id, new CreatePostRequest("T", null, null, ["https://blob.example/big.png"], null, TestFlairId)));
        Assert.Empty(dbContext.Posts);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenImageBlobNotFound()
    {
        var dbContext = CreateDbContext();
        var (author, _, community) = await SeedAsync(dbContext);
        var service = CreateService(
            dbContext, imageUploadService: new FakeImageUploadService(FakeImageUploadService.Mode.NotFound));

        await Assert.ThrowsAsync<PostImageTooLargeException>(() => service.CreateAsync(
            community.Id, author.Id, new CreatePostRequest("T", null, null, ["https://blob.example/missing.png"], null, TestFlairId)));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenImageOwnedByAnotherUser()
    {
        var dbContext = CreateDbContext();
        var (author, _, community) = await SeedAsync(dbContext);
        var service = CreateService(
            dbContext, imageUploadService: new FakeImageUploadService(FakeImageUploadService.Mode.WrongOwner));

        await Assert.ThrowsAsync<ImageOwnershipMismatchException>(() => service.CreateAsync(
            community.Id, author.Id, new CreatePostRequest("T", null, null, ["https://blob.example/other-user.png"], null, TestFlairId)));
        Assert.Empty(dbContext.Posts);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenImageHasUnsupportedContentType()
    {
        var dbContext = CreateDbContext();
        var (author, _, community) = await SeedAsync(dbContext);
        var service = CreateService(
            dbContext, imageUploadService: new FakeImageUploadService(FakeImageUploadService.Mode.BadContentType));

        await Assert.ThrowsAsync<UnsupportedImageContentTypeException>(() => service.CreateAsync(
            community.Id, author.Id, new CreatePostRequest("T", null, null, ["https://blob.example/not-an-image.exe"], null, TestFlairId)));
        Assert.Empty(dbContext.Posts);
    }

    [Fact]
    public async Task CreateAsync_PersistsAllAttachments_WhenCombined()
    {
        var dbContext = CreateDbContext();
        var (author, _, community) = await SeedAsync(dbContext);
        var service = CreateService(dbContext);

        var post = await service.CreateAsync(community.Id, author.Id, new CreatePostRequest(
            "T", "Body", "https://example.com", ["https://blob.example/1.png"], ["A", "B"], TestFlairId));

        Assert.Equal("Body", post.BodyMarkdown);
        Assert.Equal("https://example.com", post.Url);
        Assert.Equal(1, await dbContext.PostImages.CountAsync(i => i.PostId == post.Id));
        Assert.Equal(2, await dbContext.PollOptions.CountAsync(o => o.PostId == post.Id));
    }

    [Fact]
    public async Task CreateAsync_SetsFlairId()
    {
        var dbContext = CreateDbContext();
        var (author, _, community) = await SeedAsync(dbContext);
        var service = CreateService(dbContext);

        var post = await service.CreateAsync(community.Id, author.Id,
            new CreatePostRequest("T", "B", null, null, null, TestFlairId));

        Assert.Equal(TestFlairId, post.FlairId);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenFlairInvalid()
    {
        var dbContext = CreateDbContext();
        var (author, _, community) = await SeedAsync(dbContext);
        var service = new PostService(
            dbContext, new PassthroughRankingService(), new FakeContentSubmissionPipeline(), new FakeSpamFlaggingService(),
            new FakeImageUploadService(), new FakeFlairService(rejectAll: true));

        await Assert.ThrowsAsync<FlairNotFoundException>(() => service.CreateAsync(
            community.Id, author.Id, new CreatePostRequest("T", "B", null, null, null, TestFlairId)));
        Assert.Empty(dbContext.Posts);
    }
}
