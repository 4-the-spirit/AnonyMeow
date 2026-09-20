using AnonyMeow.Services.PiiDetection;

namespace AnonyMeow.UnitTests.Services.PiiDetection;

public class AddressHeuristicDetectorTests
{
    [Theory]
    [InlineData("I live at 123 Main St")]
    [InlineData("send it to 42 Oak Avenue please")]
    public void IsMatch_ReturnsTrue_ForStreetAddresses(string text) =>
        Assert.True(new AddressHeuristicDetector().IsMatch(text));

    [Theory]
    [InlineData("no address here")]
    [InlineData("I have 5 cats")]
    public void IsMatch_ReturnsFalse_WhenNoAddressPresent(string text) =>
        Assert.False(new AddressHeuristicDetector().IsMatch(text));
}
