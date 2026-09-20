using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class FlairServiceTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(AppUser Mod, Community CommunityA, Community CommunityB, Post Post)> SeedAsync(AppDbContext dbContext)
    {
        var mod = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "mod", CreatedAtUtc = DateTimeOffset.UtcNow };
        var communityA = new Community { Id = Guid.NewGuid(), Name = "ca", CreatedByUserId = mod.Id, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        var communityB = new Community { Id = Guid.NewGuid(), Name = "cb", CreatedByUserId = mod.Id, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        var post = new Post
        {
            Id = Guid.NewGuid(), CommunityId = communityA.Id, AuthorId = mod.Id,
            Title = "T", BodyMarkdown = "B", CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Users.Add(mod);
        dbContext.Communities.AddRange(communityA, communityB);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();
        return (mod, communityA, communityB, post);
    }

    [Fact]
    public async Task CreateAsync_Throws_OnDuplicateNameInSameCommunity()
    {
        var dbContext = CreateDbContext();
        var (mod, communityA, _, _) = await SeedAsync(dbContext);
        var service = new FlairService(dbContext);
        await service.CreateAsync(communityA.Id, "Discussion", "#ff0000", mod.Id);

        await Assert.ThrowsAsync<FlairNameConflictException>(
            () => service.CreateAsync(communityA.Id, "Discussion", "#00ff00", mod.Id));
    }

    [Fact]
    public async Task CreateAsync_AllowsSameNameInDifferentCommunities()
    {
        var dbContext = CreateDbContext();
        var (mod, communityA, communityB, _) = await SeedAsync(dbContext);
        var service = new FlairService(dbContext);

        await service.CreateAsync(communityA.Id, "Discussion", "#ff0000", mod.Id);
        var second = await service.CreateAsync(communityB.Id, "Discussion", "#00ff00", mod.Id);

        Assert.Equal("Discussion", second.Name);
    }

    [Fact]
    public async Task AssignToPostAsync_SetsFlairId_WhenSameCommunity()
    {
        var dbContext = CreateDbContext();
        var (mod, communityA, _, post) = await SeedAsync(dbContext);
        var service = new FlairService(dbContext);
        var flair = await service.CreateAsync(communityA.Id, "Discussion", "#ff0000", mod.Id);

        await service.AssignToPostAsync(post, flair.Id);

        Assert.Equal(flair.Id, post.FlairId);
    }

    [Fact]
    public async Task AssignToPostAsync_Throws_WhenFlairBelongsToDifferentCommunity()
    {
        var dbContext = CreateDbContext();
        var (mod, _, communityB, post) = await SeedAsync(dbContext);
        var service = new FlairService(dbContext);
        var otherCommunityFlair = await service.CreateAsync(communityB.Id, "OffTopic", "#0000ff", mod.Id);

        await Assert.ThrowsAsync<FlairCommunityMismatchException>(
            () => service.AssignToPostAsync(post, otherCommunityFlair.Id));
    }

    [Fact]
    public async Task AssignToPostAsync_Throws_WhenFlairDoesNotExist()
    {
        var dbContext = CreateDbContext();
        var (_, _, _, post) = await SeedAsync(dbContext);
        var service = new FlairService(dbContext);

        await Assert.ThrowsAsync<FlairNotFoundException>(() => service.AssignToPostAsync(post, Guid.NewGuid()));
    }

    [Fact]
    public async Task AssignToPostAsync_NullFlairId_ClearsFlair()
    {
        var dbContext = CreateDbContext();
        var (mod, communityA, _, post) = await SeedAsync(dbContext);
        var service = new FlairService(dbContext);
        var flair = await service.CreateAsync(communityA.Id, "Discussion", "#ff0000", mod.Id);
        await service.AssignToPostAsync(post, flair.Id);

        await service.AssignToPostAsync(post, null);

        Assert.Null(post.FlairId);
    }

    [Fact]
    public async Task UpdateAsync_RenamesAndRecolorsCustomFlair()
    {
        var dbContext = CreateDbContext();
        var (mod, communityA, _, _) = await SeedAsync(dbContext);
        var service = new FlairService(dbContext);
        var flair = await service.CreateAsync(communityA.Id, "Discussion", "#ff0000", mod.Id);

        var updated = await service.UpdateAsync(flair, "General Chat", "#00ff00");

        Assert.Equal("General Chat", updated.Name);
        Assert.Equal("#00ff00", updated.ColorHex);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenFlairIsDefault()
    {
        var dbContext = CreateDbContext();
        var (mod, communityA, _, _) = await SeedAsync(dbContext);
        var service = new FlairService(dbContext);
        var defaultFlair = await service.CreateAsync(communityA.Id, "שאלה", "#6B7280", mod.Id);
        defaultFlair.IsDefault = true;
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<FlairImmutableException>(() => service.UpdateAsync(defaultFlair, "New Name", "#ffffff"));
    }

    [Fact]
    public async Task UpdateAsync_Throws_OnDuplicateNameInSameCommunity()
    {
        var dbContext = CreateDbContext();
        var (mod, communityA, _, _) = await SeedAsync(dbContext);
        var service = new FlairService(dbContext);
        await service.CreateAsync(communityA.Id, "Existing", "#ff0000", mod.Id);
        var flairToRename = await service.CreateAsync(communityA.Id, "Renameable", "#00ff00", mod.Id);

        await Assert.ThrowsAsync<FlairNameConflictException>(
            () => service.UpdateAsync(flairToRename, "Existing", "#0000ff"));
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenFlairIsDefault()
    {
        var dbContext = CreateDbContext();
        var (mod, communityA, _, _) = await SeedAsync(dbContext);
        var service = new FlairService(dbContext);
        var defaultFlair = await service.CreateAsync(communityA.Id, "דיון", "#6B7280", mod.Id);
        defaultFlair.IsDefault = true;
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<FlairImmutableException>(() => service.DeleteAsync(defaultFlair));
    }
}
