using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class ReactionServiceTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task AddAsync_IsIdempotent_ForSameUserAndEmoji()
    {
        var dbContext = CreateDbContext();
        var service = new ReactionService(dbContext);
        var targetId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await service.AddAsync(ReactionTargetType.Post, targetId, userId, "🔥");
        await service.AddAsync(ReactionTargetType.Post, targetId, userId, "🔥");

        Assert.Equal(1, await dbContext.Reactions.CountAsync());
    }

    [Fact]
    public async Task AddAsync_ReplacesExistingDifferentEmojiFromSameUser()
    {
        var dbContext = CreateDbContext();
        var service = new ReactionService(dbContext);
        var targetId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await service.AddAsync(ReactionTargetType.Post, targetId, userId, "🔥");
        await service.AddAsync(ReactionTargetType.Post, targetId, userId, "❤️");

        var reaction = await dbContext.Reactions.SingleAsync();
        Assert.Equal("❤️", reaction.Emoji);
    }

    [Fact]
    public async Task AddAsync_DoesNotAffectOtherUsersReactionsWhenReplacing()
    {
        var dbContext = CreateDbContext();
        var service = new ReactionService(dbContext);
        var targetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await service.AddAsync(ReactionTargetType.Post, targetId, otherUserId, "🔥");
        await service.AddAsync(ReactionTargetType.Post, targetId, userId, "🔥");
        await service.AddAsync(ReactionTargetType.Post, targetId, userId, "❤️");

        Assert.Equal(2, await dbContext.Reactions.CountAsync());
        Assert.Equal("🔥", await dbContext.Reactions
            .Where(r => r.AppUserId == otherUserId)
            .Select(r => r.Emoji)
            .SingleAsync());
    }

    [Fact]
    public async Task RemoveAsync_NoExistingReaction_IsNoOp()
    {
        var dbContext = CreateDbContext();
        var service = new ReactionService(dbContext);

        await service.RemoveAsync(ReactionTargetType.Post, Guid.NewGuid(), Guid.NewGuid(), "🔥");

        Assert.Equal(0, await dbContext.Reactions.CountAsync());
    }

    [Fact]
    public async Task RemoveAsync_RemovesOnlyMatchingUsersReaction()
    {
        var dbContext = CreateDbContext();
        var service = new ReactionService(dbContext);
        var targetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        await service.AddAsync(ReactionTargetType.Post, targetId, userId, "🔥");
        await service.AddAsync(ReactionTargetType.Post, targetId, otherUserId, "❤️");

        await service.RemoveAsync(ReactionTargetType.Post, targetId, userId, "🔥");

        var remaining = await dbContext.Reactions.SingleAsync();
        Assert.Equal("❤️", remaining.Emoji);
    }

    [Fact]
    public async Task GetSummaryAsync_GroupsByEmoji_AndFlagsViewerReaction()
    {
        var dbContext = CreateDbContext();
        var service = new ReactionService(dbContext);
        var targetId = Guid.NewGuid();
        var viewer = Guid.NewGuid();
        var otherFireUser = Guid.NewGuid();
        var otherHeartUser = Guid.NewGuid();

        await service.AddAsync(ReactionTargetType.Post, targetId, viewer, "🔥");
        await service.AddAsync(ReactionTargetType.Post, targetId, otherFireUser, "🔥");
        await service.AddAsync(ReactionTargetType.Post, targetId, otherHeartUser, "❤️");

        var summary = await service.GetSummaryAsync(ReactionTargetType.Post, targetId, viewer);

        var fire = summary.Single(r => r.Emoji == "🔥");
        Assert.Equal(2, fire.Count);
        Assert.True(fire.ReactedByViewer);

        var heart = summary.Single(r => r.Emoji == "❤️");
        Assert.Equal(1, heart.Count);
        Assert.False(heart.ReactedByViewer);
    }
}
