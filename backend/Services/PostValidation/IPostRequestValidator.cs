using AnonyMeow.Dtos.Posts;

namespace AnonyMeow.Services.PostValidation;

public interface IPostRequestValidator
{
    // Empty dictionary means valid; a non-empty dictionary maps field name -> error messages.
    IDictionary<string, string[]> Validate(CreatePostRequest request);
}
