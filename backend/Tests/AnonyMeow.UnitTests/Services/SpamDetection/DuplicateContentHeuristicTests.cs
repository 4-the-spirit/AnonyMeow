using AnonyMeow.Common.Options;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services.ContentSubmission;
using AnonyMeow.Services.SpamDetection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AnonyMeow.UnitTests.Services.SpamDetection;

public class DuplicateContentHeuristicTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static DuplicateContentHeuristic CreateHeuristic(AppDbContext dbContext, int lookbackHours = 24) =>
        new(dbContext, Options.Create(new SpamDetectionOptions { DuplicateContentLookbackHours = lookbackHours }));

    [Fact]
    public async Task IsMatchAsync_ReturnsTrue_WhenIdenticalCommentTextRepeated()
    {
        var dbContext = CreateDbContext();
        var author = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "dup", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(author);
        dbContext.Comments.AddRange(
            new Comment { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), AuthorId = author.Id, BodyMarkdown = "Buy now!", CreatedAtUtc = DateTimeOffset.UtcNow },
            new Comment { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), AuthorId = author.Id, BodyMarkdown = "Buy now!", CreatedAtUtc = DateTimeOffset.UtcNow });
        await dbContext.SaveChangesAsync();
        var heuristic = CreateHeuristic(dbContext);

        var isMatch = await heuristic.IsMatchAsync(new SpamDetectionContext(author.Id, ContentSubmissionType.Comment, "Buy now!"));

        Assert.True(isMatch);
    }

    [Fact]
    public async Task IsMatchAsync_ReturnsFalse_WhenTextIsUnique()
    {
        var dbContext = CreateDbContext();
        var author = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "uniq", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(author);
        dbContext.Comments.Add(new Comment
        {
            Id = Guid.NewGuid(), PostId = Guid.NewGuid(), AuthorId = author.Id, BodyMarkdown = "Only submission", CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var heuristic = CreateHeuristic(dbContext);

        var isMatch = await heuristic.IsMatchAsync(new SpamDetectionContext(author.Id, ContentSubmissionType.Comment, "Only submission"));

        Assert.False(isMatch);
    }

    [Fact]
    public async Task IsMatchAsync_IgnoresSubmissionsOutsideLookbackWindow()
    {
        var dbContext = CreateDbContext();
        var author = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "old", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(author);
        dbContext.Comments.AddRange(
            new Comment { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), AuthorId = author.Id, BodyMarkdown = "Old repeat", CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-48) },
            new Comment { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), AuthorId = author.Id, BodyMarkdown = "Old repeat", CreatedAtUtc = DateTimeOffset.UtcNow });
        await dbContext.SaveChangesAsync();
        var heuristic = CreateHeuristic(dbContext, lookbackHours: 24);

        var isMatch = await heuristic.IsMatchAsync(new SpamDetectionContext(author.Id, ContentSubmissionType.Comment, "Old repeat"));

        Assert.False(isMatch);
    }

    [Fact]
    public async Task IsMatchAsync_ReturnsFalse_ForEmptyText()
    {
        var dbContext = CreateDbContext();
        var author = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "empty", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(author);
        await dbContext.SaveChangesAsync();
        var heuristic = CreateHeuristic(dbContext);

        var isMatch = await heuristic.IsMatchAsync(new SpamDetectionContext(author.Id, ContentSubmissionType.Comment, ""));

        Assert.False(isMatch);
    }
}
