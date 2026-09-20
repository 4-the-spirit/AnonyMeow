using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services.Ranking.Strategies;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services.Ranking;

public class SortStrategyTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Post MakePost(Guid communityId, Guid authorId, DateTimeOffset createdAt) => new()
    {
        Id = Guid.NewGuid(), CommunityId = communityId, AuthorId = authorId,
        Title = "T", BodyMarkdown = "B", CreatedAtUtc = createdAt
    };

    private static void AddVote(AppDbContext dbContext, Guid postId, sbyte value) =>
        dbContext.Votes.Add(new Vote { Id = Guid.NewGuid(), TargetType = VoteTargetType.Post, TargetId = postId, VoterId = Guid.NewGuid(), Value = value });

    [Fact]
    public async Task NewSortStrategy_OrdersByCreatedAtDescending()
    {
        var dbContext = CreateDbContext();
        var communityId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var older = MakePost(communityId, authorId, DateTimeOffset.UtcNow.AddHours(-2));
        var newer = MakePost(communityId, authorId, DateTimeOffset.UtcNow);
        dbContext.Posts.AddRange(older, newer);
        await dbContext.SaveChangesAsync();

        var result = new NewSortStrategy().ApplyToPosts(dbContext.Posts).ToList();

        Assert.Equal([newer.Id, older.Id], result.Select(p => p.Id));
    }

    [Fact]
    public async Task TopSortStrategy_OrdersByScoreDescending()
    {
        var dbContext = CreateDbContext();
        var communityId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var lowScore = MakePost(communityId, authorId, DateTimeOffset.UtcNow);
        var highScore = MakePost(communityId, authorId, DateTimeOffset.UtcNow);
        dbContext.Posts.AddRange(lowScore, highScore);
        AddVote(dbContext, lowScore.Id, 1);
        AddVote(dbContext, highScore.Id, 1);
        AddVote(dbContext, highScore.Id, 1);
        await dbContext.SaveChangesAsync();

        var result = new TopSortStrategy(dbContext).ApplyToPosts(dbContext.Posts).ToList();

        Assert.Equal([highScore.Id, lowScore.Id], result.Select(p => p.Id));
    }

    [Fact]
    public async Task HotSortStrategy_DoesNotError_AndReturnsAllItems()
    {
        var dbContext = CreateDbContext();
        var communityId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var a = MakePost(communityId, authorId, DateTimeOffset.UtcNow.AddHours(-1));
        var b = MakePost(communityId, authorId, DateTimeOffset.UtcNow);
        dbContext.Posts.AddRange(a, b);
        AddVote(dbContext, a.Id, 1);
        await dbContext.SaveChangesAsync();

        var result = new HotSortStrategy(dbContext).ApplyToPosts(dbContext.Posts).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ControversialSortStrategy_RanksEvenSplitAboveLopsided()
    {
        var dbContext = CreateDbContext();
        var communityId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var evenSplit = MakePost(communityId, authorId, DateTimeOffset.UtcNow);
        var lopsided = MakePost(communityId, authorId, DateTimeOffset.UtcNow);
        dbContext.Posts.AddRange(evenSplit, lopsided);
        AddVote(dbContext, evenSplit.Id, 1);
        AddVote(dbContext, evenSplit.Id, -1);
        AddVote(dbContext, lopsided.Id, 1);
        AddVote(dbContext, lopsided.Id, 1);
        await dbContext.SaveChangesAsync();

        var result = new ControversialSortStrategy(dbContext).ApplyToPosts(dbContext.Posts).ToList();

        Assert.Equal([evenSplit.Id, lopsided.Id], result.Select(p => p.Id));
    }

    [Fact]
    public async Task TrendingSortStrategy_RanksHeavilyDiscussedAboveHigherVotedButQuiet()
    {
        var dbContext = CreateDbContext();
        var communityId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var quietButHigherVoted = MakePost(communityId, authorId, DateTimeOffset.UtcNow);
        var discussed = MakePost(communityId, authorId, DateTimeOffset.UtcNow);
        dbContext.Posts.AddRange(quietButHigherVoted, discussed);
        AddVote(dbContext, quietButHigherVoted.Id, 1);
        AddVote(dbContext, quietButHigherVoted.Id, 1);
        AddVote(dbContext, quietButHigherVoted.Id, 1);
        AddVote(dbContext, discussed.Id, 1);
        AddVote(dbContext, discussed.Id, 1);
        for (var i = 0; i < 5; i++)
        {
            dbContext.Comments.Add(new Comment
            {
                Id = Guid.NewGuid(), PostId = discussed.Id, AuthorId = authorId,
                BodyMarkdown = "comment", CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        await dbContext.SaveChangesAsync();

        var result = new TrendingSortStrategy(dbContext).ApplyToPosts(dbContext.Posts).ToList();

        Assert.Equal([discussed.Id, quietButHigherVoted.Id], result.Select(p => p.Id));
    }

    [Fact]
    public async Task TrendingSortStrategy_ExcludesRemovedCommentsFromEngagementScore()
    {
        var dbContext = CreateDbContext();
        var communityId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var post = MakePost(communityId, authorId, DateTimeOffset.UtcNow);
        dbContext.Posts.Add(post);
        dbContext.Comments.Add(new Comment
        {
            Id = Guid.NewGuid(), PostId = post.Id, AuthorId = authorId,
            BodyMarkdown = "removed", IsRemoved = true, CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var result = new TrendingSortStrategy(dbContext).ApplyToPosts(dbContext.Posts).ToList();

        Assert.Equal(post.Id, Assert.Single(result).Id);
    }

    [Fact]
    public async Task PinnedSortStrategy_ReturnsOnlyPinnedPosts_NewestFirst()
    {
        var dbContext = CreateDbContext();
        var communityId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var olderPinned = MakePost(communityId, authorId, DateTimeOffset.UtcNow.AddHours(-2));
        olderPinned.IsPinned = true;
        var newerPinned = MakePost(communityId, authorId, DateTimeOffset.UtcNow);
        newerPinned.IsPinned = true;
        var unpinned = MakePost(communityId, authorId, DateTimeOffset.UtcNow);
        dbContext.Posts.AddRange(olderPinned, newerPinned, unpinned);
        await dbContext.SaveChangesAsync();

        var result = new PinnedSortStrategy().ApplyToPosts(dbContext.Posts).ToList();

        Assert.Equal([newerPinned.Id, olderPinned.Id], result.Select(p => p.Id));
    }
}
