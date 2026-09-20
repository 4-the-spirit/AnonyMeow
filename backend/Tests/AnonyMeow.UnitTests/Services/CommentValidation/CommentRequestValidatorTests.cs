using AnonyMeow.Services.CommentValidation;

namespace AnonyMeow.UnitTests.Services.CommentValidation;

public class CommentRequestValidatorTests
{
    private static readonly CommentRequestValidator Validator = new();

    [Fact]
    public void Validate_EmptyBody_ReturnsError()
    {
        var errors = Validator.Validate("");
        Assert.True(errors.ContainsKey("bodyMarkdown"));
    }

    [Fact]
    public void Validate_WhitespaceOnlyBody_ReturnsError()
    {
        var errors = Validator.Validate("   ");
        Assert.True(errors.ContainsKey("bodyMarkdown"));
    }

    [Fact]
    public void Validate_NormalBody_IsValid()
    {
        var errors = Validator.Validate("A perfectly normal comment.");
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_BodyOverMaxLength_ReturnsError()
    {
        var errors = Validator.Validate(new string('a', CommentRequestValidator.MaxBodyLength + 1));
        Assert.True(errors.ContainsKey("bodyMarkdown"));
    }

    [Fact]
    public void Validate_BodyAtMaxLength_IsValid()
    {
        var errors = Validator.Validate(new string('a', CommentRequestValidator.MaxBodyLength));
        Assert.Empty(errors);
    }
}
