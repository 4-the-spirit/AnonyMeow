using AnonyMeow.Common.Options;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Services.PostValidation;
using Microsoft.Extensions.Options;

namespace AnonyMeow.UnitTests.Services.PostValidation;

public class PostRequestValidatorTests
{
    private static readonly Guid TestFlairId = Guid.NewGuid();

    private static PostRequestValidator CreateValidator(int maxImageCount = 6) =>
        new(Options.Create(new PostImageOptions { MaxImageCount = maxImageCount }));

    [Fact]
    public void Validate_MissingTitle_ReturnsError()
    {
        var request = new CreatePostRequest("", "Body", null, null, null, TestFlairId);
        var errors = CreateValidator().Validate(request);
        Assert.True(errors.ContainsKey("title"));
    }

    [Fact]
    public void Validate_TitleOnlyWithNoOtherContent_ReturnsError()
    {
        var request = new CreatePostRequest("Title", null, null, null, null, TestFlairId);
        var errors = CreateValidator().Validate(request);
        Assert.True(errors.ContainsKey("body"));
    }

    [Fact]
    public void Validate_BodyOnly_IsValid()
    {
        var request = new CreatePostRequest("Title", "Body", null, null, null, TestFlairId);
        var errors = CreateValidator().Validate(request);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ImagesOnly_IsValid()
    {
        var request = new CreatePostRequest("Title", null, null, ["https://blob.example/a.png"], null, TestFlairId);
        var errors = CreateValidator().Validate(request);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_UrlOnly_IsValid()
    {
        var request = new CreatePostRequest("Title", null, "https://example.com", null, null, TestFlairId);
        var errors = CreateValidator().Validate(request);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_PollOnly_IsValid()
    {
        var request = new CreatePostRequest("Title", null, null, null, ["A", "B"], TestFlairId);
        var errors = CreateValidator().Validate(request);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_AllAttachmentsCombined_IsValid()
    {
        var request = new CreatePostRequest(
            "Title", "Body", "https://example.com", ["https://blob.example/a.png"], ["A", "B"], TestFlairId);
        var errors = CreateValidator().Validate(request);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MalformedUrl_ReturnsError()
    {
        var request = new CreatePostRequest("Title", null, "/relative/path", null, null, TestFlairId);
        var errors = CreateValidator().Validate(request);
        Assert.True(errors.ContainsKey("url"));
    }

    [Fact]
    public void Validate_PollWithOneOption_ReturnsError()
    {
        var request = new CreatePostRequest("Title", null, null, null, ["OnlyOne"], TestFlairId);
        var errors = CreateValidator().Validate(request);
        Assert.True(errors.ContainsKey("pollOptions"));
    }

    [Fact]
    public void Validate_ImageCountOverConfiguredMax_ReturnsError()
    {
        var request = new CreatePostRequest("Title", null, null, ["a", "b", "c"], null, TestFlairId);
        var errors = CreateValidator(maxImageCount: 2).Validate(request);
        Assert.True(errors.ContainsKey("imageUrls"));
    }

    [Fact]
    public void Validate_ImageCountWithinConfiguredMax_IsValid()
    {
        var request = new CreatePostRequest("Title", null, null, ["a", "b"], null, TestFlairId);
        var errors = CreateValidator(maxImageCount: 2).Validate(request);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_TitleOverMaxLength_ReturnsError()
    {
        var request = new CreatePostRequest(new string('a', 301), "Body", null, null, null, TestFlairId);
        var errors = CreateValidator().Validate(request);
        Assert.True(errors.ContainsKey("title"));
    }

    [Fact]
    public void Validate_TitleAtMaxLength_IsValid()
    {
        var request = new CreatePostRequest(new string('a', 300), "Body", null, null, null, TestFlairId);
        var errors = CreateValidator().Validate(request);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_BodyOverMaxLength_ReturnsError()
    {
        var request = new CreatePostRequest("Title", new string('a', 40_001), null, null, null, TestFlairId);
        var errors = CreateValidator().Validate(request);
        Assert.True(errors.ContainsKey("body"));
    }

    [Fact]
    public void Validate_BodyAtMaxLength_IsValid()
    {
        var request = new CreatePostRequest("Title", new string('a', 40_000), null, null, null, TestFlairId);
        var errors = CreateValidator().Validate(request);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MissingFlairId_ReturnsError()
    {
        var request = new CreatePostRequest("Title", "Body", null, null, null, Guid.Empty);
        var errors = CreateValidator().Validate(request);
        Assert.True(errors.ContainsKey("flairId"));
    }

    [Fact]
    public void Validate_FlairIdProvided_IsValid()
    {
        var request = new CreatePostRequest("Title", "Body", null, null, null, TestFlairId);
        var errors = CreateValidator().Validate(request);
        Assert.Empty(errors);
    }
}
