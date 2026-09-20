using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class SavedPostServiceTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(AppUser User, Post PostA, Post PostB)> SeedAsync(AppDbContext dbContext)
    {
        var user = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "user", CreatedAtUtc = DateTimeOffset.UtcNow };
        var community = new Community { Id = Guid.NewGuid(), Name = "c1", CreatedByUserId = user.Id, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        var postA = new Post { Id = Guid.NewGuid(), CommunityId = community.Id, AuthorId = user.Id, Title = "A", CreatedAtUtc = DateTimeOffset.UtcNow };
        var postB = new Post { Id = Guid.NewGuid(), CommunityId = community.Id, AuthorId = user.Id, Title = "B", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(user);
        dbContext.Communities.Add(community);
        dbContext.Posts.AddRange(postA, postB);
        await dbContext.SaveChangesAsync();
        return (user, postA, postB);
    }

    [Fact]
    public async Task SaveAsync_IsIdempotent_OnRepeatedCalls()
    {
        var dbContext = CreateDbContext();
        var (user, post, _) = await SeedAsync(dbContext);
        var service = new SavedPostService(dbContext);

        await service.SaveAsync(user.Id, post.Id);
        await service.SaveAsync(user.Id, post.Id);

        Assert.Equal(1, await dbContext.SavedPosts.CountAsync());
    }

    [Fact]
    public async Task UnsaveAsync_NotSaved_IsNoOp()
    {
        var dbContext = CreateDbContext();
        var (user, post, _) = await SeedAsync(dbContext);
        var service = new SavedPostService(dbContext);

        await service.UnsaveAsync(user.Id, post.Id);

        Assert.Equal(0, await dbContext.SavedPosts.CountAsync());
    }

    [Fact]
    public async Task UnsaveAsync_RemovesExistingSave()
    {
        var dbContext = CreateDbContext();
        var (user, post, _) = await SeedAsync(dbContext);
        var service = new SavedPostService(dbContext);
        await service.SaveAsync(user.Id, post.Id);

        await service.UnsaveAsync(user.Id, post.Id);

        Assert.Equal(0, await dbContext.SavedPosts.CountAsync());
    }

    [Fact]
    public async Task ListSavedAsync_OrdersMostRecentlySavedFirst()
    {
        var dbContext = CreateDbContext();
        var (user, postA, postB) = await SeedAsync(dbContext);
        var service = new SavedPostService(dbContext);
        await service.SaveAsync(user.Id, postA.Id);
        await service.SaveAsync(user.Id, postB.Id);

        var (items, totalCount) = await service.ListSavedAsync(user.Id, 1, 20);

        Assert.Equal(2, totalCount);
        Assert.Equal([postB.Id, postA.Id], items.Select(p => p.Id));
    }

    [Fact]
    public async Task ListSavedAsync_ExcludesOtherUsersSaves()
    {
        var dbContext = CreateDbContext();
        var (user, postA, _) = await SeedAsync(dbContext);
        var otherUser = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "other", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(otherUser);
        await dbContext.SaveChangesAsync();
        var service = new SavedPostService(dbContext);
        await service.SaveAsync(user.Id, postA.Id);

        var (items, totalCount) = await service.ListSavedAsync(otherUser.Id, 1, 20);

        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }
}
