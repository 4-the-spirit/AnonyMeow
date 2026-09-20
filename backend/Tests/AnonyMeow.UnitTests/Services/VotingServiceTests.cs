using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class VotingServiceTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(AppUser Author, AppUser Voter, Post Post)> SeedAsync(AppDbContext dbContext)
    {
        var author = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "author", Karma = 0, CreatedAtUtc = DateTimeOffset.UtcNow };
        var voter = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "voter", Karma = 0, CreatedAtUtc = DateTimeOffset.UtcNow };
        var community = new Community { Id = Guid.NewGuid(), Name = "c1", CreatedByUserId = author.Id, CreatedAtUtc = DateTimeOffset.UtcNow, IconImageUrl = "https://example.com/icon.png", BannerImageUrl = "https://example.com/banner.png" };
        var post = new Post { Id = Guid.NewGuid(), CommunityId = community.Id, AuthorId = author.Id, Title = "T", BodyMarkdown = "B", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.AddRange(author, voter);
        dbContext.Communities.Add(community);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();
        return (author, voter, post);
    }

    [Fact]
    public async Task CastVoteAsync_NoneToUp_AddsOneKarma()
    {
        var dbContext = CreateDbContext();
        var (author, voter, post) = await SeedAsync(dbContext);
        var service = new VotingService(dbContext);

        await service.CastVoteAsync(VoteTargetType.Post, post.Id, voter.Id, 1);

        Assert.Equal(1, (await dbContext.Users.FindAsync(author.Id))!.Karma);
    }

    [Fact]
    public async Task CastVoteAsync_NoneToDown_SubtractsOneKarma()
    {
        var dbContext = CreateDbContext();
        var (author, voter, post) = await SeedAsync(dbContext);
        var service = new VotingService(dbContext);

        await service.CastVoteAsync(VoteTargetType.Post, post.Id, voter.Id, -1);

        Assert.Equal(-1, (await dbContext.Users.FindAsync(author.Id))!.Karma);
    }

    [Fact]
    public async Task CastVoteAsync_UpToDown_SubtractsTwoKarma()
    {
        var dbContext = CreateDbContext();
        var (author, voter, post) = await SeedAsync(dbContext);
        var service = new VotingService(dbContext);

        await service.CastVoteAsync(VoteTargetType.Post, post.Id, voter.Id, 1);
        await service.CastVoteAsync(VoteTargetType.Post, post.Id, voter.Id, -1);

        Assert.Equal(-1, (await dbContext.Users.FindAsync(author.Id))!.Karma);
        Assert.Equal(1, await dbContext.Votes.CountAsync(v => v.TargetType == VoteTargetType.Post && v.TargetId == post.Id));
    }

    [Fact]
    public async Task RemoveVoteAsync_UpToRemoved_SubtractsOneKarma()
    {
        var dbContext = CreateDbContext();
        var (author, voter, post) = await SeedAsync(dbContext);
        var service = new VotingService(dbContext);

        await service.CastVoteAsync(VoteTargetType.Post, post.Id, voter.Id, 1);
        await service.RemoveVoteAsync(VoteTargetType.Post, post.Id, voter.Id);

        Assert.Equal(0, (await dbContext.Users.FindAsync(author.Id))!.Karma);
        Assert.Equal(0, await dbContext.Votes.CountAsync(v => v.TargetType == VoteTargetType.Post && v.TargetId == post.Id));
    }

    [Fact]
    public async Task CastVoteAsync_SelfVote_Throws()
    {
        var dbContext = CreateDbContext();
        var (author, _, post) = await SeedAsync(dbContext);
        var service = new VotingService(dbContext);

        await Assert.ThrowsAsync<SelfVoteException>(() => service.CastVoteAsync(VoteTargetType.Post, post.Id, author.Id, 1));
    }

    [Fact]
    public async Task RemoveVoteAsync_NoExistingVote_IsNoOp()
    {
        var dbContext = CreateDbContext();
        var (_, voter, post) = await SeedAsync(dbContext);
        var service = new VotingService(dbContext);

        await service.RemoveVoteAsync(VoteTargetType.Post, post.Id, voter.Id);

        Assert.Equal(0, await dbContext.Votes.CountAsync());
    }

    [Fact]
    public async Task GetViewerVoteAsync_NoVote_ReturnsNull()
    {
        var dbContext = CreateDbContext();
        var (_, voter, post) = await SeedAsync(dbContext);
        var service = new VotingService(dbContext);

        var viewerVote = await service.GetViewerVoteAsync(VoteTargetType.Post, post.Id, voter.Id);

        Assert.Null(viewerVote);
    }

    [Fact]
    public async Task GetViewerVoteAsync_Upvoted_ReturnsOne()
    {
        var dbContext = CreateDbContext();
        var (_, voter, post) = await SeedAsync(dbContext);
        var service = new VotingService(dbContext);

        await service.CastVoteAsync(VoteTargetType.Post, post.Id, voter.Id, 1);
        var viewerVote = await service.GetViewerVoteAsync(VoteTargetType.Post, post.Id, voter.Id);

        Assert.Equal((sbyte)1, viewerVote);
    }

    [Fact]
    public async Task GetViewerVoteAsync_Downvoted_ReturnsNegativeOne()
    {
        var dbContext = CreateDbContext();
        var (_, voter, post) = await SeedAsync(dbContext);
        var service = new VotingService(dbContext);

        await service.CastVoteAsync(VoteTargetType.Post, post.Id, voter.Id, -1);
        var viewerVote = await service.GetViewerVoteAsync(VoteTargetType.Post, post.Id, voter.Id);

        Assert.Equal((sbyte)-1, viewerVote);
    }

    [Fact]
    public async Task GetViewerVoteAsync_DifferentViewer_DoesNotSeeAnotherUsersVote()
    {
        var dbContext = CreateDbContext();
        var (author, voter, post) = await SeedAsync(dbContext);
        var service = new VotingService(dbContext);

        await service.CastVoteAsync(VoteTargetType.Post, post.Id, voter.Id, 1);
        var authorViewerVote = await service.GetViewerVoteAsync(VoteTargetType.Post, post.Id, author.Id);

        Assert.Null(authorViewerVote);
    }
}
