using AnonyMeow.Services.PiiDetection;

namespace AnonyMeow.UnitTests.Services.PiiDetection;

public class PhoneNumberDetectorTests
{
    [Theory]
    [InlineData("call me at 555-123-4567")]
    [InlineData("(555) 123-4567 works too")]
    [InlineData("+1 555.123.4567")]
    public void IsMatch_ReturnsTrue_ForPhoneNumbers(string text) =>
        Assert.True(new PhoneNumberDetector().IsMatch(text));

    [Theory]
    [InlineData("no phone number here")]
    [InlineData("just some random digits 12345")]
    public void IsMatch_ReturnsFalse_WhenNoPhoneNumberPresent(string text) =>
        Assert.False(new PhoneNumberDetector().IsMatch(text));
}
