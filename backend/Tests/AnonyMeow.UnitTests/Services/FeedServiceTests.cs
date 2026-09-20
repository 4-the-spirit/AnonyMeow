using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using AnonyMeow.Services.Ranking;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class FeedServiceTests
{
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

    [Fact]
    public async Task GetFeedAsync_ReturnsPosts_OnlyFromJoinedCommunities()
    {
        var dbContext = CreateDbContext();
        var user = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "user", CreatedAtUtc = DateTimeOffset.UtcNow };
        var joinedCommunity = new Community { Id = Guid.NewGuid(), Name = "joined", CreatedByUserId = user.Id, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        var otherCommunity = new Community { Id = Guid.NewGuid(), Name = "other", CreatedByUserId = user.Id, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        var joinedPost = new Post { Id = Guid.NewGuid(), CommunityId = joinedCommunity.Id, AuthorId = user.Id, Title = "In feed", CreatedAtUtc = DateTimeOffset.UtcNow };
        var otherPost = new Post { Id = Guid.NewGuid(), CommunityId = otherCommunity.Id, AuthorId = user.Id, Title = "Not in feed", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(user);
        dbContext.Communities.AddRange(joinedCommunity, otherCommunity);
        dbContext.Posts.AddRange(joinedPost, otherPost);
        dbContext.CommunityMemberships.Add(new CommunityMembership
        {
            CommunityId = joinedCommunity.Id, AppUserId = user.Id, Role = CommunityRole.Member, JoinedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var service = new FeedService(dbContext, new PassthroughRankingService());

        var (items, totalCount) = await service.GetFeedAsync(user.Id, SortOrder.New, 1, 20);

        Assert.Equal(1, totalCount);
        Assert.Equal(joinedPost.Id, Assert.Single(items).Id);
    }

    [Fact]
    public async Task GetFeedAsync_ExcludesRemovedPosts()
    {
        var dbContext = CreateDbContext();
        var user = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "user", CreatedAtUtc = DateTimeOffset.UtcNow };
        var community = new Community { Id = Guid.NewGuid(), Name = "c1", CreatedByUserId = user.Id, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        var removedPost = new Post { Id = Guid.NewGuid(), CommunityId = community.Id, AuthorId = user.Id, Title = "Removed", IsRemoved = true, CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(user);
        dbContext.Communities.Add(community);
        dbContext.Posts.Add(removedPost);
        dbContext.CommunityMemberships.Add(new CommunityMembership
        {
            CommunityId = community.Id, AppUserId = user.Id, Role = CommunityRole.Member, JoinedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var service = new FeedService(dbContext, new PassthroughRankingService());

        var (items, totalCount) = await service.GetFeedAsync(user.Id, SortOrder.New, 1, 20);

        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetFeedAsync_NoJoinedCommunities_ReturnsEmpty()
    {
        var dbContext = CreateDbContext();
        var user = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "user", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var service = new FeedService(dbContext, new PassthroughRankingService());

        var (items, totalCount) = await service.GetFeedAsync(user.Id, SortOrder.New, 1, 20);

        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetFeedAsync_AnonymousUser_ReturnsPostsFromEveryCommunity()
    {
        var dbContext = CreateDbContext();
        var user = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "user", CreatedAtUtc = DateTimeOffset.UtcNow };
        var communityA = new Community { Id = Guid.NewGuid(), Name = "a", CreatedByUserId = user.Id, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        var communityB = new Community { Id = Guid.NewGuid(), Name = "b", CreatedByUserId = user.Id, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        var postA = new Post { Id = Guid.NewGuid(), CommunityId = communityA.Id, AuthorId = user.Id, Title = "In A", CreatedAtUtc = DateTimeOffset.UtcNow };
        var postB = new Post { Id = Guid.NewGuid(), CommunityId = communityB.Id, AuthorId = user.Id, Title = "In B", CreatedAtUtc = DateTimeOffset.UtcNow };
        var removedPost = new Post { Id = Guid.NewGuid(), CommunityId = communityA.Id, AuthorId = user.Id, Title = "Removed", IsRemoved = true, CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(user);
        dbContext.Communities.AddRange(communityA, communityB);
        dbContext.Posts.AddRange(postA, postB, removedPost);
        await dbContext.SaveChangesAsync();
        var service = new FeedService(dbContext, new PassthroughRankingService());

        var (items, totalCount) = await service.GetFeedAsync(null, SortOrder.New, 1, 20);

        Assert.Equal(2, totalCount);
        Assert.Contains(items, p => p.Id == postA.Id);
        Assert.Contains(items, p => p.Id == postB.Id);
        Assert.DoesNotContain(items, p => p.Id == removedPost.Id);
    }
}
