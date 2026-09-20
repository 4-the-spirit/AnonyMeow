using AnonyMeow.Common.Options;
using AnonyMeow.Services.ContentSubmission;
using AnonyMeow.Services.SpamDetection;
using Microsoft.Extensions.Options;

namespace AnonyMeow.UnitTests.Services.SpamDetection;

public class LinkSpamHeuristicTests
{
    private static LinkSpamHeuristic CreateHeuristic(params string[] blockedDomains) =>
        new(Options.Create(new SpamDetectionOptions { BlockedLinkDomains = [.. blockedDomains] }));

    [Fact]
    public async Task IsMatchAsync_ReturnsTrue_ForBlockedDomain()
    {
        var heuristic = CreateHeuristic("spam-example.com");

        var isMatch = await heuristic.IsMatchAsync(
            new SpamDetectionContext(Guid.NewGuid(), ContentSubmissionType.Post, "Check this out: https://spam-example.com/deal"));

        Assert.True(isMatch);
    }

    [Fact]
    public async Task IsMatchAsync_MatchesSubdomainOfBlockedDomain()
    {
        var heuristic = CreateHeuristic("spam-example.com");

        var isMatch = await heuristic.IsMatchAsync(
            new SpamDetectionContext(Guid.NewGuid(), ContentSubmissionType.Post, "https://promo.spam-example.com/deal"));

        Assert.True(isMatch);
    }

    [Fact]
    public async Task IsMatchAsync_ReturnsFalse_ForAllowedDomain()
    {
        var heuristic = CreateHeuristic("spam-example.com");

        var isMatch = await heuristic.IsMatchAsync(
            new SpamDetectionContext(Guid.NewGuid(), ContentSubmissionType.Post, "https://legit-example.com/post"));

        Assert.False(isMatch);
    }

    [Fact]
    public async Task IsMatchAsync_ReturnsFalse_WhenBlocklistEmpty()
    {
        var heuristic = CreateHeuristic();

        var isMatch = await heuristic.IsMatchAsync(
            new SpamDetectionContext(Guid.NewGuid(), ContentSubmissionType.Post, "https://anything.example.com"));

        Assert.False(isMatch);
    }

    [Fact]
    public async Task IsMatchAsync_ReturnsFalse_WhenNoUrlInText()
    {
        var heuristic = CreateHeuristic("spam-example.com");

        var isMatch = await heuristic.IsMatchAsync(
            new SpamDetectionContext(Guid.NewGuid(), ContentSubmissionType.Post, "just plain text, no links"));

        Assert.False(isMatch);
    }
}
