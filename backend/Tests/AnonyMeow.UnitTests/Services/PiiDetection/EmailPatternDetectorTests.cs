using AnonyMeow.Services.PiiDetection;

namespace AnonyMeow.UnitTests.Services.PiiDetection;

public class EmailPatternDetectorTests
{
    [Theory]
    [InlineData("reach me at someone@example.com please")]
    [InlineData("first.last+tag@sub.example.co.uk")]
    public void IsMatch_ReturnsTrue_ForEmailAddresses(string text) =>
        Assert.True(new EmailPatternDetector().IsMatch(text));

    [Theory]
    [InlineData("no email here")]
    [InlineData("just some text with an @ sign")]
    public void IsMatch_ReturnsFalse_WhenNoEmailPresent(string text) =>
        Assert.False(new EmailPatternDetector().IsMatch(text));
}
