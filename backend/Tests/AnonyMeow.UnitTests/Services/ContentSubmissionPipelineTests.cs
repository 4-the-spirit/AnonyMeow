using AnonyMeow.Data;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services.ContentSubmission;
using AnonyMeow.Services.PiiDetection;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class ContentSubmissionPipelineTests
{
    private class FakePiiDetectionService(IReadOnlyList<PiiDetectionMatch> matches) : IPiiDetectionService
    {
        public IReadOnlyList<PiiDetectionMatch> Detect(string text, ContentSubmissionType contentType) => matches;
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task EvaluateAsync_ReturnsAllowed_WhenNoMatches()
    {
        var dbContext = CreateDbContext();
        var pipeline = new ContentSubmissionPipeline(new FakePiiDetectionService([]), dbContext);

        var result = await pipeline.EvaluateAsync(new ContentSubmissionRequest(ContentSubmissionType.Post, Guid.NewGuid(), "clean text"));

        Assert.False(result.IsBlocked);
        Assert.Empty(result.DetectedCategories);
        Assert.Equal(0, await dbContext.PiiDetectionLogs.CountAsync());
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsBlocked_AndLogsEveryMatch_WhenAnyMatchIsBlocking()
    {
        var dbContext = CreateDbContext();
        var matches = new List<PiiDetectionMatch>
        {
            new(PiiDetectorType.EmailPattern, "Email", WasBlocked: true),
            new(PiiDetectorType.AddressHeuristic, "Street Address", WasBlocked: false)
        };
        var pipeline = new ContentSubmissionPipeline(new FakePiiDetectionService(matches), dbContext);
        var authorId = Guid.NewGuid();

        var result = await pipeline.EvaluateAsync(new ContentSubmissionRequest(ContentSubmissionType.Comment, authorId, "text"));

        Assert.True(result.IsBlocked);
        Assert.Equal(["Email"], result.DetectedCategories);
        Assert.Equal(2, await dbContext.PiiDetectionLogs.CountAsync());
        var logs = await dbContext.PiiDetectionLogs.ToListAsync();
        Assert.All(logs, l => Assert.Equal(authorId, l.UserId));
        Assert.Contains(logs, l => l.MatchedCategory == "Email" && l.WasBlocked);
        Assert.Contains(logs, l => l.MatchedCategory == "Street Address" && !l.WasBlocked);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsNotBlocked_WhenOnlyLogOnlyMatchesExist()
    {
        var dbContext = CreateDbContext();
        var matches = new List<PiiDetectionMatch> { new(PiiDetectorType.AddressHeuristic, "Street Address", WasBlocked: false) };
        var pipeline = new ContentSubmissionPipeline(new FakePiiDetectionService(matches), dbContext);

        var result = await pipeline.EvaluateAsync(new ContentSubmissionRequest(ContentSubmissionType.Post, Guid.NewGuid(), "text"));

        Assert.False(result.IsBlocked);
        Assert.Empty(result.DetectedCategories);
        Assert.Equal(1, await dbContext.PiiDetectionLogs.CountAsync());
    }
}
