using AnonyMeow.Common.Options;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services.ContentSubmission;
using AnonyMeow.Services.PiiDetection;
using Microsoft.Extensions.Options;

namespace AnonyMeow.UnitTests.Services.PiiDetection;

public class CompositePiiDetectionServiceTests
{
    private class FakeDetector(PiiDetectorType type, string category, bool isMatch) : IPiiDetector
    {
        public PiiDetectorType DetectorType => type;
        public string Category => category;
        public bool IsMatch(string text) => isMatch;
    }

    // No mocking library in this project (see the rest of Services/*Tests.cs) — a minimal
    // hand-rolled IOptionsMonitor<T> fake, same convention as every other hand-rolled fake.
    private class FakeOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = value;
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    [Fact]
    public void Detect_ReturnsMatch_ForBlockModeDetector()
    {
        var options = new PiiDetectionOptions
        {
            DetectorModes = new() { [PiiDetectorType.EmailPattern] = PiiEnforcementMode.Block },
            AppliesTo = [ContentSubmissionType.Post]
        };
        var detector = new FakeDetector(PiiDetectorType.EmailPattern, "Email", isMatch: true);
        var service = new CompositePiiDetectionService([detector], new FakeOptionsMonitor<PiiDetectionOptions>(options));

        var matches = service.Detect("some text", ContentSubmissionType.Post);

        var match = Assert.Single(matches);
        Assert.Equal("Email", match.MatchedCategory);
        Assert.True(match.WasBlocked);
    }

    [Fact]
    public void Detect_ReturnsMatch_ButNotBlocked_ForLogOnlyModeDetector()
    {
        var options = new PiiDetectionOptions
        {
            DetectorModes = new() { [PiiDetectorType.AddressHeuristic] = PiiEnforcementMode.LogOnly },
            AppliesTo = [ContentSubmissionType.Post]
        };
        var detector = new FakeDetector(PiiDetectorType.AddressHeuristic, "Street Address", isMatch: true);
        var service = new CompositePiiDetectionService([detector], new FakeOptionsMonitor<PiiDetectionOptions>(options));

        var matches = service.Detect("some text", ContentSubmissionType.Post);

        var match = Assert.Single(matches);
        Assert.False(match.WasBlocked);
    }

    [Fact]
    public void Detect_SkipsDisabledDetector_EntirelyNotLogged()
    {
        var options = new PiiDetectionOptions
        {
            DetectorModes = new() { [PiiDetectorType.EmailPattern] = PiiEnforcementMode.Disabled },
            AppliesTo = [ContentSubmissionType.Post]
        };
        var detector = new FakeDetector(PiiDetectorType.EmailPattern, "Email", isMatch: true);
        var service = new CompositePiiDetectionService([detector], new FakeOptionsMonitor<PiiDetectionOptions>(options));

        var matches = service.Detect("some text", ContentSubmissionType.Post);

        Assert.Empty(matches);
    }

    [Fact]
    public void Detect_ReturnsEmpty_WhenContentTypeNotInAppliesTo()
    {
        var options = new PiiDetectionOptions
        {
            DetectorModes = new() { [PiiDetectorType.EmailPattern] = PiiEnforcementMode.Block },
            AppliesTo = [ContentSubmissionType.Post]
        };
        var detector = new FakeDetector(PiiDetectorType.EmailPattern, "Email", isMatch: true);
        var service = new CompositePiiDetectionService([detector], new FakeOptionsMonitor<PiiDetectionOptions>(options));

        var matches = service.Detect("some text", ContentSubmissionType.DirectMessage);

        Assert.Empty(matches);
    }

    [Fact]
    public void Detect_ReturnsEmpty_WhenNoDetectorMatches()
    {
        var options = new PiiDetectionOptions
        {
            DetectorModes = new() { [PiiDetectorType.EmailPattern] = PiiEnforcementMode.Block },
            AppliesTo = [ContentSubmissionType.Post]
        };
        var detector = new FakeDetector(PiiDetectorType.EmailPattern, "Email", isMatch: false);
        var service = new CompositePiiDetectionService([detector], new FakeOptionsMonitor<PiiDetectionOptions>(options));

        var matches = service.Detect("some text", ContentSubmissionType.Post);

        Assert.Empty(matches);
    }
}
