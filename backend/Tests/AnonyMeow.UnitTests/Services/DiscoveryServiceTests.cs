using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using AnonyMeow.Services.Ranking;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class DiscoveryServiceTests
{
    // Trending's exact scoring formula is a TrendingSortStrategy concern (covered separately in
    // SortStrategyTests) — this fake isolates DiscoveryService's own responsibility: window and
    // community-scope filtering, not ranking.
    private class PassthroughRankingService : IRankingService
    {
        public IQueryable<Post> ApplyPostSort(IQueryable<Post> posts, SortOrder sortOrder) =>
            posts.OrderByDescending(p => p.CreatedAtUtc);

        public IQueryable<Comment> ApplyCommentSort(IQueryable<Comment> comments, SortOrder sortOrder) =>
            comments.OrderByDescending(c => c.CreatedAtUtc);
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Post MakePost(Guid communityId, Guid authorId, DateTimeOffset createdAt, bool isRemoved = false) => new()
    {
        Id = Guid.NewGuid(), CommunityId = communityId, AuthorId = authorId,
        Title = "T", BodyMarkdown = "B", IsRemoved = isRemoved, CreatedAtUtc = createdAt
    };

    [Fact]
    public async Task GetTrendingPostsAsync_ExcludesPostsOutsideWindow()
    {
        var dbContext = CreateDbContext();
        var communityId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var withinDay = MakePost(communityId, authorId, DateTimeOffset.UtcNow.AddHours(-2));
        var outsideDay = MakePost(communityId, authorId, DateTimeOffset.UtcNow.AddDays(-2));
        dbContext.Posts.AddRange(withinDay, outsideDay);
        await dbContext.SaveChangesAsync();
        var service = new DiscoveryService(dbContext, new PassthroughRankingService());

        var (items, totalCount) = await service.GetTrendingPostsAsync(null, TrendingWindow.Day, 1, 20);

        Assert.Equal(1, totalCount);
        Assert.Equal(withinDay.Id, Assert.Single(items).Id);
    }

    [Fact]
    public async Task GetTrendingPostsAsync_WeekWindow_IncludesPostFromFiveDaysAgo()
    {
        var dbContext = CreateDbContext();
        var communityId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var fiveDaysAgo = MakePost(communityId, authorId, DateTimeOffset.UtcNow.AddDays(-5));
        dbContext.Posts.Add(fiveDaysAgo);
        await dbContext.SaveChangesAsync();
        var service = new DiscoveryService(dbContext, new PassthroughRankingService());

        var (items, totalCount) = await service.GetTrendingPostsAsync(null, TrendingWindow.Week, 1, 20);

        Assert.Equal(1, totalCount);
        Assert.Equal(fiveDaysAgo.Id, Assert.Single(items).Id);
    }

    [Fact]
    public async Task GetTrendingPostsAsync_ScopedToCommunity_ExcludesOtherCommunities()
    {
        var dbContext = CreateDbContext();
        var scopedCommunityId = Guid.NewGuid();
        var otherCommunityId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var scopedPost = MakePost(scopedCommunityId, authorId, DateTimeOffset.UtcNow);
        var otherPost = MakePost(otherCommunityId, authorId, DateTimeOffset.UtcNow);
        dbContext.Posts.AddRange(scopedPost, otherPost);
        await dbContext.SaveChangesAsync();
        var service = new DiscoveryService(dbContext, new PassthroughRankingService());

        var (items, totalCount) = await service.GetTrendingPostsAsync(scopedCommunityId, TrendingWindow.Week, 1, 20);

        Assert.Equal(1, totalCount);
        Assert.Equal(scopedPost.Id, Assert.Single(items).Id);
    }

    [Fact]
    public async Task GetTrendingPostsAsync_ExcludesRemovedPosts()
    {
        var dbContext = CreateDbContext();
        var communityId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var removed = MakePost(communityId, authorId, DateTimeOffset.UtcNow, isRemoved: true);
        dbContext.Posts.Add(removed);
        await dbContext.SaveChangesAsync();
        var service = new DiscoveryService(dbContext, new PassthroughRankingService());

        var (items, totalCount) = await service.GetTrendingPostsAsync(null, TrendingWindow.Week, 1, 20);

        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }
}
