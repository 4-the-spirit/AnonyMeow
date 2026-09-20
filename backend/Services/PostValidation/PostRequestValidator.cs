using AnonyMeow.Common.Options;
using AnonyMeow.Dtos.Posts;
using Microsoft.Extensions.Options;

namespace AnonyMeow.Services.PostValidation;

// A post can independently and optionally carry a body, images, a link, and a poll — there is no
// longer a discriminating "type" to dispatch validation strategies on, so this is one class with
// small single-purpose validation methods rather than a keyed-DI strategy per type.
public class PostRequestValidator(IOptions<PostImageOptions> imageOptions) : IPostRequestValidator
{
    public IDictionary<string, string[]> Validate(CreatePostRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        PostValidationHelpers.RequireTitle(request, errors);
        ValidateHasContent(request, errors);
        PostValidationHelpers.ValidateBodyLength(request.BodyMarkdown, errors);
        ValidateUrl(request, errors);
        ValidateImages(request, errors);
        ValidatePoll(request, errors);
        ValidateFlair(request, errors);
        return errors;
    }

    private static void ValidateFlair(CreatePostRequest request, IDictionary<string, string[]> errors)
    {
        if (request.FlairId == Guid.Empty)
        {
            errors["flairId"] = ["A tag is required."];
        }
    }

    private static void ValidateHasContent(CreatePostRequest request, IDictionary<string, string[]> errors)
    {
        var hasBody = !string.IsNullOrWhiteSpace(request.BodyMarkdown);
        var hasImages = request.ImageUrls is { Count: > 0 };
        var hasUrl = !string.IsNullOrWhiteSpace(request.Url);
        var hasPoll = request.PollOptions?.Count(o => !string.IsNullOrWhiteSpace(o)) >= 2;

        if (!hasBody && !hasImages && !hasUrl && !hasPoll)
        {
            errors["body"] = ["Add a body, image, link, or poll."];
        }
    }

    private static void ValidateUrl(CreatePostRequest request, IDictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return;
        }

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors["url"] = ["A well-formed absolute URL is required."];
        }
    }

    private void ValidateImages(CreatePostRequest request, IDictionary<string, string[]> errors)
    {
        if (request.ImageUrls is not { Count: > 0 } imageUrls)
        {
            return;
        }

        if (imageUrls.Count > imageOptions.Value.MaxImageCount)
        {
            errors["imageUrls"] = [$"A post can have at most {imageOptions.Value.MaxImageCount} images."];
        }
    }

    private static void ValidatePoll(CreatePostRequest request, IDictionary<string, string[]> errors)
    {
        if (request.PollOptions is null)
        {
            return;
        }

        var optionCount = request.PollOptions.Count(o => !string.IsNullOrWhiteSpace(o));
        if (optionCount < 2)
        {
            errors["pollOptions"] = ["At least 2 poll options are required."];
        }
    }
}
