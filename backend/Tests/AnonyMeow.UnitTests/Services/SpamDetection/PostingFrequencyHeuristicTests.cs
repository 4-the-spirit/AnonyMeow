using AnonyMeow.Common.Options;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services.ContentSubmission;
using AnonyMeow.Services.SpamDetection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AnonyMeow.UnitTests.Services.SpamDetection;

public class PostingFrequencyHeuristicTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static PostingFrequencyHeuristic CreateHeuristic(AppDbContext dbContext, SpamDetectionOptions? options = null) =>
        new(dbContext, Options.Create(options ?? new SpamDetectionOptions
        {
            NewAccountWindowMinutes = 60,
            MaxSubmissionsForNewAccounts = 2,
            PostingFrequencyWindowMinutes = 10
        }));

    [Fact]
    public async Task IsMatchAsync_ReturnsFalse_ForAccountOlderThanNewAccountWindow()
    {
        var dbContext = CreateDbContext();
        var author = new AppUser
        {
            Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "old",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };
        dbContext.Users.Add(author);
        for (var i = 0; i < 5; i++)
        {
            dbContext.Posts.Add(new Post
            {
                Id = Guid.NewGuid(), CommunityId = Guid.NewGuid(), AuthorId = author.Id,
                Title = $"P{i}", CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        await dbContext.SaveChangesAsync();
        var heuristic = CreateHeuristic(dbContext);

        var isMatch = await heuristic.IsMatchAsync(new SpamDetectionContext(author.Id, ContentSubmissionType.Post, "text"));

        Assert.False(isMatch);
    }

    [Fact]
    public async Task IsMatchAsync_ReturnsTrue_WhenNewAccountExceedsThreshold()
    {
        var dbContext = CreateDbContext();
        var author = new AppUser
        {
            Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "new",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Users.Add(author);
        for (var i = 0; i < 3; i++)
        {
            dbContext.Posts.Add(new Post
            {
                Id = Guid.NewGuid(), CommunityId = Guid.NewGuid(), AuthorId = author.Id,
                Title = $"P{i}", CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        await dbContext.SaveChangesAsync();
        var heuristic = CreateHeuristic(dbContext);

        var isMatch = await heuristic.IsMatchAsync(new SpamDetectionContext(author.Id, ContentSubmissionType.Post, "text"));

        Assert.True(isMatch);
    }

    [Fact]
    public async Task IsMatchAsync_ReturnsFalse_WhenNewAccountUnderThreshold()
    {
        var dbContext = CreateDbContext();
        var author = new AppUser
        {
            Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "new",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Users.Add(author);
        dbContext.Posts.Add(new Post
        {
            Id = Guid.NewGuid(), CommunityId = Guid.NewGuid(), AuthorId = author.Id,
            Title = "P0", CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var heuristic = CreateHeuristic(dbContext);

        var isMatch = await heuristic.IsMatchAsync(new SpamDetectionContext(author.Id, ContentSubmissionType.Post, "text"));

        Assert.False(isMatch);
    }
}
