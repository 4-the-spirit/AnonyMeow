using AnonyMeow.Domain.Enums;
using AnonyMeow.Services.ContentSubmission;
using AnonyMeow.Services.SpamDetection;

namespace AnonyMeow.UnitTests.Services.SpamDetection;

public class CompositeSpamDetectionServiceTests
{
    private class FakeHeuristic(SpamFlagReason reason, bool isMatch) : ISpamHeuristic
    {
        public SpamFlagReason Reason => reason;
        public Task<bool> IsMatchAsync(SpamDetectionContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(isMatch);
    }

    [Fact]
    public async Task DetectAsync_ReturnsReasons_FromAllMatchingHeuristics()
    {
        var service = new CompositeSpamDetectionService([
            new FakeHeuristic(SpamFlagReason.RateLimitExceeded, true),
            new FakeHeuristic(SpamFlagReason.DuplicateContent, true),
            new FakeHeuristic(SpamFlagReason.LinkSpam, false)
        ]);

        var reasons = await service.DetectAsync(new SpamDetectionContext(Guid.NewGuid(), ContentSubmissionType.Post, "text"));

        Assert.Equal([SpamFlagReason.RateLimitExceeded, SpamFlagReason.DuplicateContent], reasons);
    }

    [Fact]
    public async Task DetectAsync_ReturnsEmpty_WhenNoHeuristicMatches()
    {
        var service = new CompositeSpamDetectionService([
            new FakeHeuristic(SpamFlagReason.RateLimitExceeded, false),
            new FakeHeuristic(SpamFlagReason.LinkSpam, false)
        ]);

        var reasons = await service.DetectAsync(new SpamDetectionContext(Guid.NewGuid(), ContentSubmissionType.Post, "text"));

        Assert.Empty(reasons);
    }
}
