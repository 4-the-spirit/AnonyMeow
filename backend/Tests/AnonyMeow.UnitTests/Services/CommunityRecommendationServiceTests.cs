using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class CommunityRecommendationServiceTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Community MakeCommunity(Guid creatorId, string name) => new()
    {
        Id = Guid.NewGuid(), Name = name, CreatedByUserId = creatorId, CreatedAtUtc = DateTimeOffset.UtcNow,
        IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png"
    };

    private static void AddMember(AppDbContext dbContext, Guid communityId, Guid userId) =>
        dbContext.CommunityMemberships.Add(new CommunityMembership
        {
            CommunityId = communityId, AppUserId = userId, Role = CommunityRole.Member, JoinedAtUtc = DateTimeOffset.UtcNow
        });

    [Fact]
    public async Task GetRecommendedCommunitiesAsync_ExcludesAlreadyJoinedCommunities()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var joined = MakeCommunity(userId, "joined");
        var notJoined = MakeCommunity(userId, "notjoined");
        dbContext.Communities.AddRange(joined, notJoined);
        AddMember(dbContext, joined.Id, userId);
        await dbContext.SaveChangesAsync();
        var service = new CommunityRecommendationService(dbContext);

        var (items, totalCount) = await service.GetRecommendedCommunitiesAsync(userId, 1, 20);

        Assert.Equal(1, totalCount);
        Assert.Equal(notJoined.Id, Assert.Single(items).Id);
    }

    [Fact]
    public async Task GetRecommendedCommunitiesAsync_OrdersByMemberCountDescending()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var popular = MakeCommunity(userId, "popular");
        var quiet = MakeCommunity(userId, "quiet");
        dbContext.Communities.AddRange(popular, quiet);
        AddMember(dbContext, popular.Id, Guid.NewGuid());
        AddMember(dbContext, popular.Id, Guid.NewGuid());
        AddMember(dbContext, quiet.Id, Guid.NewGuid());
        await dbContext.SaveChangesAsync();
        var service = new CommunityRecommendationService(dbContext);

        var (items, _) = await service.GetRecommendedCommunitiesAsync(userId, 1, 20);

        Assert.Equal([popular.Id, quiet.Id], items.Select(c => c.Id));
    }

    [Fact]
    public async Task GetRecommendedCommunitiesAsync_NoOtherCommunities_ReturnsEmpty()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var joined = MakeCommunity(userId, "joined");
        dbContext.Communities.Add(joined);
        AddMember(dbContext, joined.Id, userId);
        await dbContext.SaveChangesAsync();
        var service = new CommunityRecommendationService(dbContext);

        var (items, totalCount) = await service.GetRecommendedCommunitiesAsync(userId, 1, 20);

        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetRecommendedCommunitiesAsync_NullUserId_SkipsExclusionAndReturnsAllCommunities()
    {
        var dbContext = CreateDbContext();
        var someoneElse = Guid.NewGuid();
        var communityA = MakeCommunity(someoneElse, "communitya");
        var communityB = MakeCommunity(someoneElse, "communityb");
        dbContext.Communities.AddRange(communityA, communityB);
        AddMember(dbContext, communityA.Id, someoneElse);
        await dbContext.SaveChangesAsync();
        var service = new CommunityRecommendationService(dbContext);

        var (items, totalCount) = await service.GetRecommendedCommunitiesAsync(null, 1, 20);

        Assert.Equal(2, totalCount);
        Assert.Equal(
            new[] { communityA.Id, communityB.Id }.OrderBy(id => id),
            items.Select(c => c.Id).OrderBy(id => id));
    }
}
