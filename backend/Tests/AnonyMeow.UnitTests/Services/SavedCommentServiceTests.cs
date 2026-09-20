using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class SavedCommentServiceTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(AppUser User, Comment CommentA, Comment CommentB)> SeedAsync(AppDbContext dbContext)
    {
        var user = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "user", CreatedAtUtc = DateTimeOffset.UtcNow };
        var community = new Community { Id = Guid.NewGuid(), Name = "c1", CreatedByUserId = user.Id, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        var post = new Post { Id = Guid.NewGuid(), CommunityId = community.Id, AuthorId = user.Id, Title = "P", CreatedAtUtc = DateTimeOffset.UtcNow };
        var commentA = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = user.Id, BodyMarkdown = "A", CreatedAtUtc = DateTimeOffset.UtcNow };
        var commentB = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = user.Id, BodyMarkdown = "B", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(user);
        dbContext.Communities.Add(community);
        dbContext.Posts.Add(post);
        dbContext.Comments.AddRange(commentA, commentB);
        await dbContext.SaveChangesAsync();
        return (user, commentA, commentB);
    }

    [Fact]
    public async Task SaveAsync_IsIdempotent_OnRepeatedCalls()
    {
        var dbContext = CreateDbContext();
        var (user, comment, _) = await SeedAsync(dbContext);
        var service = new SavedCommentService(dbContext);

        await service.SaveAsync(user.Id, comment.Id);
        await service.SaveAsync(user.Id, comment.Id);

        Assert.Equal(1, await dbContext.SavedComments.CountAsync());
    }

    [Fact]
    public async Task UnsaveAsync_NotSaved_IsNoOp()
    {
        var dbContext = CreateDbContext();
        var (user, comment, _) = await SeedAsync(dbContext);
        var service = new SavedCommentService(dbContext);

        await service.UnsaveAsync(user.Id, comment.Id);

        Assert.Equal(0, await dbContext.SavedComments.CountAsync());
    }

    [Fact]
    public async Task UnsaveAsync_RemovesExistingSave()
    {
        var dbContext = CreateDbContext();
        var (user, comment, _) = await SeedAsync(dbContext);
        var service = new SavedCommentService(dbContext);
        await service.SaveAsync(user.Id, comment.Id);

        await service.UnsaveAsync(user.Id, comment.Id);

        Assert.Equal(0, await dbContext.SavedComments.CountAsync());
    }

    [Fact]
    public async Task ListSavedAsync_OrdersMostRecentlySavedFirst()
    {
        var dbContext = CreateDbContext();
        var (user, commentA, commentB) = await SeedAsync(dbContext);
        var service = new SavedCommentService(dbContext);
        await service.SaveAsync(user.Id, commentA.Id);
        await service.SaveAsync(user.Id, commentB.Id);

        var (items, totalCount) = await service.ListSavedAsync(user.Id, 1, 20);

        Assert.Equal(2, totalCount);
        Assert.Equal([commentB.Id, commentA.Id], items.Select(c => c.Id));
    }

    [Fact]
    public async Task ListSavedAsync_ExcludesOtherUsersSaves()
    {
        var dbContext = CreateDbContext();
        var (user, commentA, _) = await SeedAsync(dbContext);
        var otherUser = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "other", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(otherUser);
        await dbContext.SaveChangesAsync();
        var service = new SavedCommentService(dbContext);
        await service.SaveAsync(user.Id, commentA.Id);

        var (items, totalCount) = await service.ListSavedAsync(otherUser.Id, 1, 20);

        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }
}
